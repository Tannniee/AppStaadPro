using System;
using System.Diagnostics;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        public static int[] GetSelectedBeamIDsSafe(object os, out string error)
        {
            error = string.Empty;
            if (os == null)
                return Array.Empty<int>();

            try
            {
                object openStaad = os;
                if (!TryGetSectionGeometry(openStaad, out object geometryObj, out error))
                    return Array.Empty<int>();

                return GetSelectedBeamIDs(geometryObj, out error);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return Array.Empty<int>();
            }
        }

        private static int[] GetSelectedBeamIDs(object geometry)
        {
            string ignoredError;
            return GetSelectedBeamIDs(geometry, out ignoredError);
        }

        private static int[] GetSelectedBeamIDs(object geometry, out string error)
        {
            error = string.Empty;
            if (geometry == null)
                return Array.Empty<int>();

            if (OpenStaadSelectionAdapter.TryGetSelectedAnalyticalBeamIdsFromGeometry(
                    geometry,
                    true,
                    out int[] beamIds,
                    out error))
            {
                if (beamIds.Length == 0 && !string.IsNullOrWhiteSpace(error))
                    Debug.WriteLine(error);

                return beamIds;
            }

            Debug.WriteLine("Get selected beam IDs failed: " + error);
            return Array.Empty<int>();
        }
    }
}
