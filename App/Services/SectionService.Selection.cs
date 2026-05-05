using System;
using System.Diagnostics;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        public static int[] GetSelectedBeamIDsSafe(dynamic os, out string error)
        {
            error = string.Empty;
            if (os == null)
                return Array.Empty<int>();

            try
            {
                return GetSelectedBeamIDs(os.Geometry, out error);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return Array.Empty<int>();
            }
        }

        private static int[] GetSelectedBeamIDs(dynamic geometry)
        {
            string ignoredError;
            return GetSelectedBeamIDs(geometry, out ignoredError);
        }

        private static int[] GetSelectedBeamIDs(dynamic geometry, out string error)
        {
            error = string.Empty;
            if (geometry == null)
                return Array.Empty<int>();

            object geometryObj = geometry;
            if (OpenStaadSelectionAdapter.TryGetSelectedAnalyticalBeamIdsFromGeometry(
                    geometryObj,
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
