using System;
using System.Collections.Generic;
using System.Globalization;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core.Advance
{
    internal static class AdvanceSelectionService
    {
        internal static bool TryGetSelectedNodes(out int[] nodeIds, out string error)
        {
            nodeIds = Array.Empty<int>();
            error = string.Empty;

            dynamic os = StaadManager.GetOpenSTAAD();
            if (os == null)
            {
                error = "Could not connect to OpenSTAAD.";
                return false;
            }

            object openStaad = os;
            int ignoredInvalidCount;
            if (!OpenStaadSelectionAdapter.TryGetSelectedNodeIds(
                    openStaad,
                    true,
                    true,
                    out nodeIds,
                    out ignoredInvalidCount,
                    out error))
                return false;

            if (nodeIds.Length == 0)
                error = string.Empty;

            return true;
        }

        internal static bool TryGetThreeSelectedNodes(out int[] nodeIds, out string error)
        {
            nodeIds = Array.Empty<int>();
            error = string.Empty;

            dynamic os = StaadManager.GetOpenSTAAD();
            if (os == null)
            {
                error = "Could not connect to OpenSTAAD.";
                return false;
            }

            object openStaad = os;
            if (!OpenStaadSelectionAdapter.TryGetSelectedNodeIds(
                    openStaad,
                    true,
                    true,
                    out nodeIds,
                    out int invalidCount,
                    out error))
                return false;

            if (nodeIds.Length != 3)
            {
                error = invalidCount > 0
                    ? $"Please select exactly 3 valid nodes in STAAD. Current valid selection: {nodeIds.Length} (ignored {invalidCount} invalid/deleted node IDs)."
                    : $"Please select exactly 3 nodes in STAAD. Current selection: {nodeIds.Length}.";
                return false;
            }

            return true;
        }

        internal static bool TryGetSelectedFrameNodeIds(out int[] nodeIds, out string message)
        {
            nodeIds = Array.Empty<int>();
            message = string.Empty;

            dynamic os = StaadManager.GetOpenSTAAD();
            if (os == null)
            {
                message = "Could not connect to OpenSTAAD.";
                return false;
            }

            object openStaad = os;
            object geometry = os.Geometry;
            if (!OpenStaadSelectionAdapter.TryGetSelectedBeamIds(
                    openStaad,
                    true,
                    true,
                    out int[] selectedBeamIds,
                    out string beamError))
            {
                message = "Could not read selected beams: " + beamError;
                return false;
            }

            if (selectedBeamIds.Length > 0)
            {
                if (!TryCollectUniqueNodesFromBeams(geometry, selectedBeamIds, out nodeIds, out int beamReadFailures))
                {
                    message = "Could not read connected nodes from the selected beams.";
                    return false;
                }

                if (nodeIds.Length == 0)
                {
                    message = "Selected beams do not contain any readable node incidence.";
                    return false;
                }

                message = beamReadFailures > 0
                    ? $"Captured {nodeIds.Length} node(s) from {selectedBeamIds.Length} selected beam(s). Ignored {beamReadFailures} unreadable beam(s)."
                    : $"Captured {nodeIds.Length} node(s) from {selectedBeamIds.Length} selected beam(s).";
                return true;
            }

            if (!OpenStaadSelectionAdapter.TryGetSelectedNodeIds(
                    openStaad,
                    true,
                    true,
                    out nodeIds,
                    out int invalidNodeCount,
                    out string nodeError))
            {
                message = "Could not read selected nodes: " + nodeError;
                return false;
            }

            if (nodeIds.Length == 0)
            {
                message = invalidNodeCount > 0
                    ? $"No valid node found in selection (ignored {invalidNodeCount} invalid/deleted node IDs)."
                    : "Please select frame beams or nodes in STAAD first.";
                return false;
            }

            message = $"Captured {nodeIds.Length} selected node(s).";
            return true;
        }

        internal static bool TryGetSingleSelectedBeamId(out int beamId, out string message)
        {
            beamId = 0;
            message = string.Empty;

            dynamic os = StaadManager.GetOpenSTAAD();
            if (os == null)
            {
                message = "Could not connect to OpenSTAAD.";
                return false;
            }

            object openStaad = os;
            if (!OpenStaadSelectionAdapter.TryGetSelectedBeamIds(
                    openStaad,
                    true,
                    true,
                    out int[] beamIds,
                    out string error))
            {
                message = "Could not read selected beams: " + error;
                return false;
            }

            if (beamIds.Length != 1)
            {
                message = $"Please select exactly 1 beam. Current valid selection: {beamIds.Length}.";
                if (!string.IsNullOrWhiteSpace(error))
                    message += "\n" + error;
                return false;
            }

            beamId = beamIds[0];
            return true;
        }

        internal static string GetCoordinateLengthUnitLabel()
        {
            dynamic os = StaadManager.GetOpenSTAAD();
            if (os == null)
                return "?";

            try
            {
                int baseUnit = Convert.ToInt32(os.GetBaseUnit(), CultureInfo.InvariantCulture);
                if (baseUnit == AdvanceConstants.BaseUnitMetric)
                    return "m";

                if (baseUnit == AdvanceConstants.BaseUnitEnglish)
                    return "in";
            }
            catch
            {
            }

            return "?";
        }

        internal static int[] FilterValidNodeIds(object geometry, int[] rawNodeIds, out int invalidCount)
        {
            invalidCount = 0;
            if (rawNodeIds == null || rawNodeIds.Length == 0)
                return Array.Empty<int>();

            HashSet<int> existingNodeIds = TryGetExistingNodeIds(geometry);
            var seen = new HashSet<int>();
            var valid = new List<int>(rawNodeIds.Length);

            foreach (int nodeId in rawNodeIds)
            {
                if (nodeId <= 0)
                {
                    invalidCount++;
                    continue;
                }

                if (!seen.Add(nodeId))
                    continue;

                if (existingNodeIds != null && !existingNodeIds.Contains(nodeId))
                {
                    invalidCount++;
                    continue;
                }

                if (TryGetNodePoint(geometry, nodeId, out _, out _))
                {
                    valid.Add(nodeId);
                }
                else
                {
                    invalidCount++;
                }
            }

            return valid.ToArray();
        }

        internal static HashSet<int> TryGetExistingNodeIds(object geometry)
        {
            return OpenStaadGeometryAdapter.TryGetNodeList(geometry, out int[] nodeIds, out _)
                ? new HashSet<int>(nodeIds)
                : null;
        }

        internal static bool TryLoadSelectedNodeData(
            object geometry,
            int[] selectedNodeIds,
            out int[] validNodeIds,
            out List<AdvanceNodeData> nodes,
            out string message)
        {
            validNodeIds = FilterValidNodeIds(geometry, selectedNodeIds, out int invalidCount);
            nodes = new List<AdvanceNodeData>(validNodeIds.Length);
            message = string.Empty;

            if (validNodeIds.Length < 2)
            {
                message = invalidCount > 0
                    ? $"Need at least 2 valid nodes (ignored {invalidCount} invalid/deleted node IDs)."
                    : "Need at least 2 valid nodes.";
                return false;
            }

            foreach (int nodeId in validNodeIds)
            {
                if (!TryGetNodePoint(geometry, nodeId, out AdvancePoint3D point, out string readError))
                {
                    message = $"Could not read node {nodeId}: {readError}";
                    return false;
                }

                nodes.Add(new AdvanceNodeData { Id = nodeId, Point = point });
            }

            return true;
        }

        internal static bool TryCollectUniqueNodesFromBeams(
            object geometry,
            IReadOnlyList<int> beamIds,
            out int[] nodeIds,
            out int failedBeamCount)
        {
            nodeIds = Array.Empty<int>();
            failedBeamCount = 0;

            if (beamIds == null || beamIds.Count == 0)
                return true;

            var seen = new HashSet<int>();
            var collected = new List<int>(beamIds.Count * 2);
            foreach (int beamId in beamIds)
            {
                if (!TryGetBeamIncidence(geometry, beamId, out int nodeA, out int nodeB))
                {
                    failedBeamCount++;
                    continue;
                }

                if (nodeA > 0 && seen.Add(nodeA))
                    collected.Add(nodeA);

                if (nodeB > 0 && seen.Add(nodeB))
                    collected.Add(nodeB);
            }

            nodeIds = collected.ToArray();
            return true;
        }

        internal static bool TryGetNodePoint(object geometry, int nodeNo, out AdvancePoint3D point, out string error)
        {
            point = default;
            error = string.Empty;

            if (!OpenStaadGeometryAdapter.TryGetNodeCoordinates(geometry, nodeNo, out double x, out double y, out double z, out error))
                return false;

            point = new AdvancePoint3D(x, y, z);
            return true;
        }

        internal static bool TryGetBeamIncidence(object geometry, int beamId, out int nodeA, out int nodeB)
        {
            return OpenStaadGeometryAdapter.TryGetMemberIncidence(geometry, beamId, out nodeA, out nodeB, out _);
        }

    }
}
