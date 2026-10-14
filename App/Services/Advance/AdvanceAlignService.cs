using System;
using System.Collections.Generic;

namespace StaadSteelApp.Core.Advance
{
    internal static class AdvanceAlignService
    {
        internal static bool TryCreateAlignedNodeFromThreeNodes(
            int lineNodeA,
            int lineNodeB,
            int rootNode,
            AlignKeepAxis keepAxis,
            out int newNodeId,
            out string message)
        {
            newNodeId = 0;
            message = string.Empty;

            if (!AdvanceOpenStaadService.TryGetGeometry(out _, out object geometry, out message))
                return false;

            if (!AdvanceSelectionService.TryGetNodePoint(geometry, lineNodeA, out AdvancePoint3D a, out string readAError))
            {
                message = $"Could not read coordinates of line node {lineNodeA}: {readAError}";
                return false;
            }

            if (!AdvanceSelectionService.TryGetNodePoint(geometry, lineNodeB, out AdvancePoint3D b, out string readBError))
            {
                message = $"Could not read coordinates of line node {lineNodeB}: {readBError}";
                return false;
            }

            if (!AdvanceSelectionService.TryGetNodePoint(geometry, rootNode, out AdvancePoint3D r, out string readRootError))
            {
                message = $"Could not read coordinates of root node {rootNode}: {readRootError}";
                return false;
            }

            if (!AdvanceGeometryService.TryComputeAlignedPoint(a, b, r, keepAxis, out AdvancePoint3D aligned, out string computeError))
            {
                message = computeError;
                return false;
            }

            if (!AdvanceWriteService.TryAddNode(geometry, aligned, out newNodeId, out string addError))
            {
                message = addError;
                return false;
            }

            message =
                $"Created node {newNodeId} at " +
                $"({aligned.X.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}, " +
                $"{aligned.Y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}, " +
                $"{aligned.Z.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}).";

            return true;
        }

        internal static bool TryAlignNodesToTemplate(
            int[] targetNodeIds,
            int templateNodeId,
            bool alignX,
            bool alignY,
            bool alignZ,
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

            if (!alignX && !alignY && !alignZ)
            {
                message = "Please select at least one axis (X/Y/Z).";
                return false;
            }

            if (!AdvanceOpenStaadService.TryGetGeometry(out _, out object geometry, out message))
                return false;

            if (!AdvanceSelectionService.TryGetNodePoint(geometry, templateNodeId, out AdvancePoint3D templatePoint, out string templateError))
            {
                message = $"Could not read template node {templateNodeId}: {templateError}";
                return false;
            }

            return TryAlignNodesToPoint(
                geometry,
                targetNodeIds,
                templatePoint,
                alignX,
                alignY,
                alignZ,
                $"template node {templateNodeId}",
                out updatedCount,
                out message);
        }

        internal static bool TryAlignNodesToCoordinates(
            int[] targetNodeIds,
            bool alignX,
            double targetX,
            bool alignY,
            double targetY,
            bool alignZ,
            double targetZ,
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

            if (!alignX && !alignY && !alignZ)
            {
                message = "Please select at least one axis (X/Y/Z).";
                return false;
            }

            if (!AdvanceOpenStaadService.TryGetGeometry(out _, out object geometry, out message))
                return false;

            AdvancePoint3D targetPoint = new AdvancePoint3D(targetX, targetY, targetZ);
            return TryAlignNodesToPoint(
                geometry,
                targetNodeIds,
                targetPoint,
                alignX,
                alignY,
                alignZ,
                $"manual coordinates ({AdvanceGeometryService.FormatCoordinate(targetX)}, {AdvanceGeometryService.FormatCoordinate(targetY)}, {AdvanceGeometryService.FormatCoordinate(targetZ)})",
                out updatedCount,
                out message);
        }

        internal static bool TryCopyNodesToTemplate(
            int[] sourceNodeIds,
            int templateNodeId,
            bool alignX,
            bool alignY,
            bool alignZ,
            out int createdCount,
            out string message)
        {
            createdCount = 0;
            message = string.Empty;

            if (sourceNodeIds == null || sourceNodeIds.Length == 0)
            {
                message = "No source node provided for copying.";
                return false;
            }

            if (!alignX && !alignY && !alignZ)
            {
                message = "Please select at least one axis (X/Y/Z).";
                return false;
            }

            if (!AdvanceOpenStaadService.TryGetGeometry(out _, out object geometry, out message))
                return false;

            if (!AdvanceSelectionService.TryGetNodePoint(geometry, templateNodeId, out AdvancePoint3D templatePoint, out string templateError))
            {
                message = $"Could not read template node {templateNodeId}: {templateError}";
                return false;
            }

            return TryCopyNodesToPoint(
                geometry,
                sourceNodeIds,
                templatePoint,
                alignX,
                alignY,
                alignZ,
                $"template node {templateNodeId}",
                out createdCount,
                out message);
        }

