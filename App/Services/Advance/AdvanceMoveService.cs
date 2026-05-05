using System.Collections.Generic;

namespace StaadSteelApp.Core.Advance
{
    internal static class AdvanceMoveService
    {
        internal static bool TryMoveNodesToCoordinates(
            int[] targetNodeIds,
            int rootNodeId,
            double targetX,
            double targetY,
            double targetZ,
            out int updatedCount,
            out string message)
        {
            return TryMoveNodesInternal(
                targetNodeIds,
                rootNodeId,
                targetDescription: $"coordinates ({AdvanceGeometryService.FormatCoordinate(targetX)}, {AdvanceGeometryService.FormatCoordinate(targetY)}, {AdvanceGeometryService.FormatCoordinate(targetZ)})",
                targetPointReader: delegate(object geometry, out AdvancePoint3D point, out string error)
                {
                    point = new AdvancePoint3D(targetX, targetY, targetZ);
                    error = string.Empty;
                    return true;
                },
                out updatedCount,
                out message);
        }

        internal static bool TryMoveNodesToNode(
            int[] targetNodeIds,
            int rootNodeId,
            int targetNodeId,
            out int updatedCount,
            out string message)
        {
            if (targetNodeId <= 0)
            {
                updatedCount = 0;
                message = "Target node must be a valid positive integer.";
                return false;
            }

            return TryMoveNodesInternal(
                targetNodeIds,
                rootNodeId,
                targetDescription: $"node {targetNodeId}",
                targetPointReader: delegate(object geometry, out AdvancePoint3D point, out string error)
                {
                    return AdvanceSelectionService.TryGetNodePoint(geometry, targetNodeId, out point, out error);
                },
                out updatedCount,
                out message);
        }

        private static bool TryMoveNodesInternal(
            int[] targetNodeIds,
            int rootNodeId,
            string targetDescription,
            TargetPointReader targetPointReader,
            out int updatedCount,
            out string message)
        {
            updatedCount = 0;
            message = string.Empty;

            if (targetNodeIds == null || targetNodeIds.Length == 0)
            {
                message = "No captured frame nodes found.";
                return false;
            }

            if (rootNodeId <= 0)
            {
                message = "Root node must be a valid positive integer.";
                return false;
            }

            dynamic os = StaadManager.GetOpenSTAAD();
            if (os == null)
            {
                message = "Could not connect to OpenSTAAD.";
                return false;
            }

            object geometry = os.Geometry;
            int[] validTargets = AdvanceSelectionService.FilterValidNodeIds(geometry, targetNodeIds, out int invalidCount);
            if (validTargets.Length == 0)
            {
                message = invalidCount > 0
                    ? $"No valid captured node found (ignored {invalidCount} invalid/deleted node IDs)."
                    : "No valid captured node found.";
                return false;
            }

            if (!ContainsNode(validTargets, rootNodeId))
            {
                message = $"Root node {rootNodeId} must belong to the captured frame.";
                return false;
            }

            if (!AdvanceSelectionService.TryGetNodePoint(geometry, rootNodeId, out AdvancePoint3D rootPoint, out string rootError))
            {
                message = $"Could not read root node {rootNodeId}: {rootError}";
                return false;
            }

            if (!targetPointReader(geometry, out AdvancePoint3D targetPoint, out string targetError))
            {
                message = $"Could not read move target from {targetDescription}: {targetError}";
                return false;
            }

            AdvancePoint3D delta = AdvanceGeometryService.Subtract(targetPoint, rootPoint);
            var failed = new List<string>();
            foreach (int nodeId in validTargets)
            {
                if (!AdvanceSelectionService.TryGetNodePoint(geometry, nodeId, out AdvancePoint3D current, out string readError))
                {
                    failed.Add($"Node {nodeId}: read failed ({readError})");
                    continue;
                }

                AdvancePoint3D next = AdvanceGeometryService.Add(current, delta);
                if (!AdvanceWriteService.TrySetNodePoint(geometry, nodeId, next, out string writeError))
                {
                    failed.Add($"Node {nodeId}: set failed ({writeError})");
                    continue;
                }

                updatedCount++;
            }

            if (updatedCount == 0)
            {
                string detail = failed.Count > 0 ? failed[0] : "No node was updated.";
                message = "Move failed. " + detail;
                return false;
            }

            message =
                $"Moved {updatedCount}/{validTargets.Length} node(s) using root node {rootNodeId} -> {targetDescription}." +
                $"\nDelta: ({AdvanceGeometryService.FormatCoordinate(delta.X)}, {AdvanceGeometryService.FormatCoordinate(delta.Y)}, {AdvanceGeometryService.FormatCoordinate(delta.Z)}).";

            if (invalidCount > 0)
                message += $"\nIgnored {invalidCount} invalid/deleted captured node ID(s).";

            if (failed.Count > 0)
                message += "\nSome nodes failed. First: " + failed[0];

            return true;
        }

        private static bool ContainsNode(int[] nodeIds, int nodeId)
        {
            for (int i = 0; i < nodeIds.Length; i++)
            {
                if (nodeIds[i] == nodeId)
                    return true;
            }

            return false;
        }

        private delegate bool TargetPointReader(object geometry, out AdvancePoint3D point, out string error);
    }
}
