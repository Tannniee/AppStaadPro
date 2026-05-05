using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Office.Interop.Excel;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        public static void AssignSectionToSelection(dynamic os, SectionInfo info)
        {
             if (os == null) return;
             if (info == null) return;
             
             // 1. Get Selected Beams
             int[] arrBeams = GetSelectedBeamIDs(os.Geometry);
             
             if (arrBeams.Length == 0)
             {
                 StaadSteelApp.TopMostMessageBox.ShowWarning("No beams selected in STAAD. Please select beams first.", "Check Selection");
                 return;
             }

             // 2. Resolve property ID safely for current model:
             // copied PropertyRefNo can be stale across files/sessions.
             int propID = ResolveTargetPropertyId(os, info);

             if (propID <= 0)
             {
                 string reason = propID < 0 ? $" (Error {propID})" : "";
                 StaadSteelApp.TopMostMessageBox.ShowError($"Could not create or find property '{info.Name}'{reason}.", "Creation Failed");
                 return;
             }

             // 3. Assign
             try 
             {
                 object openStaad = os;
                 if (!OpenStaadPropertyAdapter.TryGetPropertyObject(openStaad, out object propObj, out string propertyError))
                 {
                     StaadSteelApp.TopMostMessageBox.ShowError(propertyError, "Assignment Error");
                     return;
                 }

                 bool assigned = TryAssignPropertyToMembers(propObj, arrBeams, propID, out string assignError);
                 if (!assigned)
                 {
                     StaadSteelApp.TopMostMessageBox.ShowError(
                         $"Failed to assign property '{info.Name}' (ID: {propID}) to selected beams.\n{assignError}",
                         "Assignment Error");
                     return;
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
                 StaadSteelApp.TopMostMessageBox.ShowSuccess(msg, "Successfully Assigned");
             }
             catch (Exception ex)
             {
                 StaadSteelApp.TopMostMessageBox.ShowError("Error assigning property: " + ex.Message + "\n" + ex.InnerException?.Message, "Assignment Error");
             }
        }

        private static bool TryAssignMaterialToMembers(dynamic os, string material, int[] beams, out string error)
        {
            error = "";
            if (os == null || beams == null || beams.Length == 0)
            {
                error = "No members selected.";
                return false;
            }

            object openStaad = os;
            if (!OpenStaadPropertyAdapter.TryGetPropertyObject(openStaad, out object propObj, out error))
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

        private static bool TryAssignPropertyToMembers(object propObj, int[] beams, int propID, out string error)
        {
            error = "";
            if (propObj == null || beams == null || beams.Length == 0 || propID <= 0)
            {
                error = "Invalid assign input.";
                return false;
            }

            // First attempt: bulk assignment (faster)
            if (!OpenStaadPropertyAdapter.TryAssignBeamProperty(propObj, beams, propID, out string bulkError))
                error = "Bulk AssignBeamProperty failed: " + bulkError;

            if (VerifyAssignedProperty(propObj, beams, propID, attempts: 2, delayMs: 15, maxChecks: 3))
                return true;

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
            if (attempted == beams.Length && VerifyAssignedProperty(propObj, beams, propID, attempts: 2, delayMs: 15, maxChecks: verifyChecks))
                return true;

            if (string.IsNullOrWhiteSpace(error))
                error = $"Assignment verification failed after processing {attempted}/{beams.Length} beams.";
            else
                error += $" | Assignment verification failed after processing {attempted}/{beams.Length} beams.";

            return false;
        }

        private static bool VerifyAssignedProperty(object propObj, int[] beams, int propID, int attempts = 1, int delayMs = 0, int maxChecks = int.MaxValue)
        {
            if (propObj == null || beams == null || beams.Length == 0 || propID <= 0)
                return false;

            int[] beamsToCheck = GetVerificationBeamSet(beams, maxChecks);
            if (beamsToCheck.Length == 0)
                return false;

            string expectedSectionName = "";
            TryGetSectionPropertyNameById(propObj, propID, out expectedSectionName);

            int retries = Math.Max(attempts, 1);
            for (int attempt = 0; attempt < retries; attempt++)
            {
                int matched = 0;
                foreach (int beam in beamsToCheck)
                {
                    if (IsBeamAssignedToProperty(propObj, beam, propID, expectedSectionName))
                        matched++;
                }

                if (matched == beamsToCheck.Length)
                    return true;

                if (attempt < retries - 1 && delayMs > 0)
                    Thread.Sleep(delayMs);
            }

            return false;
        }

        private static bool IsBeamAssignedToProperty(object propObj, int beamNo, int propID, string expectedSectionName)
        {
            if (beamNo <= 0 || propObj == null || propID <= 0)
                return false;

            if (TryReadBeamPropertyRefNo(propObj, beamNo, out int assignedProp) && assignedProp == propID)
                return true;

            if (!string.IsNullOrWhiteSpace(expectedSectionName)
                && TryReadBeamSectionName(propObj, beamNo, out string assignedName)
                && AreSectionNamesEquivalent(assignedName, expectedSectionName))
            {
                return true;
            }

            return false;
        }

        private static int[] GetVerificationBeamSet(int[] beams, int maxChecks)
        {
            if (beams == null || beams.Length == 0)
                return Array.Empty<int>();

            int limit = Math.Max(1, maxChecks);
            if (beams.Length <= limit)
                return beams.Distinct().ToArray();

            var checks = new List<int>(limit);
            int lastIndex = beams.Length - 1;
            int midIndex = lastIndex / 2;

            AddCheckBeam(checks, beams[0], limit);
            AddCheckBeam(checks, beams[midIndex], limit);
            AddCheckBeam(checks, beams[lastIndex], limit);

            if (checks.Count < limit)
            {
                double step = (double)lastIndex / (limit - 1);
                for (int i = 1; i < limit - 1 && checks.Count < limit; i++)
                {
                    int index = (int)Math.Round(i * step);
                    index = Math.Max(0, Math.Min(lastIndex, index));
                    AddCheckBeam(checks, beams[index], limit);
                }
            }

            return checks.ToArray();
        }

        private static void AddCheckBeam(List<int> checks, int beam, int limit)
        {
            if (beam <= 0 || checks.Count >= limit || checks.Contains(beam))
                return;

            checks.Add(beam);
        }

        private static bool TryReadBeamPropertyRefNo(object propObj, int beamNo, out int propNo)
        {
            propNo = 0;
            if (propObj == null || beamNo <= 0) return false;

            bool ok = OpenStaadPropertyAdapter.TryGetBeamSectionPropertyRefNoFromProperty(propObj, beamNo, out propNo, out string error);
            if (!ok)
                Debug.WriteLine($"GetBeamSectionPropertyRefNo failed for beam {beamNo}: {error}");

            return ok;
        }

        private static bool TryReadBeamSectionName(object propObj, int beamNo, out string sectionName)
        {
            sectionName = "";
            if (propObj == null || beamNo <= 0)
                return false;

            bool ok = OpenStaadPropertyAdapter.TryGetBeamSectionNameFromProperty(propObj, beamNo, out sectionName, out string error);
            if (!ok)
                Debug.WriteLine($"GetBeamSectionName failed for beam {beamNo}: {error}");

            return ok;
        }

        private static bool AreSectionNamesEquivalent(string left, string right)
        {
            string a = (left ?? "").Trim();
            string b = (right ?? "").Trim();
            if (a.Length == 0 || b.Length == 0)
                return false;

            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAssignSuccess(object ret)
        {
            if (ret == null) return false;
            if (ret is bool b) return b;
            if (ret is int i) return i != 0;
            if (ret is short s) return s != 0;
            if (ret is double d) return Math.Abs(d) > 0.0001;

            try
            {
                return Convert.ToInt32(ret) != 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static int ResolveTargetPropertyId(dynamic os, SectionInfo info)
        {
            if (os == null || info == null) return 0;

            // Prefer copied property reference only if it exists in current model.
            if (info.PropertyRefNo > 0)
            {
                if (TryGetSectionPropertyNameById(os.Property, info.PropertyRefNo, out string _))
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

        private static int GetOrCreateUPTSection(dynamic os, SectionInfo info)
        {
            Func<dynamic, string, int> checkPropertyExists = CheckPropertyExists;
            return SectionUptModule.GetOrCreateUPTSection(os, info, checkPropertyExists);
        }

        private static int GetOrCreateProperty(dynamic os, SectionInfo info)
        {
            Func<dynamic, SectionInfo, int> getOrCreateUPTSection = GetOrCreateUPTSection;
            Func<dynamic, string, int> checkPropertyExists = CheckPropertyExists;
            return SectionPropertyCreateModule.GetOrCreateProperty(
                os,
                info,
                getOrCreateUPTSection,
                checkPropertyExists);
        }

        private static int CheckPropertyExists(dynamic os, string name)
        {
             if (os == null || string.IsNullOrWhiteSpace(name)) return 0;

             string targetName = name.Trim();
             object propObj = os.Property;
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
