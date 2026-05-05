using System;
using System.Diagnostics;

namespace StaadSteelApp.Core.Interop
{
    internal static partial class OpenStaadSelectionAdapter
    {
        internal static bool TryGetSelectedAnalyticalBeamIds(
            object openStaad,
            bool includePhysicalMemberFallback,
            out int[] beamIds,
            out string error)
        {
            beamIds = Array.Empty<int>();
            error = string.Empty;

            if (!TryGetGeometry(openStaad, out object geometry, out error))
                return false;

            return TryGetSelectedAnalyticalBeamIdsFromGeometry(
                geometry,
                includePhysicalMemberFallback,
                out beamIds,
                out error);
        }

        internal static bool TryGetSelectedAnalyticalBeamIdsFromGeometry(
            object geometry,
            bool includePhysicalMemberFallback,
            out int[] beamIds,
            out string error)
        {
            beamIds = Array.Empty<int>();
            error = string.Empty;

            if (geometry == null)
            {
                error = "OpenSTAAD Geometry object is not available.";
                return false;
            }

            if (!TryGetSelectionCount(geometry, "GetNoOfSelectedBeams", out int beamCount, out error))
                return false;

            if (beamCount > 0)
            {
                beamIds = NormalizeIds(TryGetSelectedIds(geometry, "GetSelectedBeams", beamCount, new[] { 0, 1 }), out _);
                if (beamIds.Length > 0)
                    return true;
            }

            if (!includePhysicalMemberFallback)
                return true;

            if (!TryGetSelectionCount(geometry, "GetNoOfSelectedPhysicalMembers", out int physicalCount, out error))
                return false;

            if (physicalCount <= 0)
                return true;

            int[] physicalMembers = NormalizeIds(
                TryGetSelectedIds(geometry, "GetSelectedPhysicalMembers", physicalCount, new[] { 0, 1 }),
                out _);
            if (physicalMembers.Length == 0)
                return true;

            beamIds = ExpandPhysicalSelectionToAnalyticalMembers(geometry, physicalMembers);
            if (beamIds.Length == 0)
                Debug.WriteLine("Physical member(s) are selected, but analytical members could not be resolved.");

            return true;
        }
    }
}
