using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
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
    }
}