        internal static bool TryCopyNodesToCoordinates(
            int[] sourceNodeIds,
            bool alignX,
            double targetX,
            bool alignY,
            double targetY,
            bool alignZ,
            double targetZ,
            out int createdCount,
            out string message)
        {
            createdCount = 0;
            message = string.Empty;

            if (sourceNodeIds == null || sourceNodeIds.Length == 0)
            {
                message = "No source node provided for copying.";
                return false;
            }

            if (!alignX && !alignY && !alignZ)
            {
                message = "Please select at least one axis (X/Y/Z).";
                return false;
            }

            if (!AdvanceOpenStaadService.TryGetGeometry(out _, out object geometry, out message))
                return false;

            AdvancePoint3D targetPoint = new AdvancePoint3D(targetX, targetY, targetZ);
            return TryCopyNodesToPoint(
                geometry,
                sourceNodeIds,
                targetPoint,
                alignX,
                alignY,
                alignZ,
                $"manual coordinates ({AdvanceGeometryService.FormatCoordinate(targetX)}, {AdvanceGeometryService.FormatCoordinate(targetY)}, {AdvanceGeometryService.FormatCoordinate(targetZ)})",
                out createdCount,
                out message);
        }

        private static bool TryAlignNodesToPoint(
            object geometry,
            int[] targetNodeIds,
            AdvancePoint3D targetPoint,
            bool alignX,
            bool alignY,
            bool alignZ,
            string targetDescription,
            out int updatedCount,
            out string message)
        {
            updatedCount = 0;
            message = string.Empty;

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

                AdvancePoint3D next = new AdvancePoint3D(
                    alignX ? targetPoint.X : current.X,
                    alignY ? targetPoint.Y : current.Y,
                    alignZ ? targetPoint.Z : current.Z);

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
                message = "Alignment failed. " + detail;
                return false;
            }

            message = $"Aligned {updatedCount}/{validTargets.Length} node(s) to {targetDescription}.";
            if (failed.Count > 0)
                message += "\nSome nodes failed. First: " + failed[0];

            return true;
        }

        private static bool TryCopyNodesToPoint(
            object geometry,
            int[] sourceNodeIds,
            AdvancePoint3D targetPoint,
            bool alignX,
            bool alignY,
            bool alignZ,
            string targetDescription,
            out int createdCount,
            out string message)
        {
            createdCount = 0;
            message = string.Empty;

            int[] validSources = AdvanceSelectionService.FilterValidNodeIds(geometry, sourceNodeIds, out int invalidCount);
            if (validSources.Length == 0)
            {
                message = invalidCount > 0
                    ? $"No valid source node found (ignored {invalidCount} invalid/deleted node IDs)."
                    : "No valid source node found.";
                return false;
            }

            var createdNodeIds = new List<int>();
            var failed = new List<string>();
            int skippedUnchanged = 0;

            foreach (int nodeId in validSources)
            {
                if (!AdvanceSelectionService.TryGetNodePoint(geometry, nodeId, out AdvancePoint3D current, out string readError))
                {
                    failed.Add($"Node {nodeId}: read failed ({readError})");
                    continue;
                }

                AdvancePoint3D next = new AdvancePoint3D(
                    alignX ? targetPoint.X : current.X,
                    alignY ? targetPoint.Y : current.Y,
                    alignZ ? targetPoint.Z : current.Z);

                if (PointsMatch(current, next))
                {
                    skippedUnchanged++;
                    continue;
                }

                if (!AdvanceWriteService.TryAddNode(geometry, next, out int newNodeId, out string addError))
                {
                    failed.Add($"Node {nodeId}: copy failed ({addError})");
                    continue;
                }

                createdNodeIds.Add(newNodeId);
            }

            createdCount = createdNodeIds.Count;
            if (createdCount == 0)
            {
                if (failed.Count > 0)
                {
                    message = "Copy failed. " + failed[0];
                }
                else
                {
                    message = "No new node was created because all selected nodes already match the requested coordinates.";
                }

                return false;
            }

            AdvanceWriteService.HighlightNodes(geometry, createdNodeIds);

            message = $"Copied {createdCount}/{validSources.Length} node(s) to {targetDescription}. Original nodes were not changed.";
            if (skippedUnchanged > 0)
                message += $"\nSkipped {skippedUnchanged} node(s) already at the requested coordinates.";
            if (failed.Count > 0)
                message += "\nSome nodes failed. First: " + failed[0];

            return true;
        }

        private static bool PointsMatch(AdvancePoint3D left, AdvancePoint3D right)
        {
            return Math.Abs(left.X - right.X) <= AdvanceConstants.CoordEpsilon &&
                   Math.Abs(left.Y - right.Y) <= AdvanceConstants.CoordEpsilon &&
                   Math.Abs(left.Z - right.Z) <= AdvanceConstants.CoordEpsilon;
        }
    }
}
