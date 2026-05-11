using System.Collections.Generic;
using System.Globalization;

namespace StaadSteelApp.Core.Advance
{
    internal static class AdvanceConnectService
    {
        internal static bool TryConnectSelectedNodes(
            int[] selectedNodeIds,
            AlignKeepAxis sortAxis,
            out int createdCount,
            out string message)
        {
            createdCount = 0;
            message = string.Empty;

            if (selectedNodeIds == null || selectedNodeIds.Length < 2)
            {
                message = "Please select at least 2 nodes in STAAD.";
                return false;
            }

            if (!AdvanceOpenStaadService.TryGetGeometry(out _, out object geometry, out message))
                return false;

            if (!AdvanceSelectionService.TryLoadSelectedNodeData(geometry, selectedNodeIds, out int[] validNodeIds, out List<AdvanceNodeData> nodes, out message))
                return false;

            var existingEdges = AdvanceWriteService.GetExistingEdges(geometry, validNodeIds);
            nodes.Sort((left, right) => AdvanceGeometryService.CompareNodeByAxis(left, right, sortAxis));

            List<AdvanceEdgeKey> candidateEdges = AdvanceGeometryService.BuildChainEdges(nodes);
            if (candidateEdges.Count == 0)
            {
                message = "No valid beam path found after sorting the selected nodes.";
                return false;
            }

            return AdvanceWriteService.TryCreateBeamEdges(
                geometry,
                existingEdges,
                candidateEdges,
                $"Created {{0}} beam(s) after sorting by {sortAxis}.",
                out createdCount,
                out message);
        }

        internal static bool TryConnectSelectedNodesByGap(
            int[] selectedNodeIds,
            AlignKeepAxis gapAxis,
            double gapLimit,
            AlignKeepAxis sortAxis,
            out int createdCount,
            out string message)
        {
            createdCount = 0;
            message = string.Empty;

            if (selectedNodeIds == null || selectedNodeIds.Length < 2)
            {
                message = "Please select at least 2 nodes in STAAD.";
                return false;
            }

            if (gapLimit <= 0)
            {
                message = "Gap limit must be greater than zero.";
                return false;
            }

            if (!AdvanceOpenStaadService.TryGetGeometry(out _, out object geometry, out message))
                return false;

            if (!AdvanceSelectionService.TryLoadSelectedNodeData(geometry, selectedNodeIds, out int[] validNodeIds, out List<AdvanceNodeData> nodes, out message))
                return false;

            List<List<AdvanceNodeData>> groups = AdvanceGeometryService.BuildGapGroups(nodes, gapAxis, gapLimit);
            int groupsWithEnoughNodes = 0;
            var candidateEdges = new List<AdvanceEdgeKey>();
            foreach (List<AdvanceNodeData> group in groups)
            {
                if (group.Count < 2)
                    continue;

                groupsWithEnoughNodes++;
                group.Sort((left, right) => AdvanceGeometryService.CompareNodeByAxis(left, right, sortAxis));
                candidateEdges.AddRange(AdvanceGeometryService.BuildChainEdges(group));
            }

            if (candidateEdges.Count == 0)
            {
                message = "No valid beam path found after grouping selected nodes by gap.";
                return false;
            }

            var existingEdges = AdvanceWriteService.GetExistingEdges(geometry, validNodeIds);
            return AdvanceWriteService.TryCreateBeamEdges(
                geometry,
                existingEdges,
                candidateEdges,
                $"Created {{0}} beam(s) from {groupsWithEnoughNodes} group(s) using {gapAxis} gap {gapLimit.ToString("0.###", CultureInfo.InvariantCulture)} and sort {sortAxis}.",
                out createdCount,
                out message);
        }
    }
}
