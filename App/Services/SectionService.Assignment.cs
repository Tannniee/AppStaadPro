using System;
using System.Collections.Generic;
using System.Diagnostics;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        public static SectionAssignmentResult AssignSectionToSelection(object os, SectionInfo info)
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

             int[] arrBeams = GetSelectedBeamIDs(geometryObj);
             
             if (arrBeams.Length == 0)
             {
                 return SectionAssignmentResult.Warning("Check Selection", "No beams selected in STAAD. Please select beams first.");
             }

             // 2. Resolve property ID safely for current model:
             // copied PropertyRefNo can be stale across files/sessions.
             int propID = ResolveTargetPropertyId(os, info);

             if (propID <= 0)
             {
                 string reason = propID < 0 ? $" (Error {propID})" : "";
                 return SectionAssignmentResult.Error("Creation Failed", $"Could not create or find property '{info.Name}'{reason}.");
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
                         $"Failed to assign property '{info.Name}' (ID: {propID}) to selected beams.\n{assignError}");
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
                 string materialText = "STEEL"; 
                 if (!string.IsNullOrWhiteSpace(mat) && !mat.Equals("None", StringComparison.OrdinalIgnoreCase))
                 {
                     materialText = mat;
                     if (!materialAssigned) materialText += $" (Error: {materialError})";
                 }
                 
                 string msg = $"- {info.Name} (ID: {propID})\n- {arrBeams.Length} beams assigned\n- Material: {materialText}";
                 if (!verified)
                 {
                     string warning = string.IsNullOrWhiteSpace(assignError)
                         ? "STAAD accepted the assignment, but the app could not confirm the read-back immediately."
                         : assignError;
                     return SectionAssignmentResult.SuccessWarning(
                         "Assignment Sent",
                         msg + "\n- " + warning,
                         propID,
                         arrBeams.Length);
                 }

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

            if (VerifyAssignedProperty(propObj, beams, propID, attempts: 4, delayMs: 50, maxChecks: 3))
            {
                verified = true;
                return true;
            }

            // Fallback: assign one-by-one for COM builds that reject array variants.
            int attempted = 0;
            int failed = 0;
            foreach (int beam in beams)
            {
                if (OpenStaadPropertyAdapter.TryAssignBeamProperty(propObj, beam, propID, out string singleError))
                {
                    attempted++;
                    continue;
                }

                failed++;
                Debug.WriteLine($"AssignBeamProperty failed for beam {beam}: {singleError}");
                if (string.IsNullOrWhiteSpace(error))
                    error = singleError;
            }

            int verifyChecks = beams.Length <= 12 ? beams.Length : 6;
            if (attempted == beams.Length && VerifyAssignedProperty(propObj, beams, propID, attempts: 4, delayMs: 50, maxChecks: verifyChecks))
            {
                verified = true;
                return true;
            }

            if (bulkAccepted || attempted == beams.Length)
            {
                error = "STAAD accepted the section assignment command, but OpenSTAAD read-back did not confirm it immediately. If the member already shows the new section in STAAD, the assignment is valid; refresh Used Sections if the app display is stale.";
                return true;
            }

            if (string.IsNullOrWhiteSpace(error))
                error = $"Assignment verification failed after processing {attempted}/{beams.Length} beams.";
            else
                error += $" | Assignment verification failed after processing {attempted}/{beams.Length} beams.";

            return false;
        }

        private static int ResolveTargetPropertyId(object os, SectionInfo info)
        {
            if (os == null || info == null) return 0;

            // Prefer copied property reference only if it exists in current model.
            if (info.PropertyRefNo > 0)
            {
                object openStaad = os;
                if (TryGetSectionProperty(openStaad, out object propObj, out string _) &&
                    TryGetSectionPropertyNameById(propObj, info.PropertyRefNo, out string _))
                    return info.PropertyRefNo;

                Debug.WriteLine($"PropertyRefNo {info.PropertyRefNo} is not valid in current model. Falling back to name/create.");
            }

            // Try by name to avoid duplicate create.
            if (!string.IsNullOrWhiteSpace(info.Name))
            {
                try
                {
                    int byName = CheckPropertyExists(os, info.Name);
                    if (byName > 0) return byName;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"CheckPropertyExists('{info.Name}') failed: {ex.Message}");
                }
            }

            return GetOrCreateProperty(os, info);
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

        private static int GetOrCreateUPTSection(object os, SectionInfo info)
        {
            Func<object, string, int> checkPropertyExists = CheckPropertyExists;
            return SectionUptModule.GetOrCreateUPTSection(os, info, checkPropertyExists);
        }

        private static int GetOrCreateProperty(object os, SectionInfo info)
        {
            Func<object, SectionInfo, int> getOrCreateUPTSection = (model, section) => GetOrCreateUPTSection(model, section);
            Func<object, string, int> checkPropertyExists = CheckPropertyExists;
            return SectionPropertyCreateModule.GetOrCreateProperty(
                os,
                info,
                getOrCreateUPTSection,
                checkPropertyExists);
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
