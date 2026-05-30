using System;
using System.Collections.Generic;
using System.Linq;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static class ViewFilterService
    {
        public static ServiceOperationResult HideSelectedBeams(object openStaad)
        {
            int[] selectedBeams = SectionService.GetSelectedBeamIDsSafe(openStaad, out string selectionError);
            if (selectedBeams.Length == 0)
                return ServiceOperationResult.Info("View Filter", BuildNoSelectionMessage(selectionError));

            if (!OpenStaadViewAdapter.TryGetVisibleMembers(openStaad, out int[] visibleBeams, out string visibleError))
                return ServiceOperationResult.Error("View Filter", "Could not read current visible beams: " + visibleError);

            if (visibleBeams.Length == 0)
                return ServiceOperationResult.Info("View Filter", "No visible beams found in the active STAAD view.");

            var selectedSet = new HashSet<int>(selectedBeams);
            int[] remainingVisible = visibleBeams.Where(id => !selectedSet.Contains(id)).ToArray();
            int hiddenCount = visibleBeams.Length - remainingVisible.Length;

            if (hiddenCount == 0)
                return ServiceOperationResult.Info("View Filter", "Selected beams are not visible in the active STAAD view.");

            int[] repairBeams = GetVisibleConnectedBeams(openStaad, selectedBeams, remainingVisible);

            if (!OpenStaadViewAdapter.TryHideMembers(openStaad, selectedBeams, out string hideError))
                return ServiceOperationResult.Error("View Filter", "Could not hide selected beams: " + hideError);

            if (repairBeams.Length > 0 &&
                !OpenStaadViewAdapter.TryShowMembers(openStaad, repairBeams, out string repairError) &&
                !OpenStaadViewAdapter.TrySetVisibleMembers(openStaad, remainingVisible, out repairError))
            {
                return ServiceOperationResult.Error("View Filter", "Could not restore connected visible beams: " + repairError);
            }

            return ServiceOperationResult.Success("View Filter", $"Hidden {hiddenCount} selected beam(s).");
        }

        public static ServiceOperationResult ViewOnlySelectedBeams(object openStaad)
        {
            int[] selectedBeams = SectionService.GetSelectedBeamIDsSafe(openStaad, out string selectionError);
            if (selectedBeams.Length == 0)
                return ServiceOperationResult.Info("View Filter", BuildNoSelectionMessage(selectionError));

            if (!OpenStaadViewAdapter.TryGetVisibleMembers(openStaad, out int[] visibleBeams, out string visibleError))
                return ServiceOperationResult.Error("View Filter", "Could not read current visible beams: " + visibleError);

            if (visibleBeams.Length == 0)
                return ServiceOperationResult.Info("View Filter", "No visible beams found in the active STAAD view.");

            var selectedSet = new HashSet<int>(selectedBeams);
            int[] selectedVisible = visibleBeams.Where(id => selectedSet.Contains(id)).ToArray();

            if (selectedVisible.Length == 0)
                return ServiceOperationResult.Info("View Filter", "Selected beams are not visible in the active STAAD view.");

            if (!OpenStaadViewAdapter.TrySetVisibleMembers(openStaad, selectedVisible, out string viewError))
                return ServiceOperationResult.Error("View Filter", "Could not isolate selected beams: " + viewError);

            return ServiceOperationResult.Success(
                "View Filter",
                $"Viewing {selectedVisible.Length} selected beam(s); hidden {visibleBeams.Length - selectedVisible.Length} other visible beam(s).");
        }

        private static string BuildNoSelectionMessage(string selectionError)
        {
            if (!string.IsNullOrWhiteSpace(selectionError))
                return "No selected beams found in STAAD. " + selectionError;

            return "No beams selected in STAAD.";
        }

        private static int[] GetVisibleConnectedBeams(object openStaad, int[] selectedBeams, int[] remainingVisible)
        {
            if (selectedBeams == null || selectedBeams.Length == 0 || remainingVisible == null || remainingVisible.Length == 0)
                return Array.Empty<int>();

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Geometry", out object geometry, out _))
                return Array.Empty<int>();

            var remainingSet = new HashSet<int>(remainingVisible);
            var selectedSet = new HashSet<int>(selectedBeams);
            var repairSet = new HashSet<int>();

            foreach (int beamId in selectedBeams)
            {
                if (!OpenStaadGeometryAdapter.TryGetMemberIncidence(geometry, beamId, out int nodeA, out int nodeB, out _))
                    continue;

                AddVisibleConnectedBeams(geometry, nodeA, remainingSet, selectedSet, repairSet);
                AddVisibleConnectedBeams(geometry, nodeB, remainingSet, selectedSet, repairSet);
            }

            return repairSet.ToArray();
        }

        private static void AddVisibleConnectedBeams(
            object geometry,
            int nodeId,
            HashSet<int> remainingSet,
            HashSet<int> selectedSet,
            HashSet<int> repairSet)
        {
            if (nodeId <= 0)
                return;

            if (!OpenStaadGeometryAdapter.TryGetBeamsConnectedAtNode(geometry, nodeId, out int[] connected, out _))
                return;

            foreach (int connectedBeam in connected)
            {
                if (remainingSet.Contains(connectedBeam) && !selectedSet.Contains(connectedBeam))
                    repairSet.Add(connectedBeam);
            }
        }
    }
}
