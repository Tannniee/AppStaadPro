using System;

namespace StaadSteelApp.Core.Interop
{
    internal static partial class OpenStaadSelectionAdapter
    {
        internal static bool TryGetSelectedNodeIds(
            object openStaad,
            bool validateExisting,
            bool includeHiddenEntities,
            out int[] nodeIds,
            out int invalidCount,
            out string error)
        {
            return TryGetSelectedNodeIds(
                openStaad,
                validateExisting,
                includeHiddenEntities,
                new[] { 1, 0 },
                out nodeIds,
                out invalidCount,
                out error);
        }

        internal static bool TryGetSelectedNodeIds(
            object openStaad,
            bool validateExisting,
            bool includeHiddenEntities,
            int[] sortFlags,
            out int[] nodeIds,
            out int invalidCount,
            out string error)
        {
            nodeIds = Array.Empty<int>();
            invalidCount = 0;
            error = string.Empty;

            if (!TryGetGeometry(openStaad, out object geometry, out error))
                return false;

            return TryGetSelectedNodeIdsFromGeometry(
                geometry,
                validateExisting,
                includeHiddenEntities,
                sortFlags,
                out nodeIds,
                out invalidCount,
                out error);
        }

        internal static bool TryGetSelectedNodeIdsFromGeometry(
            object geometry,
            bool validateExisting,
            bool includeHiddenEntities,
            out int[] nodeIds,
            out int invalidCount,
            out string error)
        {
            return TryGetSelectedNodeIdsFromGeometry(
                geometry,
                validateExisting,
                includeHiddenEntities,
                new[] { 1, 0 },
                out nodeIds,
                out invalidCount,
                out error);
        }

        internal static bool TryGetSelectedNodeIdsFromGeometry(
            object geometry,
            bool validateExisting,
            bool includeHiddenEntities,
            int[] sortFlags,
            out int[] nodeIds,
            out int invalidCount,
            out string error)
        {
            nodeIds = Array.Empty<int>();
            invalidCount = 0;
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

                if (!TryGetSelectionCount(geometry, "GetNoOfSelectedNodes", out int count, out error))
                    return false;

                if (count <= 0)
                    return true;

                int[] rawNodeIds = TryGetSelectedIds(geometry, "GetSelectedNodes", count, sortFlags ?? new[] { 1, 0 });
                nodeIds = validateExisting
                    ? FilterValidNodeIds(geometry, rawNodeIds, out invalidCount)
                    : NormalizeIds(rawNodeIds, out invalidCount);
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
