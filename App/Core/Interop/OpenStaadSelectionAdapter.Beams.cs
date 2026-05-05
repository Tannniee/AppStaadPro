using System;

namespace StaadSteelApp.Core.Interop
{
    internal static partial class OpenStaadSelectionAdapter
    {
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
