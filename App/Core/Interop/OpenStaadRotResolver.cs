using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace StaadSteelApp.Core.Interop
{
    internal sealed class OpenStaadRotCandidate
    {
        internal OpenStaadRotCandidate(object openStaad, StaadModelIdentity identity)
        {
            OpenStaad = openStaad;
            Identity = identity;
        }

        internal object OpenStaad { get; }
        internal StaadModelIdentity Identity { get; }
    }

    internal static class OpenStaadRotResolver
    {
        private static readonly object CacheLock = new object();
        private static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(5);
        private static DateTime _cacheCreatedUtc = DateTime.MinValue;
        private static IReadOnlyList<OpenStaadRotCandidate> _cachedCandidates = Array.Empty<OpenStaadRotCandidate>();

        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable runningObjectTable);

        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(int reserved, out IBindCtx bindContext);

        internal static IReadOnlyList<OpenStaadRotCandidate> GetCandidates(bool forceRefresh, out string diagnostics)
        {
            lock (CacheLock)
            {
                if (!forceRefresh && DateTime.UtcNow - _cacheCreatedUtc <= CacheLifetime)
                {
                    diagnostics = string.Empty;
                    return _cachedCandidates;
                }

                IReadOnlyList<OpenStaadRotCandidate> candidates = EnumerateCandidates(out diagnostics);
                _cachedCandidates = candidates;
                _cacheCreatedUtc = DateTime.UtcNow;
                return candidates;
            }
        }

        internal static void InvalidateCache()
        {
            lock (CacheLock)
            {
                _cacheCreatedUtc = DateTime.MinValue;
                _cachedCandidates = Array.Empty<OpenStaadRotCandidate>();
            }
        }

        internal static bool TryCreateIdentity(
            object openStaad,
            string monikerDisplayName,
            bool legacyFallback,
            out StaadModelIdentity identity,
            out string error)
        {
            identity = null;
            error = string.Empty;

            if (openStaad == null)
            {
                error = "OpenSTAAD object is null.";
                return false;
            }

            string fullPath = string.Empty;
            OpenStaadModelAdapter.TryGetStaadFile(openStaad, true, out fullPath, out string pathError);

            int processId = 0;
            OpenStaadModelAdapter.TryGetProcessId(openStaad, out processId, out string processError);

            long mainWindowHandle = 0;
            OpenStaadModelAdapter.TryGetMainWindowHandle(openStaad, out mainWindowHandle, out string windowError);

            if (string.IsNullOrWhiteSpace(fullPath) && processId <= 0)
            {
                error = "OpenSTAAD identity validation failed. GetSTAADFile: " +
                    (string.IsNullOrWhiteSpace(pathError) ? "empty" : pathError) +
                    "; GetProcessId: " +
                    (string.IsNullOrWhiteSpace(processError) ? "empty" : processError) +
                    (string.IsNullOrWhiteSpace(windowError) ? string.Empty : "; GetMainWindowHandle: " + windowError);
                return false;
            }

            identity = new StaadModelIdentity(
                fullPath,
                processId,
                mainWindowHandle,
                monikerDisplayName,
                legacyFallback);
            return true;
        }

        private static IReadOnlyList<OpenStaadRotCandidate> EnumerateCandidates(out string diagnostics)
        {
            var candidates = new List<OpenStaadRotCandidate>();
            var errors = new List<string>();
            IRunningObjectTable runningObjectTable = null;
            IEnumMoniker monikerEnumerator = null;
            IBindCtx bindContext = null;

            try
            {
                int rotCode = GetRunningObjectTable(0, out runningObjectTable);
                if (rotCode != 0 || runningObjectTable == null)
                {
                    diagnostics = "GetRunningObjectTable failed with HRESULT 0x" + rotCode.ToString("X8") + ".";
                    return candidates;
                }

                int bindCode = CreateBindCtx(0, out bindContext);
                if (bindCode != 0 || bindContext == null)
                {
                    diagnostics = "CreateBindCtx failed with HRESULT 0x" + bindCode.ToString("X8") + ".";
                    return candidates;
                }

                runningObjectTable.EnumRunning(out monikerEnumerator);
                if (monikerEnumerator == null)
                {
                    diagnostics = "The Running Object Table returned no moniker enumerator.";
                    return candidates;
                }

                var monikers = new IMoniker[1];
                while (monikerEnumerator.Next(1, monikers, IntPtr.Zero) == 0)
                {
                    IMoniker moniker = monikers[0];
                    monikers[0] = null;
                    if (moniker == null)
                        continue;

                    try
                    {
                        moniker.GetDisplayName(bindContext, null, out string displayName);
                        if (!LooksLikeStaadFileMoniker(displayName))
                            continue;

                        object openStaad = null;
                        try
                        {
                            openStaad = Marshal.BindToMoniker(displayName);
                        }
                        catch
                        {
                            runningObjectTable.GetObject(moniker, out openStaad);
                        }

                        if (!TryCreateIdentity(openStaad, displayName, false, out StaadModelIdentity identity, out string identityError))
                        {
                            errors.Add(displayName + ": " + identityError);
                            continue;
                        }

                        candidates.Add(new OpenStaadRotCandidate(openStaad, identity));
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex.Message);
                    }
                    finally
                    {
                        try
                        {
                            if (Marshal.IsComObject(moniker))
                                Marshal.ReleaseComObject(moniker);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
            }
            finally
            {
                ReleaseComObject(monikerEnumerator);
                ReleaseComObject(bindContext);
                ReleaseComObject(runningObjectTable);
            }

            IReadOnlyList<OpenStaadRotCandidate> distinct = candidates
                .GroupBy(candidate => candidate.Identity.CacheKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(candidate => candidate.Identity.ProcessId)
                .ThenBy(candidate => candidate.Identity.FullPath, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            diagnostics = string.Join(" | ", errors.Where(item => !string.IsNullOrWhiteSpace(item)));
            return distinct;
        }

        private static bool LooksLikeStaadFileMoniker(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return false;

            string value = displayName.Trim().Trim('"');
            return value.EndsWith(".std", StringComparison.OrdinalIgnoreCase);
        }

        private static void ReleaseComObject(object value)
        {
            if (value == null)
                return;

            try
            {
                if (Marshal.IsComObject(value))
                    Marshal.ReleaseComObject(value);
            }
            catch
            {
            }
        }
    }
}
