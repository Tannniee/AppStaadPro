using System;
using System.Collections.Generic;
using System.Globalization;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core.Advance
{
    internal static class AdvanceWriteService
    {
        internal static bool TryAddNode(object geometry, AdvancePoint3D point, out int nodeNo, out string error)
        {
            nodeNo = 0;
            error = string.Empty;

            return OpenStaadGeometryAdapter.TryAddNode(geometry, point.X, point.Y, point.Z, out nodeNo, out error);
        }

        internal static bool TrySetNodePoint(object geometry, int nodeNo, AdvancePoint3D point, out string error)
        {
            error = string.Empty;

            return OpenStaadGeometryAdapter.TrySetNodeCoordinate(geometry, nodeNo, point.X, point.Y, point.Z, out error);
        }

        internal static bool TryCreateBeamEdges(
            object geometry,
            ISet<AdvanceEdgeKey> existingEdges,
            IReadOnlyList<AdvanceEdgeKey> candidateEdges,
            string successMessageFormat,
            out int createdCount,
            out string message)
        {
            var createdBeamIds = new List<int>();
            int skippedExisting = 0;

            foreach (AdvanceEdgeKey edge in candidateEdges)
            {
                if (existingEdges.Contains(edge))
                {
                    skippedExisting++;
                    continue;
                }

                if (OpenStaadGeometryAdapter.TryAddBeam(geometry, edge.NodeA, edge.NodeB, out int beamId, out _) &&
                    beamId > 0)
                {
                    createdBeamIds.Add(beamId);
                    existingEdges.Add(edge);
                }
            }

            createdCount = createdBeamIds.Count;
            if (createdCount == 0)
            {
                message = skippedExisting > 0
                    ? "No new beams were created. Matching beams already exist."
                    : "OpenSTAAD did not create any new beam.";
                return false;
            }

            HighlightBeams(geometry, createdBeamIds);

            message = string.Format(CultureInfo.InvariantCulture, successMessageFormat, createdCount);
            if (skippedExisting > 0)
                message += $" Skipped {skippedExisting} existing connection(s).";

            return true;
        }

        internal static HashSet<AdvanceEdgeKey> GetExistingEdges(object geometry, IReadOnlyList<int> selectedNodeIds)
        {
            var selectedNodeSet = new HashSet<int>(selectedNodeIds);
            var beamIds = new HashSet<int>();
            var edges = new HashSet<AdvanceEdgeKey>();

            foreach (int nodeId in selectedNodeIds)
            {
                if (!OpenStaadGeometryAdapter.TryGetBeamsConnectedAtNode(geometry, nodeId, out int[] connectedBeamIds, out _))
                    continue;

                foreach (int beamId in connectedBeamIds)
                {
                    if (beamId > 0)
                        beamIds.Add(beamId);
                }
            }

            foreach (int beamId in beamIds)
            {
                if (OpenStaadGeometryAdapter.TryGetMemberIncidence(geometry, beamId, out int nodeA, out int nodeB, out _) &&
                    selectedNodeSet.Contains(nodeA) &&
                    selectedNodeSet.Contains(nodeB))
                {
                    edges.Add(new AdvanceEdgeKey(nodeA, nodeB));
                }
            }

            return edges;
        }

        internal static void HighlightBeams(object geometry, IReadOnlyList<int> beamIds)
        {
            if (beamIds == null || beamIds.Count == 0)
                return;

            OpenStaadGeometryAdapter.TryHighlightBeams(geometry, beamIds, out _);
        }

        internal static void HighlightNodes(object geometry, IReadOnlyList<int> nodeIds)
        {
            if (nodeIds == null || nodeIds.Count == 0)
                return;

            OpenStaadGeometryAdapter.TryHighlightNodes(geometry, nodeIds, out _);
        }
    }
}
