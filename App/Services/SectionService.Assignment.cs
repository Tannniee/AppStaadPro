using System;
using System.Collections.Generic;
using System.Diagnostics;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        public static SectionAssignmentResult AssignSectionToSelection(
            object os,
            SectionInfo info,
            SectionTransferContext transferContext = null)
        {
             if (os == null)
                 return SectionAssignmentResult.Error("Assignment Error", "Not connected to STAAD.");

             if (info == null)
                 return SectionAssignmentResult.Error("Assignment Error", "No section information was provided.");
             
             // 1. Get Selected Beams
             object openStaad = os;
             if (!TryGetSectionGeometry(openStaad, out object geometryObj, out string geometryError))
             {
                 return SectionAssignmentResult.Warning("Check Selection", "Could not read selected beams: " + geometryError);
             }

             int[] arrBeams = GetSelectedBeamIDs(geometryObj, out string selectionWarning);
             int selectedMemberCount = arrBeams.Length;
             bool selectedPhysicalMembers = TryGetSelectedPhysicalMemberCount(geometryObj, out int physicalMemberCount);
             if (selectedPhysicalMembers)
                 selectedMemberCount = physicalMemberCount;
             
             if (arrBeams.Length == 0)
             {
                 string selectionDetail = string.IsNullOrWhiteSpace(selectionWarning)
                     ? "No beams selected in STAAD. Please select beams first."
                     : "No valid analytical beams were found in the STAAD selection.\n" + selectionWarning;
                 return SectionAssignmentResult.Warning("Check Selection", selectionDetail);
             }

             TargetPropertyResolution resolution;
             try
             {
                 resolution = ResolveTargetProperty(os, info, transferContext);
             }
             catch (Exception ex)
             {
                 AppLogger.Exception("Error resolving destination section property.", ex);
                 return SectionAssignmentResult.Error(
                     "Creation Failed",
                     $"Could not create or find an equivalent property for '{info.Name}'.\n{ex.Message}");
             }

             int propID = resolution.PropertyId;
             if (propID <= 0)
             {
                 string detail = string.IsNullOrWhiteSpace(resolution.ErrorMessage)
                     ? "No equivalent destination property was found or created."
                     : resolution.ErrorMessage;
                 return SectionAssignmentResult.Error(
                     "Creation Failed",
                     $"Could not create or find an equivalent property for '{info.Name}'.\n{detail}");
             }

             // 3. Assign
             try 
             {
                 if (!TryGetSectionProperty(openStaad, out object propObj, out string propertyError))
                 {
                     return SectionAssignmentResult.Error("Assignment Error", propertyError);
                 }

                 bool assigned = TryAssignPropertyToMembers(propObj, arrBeams, propID, out string assignError, out bool verified);
                 if (!assigned)
                 {
                     return SectionAssignmentResult.Error(
                         "Assignment Error",
                         $"Failed to assign property '{resolution.PropertyName}' (ID: {propID}) to selected beams.\n{assignError}");
                 }

                 UpdateUsedSectionsCacheAfterAssignment(os, arrBeams, propID);

                 bool materialAssigned = false;
                 string materialError = "";
                 string mat = info.Material ?? "";
                 if (!string.IsNullOrWhiteSpace(mat) && !mat.Equals("None", StringComparison.OrdinalIgnoreCase))
                 {
                     materialAssigned = TryAssignMaterialToMembers(os, mat, arrBeams, out materialError);
                 }
                 
                 // If we get here, it didn't throw.
                 string materialText = "Not changed";
                 if (!string.IsNullOrWhiteSpace(mat) && !mat.Equals("None", StringComparison.OrdinalIgnoreCase))
                 {
                     materialText = mat;
                     if (!materialAssigned) materialText += $" (Error: {materialError})";
                 }
                 
                 string msg = BuildSectionAssignmentMessage(
                     resolution.PropertyName,
                     propID,
                     selectedMemberCount,
                     selectedPhysicalMembers,
                     arrBeams.Length,
                     materialText,
                     resolution.Description,
                     selectionWarning);

                 return SectionAssignmentResult.Success("Successfully Assigned", msg, propID, arrBeams.Length);
             }
             catch (Exception ex)
             {
                 AppLogger.Exception("Error assigning section property.", ex);
                 string innerMessage = ex.InnerException == null ? "" : "\n" + ex.InnerException.Message;
                 return SectionAssignmentResult.Error("Assignment Error", "Error assigning property: " + ex.Message + innerMessage);
             }
        }

        private static bool TryAssignMaterialToMembers(object os, string material, int[] beams, out string error)
        {
            error = "";
            if (os == null || beams == null || beams.Length == 0)
            {
                error = "No members selected.";
                return false;
            }

            object openStaad = os;
            if (!TryGetSectionProperty(openStaad, out object propObj, out error))
                return false;

            if (!OpenStaadPropertyAdapter.TrySetMaterialName(propObj, material, out string setMaterialError))
                Debug.WriteLine($"SetMaterialName failed for material '{material}': {setMaterialError}");

            if (OpenStaadPropertyAdapter.TryAssignMaterialToMember(propObj, material, beams, out string bulkError))
            {
                return true;
            }

            error = bulkError;

            int success = 0;
            foreach (int beam in beams)
            {
                if (OpenStaadPropertyAdapter.TryAssignMaterialToMember(propObj, material, beam, out string singleError))
                {
                    success++;
                    continue;
                }

                Debug.WriteLine($"AssignMaterialToMember failed for beam {beam}: {singleError}");
                if (string.IsNullOrEmpty(error))
                    error = singleError;
            }

            if (success == beams.Length)
                return true;

            if (success > 0)
                error = $"assigned to {success}/{beams.Length} members";

            return false;
        }

        private static bool TryGetSelectedPhysicalMemberCount(object geometryObj, out int physicalMemberCount)
        {
            physicalMemberCount = 0;
            if (!OpenStaadSelectionAdapter.TryGetSelectedPhysicalMemberIdsFromGeometry(
                    geometryObj,
                    out int[] physicalMemberIds,
                    out string physicalError))
            {
                Debug.WriteLine("Get selected physical members failed: " + physicalError);
                return false;
            }

            physicalMemberCount = physicalMemberIds.Length;
            return physicalMemberCount > 0;
        }

        private static string BuildSectionAssignmentMessage(
            string sectionName,
            int propID,
            int selectedMemberCount,
            bool selectedPhysicalMembers,
            int analyticalBeamCount,
            string materialText,
            string resolutionDescription,
            string selectionWarning)
        {
            string selectionLine = selectedPhysicalMembers
                ? $"- {selectedMemberCount} selected physical member(s)"
                : $"- {selectedMemberCount} selected beam(s)";

            string assignedLine = selectedPhysicalMembers && analyticalBeamCount != selectedMemberCount
                ? $"- {analyticalBeamCount} analytical beam(s) assigned"
                : $"- {analyticalBeamCount} beam(s) assigned";

            string resolutionLine = string.IsNullOrWhiteSpace(resolutionDescription)
                ? string.Empty
                : "\n- " + resolutionDescription;

            string selectionWarningLine = string.IsNullOrWhiteSpace(selectionWarning)
                ? string.Empty
                : "\n- Selection note: " + selectionWarning;

            return $"- {sectionName} (ID: {propID}){resolutionLine}\n{selectionLine}\n{assignedLine}\n- Material: {materialText}{selectionWarningLine}";
        }

        private static bool TryAssignPropertyToMembers(object propObj, int[] beams, int propID, out string error, out bool verified)
        {
            error = "";
            verified = false;
            if (propObj == null || beams == null || beams.Length == 0 || propID <= 0)
            {
                error = "Invalid assign input.";
                return false;
            }

            // First attempt: bulk assignment (faster)
            bool bulkAccepted = OpenStaadPropertyAdapter.TryAssignBeamProperty(propObj, beams, propID, out string bulkError);
            if (!bulkAccepted)
                error = "Bulk AssignBeamProperty failed: " + bulkError;

            int verificationChecks = beams.Length <= 50 ? beams.Length : 12;
            if (VerifyAssignedProperty(propObj, beams, propID, attempts: 6, delayMs: 100, maxChecks: verificationChecks))
            {
                verified = true;
                return true;
            }

            // Fallback: assign one-by-one for COM builds that reject array variants.
            int attempted = 0;
            foreach (int beam in beams)
            {
                if (OpenStaadPropertyAdapter.TryAssignBeamProperty(propObj, beam, propID, out string singleError))
                {
                    attempted++;
                    continue;
                }

                Debug.WriteLine($"AssignBeamProperty failed for beam {beam}: {singleError}");
                if (string.IsNullOrWhiteSpace(error))
                    error = singleError;
            }

            if (attempted == beams.Length &&
                VerifyAssignedProperty(propObj, beams, propID, attempts: 6, delayMs: 100, maxChecks: verificationChecks))
            {
                verified = true;
                return true;
            }

            if (string.IsNullOrWhiteSpace(error))
                error = $"OpenSTAAD accepted the assignment command, but read-back did not confirm property {propID} after processing {attempted}/{beams.Length} beams.";
            else
                error += $" | Read-back did not confirm property {propID} after processing {attempted}/{beams.Length} beams.";

            return false;
        }

        private static bool TryGetSectionPropertyNameById(object propObj, int propID, out string sectionName)
        {
            sectionName = "";
            if (propObj == null || propID <= 0) return false;

            bool ok = OpenStaadPropertyAdapter.TryGetSectionPropertyName(propObj, propID, out sectionName, out string error);
            if (!ok)
                Debug.WriteLine($"GetSectionPropertyName failed for prop {propID}: {error}");

            return ok;
        }

        private static int GetOrCreateUPTSection(
            object os,
            SectionInfo info,
            Func<object, string, int> checkPropertyExists = null)
        {
            Func<object, string, int> checker = checkPropertyExists ?? CheckPropertyExists;
            return SectionUptModule.GetOrCreateUPTSection(os, info, checker);
        }

        private static int GetOrCreateProperty(
            object os,
            SectionInfo info,
            Func<object, string, int> checkPropertyExists = null)
        {
            Func<object, string, int> checker = checkPropertyExists ?? CheckPropertyExists;
            Func<object, SectionInfo, int> getOrCreateUPTSection =
                (model, section) => GetOrCreateUPTSection(model, section, checker);
            return SectionPropertyCreateModule.GetOrCreateProperty(
                os,
                info,
                getOrCreateUPTSection,
                checker);
        }

        private static int CheckPropertyExists(object os, string name)
        {
             if (os == null || string.IsNullOrWhiteSpace(name)) return 0;

             string targetName = name.Trim();
             object openStaad = os;
             if (!TryGetSectionProperty(openStaad, out object propObj, out string propertyError))
             {
                 Debug.WriteLine("Property object is not available: " + propertyError);
                 return 0;
             }

             if (!OpenStaadPropertyAdapter.TryGetSectionPropertyList(propObj, out int[] ids, out string listError))
             {
                 Debug.WriteLine("GetSectionPropertyList failed: " + listError);
                 return 0;
             }
             
             // Loop and Check Names
             // GetSectionPropertyName(PropID, ref Name)
             foreach (int id in ids)
             {
                  if (!OpenStaadPropertyAdapter.TryGetSectionPropertyName(propObj, id, out string pName, out _))
                      continue;

                  if (!string.IsNullOrEmpty(pName) && pName.Equals(targetName, StringComparison.OrdinalIgnoreCase)) return id;
             }
              return 0;
         }
    }
}
