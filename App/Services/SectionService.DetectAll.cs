using System;
using System.Diagnostics;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        public static ServiceOperationResult SelectSameSection_DetectAll(object openStaad = null)
        {
            if (openStaad == null && !TryGetSectionOpenStaad(out openStaad, out string connectionError))
            {
                Debug.WriteLine(connectionError);
                return ServiceOperationResult.Error("Connection Error", connectionError);
            }

            if (!TryGetSectionGeometry(openStaad, out object geometryObj, out string geometryError))
            {
                Debug.WriteLine(geometryError);
                return ServiceOperationResult.Error("Geometry Missing", geometryError);
            }

            int[] arrBeams = GetSelectedBeamIDs(geometryObj);
            if (arrBeams.Length == 0)
            {
                return ServiceOperationResult.Info(
                    "Select Beam",
                    "Please select a source member (beam/physical member).");
            }

            if (!TryGetSectionProperty(openStaad, out object propObj, out string propertyError))
            {
                return ServiceOperationResult.Error("Property Missing", propertyError);
            }

            if (!TryResolveSourceBeamProperty(openStaad, propObj, arrBeams, out int sourceID, out int propID))
            {
                return ServiceOperationResult.Error("Property Missing", "Selected beam has no property.");
            }

            if (!OpenStaadPropertyAdapter.TryGetSectionPropertyAssignedBeamCount(propObj, propID, out int count, out string countError))
            {
                return ServiceOperationResult.Error(
                    "Detect All Error",
                    $"Could not read assigned beam count for property {propID}: {countError}");
            }

            if (count <= 0)
            {
                return ServiceOperationResult.Info(
                    "Detect All",
                    $"Property {propID} has no assigned analytical members.");
            }

            int[] targetBeams = Array.Empty<int>();
            if (!OpenStaadPropertyAdapter.TryGetSectionPropertyAssignedBeamList(propObj, propID, count, out targetBeams, out string listError))
            {
                Debug.WriteLine("GetSectionPropertyAssignedBeamList failed: " + listError);
            }

            if (targetBeams.Length == 0)
            {
                return ServiceOperationResult.Warning(
                    "Detect All",
                    $"Could not get assigned beam list for property {propID}.");
            }

            bool selected = OpenStaadGeometryAdapter.TryHighlightBeams(geometryObj, targetBeams, out string highlightError);
            if (!selected && !string.IsNullOrWhiteSpace(highlightError))
                Debug.WriteLine("Highlight assigned beams failed: " + highlightError);

            if (!selected)
            {
                return ServiceOperationResult.Warning(
                    "Detect All",
                    $"Property found (ID: {propID}) but could not highlight assigned members.");
            }

            return ServiceOperationResult.Success();
        }

        private static bool TryResolveSourceBeamProperty(object openStaad, object propObj, int[] beamIds, out int sourceID, out int propID)
        {
            sourceID = 0;
            propID = 0;
            if (openStaad == null || propObj == null || beamIds == null || beamIds.Length == 0) return false;

            string sourceName = "";

            foreach (int beam in beamIds)
            {
                if (beam <= 0) continue;

                if (sourceID == 0) sourceID = beam;

                if (TryReadBeamPropertyRefNo(propObj, beam, out int beamProp) && beamProp > 0)
                {
                    sourceID = beam;
                    propID = beamProp;
                    return true;
                }

                if (string.IsNullOrWhiteSpace(sourceName))
                {
                    if (!OpenStaadPropertyAdapter.TryGetBeamSectionName(openStaad, beam, out sourceName, out string sectionError))
                        Debug.WriteLine($"GetBeamSectionName failed for beam {beam}: {sectionError}");
                }
            }

            if (!string.IsNullOrWhiteSpace(sourceName))
            {
                try
                {
                    propID = CheckPropertyExists(openStaad, sourceName);
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
