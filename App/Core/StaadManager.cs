using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
// Legacy typed COM reference removed; OpenSTAAD is accessed dynamically.
using System.Windows.Forms;

namespace StaadSteelApp.Core
{
    public static class StaadManager
    {
        // Use dynamic for OpenSTAAD to avoid COM dependency issues
        private static dynamic _cachedOpenStaad;

        public static dynamic GetOpenSTAAD()
        {
            // If already connected, test connection (optional)
            if (_cachedOpenStaad != null)
            {
                try
                {
                    // Basic ping: read a stable model-level value.
                    // If it fails, we assume connection lost
                    object test = _cachedOpenStaad.GetBaseUnit();
                    return _cachedOpenStaad;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Cached OpenSTAAD ping failed; clearing cache: " + ex.Message);
                    _cachedOpenStaad = null;
                }
            }

            try
            {
                // Equivalent to GetObject(, "StaadPro.OpenSTAAD")
                _cachedOpenStaad = Marshal.GetActiveObject("StaadPro.OpenSTAAD");
            }
            catch (Exception ex)
            {
                StaadSteelApp.TopMostMessageBox.Show(
                    "Could not connect to OpenSTAAD. Make sure STAAD.Pro is open.\nDetails: " + ex.Message,
                    "Connection Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return null;
            }

            return _cachedOpenStaad;
        }

        public static void FinishStaad(bool refreshView = true)
        {
            if (_cachedOpenStaad == null) return;
            // STAAD doesn't have a direct "view refresh" in the same way, but usually interactions update immediately.
            // We can leave this empty or add specific STAAD view commands if needed.
        }
    }
}
