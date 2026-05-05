using System;
using System.Diagnostics;
using System.Globalization;

namespace StaadSteelApp.Core.Interop
{
    internal static partial class OpenStaadSelectionAdapter
    {
        private static bool TryGetGeometry(object openStaad, out object geometry, out string error)
        {
            geometry = null;
            error = string.Empty;

            if (openStaad == null)
            {
                error = "Could not connect to OpenSTAAD.";
                return false;
            }

            try
            {
                dynamic dynOpenStaad = openStaad;
                geometry = dynOpenStaad.Geometry;
                if (geometry == null)
                {
                    error = "OpenSTAAD Geometry object is not available.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool TryGetSelectionCount(object geometry, string methodName, out int count, out string error)
        {
            count = 0;
            error = string.Empty;

            try
            {
                dynamic dynGeometry = geometry;
                if (string.Equals(methodName, "GetNoOfSelectedNodes", StringComparison.Ordinal))
                {
                    count = Convert.ToInt32(dynGeometry.GetNoOfSelectedNodes(), CultureInfo.InvariantCulture);
                    return true;
                }

                if (string.Equals(methodName, "GetNoOfSelectedBeams", StringComparison.Ordinal))
                {
                    count = Convert.ToInt32(dynGeometry.GetNoOfSelectedBeams(), CultureInfo.InvariantCulture);
                    return true;
                }

                if (string.Equals(methodName, "GetNoOfSelectedPhysicalMembers", StringComparison.Ordinal))
                {
                    count = Convert.ToInt32(dynGeometry.GetNoOfSelectedPhysicalMembers(), CultureInfo.InvariantCulture);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{methodName} dynamic failed: {ex.Message}");
            }

            if (ComInvoke.TryInvoke(geometry, methodName, out object result, out error))
            {
                count = Math.Max(0, ComResult.ToInt32(result));
                return true;
            }

            Debug.WriteLine($"{methodName} reflection failed: {error}");
            count = 0;
            return false;
        }
    }
}
