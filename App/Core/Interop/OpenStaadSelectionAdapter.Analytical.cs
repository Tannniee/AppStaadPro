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

            var selectedAnalyticalMembers = new System.Collections.Generic.List<int>();
            var seenAnalyticalMembers = new System.Collections.Generic.HashSet<int>();
            int invalidBeamCount = 0;

            if (beamCount > 0)
            {
                int[] rawBeamIds = TryGetSelectedIds(geometry, "GetSelectedBeams", beamCount, new[] { 0, 1 });
                int[] validBeamIds = FilterValidBeamIds(geometry, rawBeamIds, out invalidBeamCount);
                AddUniqueIds(selectedAnalyticalMembers, seenAnalyticalMembers, validBeamIds);
            }

            if (!includePhysicalMemberFallback)
            {
                beamIds = selectedAnalyticalMembers.ToArray();
                error = BuildAnalyticalSelectionWarning(invalidBeamCount);
                return true;
            }

            if (!TryGetSelectionCount(geometry, "GetNoOfSelectedPhysicalMembers", out int physicalCount, out error))
            {
                if (selectedAnalyticalMembers.Count == 0)
                    return false;

                Debug.WriteLine("Get selected physical member count failed: " + error);
                beamIds = selectedAnalyticalMembers.ToArray();
                error = BuildAnalyticalSelectionWarning(invalidBeamCount);
                return true;
            }

            if (physicalCount > 0)
            {
                int[] physicalMembers = NormalizeIds(
                    TryGetSelectedIds(geometry, "GetSelectedPhysicalMembers", physicalCount, new[] { 0, 1 }),
                    out _);
                if (physicalMembers.Length > 0)
                {
                    int[] expandedMembers = ExpandPhysicalSelectionToAnalyticalMembers(geometry, physicalMembers);
                    AddUniqueIds(selectedAnalyticalMembers, seenAnalyticalMembers, expandedMembers);
                    if (expandedMembers.Length == 0)
                        Debug.WriteLine("Physical member(s) are selected, but analytical members could not be resolved.");
                }
            }

            beamIds = selectedAnalyticalMembers.ToArray();
            error = BuildAnalyticalSelectionWarning(invalidBeamCount);
            return true;
        }

        private static void AddUniqueIds(
            System.Collections.Generic.List<int> destination,
            System.Collections.Generic.HashSet<int> seen,
            int[] ids)
        {
            if (destination == null || seen == null || ids == null)
                return;

            foreach (int id in ids)
            {
                if (id > 0 && seen.Add(id))
                    destination.Add(id);
            }
        }

        private static string BuildAnalyticalSelectionWarning(int invalidBeamCount)
        {
            return invalidBeamCount <= 0
                ? string.Empty
                : $"Ignored {invalidBeamCount} stale or non-analytical member ID(s) returned by the STAAD selection buffer.";
        }

        internal static bool TryGetSelectedPhysicalMemberIdsFromGeometry(
            object geometry,
            out int[] physicalMemberIds,
            out string error)
        {
            physicalMemberIds = Array.Empty<int>();
            error = string.Empty;

            if (geometry == null)
            {
                error = "OpenSTAAD Geometry object is not available.";
                return false;
            }

            if (!TryGetSelectionCount(geometry, "GetNoOfSelectedPhysicalMembers", out int physicalCount, out error))
                return false;

            if (physicalCount <= 0)
                return true;

            physicalMemberIds = NormalizeIds(
                TryGetSelectedIds(geometry, "GetSelectedPhysicalMembers", physicalCount, new[] { 0, 1 }),
                out _);
            return true;
        }
    }
}
