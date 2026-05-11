namespace StaadSteelApp.Core.Advance
{
    internal static class AdvanceVectorNodeService
    {
        internal static bool TryCreateNodeByBeamVector(
            int referenceNodeId,
            int directionBeamId,
            double distance,
            out int newNodeId,
            out string message)
        {
            newNodeId = 0;
            message = string.Empty;

            if (referenceNodeId <= 0)
            {
                message = "Reference node must be a valid positive integer.";
                return false;
            }

            if (directionBeamId <= 0)
            {
                message = "Direction beam must be a valid positive integer.";
                return false;
            }

            if (!AdvanceOpenStaadService.TryGetGeometry(out _, out object geometry, out message))
                return false;

            if (!AdvanceSelectionService.TryGetNodePoint(geometry, referenceNodeId, out AdvancePoint3D referencePoint, out string referenceError))
            {
                message = $"Could not read reference node {referenceNodeId}: {referenceError}";
                return false;
            }

            if (!AdvanceSelectionService.TryGetBeamIncidence(geometry, directionBeamId, out int nodeA, out int nodeB))
            {
                message = $"Could not read incidence of direction beam {directionBeamId}.";
                return false;
            }

            if (!AdvanceSelectionService.TryGetNodePoint(geometry, nodeA, out AdvancePoint3D beamStart, out string beamStartError))
            {
                message = $"Could not read start node {nodeA} of beam {directionBeamId}: {beamStartError}";
                return false;
            }

            if (!AdvanceSelectionService.TryGetNodePoint(geometry, nodeB, out AdvancePoint3D beamEnd, out string beamEndError))
            {
                message = $"Could not read end node {nodeB} of beam {directionBeamId}: {beamEndError}";
                return false;
            }

            AdvancePoint3D beamVector = AdvanceGeometryService.Subtract(beamEnd, beamStart);
            if (!AdvanceGeometryService.TryNormalize(beamVector, out AdvancePoint3D directionUnit))
            {
                message = $"Beam {directionBeamId} has zero or near-zero length. Cannot define a vector.";
                return false;
            }

            AdvancePoint3D offset = AdvanceGeometryService.Scale(directionUnit, distance);
            AdvancePoint3D newPoint = AdvanceGeometryService.Add(referencePoint, offset);

            if (!AdvanceWriteService.TryAddNode(geometry, newPoint, out newNodeId, out string addError))
            {
                message = addError;
                return false;
            }

            message =
                $"Created node {newNodeId} from reference node {referenceNodeId} using beam {directionBeamId} direction." +
                $"\nDistance: {AdvanceGeometryService.FormatCoordinate(distance)}." +
                $"\nNew coordinates: ({AdvanceGeometryService.FormatCoordinate(newPoint.X)}, {AdvanceGeometryService.FormatCoordinate(newPoint.Y)}, {AdvanceGeometryService.FormatCoordinate(newPoint.Z)}).";

            return true;
        }
    }
}
