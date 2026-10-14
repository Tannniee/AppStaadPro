using System;
using System.Collections.Generic;

namespace StaadSteelApp.Core.Interop
{
    internal static partial class OpenStaadSelectionAdapter
    {
        internal static bool TryGetMassBreakdownBeamIds(
            object openStaad,
            out int[] beamIds,
            out bool hasSelection,
            out string error)
        {
            beamIds = Array.Empty<int>();
            hasSelection = false;
            error = string.Empty;

            if (!TryGetGeometry(openStaad, out object geometry, out error) ||
                !TryGetSelectionCount(geometry, "GetNoOfSelectedBeams", out int beamCount, out error))
                return false;

            var selected = new HashSet<int>();
            if (beamCount > 0)
            {
                hasSelection = true;
                int[] rawIds = TryGetSelectedIds(geometry, "GetSelectedBeams", beamCount, new[] { 0, 1 });
                int[] validIds = FilterValidBeamIds(geometry, rawIds, out int invalidCount);
                if (validIds.Length != beamCount)
                {
                    error = $"STAAD reports {beamCount} selected beam(s), but only {validIds.Length} valid ID(s) could be read ({invalidCount} invalid). Reselect the members and retry.";
                    return false;
                }

                foreach (int id in validIds)
                    selected.Add(id);
            }

            // Physical members need expansion because Section Manager scans analytical beams.
            if (TryGetSelectionCount(geometry, "GetNoOfSelectedPhysicalMembers", out int physicalCount, out string physicalError) &&
                physicalCount > 0)
            {
                hasSelection = true;
                int[] physicalIds = NormalizeIds(
                    TryGetSelectedIds(geometry, "GetSelectedPhysicalMembers", physicalCount, new[] { 0, 1 }),
                    out _);
                if (physicalIds.Length != physicalCount)
                {
                    error = $"STAAD reports {physicalCount} selected physical member(s), but only {physicalIds.Length} ID(s) could be read. Reselect and retry.";
                    return false;
                }

                foreach (int physicalId in physicalIds)
                {
                    int[] expanded = ExpandPhysicalSelectionToAnalyticalMembers(geometry, new[] { physicalId });
                    int[] validIds = FilterValidBeamIds(geometry, expanded, out _);
                    if (expanded.Length == 0 || validIds.Length != expanded.Length)
                    {
                        error = $"Could not resolve selected physical member {physicalId} to valid analytical beams.";
                        return false;
                    }

                    foreach (int id in validIds)
                        selected.Add(id);
                }
            }
            else if (!string.IsNullOrWhiteSpace(physicalError) && !hasSelection)
            {
                error = "Could not read STAAD physical member selection: " + physicalError;
                return false;
            }

            beamIds = new int[selected.Count];
            selected.CopyTo(beamIds);
            Array.Sort(beamIds);
            return true;
        }

        internal static bool TryGetSelectedBeamIds(
            object openStaad,
            bool validateIncidence,
            bool includeHiddenEntities,
            out int[] beamIds,
            out string error)
        {
            return TryGetSelectedBeamIds(
                openStaad,
                validateIncidence,
                includeHiddenEntities,
                new[] { 1, 0 },
                out beamIds,
                out error);
        }

        internal static bool TryGetSelectedBeamIds(
            object openStaad,
            bool validateIncidence,
            bool includeHiddenEntities,
            int[] sortFlags,
            out int[] beamIds,
            out string error)
        {
            beamIds = Array.Empty<int>();
            error = string.Empty;

            if (!TryGetGeometry(openStaad, out object geometry, out error))
                return false;

            return TryGetSelectedBeamIdsFromGeometry(
                geometry,
                validateIncidence,
                includeHiddenEntities,
                sortFlags,
                out beamIds,
                out error);
        }

        internal static bool TryGetSelectedBeamIdsFromGeometry(
            object geometry,
            bool validateIncidence,
            bool includeHiddenEntities,
            out int[] beamIds,
            out string error)
        {
            return TryGetSelectedBeamIdsFromGeometry(
                geometry,
                validateIncidence,
                includeHiddenEntities,
                new[] { 1, 0 },
                out beamIds,
                out error);
        }

        internal static bool TryGetSelectedBeamIdsFromGeometry(
            object geometry,
            bool validateIncidence,
            bool includeHiddenEntities,
            int[] sortFlags,
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

            int? previousHiddenFlag = includeHiddenEntities ? TryGetHiddenEntitiesFlag(geometry) : null;
            try
            {
                if (includeHiddenEntities)
                    TrySetHiddenEntitiesFlag(geometry, 0);

                if (!TryGetSelectionCount(geometry, "GetNoOfSelectedBeams", out int count, out error))
                    return false;

                if (count <= 0)
                    return true;

                int[] rawBeamIds = TryGetSelectedIds(geometry, "GetSelectedBeams", count, sortFlags ?? new[] { 1, 0 });
                beamIds = validateIncidence
                    ? FilterValidBeamIds(geometry, rawBeamIds, out int invalidCount)
                    : NormalizeIds(rawBeamIds, out invalidCount);

                if (invalidCount > 0)
                    error = $"Ignored {invalidCount} invalid/deleted beam ID(s) from selection.";

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            finally
            {
                if (previousHiddenFlag.HasValue)
                    TrySetHiddenEntitiesFlag(geometry, previousHiddenFlag.Value);
            }
        }
    }
}
