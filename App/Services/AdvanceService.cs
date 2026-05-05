using StaadSteelApp.Core.Advance;

namespace StaadSteelApp.Core
{
    public static class AdvanceService
    {
        public static bool TryGetSelectedNodes(out int[] nodeIds, out string error)
        {
            return AdvanceSelectionService.TryGetSelectedNodes(out nodeIds, out error);
        }

        public static bool TryGetThreeSelectedNodes(out int[] nodeIds, out string error)
        {
            return AdvanceSelectionService.TryGetThreeSelectedNodes(out nodeIds, out error);
        }

        public static bool TryGetSelectedFrameNodeIds(out int[] nodeIds, out string message)
        {
            return AdvanceSelectionService.TryGetSelectedFrameNodeIds(out nodeIds, out message);
        }

        public static bool TryGetSingleSelectedBeamId(out int beamId, out string message)
        {
            return AdvanceSelectionService.TryGetSingleSelectedBeamId(out beamId, out message);
        }

        public static bool TryRotateNodesToAxis(
            int[] targetNodeIds,
            int pivotNodeId,
            int directionBeamId,
            AlignKeepAxis targetAxis,
            out int updatedCount,
            out string message)
        {
            return AdvanceRotateService.TryRotateNodesToAxis(
                targetNodeIds,
                pivotNodeId,
                directionBeamId,
                targetAxis,
                out updatedCount,
                out message);
        }

        public static bool TryMoveNodesToCoordinates(
            int[] targetNodeIds,
            int rootNodeId,
            double targetX,
            double targetY,
            double targetZ,
            out int updatedCount,
            out string message)
        {
            return AdvanceMoveService.TryMoveNodesToCoordinates(
                targetNodeIds,
                rootNodeId,
                targetX,
                targetY,
                targetZ,
                out updatedCount,
                out message);
        }

        public static bool TryMoveNodesToNode(
            int[] targetNodeIds,
            int rootNodeId,
            int targetNodeId,
            out int updatedCount,
            out string message)
        {
            return AdvanceMoveService.TryMoveNodesToNode(
                targetNodeIds,
                rootNodeId,
                targetNodeId,
                out updatedCount,
                out message);
        }

        public static bool TryCreateNodeByBeamVector(
            int referenceNodeId,
            int directionBeamId,
            double distance,
            out int newNodeId,
            out string message)
        {
            return AdvanceVectorNodeService.TryCreateNodeByBeamVector(
                referenceNodeId,
                directionBeamId,
                distance,
                out newNodeId,
                out message);
        }

        public static bool TryAlignNodesToBeamPlanes(
            int[] targetNodeIds,
            int directionBeamId,
            bool alignToXY,
            bool alignToXZ,
            bool alignToYZ,
            out int updatedCount,
            out string message)
        {
            return AdvanceBeamPlaneService.TryAlignNodesToBeamPlanes(
                targetNodeIds,
                directionBeamId,
                alignToXY,
                alignToXZ,
                alignToYZ,
                out updatedCount,
                out message);
        }

        public static string GetCoordinateLengthUnitLabel()
        {
            return AdvanceSelectionService.GetCoordinateLengthUnitLabel();
        }

        public static bool TryCreateAlignedNodeFromThreeNodes(
            int lineNodeA,
            int lineNodeB,
            int rootNode,
            AlignKeepAxis keepAxis,
            out int newNodeId,
            out string message)
        {
            return AdvanceAlignService.TryCreateAlignedNodeFromThreeNodes(
                lineNodeA,
                lineNodeB,
                rootNode,
                keepAxis,
                out newNodeId,
                out message);
        }

        public static bool TryAlignNodesToTemplate(
            int[] targetNodeIds,
            int templateNodeId,
            bool alignX,
            bool alignY,
            bool alignZ,
            out int updatedCount,
            out string message)
        {
            return AdvanceAlignService.TryAlignNodesToTemplate(
                targetNodeIds,
                templateNodeId,
                alignX,
                alignY,
                alignZ,
                out updatedCount,
                out message);
        }

        public static bool TryAlignNodesToCoordinates(
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
            return AdvanceAlignService.TryAlignNodesToCoordinates(
                targetNodeIds,
                alignX,
                targetX,
                alignY,
                targetY,
                alignZ,
                targetZ,
                out updatedCount,
                out message);
        }

        public static bool TryAlignNodesToSegment(
            int[] targetNodeIds,
            int segmentNodeA,
            int segmentNodeB,
            out int updatedCount,
            out string message)
        {
            return AdvanceBeamSegmentService.TryAlignNodesToSegment(
                targetNodeIds,
                segmentNodeA,
                segmentNodeB,
                out updatedCount,
                out message);
        }

        public static bool TryConnectSelectedNodes(
            int[] selectedNodeIds,
            AlignKeepAxis sortAxis,
            out int createdCount,
            out string message)
        {
            return AdvanceConnectService.TryConnectSelectedNodes(
                selectedNodeIds,
                sortAxis,
                out createdCount,
                out message);
        }

        public static bool TryConnectSelectedNodesByGap(
            int[] selectedNodeIds,
            AlignKeepAxis gapAxis,
            double gapLimit,
            AlignKeepAxis sortAxis,
            out int createdCount,
            out string message)
        {
            return AdvanceConnectService.TryConnectSelectedNodesByGap(
                selectedNodeIds,
                gapAxis,
                gapLimit,
                sortAxis,
                out createdCount,
                out message);
        }
    }
}
