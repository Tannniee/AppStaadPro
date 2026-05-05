using System.Collections.Generic;

namespace StaadSteelApp.Core.Advance
{
    internal static class AdvanceRotateService
    {
        internal static bool TryRotateNodesToAxis(
            int[] targetNodeIds,
            int pivotNodeId,
            int directionBeamId,
            AlignKeepAxis targetAxis,
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

            if (pivotNodeId <= 0)
            {
                message = "Pivot node must be a valid positive integer.";
                return false;
            }

            if (directionBeamId <= 0)
            {
                message = "Direction beam must be a valid positive integer.";
                return false;
            }

            dynamic os = StaadManager.GetOpenSTAAD();
            if (os == null)
            {
                message = "Could not connect to OpenSTAAD.";
                return false;
            }

            object geometry = os.Geometry;
            int[] validTargets = AdvanceSelectionService.FilterValidNodeIds(geometry, targetNodeIds, out int invalidTargetCount);
            if (validTargets.Length == 0)
            {
                message = invalidTargetCount > 0
                    ? $"No valid captured node found (ignored {invalidTargetCount} invalid/deleted node IDs)."
                    : "No valid captured node found.";
                return false;
            }

            if (!AdvanceSelectionService.TryGetNodePoint(geometry, pivotNodeId, out AdvancePoint3D pivot, out string pivotError))
            {
                message = $"Could not read pivot node {pivotNodeId}: {pivotError}";
                return false;
            }

            if (!AdvanceSelectionService.TryGetBeamIncidence(geometry, directionBeamId, out int directionNodeA, out int directionNodeB))
            {
                message = $"Could not read incidence of direction beam {directionBeamId}.";
                return false;
            }

            if (!AdvanceSelectionService.TryGetNodePoint(geometry, directionNodeA, out AdvancePoint3D dirA, out string dirAError))
            {
                message = $"Could not read direction node {directionNodeA}: {dirAError}";
                return false;
            }

            if (!AdvanceSelectionService.TryGetNodePoint(geometry, directionNodeB, out AdvancePoint3D dirB, out string dirBError))
            {
                message = $"Could not read direction node {directionNodeB}: {dirBError}";
                return false;
            }

            AdvancePoint3D currentDirection = new AdvancePoint3D(
                dirB.X - dirA.X,
                dirB.Y - dirA.Y,
                dirB.Z - dirA.Z);

            if (!AdvanceGeometryService.TryBuildRotation(
                currentDirection,
                AdvanceGeometryService.GetAxisVector(targetAxis),
                out AdvancePoint3D rotationAxis,
                out double rotationAngleRad,
                out string rotationError))
            {
                message = rotationError;
                return false;
            }

            var nodes = new List<AdvanceNodeData>(validTargets.Length);
            foreach (int nodeId in validTargets)
            {
                if (!AdvanceSelectionService.TryGetNodePoint(geometry, nodeId, out AdvancePoint3D point, out string readError))
                {
                    message = $"Could not read node {nodeId}: {readError}";
                    return false;
                }

                nodes.Add(new AdvanceNodeData { Id = nodeId, Point = point });
            }

            if (nodes.Count == 0)
            {
                message = "No readable node found for rotate.";
                return false;
            }

            var failed = new List<string>();
            foreach (AdvanceNodeData node in nodes)
            {
                AdvancePoint3D relative = new AdvancePoint3D(
                    node.Point.X - pivot.X,
                    node.Point.Y - pivot.Y,
                    node.Point.Z - pivot.Z);

                AdvancePoint3D rotatedRelative = AdvanceGeometryService.RotateAroundAxis(relative, rotationAxis, rotationAngleRad);
                AdvancePoint3D next = new AdvancePoint3D(
                    pivot.X + rotatedRelative.X,
                    pivot.Y + rotatedRelative.Y,
                    pivot.Z + rotatedRelative.Z);

                if (!AdvanceWriteService.TrySetNodePoint(geometry, node.Id, next, out string writeError))
                {
                    failed.Add($"Node {node.Id}: set failed ({writeError})");
                    continue;
                }

                updatedCount++;
            }

            if (updatedCount == 0)
            {
                string detail = failed.Count > 0 ? failed[0] : "No node was updated.";
                message = "Rotate failed. " + detail;
                return false;
            }

            message =
                $"Rotated {updatedCount}/{nodes.Count} node(s) around pivot node {pivotNodeId}." +
                $"\nDirection beam: {directionBeamId} ({directionNodeA} -> {directionNodeB}) aligned to global {targetAxis} axis." +
                $"\nRotation angle: {AdvanceGeometryService.FormatCoordinate(rotationAngleRad * 180.0 / System.Math.PI)} deg.";

            if (invalidTargetCount > 0)
                message += $"\nIgnored {invalidTargetCount} invalid/deleted node ID(s).";

            if (failed.Count > 0)
                message += "\nSome nodes failed. First: " + failed[0];

            return true;
        }
    }
}
