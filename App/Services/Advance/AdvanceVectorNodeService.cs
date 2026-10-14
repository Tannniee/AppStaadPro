using System.Collections.Generic;

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

            AdvanceWriteService.HighlightNodes(geometry, new[] { newNodeId });

            message =
                $"Created node {newNodeId} from reference node {referenceNodeId} using beam {directionBeamId} direction." +
                $"\nDistance: {AdvanceGeometryService.FormatCoordinate(distance)}." +
                $"\nNew coordinates: ({AdvanceGeometryService.FormatCoordinate(newPoint.X)}, {AdvanceGeometryService.FormatCoordinate(newPoint.Y)}, {AdvanceGeometryService.FormatCoordinate(newPoint.Z)}).";

            return true;
        }

        internal static bool TryCreateNodesByBeamVector(
            int[] referenceNodeIds,
            int directionBeamId,
            double distance,
            out int createdCount,
            out string message)
        {
            createdCount = 0;
            message = string.Empty;

            if (referenceNodeIds == null || referenceNodeIds.Length == 0)
            {
                message = "No captured reference node found.";
                return false;
            }

            if (directionBeamId <= 0)
            {
                message = "Direction beam must be a valid positive integer.";
                return false;
            }

            if (!AdvanceOpenStaadService.TryGetGeometry(out _, out object geometry, out message))
                return false;

            int[] validReferences = AdvanceSelectionService.FilterValidNodeIds(geometry, referenceNodeIds, out int invalidCount);
            if (validReferences.Length == 0)
            {
                message = invalidCount > 0
                    ? $"No valid captured reference node found (ignored {invalidCount} invalid/deleted node IDs)."
                    : "No valid captured reference node found.";
                return false;
            }

            if (!TryGetBeamDirectionUnit(geometry, directionBeamId, out AdvancePoint3D directionUnit, out int nodeA, out int nodeB, out message))
                return false;

            AdvancePoint3D offset = AdvanceGeometryService.Scale(directionUnit, distance);
            var createdNodeIds = new List<int>(validReferences.Length);
            var mappings = new List<string>(validReferences.Length);
            var failed = new List<string>();

            foreach (int referenceNodeId in validReferences)
            {
                if (!AdvanceSelectionService.TryGetNodePoint(geometry, referenceNodeId, out AdvancePoint3D referencePoint, out string readError))
                {
                    failed.Add($"Node {referenceNodeId}: read failed ({readError})");
                    continue;
                }

                AdvancePoint3D newPoint = AdvanceGeometryService.Add(referencePoint, offset);
                if (!AdvanceWriteService.TryAddNode(geometry, newPoint, out int newNodeId, out string addError))
                {
                    failed.Add($"Node {referenceNodeId}: create failed ({addError})");
                    continue;
                }

                createdNodeIds.Add(newNodeId);
                mappings.Add($"{referenceNodeId}->{newNodeId}");
                createdCount++;
            }

            if (createdCount == 0)
            {
                string detail = failed.Count > 0 ? failed[0] : "No node was created.";
                message = "Create by beam vector failed. " + detail;
                return false;
            }

            AdvanceWriteService.HighlightNodes(geometry, createdNodeIds);

            message =
                $"Created {createdCount}/{validReferences.Length} node(s) using beam {directionBeamId} direction ({nodeA} -> {nodeB})." +
                $"\nDistance: {AdvanceGeometryService.FormatCoordinate(distance)}." +
                $"\nDelta: ({AdvanceGeometryService.FormatCoordinate(offset.X)}, {AdvanceGeometryService.FormatCoordinate(offset.Y)}, {AdvanceGeometryService.FormatCoordinate(offset.Z)})." +
                $"\nMapping: {FormatMappingPreview(mappings, 8)}.";

            if (invalidCount > 0)
                message += $"\nIgnored {invalidCount} invalid/deleted captured node ID(s).";

            if (failed.Count > 0)
                message += "\nSome nodes failed. First: " + failed[0];

            return true;
        }

        private static bool TryGetBeamDirectionUnit(
            object geometry,
            int directionBeamId,
            out AdvancePoint3D directionUnit,
            out int nodeA,
            out int nodeB,
            out string message)
        {
            directionUnit = default;
            nodeA = 0;
            nodeB = 0;
            message = string.Empty;

            if (!AdvanceSelectionService.TryGetBeamIncidence(geometry, directionBeamId, out nodeA, out nodeB))
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
            if (!AdvanceGeometryService.TryNormalize(beamVector, out directionUnit))
            {
                message = $"Beam {directionBeamId} has zero or near-zero length. Cannot define a vector.";
                return false;
            }

            return true;
        }

        private static string FormatMappingPreview(IReadOnlyList<string> mappings, int maxShown)
        {
            if (mappings == null || mappings.Count == 0)
                return "None";

            int count = mappings.Count < maxShown ? mappings.Count : maxShown;
            var shown = new string[count];
            for (int i = 0; i < count; i++)
                shown[i] = mappings[i];

            string text = string.Join(", ", shown);
            if (mappings.Count > count)
                text += ", ...";

            return text;
        }
    }
}
