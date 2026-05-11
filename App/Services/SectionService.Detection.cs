using System;
using System.Collections.Generic;
using System.Diagnostics;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        public static SectionInfo DetectSelectedSection(object os)
        {
             if (os == null) throw new StaadConnectionException("Not connected to STAAD Application.");

             object openStaad = os;
             int[] arrBeams;
             try
             {
                 if (!TryGetSectionGeometry(openStaad, out object geometryObj, out string geometryError))
                     throw new SectionDetectionException("Error verifying selection: " + geometryError);

                 arrBeams = GetSelectedBeamIDs(geometryObj);
             }
             catch (Exception ex)
             {
                 throw new SectionDetectionException("Error verifying selection: " + ex.Message, ex);
             }

             if (arrBeams.Length == 0)
                 throw new SectionDetectionException("No member selected. Please select an analytical beam or physical member in STAAD.");

             // Fast path: work with the first selected analytical beam first.
             int beamID = 0;
             foreach (int b in arrBeams)
             {
                 if (b > 0)
                 {
                     beamID = b;
                     break;
                 }
             }

             int propID = 0;
             string matName = "";
             string propName = "";

             if (beamID > 0)
             {
                 if (!OpenStaadPropertyAdapter.TryGetBeamMaterialName(openStaad, beamID, out matName, out string materialError))
                     Debug.WriteLine($"GetBeamMaterialName failed for beam {beamID}: {materialError}");

                 if (!OpenStaadPropertyAdapter.TryGetBeamSectionName(openStaad, beamID, out propName, out string sectionError))
                     Debug.WriteLine($"GetBeamSectionName failed for beam {beamID}: {sectionError}");

                 if (!OpenStaadPropertyAdapter.TryGetBeamSectionPropertyRefNo(openStaad, beamID, out propID, out string propRefError))
                     Debug.WriteLine($"GetBeamSectionPropertyRefNo failed for beam {beamID}: {propRefError}");

                 if (propID <= 0 && !string.IsNullOrWhiteSpace(propName))
                 {
                     try
                     {
                         propID = CheckPropertyExists(os, propName);
                     }
                     catch (Exception ex)
                     {
                         Debug.WriteLine($"CheckPropertyExists('{propName}') failed: {ex.Message}");
                     }
                 }
             }

             // Fallback path: if the first analytical beam has no property,
             // probe representative beams first (last/middle) before scanning all.
             if (propID <= 0 && arrBeams.Length > 1)
             {
                 var candidateOrder = new List<int>(arrBeams.Length);
                 var seenCandidates = new HashSet<int>();

                 void AddCandidateByIndex(int index)
                 {
                     if (index < 0 || index >= arrBeams.Length)
                         return;

                     int candidateBeam = arrBeams[index];
                     if (candidateBeam <= 0 || !seenCandidates.Add(candidateBeam))
                         return;

                     candidateOrder.Add(candidateBeam);
                 }

                 AddCandidateByIndex(arrBeams.Length - 1);
                 AddCandidateByIndex(arrBeams.Length / 2);
                 AddCandidateByIndex(arrBeams.Length / 4);
                 AddCandidateByIndex((arrBeams.Length * 3) / 4);

                 for (int i = 1; i < arrBeams.Length; i++)
                     AddCandidateByIndex(i);

                 foreach (int candidate in candidateOrder)
                 {
                     int candidatePropId = 0;
                     if (!OpenStaadPropertyAdapter.TryGetBeamSectionPropertyRefNo(openStaad, candidate, out candidatePropId, out string candidatePropError))
                         Debug.WriteLine($"GetBeamSectionPropertyRefNo failed for beam {candidate}: {candidatePropError}");

                     if (candidatePropId <= 0) continue;

                     beamID = candidate;
                     propID = candidatePropId;

                     if (string.IsNullOrWhiteSpace(matName))
                     {
                         OpenStaadPropertyAdapter.TryGetBeamMaterialName(openStaad, beamID, out matName, out _);
                     }

                     if (string.IsNullOrWhiteSpace(propName))
                     {
                         OpenStaadPropertyAdapter.TryGetBeamSectionName(openStaad, beamID, out propName, out _);
                     }
                     break;
                 }
             }

             if (beamID == 0) throw new SectionDetectionException("Internal Error: No valid beam ID in current selection.");

             if (propID <= 0)
             {
                 return new SectionInfo
                 {
                     Name = string.IsNullOrWhiteSpace(propName) ? "None" : propName,
                     Code = string.IsNullOrWhiteSpace(propName) ? "None" : "Unknown",
                     Material = string.IsNullOrEmpty(matName) ? "None" : matName,
                     PropType = eFramePropType.General,
                     PropertyRefNo = 0
                 };
             }

             if (string.IsNullOrWhiteSpace(propName))
             {
                 if (!OpenStaadPropertyAdapter.TryGetBeamSectionName(openStaad, beamID, out propName, out string sectionError))
                     Debug.WriteLine($"GetBeamSectionName failed for beam {beamID}: {sectionError}");
             }

             return BuildSectionInfoFromProperty(os, propID, propName, matName);
        }

        public static SectionInfo DetectSectionByPropertyId(object os, int propertyId)
        {
            if (os == null) throw new StaadConnectionException("Not connected to STAAD Application.");
            if (propertyId <= 0) throw new SectionPropertyException("Invalid property id.");

            object openStaad = os;
            if (!TryGetSectionProperty(openStaad, out object propObj, out string propertyError))
                throw new SectionPropertyException(propertyError);

            string propName = "";
            TryGetSectionPropertyNameById(propObj, propertyId, out propName);

            string matName = TryGetMaterialNameFromProperty(os, propertyId);
            return BuildSectionInfoFromProperty(os, propertyId, propName, matName);
        }
    }
}
