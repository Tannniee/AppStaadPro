using System;
using System.Diagnostics;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        public static void SelectSameSection_DetectAll()
        {
            var os = StaadManager.GetOpenSTAAD();
            if (os == null) return;

            int[] arrBeams = GetSelectedBeamIDs(os.Geometry);
            if (arrBeams.Length == 0)
            {
                StaadSteelApp.TopMostMessageBox.ShowInfo("Please select a source member (beam/physical member).", "Select Beam");
                return;
            }

            if (!TryResolveSourceBeamProperty(os, arrBeams, out int sourceID, out int propID))
            {
                StaadSteelApp.TopMostMessageBox.ShowError("Selected beam has no property.", "Property Missing");
                return;
            }

            object propObj = os.Property;
            if (!OpenStaadPropertyAdapter.TryGetSectionPropertyAssignedBeamCount(propObj, propID, out int count, out string countError))
            {
                StaadSteelApp.TopMostMessageBox.ShowError(
                    $"Could not read assigned beam count for property {propID}: {countError}",
                    "Detect All Error");
                return;
            }

            if (count <= 0)
            {
                StaadSteelApp.TopMostMessageBox.ShowInfo(
                    $"Property {propID} has no assigned analytical members.",
                    "Detect All");
                return;
            }

            int[] targetBeams = Array.Empty<int>();
            if (!OpenStaadPropertyAdapter.TryGetSectionPropertyAssignedBeamList(propObj, propID, count, out targetBeams, out string listError))
            {
                Debug.WriteLine("GetSectionPropertyAssignedBeamList failed: " + listError);
            }

            if (targetBeams.Length == 0)
            {
                StaadSteelApp.TopMostMessageBox.ShowWarning(
                    $"Could not get assigned beam list for property {propID}.",
                    "Detect All");
                return;
            }

            object geometryObj = os.Geometry;
            bool selected = OpenStaadGeometryAdapter.TryHighlightBeams(geometryObj, targetBeams, out string highlightError);
            if (!selected && !string.IsNullOrWhiteSpace(highlightError))
                Debug.WriteLine("Highlight assigned beams failed: " + highlightError);

            if (!selected)
            {
                StaadSteelApp.TopMostMessageBox.ShowWarning(
                    $"Property found (ID: {propID}) but could not highlight assigned members.",
                    "Detect All");
            }
        }

        private static bool TryResolveSourceBeamProperty(dynamic os, int[] beamIds, out int sourceID, out int propID)
        {
            sourceID = 0;
            propID = 0;
            if (os == null || beamIds == null || beamIds.Length == 0) return false;

            string sourceName = "";

            foreach (int beam in beamIds)
            {
                if (beam <= 0) continue;

                if (sourceID == 0) sourceID = beam;

                if (TryReadBeamPropertyRefNo(os.Property, beam, out int beamProp) && beamProp > 0)
                {
                    sourceID = beam;
                    propID = beamProp;
                    return true;
                }

                if (string.IsNullOrWhiteSpace(sourceName))
                {
                    object openStaad = os;
                    if (!OpenStaadPropertyAdapter.TryGetBeamSectionName(openStaad, beam, out sourceName, out string sectionError))
                        Debug.WriteLine($"GetBeamSectionName failed for beam {beam}: {sectionError}");
                }
            }

            if (!string.IsNullOrWhiteSpace(sourceName))
            {
                try
                {
                    propID = CheckPropertyExists(os, sourceName);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"CheckPropertyExists('{sourceName}') failed: {ex.Message}");
                }
            }

            return sourceID > 0 && propID > 0;
        }
    }
}
