using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static class StaadManager
    {
        private static readonly object _lock = new object();
        private static object _cachedOpenStaad;

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
            lock (_lock)
            {
                return TryGetOpenSTAADCore(out openStaad, out error);
            }
        }

        private static bool TryGetOpenSTAADCore(out object openStaad, out string error)
        {
            openStaad = null;
            error = string.Empty;

            if (_cachedOpenStaad != null)
            {
                if (OpenStaadModelAdapter.TryGetBaseUnit(_cachedOpenStaad, out _, out string pingError))
                {
                    openStaad = _cachedOpenStaad;
                    return true;
                }

                Debug.WriteLine("Cached OpenSTAAD ping failed; clearing cache: " + pingError);
                _cachedOpenStaad = null;
            }

            try
            {
                _cachedOpenStaad = Marshal.GetActiveObject("StaadPro.OpenSTAAD");
                openStaad = _cachedOpenStaad;
                return true;
            }
            catch (Exception ex)
            {
                error = "Could not connect to OpenSTAAD. Make sure STAAD.Pro is open. Details: " + ex.Message;
                return false;
            }
        }

        public static void FinishStaad(bool refreshView = true)
        {
            lock (_lock)
            {
                if (_cachedOpenStaad == null) return;
            }
        }
    }
}
