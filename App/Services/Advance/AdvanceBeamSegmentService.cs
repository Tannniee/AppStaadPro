using System.Collections.Generic;

namespace StaadSteelApp.Core.Advance
{
    internal static class AdvanceBeamSegmentService
    {
        internal static bool TryAlignNodesToSegment(
            int[] targetNodeIds,
            int segmentNodeA,
            int segmentNodeB,
            out int updatedCount,
            out string message)
        {
            updatedCount = 0;
            message = string.Empty;

            if (targetNodeIds == null || targetNodeIds.Length == 0)
            {
                message = "No target node provided for alignment.";
                return false;
            }

            if (segmentNodeA <= 0 || segmentNodeB <= 0)
            {
                message = "Beam segment nodes must be valid positive integers.";
                return false;
            }

            if (segmentNodeA == segmentNodeB)
            {
                message = "Beam segment requires 2 different nodes.";
                return false;
            }

            dynamic os = StaadManager.GetOpenSTAAD();
            if (os == null)
            {
                message = "Could not connect to OpenSTAAD.";
                return false;
            }

            object geometry = os.Geometry;
            if (!AdvanceSelectionService.TryGetNodePoint(geometry, segmentNodeA, out AdvancePoint3D startPoint, out string startError))
            {
                message = $"Could not read beam segment node {segmentNodeA}: {startError}";
                return false;
            }

            if (!AdvanceSelectionService.TryGetNodePoint(geometry, segmentNodeB, out AdvancePoint3D endPoint, out string endError))
            {
                message = $"Could not read beam segment node {segmentNodeB}: {endError}";
                return false;
            }

            double dx = endPoint.X - startPoint.X;
            double dy = endPoint.Y - startPoint.Y;
            double dz = endPoint.Z - startPoint.Z;
            double len2 = dx * dx + dy * dy + dz * dz;
            if (len2 < AdvanceConstants.AxisEpsilon)
            {
                message = "The 2 beam segment nodes are identical or too close. Cannot define a segment.";
                return false;
            }

            int[] validTargets = AdvanceSelectionService.FilterValidNodeIds(geometry, targetNodeIds, out int invalidCount);
            if (validTargets.Length == 0)
            {
                message = invalidCount > 0
                    ? $"No valid target node found (ignored {invalidCount} invalid/deleted node IDs)."
                    : "No valid target node found.";
                return false;
            }

            var failed = new List<string>();
            foreach (int nodeId in validTargets)
            {
                if (!AdvanceSelectionService.TryGetNodePoint(geometry, nodeId, out AdvancePoint3D current, out string readError))
                {
                    failed.Add($"Node {nodeId}: read failed ({readError})");
                    continue;
                }

                AdvancePoint3D projected = AdvanceGeometryService.ProjectPointToSegment(current, startPoint, endPoint);
                if (!AdvanceWriteService.TrySetNodePoint(geometry, nodeId, projected, out string writeError))
                {
                    failed.Add($"Node {nodeId}: set failed ({writeError})");
                    continue;
                }

                updatedCount++;
            }

            if (updatedCount == 0)
            {
                string detail = failed.Count > 0 ? failed[0] : "No node was updated.";
                message = "Alignment failed. " + detail;
                return false;
            }

            message = $"Aligned {updatedCount}/{validTargets.Length} node(s) to segment {segmentNodeA} -> {segmentNodeB}.";
            if (invalidCount > 0)
                message += $"\nIgnored {invalidCount} invalid/deleted target node ID(s).";

            if (failed.Count > 0)
                message += "\nSome nodes failed. First: " + failed[0];

            return true;
        }
    }
}
