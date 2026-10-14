using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static class StaadManager
    {
        private static readonly object _lock = new object();
        private static readonly TimeSpan HealthCheckInterval = TimeSpan.FromSeconds(2);
        private static StaadConnectionSnapshot _cachedConnection;
        private static DateTime _lastHealthCheckUtc = DateTime.MinValue;
        private static bool _initialized;

        public static event EventHandler<StaadActiveModelChangedEventArgs> ActiveModelChanged;

        public static StaadConnectionMode ConnectionMode
        {
            get
            {
                string value = AppSettings.LoadString("OpenStaadConnectionMode", "ActiveModel");
                return string.Equals(value, "LegacySingleInstance", StringComparison.OrdinalIgnoreCase)
                    ? StaadConnectionMode.LegacySingleInstance
                    : StaadConnectionMode.ActiveModel;
            }
            set
            {
                AppSettings.SaveString("OpenStaadConnectionMode", value.ToString());
                lock (_lock)
                {
                    _cachedConnection = null;
                    _lastHealthCheckUtc = DateTime.MinValue;
                    OpenStaadRotResolver.InvalidateCache();
                }
            }
        }

        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized)
                    return;

                StaadForegroundTracker.Start();
                _initialized = true;
            }
        }

        public static void Shutdown()
        {
            lock (_lock)
            {
                StaadForegroundTracker.Stop();
                _cachedConnection = null;
                _initialized = false;
            }
        }

        public static object GetOpenSTAAD()
        {
            if (TryGetOpenSTAAD(out object openStaad, out string error))
                return openStaad;

            Debug.WriteLine(error);
            return null;
        }

        public static object GetOpenSTAADOrThrow()
        {
            if (TryGetOpenSTAAD(out object openStaad, out string error))
                return openStaad;

            throw new StaadConnectionException(error);
        }

        public static bool TryGetOpenSTAAD(out object openStaad, out string error)
        {
            if (TryGetConnection(out StaadConnectionSnapshot connection, out error))
            {
                openStaad = connection.OpenStaad;
                return true;
            }

            openStaad = null;
            return false;
        }

        public static bool TryGetConnection(out StaadConnectionSnapshot connection, out string error)
        {
            StaadActiveModelChangedEventArgs changed = null;
            bool success;

            lock (_lock)
            {
                EnsureInitializedLocked();
                StaadModelIdentity previous = _cachedConnection?.Identity;
                success = TryGetConnectionCore(out connection, out error);
                StaadModelIdentity current = success ? connection.Identity : null;

                if (!AreSameIdentity(previous, current))
                    changed = new StaadActiveModelChangedEventArgs(previous, current);
            }

            if (changed != null)
                RaiseActiveModelChanged(changed);

            return success;
        }

        public static bool TryGetCurrentIdentity(out StaadModelIdentity identity, out string error)
        {
            if (TryGetConnection(out StaadConnectionSnapshot connection, out error))
            {
                identity = connection.Identity;
                return true;
            }

            identity = null;
            return false;
        }

        public static bool TryValidateConnectionSnapshot(
            StaadConnectionSnapshot connection,
            out string error)
        {
            error = string.Empty;
            if (connection?.OpenStaad == null || connection.Identity == null)
            {
                error = "No STAAD model is bound to this view.";
                return false;
            }

            StaadModelIdentity expectedIdentity = connection.Identity;
            if (expectedIdentity.ProcessId > 0 &&
                !StaadForegroundTracker.IsMainProcessRunning(expectedIdentity.ProcessId))
            {
                error = "The STAAD model bound to this view is no longer running.";
                return false;
            }

            if (!OpenStaadModelAdapter.TryGetBaseUnit(connection.OpenStaad, out _, out string pingError))
            {
                error = "The STAAD model bound to this view is no longer available: " + pingError;
                return false;
            }

            if (!OpenStaadRotResolver.TryCreateIdentity(
                    connection.OpenStaad,
                    expectedIdentity.MonikerDisplayName,
                    expectedIdentity.IsLegacyFallback,
                    out StaadModelIdentity currentIdentity,
                    out string identityError))
            {
                error = "Could not validate the STAAD model bound to this view: " + identityError;
                return false;
            }

            if (!string.Equals(
                    expectedIdentity.CacheKey,
                    currentIdentity.CacheKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                error = "The STAAD model bound to this view has changed.";
                return false;
            }

            return true;
        }

        public static string GetModelCacheKey(object openStaad)
        {
            if (openStaad == null)
                return "staad:none";

            if (OpenStaadRotResolver.TryCreateIdentity(
                    openStaad,
                    string.Empty,
                    false,
                    out StaadModelIdentity identity,
                    out _))
            {
                return identity.CacheKey;
            }

            return "staad:session:" + RuntimeHelpers.GetHashCode(openStaad);
        }

        private static bool TryGetConnectionCore(out StaadConnectionSnapshot connection, out string error)
        {
            connection = null;
            error = string.Empty;

            if (ConnectionMode == StaadConnectionMode.LegacySingleInstance)
                return TryGetLegacyConnection(out connection, out error);

            StaadForegroundTracker.RefreshFromForegroundWindow();
            int activeProcessId = StaadForegroundTracker.LastStaadProcessId;
            if (CanReuseCachedConnection(activeProcessId, out connection))
                return true;

            IReadOnlyList<OpenStaadRotCandidate> discoveredCandidates = OpenStaadRotResolver.GetCandidates(
                forceRefresh: false,
                out string rotDiagnostics);
            IReadOnlyList<OpenStaadRotCandidate> candidates = discoveredCandidates
                .Where(candidate => candidate.Identity.ProcessId <= 0 ||
                    StaadForegroundTracker.IsMainProcessRunning(candidate.Identity.ProcessId))
                .ToArray();
            int[] runningStaadProcessIds = StaadForegroundTracker.GetRunningMainProcessIds()
                .Where(StaadForegroundTracker.IsMainProcessRunning)
                .ToArray();

            StaadCandidateSelectionOutcome selectionOutcome = StaadCandidateSelectionPolicy.SelectIndex(
                candidates.Select(candidate => candidate.Identity.ProcessId).ToArray(),
                activeProcessId,
                runningStaadProcessIds,
                out int selectedIndex);
            OpenStaadRotCandidate selected = selectionOutcome == StaadCandidateSelectionOutcome.Selected
                ? candidates[selectedIndex]
                : null;

            if (selectionOutcome == StaadCandidateSelectionOutcome.Ambiguous)
            {
                _cachedConnection = null;
                error = candidates.Count > 0
                    ? "Multiple STAAD models are open, but the active model could not be identified safely. " +
                        "Activate the required STAAD window and try again."
                    : "Multiple STAAD processes are running, but their file monikers are not available. " +
                        "Save the required STAAD models and activate the target window before trying again.";
                if (!string.IsNullOrWhiteSpace(rotDiagnostics))
                    Debug.WriteLine("OpenSTAAD ROT diagnostics: " + rotDiagnostics);
                return false;
            }

            if (selected == null)
            {
                if (TryGetLegacyConnection(out connection, out error))
                    return true;

                if (!string.IsNullOrWhiteSpace(rotDiagnostics))
                    error += " ROT diagnostics: " + rotDiagnostics;
                return false;
            }

            _cachedConnection = new StaadConnectionSnapshot(selected.OpenStaad, selected.Identity);
            _lastHealthCheckUtc = DateTime.UtcNow;
            StaadForegroundTracker.RememberProcess(selected.Identity.ProcessId, selected.Identity.MainWindowHandle);
            connection = _cachedConnection;
            AppLogger.Info("OpenSTAAD target: " + selected.Identity.FullPath + " (PID " + selected.Identity.ProcessId + ").");
            return true;
        }

        private static bool CanReuseCachedConnection(int activeProcessId, out StaadConnectionSnapshot connection)
        {
            connection = null;
            if (_cachedConnection == null)
                return false;

            if (activeProcessId > 0 &&
                _cachedConnection.Identity.ProcessId > 0 &&
                activeProcessId != _cachedConnection.Identity.ProcessId)
            {
                return false;
            }

            if (DateTime.UtcNow - _lastHealthCheckUtc < HealthCheckInterval)
            {
                connection = _cachedConnection;
                return true;
            }

            if (!OpenStaadModelAdapter.TryGetBaseUnit(_cachedConnection.OpenStaad, out _, out string pingError))
            {
                Debug.WriteLine("Cached OpenSTAAD ping failed; clearing cache: " + pingError);
                _cachedConnection = null;
                OpenStaadRotResolver.InvalidateCache();
                return false;
            }

            if (OpenStaadRotResolver.TryCreateIdentity(
                    _cachedConnection.OpenStaad,
                    _cachedConnection.Identity.MonikerDisplayName,
                    _cachedConnection.Identity.IsLegacyFallback,
                    out StaadModelIdentity refreshedIdentity,
                    out _) &&
                !_cachedConnection.Identity.Equals(refreshedIdentity))
            {
                _cachedConnection = new StaadConnectionSnapshot(_cachedConnection.OpenStaad, refreshedIdentity);
                OpenStaadRotResolver.InvalidateCache();
                AppLogger.Info("OpenSTAAD model identity changed: " + refreshedIdentity.FullPath +
                    " (PID " + refreshedIdentity.ProcessId + ").");
            }

            _lastHealthCheckUtc = DateTime.UtcNow;
            connection = _cachedConnection;
            return true;
        }

        private static bool TryGetLegacyConnection(out StaadConnectionSnapshot connection, out string error)
        {
            connection = null;
            error = string.Empty;

            if (_cachedConnection != null &&
                OpenStaadModelAdapter.TryGetBaseUnit(_cachedConnection.OpenStaad, out _, out _))
            {
                connection = _cachedConnection;
                return true;
            }

            try
            {
                object openStaad = Marshal.GetActiveObject("StaadPro.OpenSTAAD");
                if (!OpenStaadRotResolver.TryCreateIdentity(
                        openStaad,
                        "StaadPro.OpenSTAAD",
                        true,
                        out StaadModelIdentity identity,
                        out string identityError))
                {
                    error = "Connected to OpenSTAAD, but the model identity could not be validated: " + identityError;
                    return false;
                }

                _cachedConnection = new StaadConnectionSnapshot(openStaad, identity);
                _lastHealthCheckUtc = DateTime.UtcNow;
                StaadForegroundTracker.RememberProcess(identity.ProcessId, identity.MainWindowHandle);
                connection = _cachedConnection;
                return true;
            }
            catch (Exception ex)
            {
                error = "Could not connect to OpenSTAAD. Make sure STAAD.Pro is open. Details: " + ex.Message;
                return false;
            }
        }

        private static void EnsureInitializedLocked()
        {
            if (_initialized)
                return;

            StaadForegroundTracker.Start();
            _initialized = true;
        }

        private static bool AreSameIdentity(StaadModelIdentity left, StaadModelIdentity right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;
            return left.Equals(right);
        }

        private static void RaiseActiveModelChanged(StaadActiveModelChangedEventArgs args)
        {
            try
            {
                ActiveModelChanged?.Invoke(null, args);
            }
            catch (Exception ex)
            {
                AppLogger.Exception("ActiveModelChanged handler failed.", ex);
            }
        }

        public static void FinishStaad(bool refreshView = true)
        {
            lock (_lock)
            {
                if (_cachedConnection == null) return;
            }
        }
    }
}
