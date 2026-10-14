using System;
using System.IO;

namespace StaadSteelApp.Core
{
    public enum StaadConnectionMode
    {
        ActiveModel,
        LegacySingleInstance
    }

    public sealed class StaadModelIdentity : IEquatable<StaadModelIdentity>
    {
        internal StaadModelIdentity(
            string fullPath,
            int processId,
            long mainWindowHandle,
            string monikerDisplayName,
            bool isLegacyFallback)
        {
            FullPath = NormalizePath(fullPath);
            ProcessId = processId;
            MainWindowHandle = mainWindowHandle;
            MonikerDisplayName = monikerDisplayName ?? string.Empty;
            IsLegacyFallback = isLegacyFallback;
        }

        public string FullPath { get; }
        public int ProcessId { get; }
        public long MainWindowHandle { get; }
        public string MonikerDisplayName { get; }
        public bool IsLegacyFallback { get; }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(FullPath))
                    return Path.GetFileName(FullPath);

                return ProcessId > 0 ? "Unsaved STAAD model (PID " + ProcessId + ")" : "STAAD model";
            }
        }

        public string CacheKey
        {
            get
            {
                string pathKey = string.IsNullOrWhiteSpace(FullPath)
                    ? (MonikerDisplayName ?? string.Empty)
                    : FullPath;
                return ProcessId + "|" + pathKey.ToUpperInvariant();
            }
        }

        public bool Equals(StaadModelIdentity other)
        {
            if (ReferenceEquals(other, null))
                return false;

            return ProcessId == other.ProcessId &&
                string.Equals(FullPath, other.FullPath, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(MonikerDisplayName, other.MonikerDisplayName, StringComparison.OrdinalIgnoreCase) &&
                IsLegacyFallback == other.IsLegacyFallback;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as StaadModelIdentity);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = ProcessId;
                hash = (hash * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(FullPath ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(MonikerDisplayName ?? string.Empty);
                hash = (hash * 397) ^ IsLegacyFallback.GetHashCode();
                return hash;
            }
        }

        internal static string NormalizePath(string path)
        {
            string value = (path ?? string.Empty).Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            try
            {
                return Path.GetFullPath(value).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return value;
            }
        }
    }

    public sealed class StaadConnectionSnapshot
    {
        internal StaadConnectionSnapshot(object openStaad, StaadModelIdentity identity)
        {
            OpenStaad = openStaad ?? throw new ArgumentNullException(nameof(openStaad));
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }

        public object OpenStaad { get; }
        public StaadModelIdentity Identity { get; }
    }

    public sealed class StaadActiveModelChangedEventArgs : EventArgs
    {
        internal StaadActiveModelChangedEventArgs(StaadModelIdentity previous, StaadModelIdentity current)
        {
            Previous = previous;
            Current = current;
        }

        public StaadModelIdentity Previous { get; }
        public StaadModelIdentity Current { get; }
    }
}
