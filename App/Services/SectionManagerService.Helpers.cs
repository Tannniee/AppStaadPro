using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using StaadSteelApp.Core.Interop;
using Excel = Microsoft.Office.Interop.Excel;

namespace StaadSteelApp.Core
{
    public static partial class SectionManagerService
    {
        private static string BuildSectionDisplayName(int retType, double[] props, string originalName, int propId, double width, double depth, double tf, double tw)
        {
            if (retType == 680)
            {
                double h1mm = GetAt(props, 0) * 1000.0;
                double h2mm = GetAt(props, 2) * 1000.0;
                double twmm = GetAt(props, 1) * 1000.0;
                double bfmm = GetAt(props, 3) * 1000.0;
                double tfmm = GetAt(props, 4) * 1000.0;
                double bfbmm = GetAt(props, 5, GetAt(props, 3)) * 1000.0;
                double tfbmm = GetAt(props, 6, GetAt(props, 4)) * 1000.0;

                string taperedName = SectionNamingService.GenerateSectionName(
                    eFramePropType.I,
                    h1mm, h2mm, twmm, bfmm, tfmm, bfbmm, tfbmm, 0);

                if (!string.IsNullOrWhiteSpace(taperedName))
                    return taperedName;

                return $"TAPERED-{propId}";
            }

            if (retType == 698)
            {
                double h1mm = GetAt(props, 0) * 1000.0;
                double h2mm = GetAt(props, 2, GetAt(props, 0)) * 1000.0;
                double twmm = GetAt(props, 1) * 1000.0;
                double bfmm = GetAt(props, 3) * 1000.0;
                double tfmm = GetAt(props, 4) * 1000.0;
                double bfbmm = GetAt(props, 5, GetAt(props, 3)) * 1000.0;
                double tfbmm = GetAt(props, 6, GetAt(props, 4)) * 1000.0;

                string uptIName = SectionNamingService.GenerateSectionName(
                    eFramePropType.I,
                    h1mm, h2mm, twmm, bfmm, tfmm, bfbmm, tfbmm, 0);

                if (!string.IsNullOrWhiteSpace(uptIName))
                    return uptIName;
            }

            if (retType == 690)
            {
                double hmm = GetAt(props, 1, depth) * 1000.0;
                double twmm = GetAt(props, 2, tw) * 1000.0;
                double bfmm = GetAt(props, 3, width) * 1000.0;
                double tfmm = GetAt(props, 4, tf) * 1000.0;
                double bfbmm = GetAt(props, 10, GetAt(props, 3, width)) * 1000.0;
                double tfbmm = GetAt(props, 11, GetAt(props, 4, tf)) * 1000.0;

                string uptWideFlangeName = SectionNamingService.GenerateSectionName(
                    eFramePropType.I,
                    hmm, hmm, twmm, bfmm, tfmm, bfbmm, tfbmm, 0);

                if (!string.IsNullOrWhiteSpace(uptWideFlangeName))
                    return uptWideFlangeName;
            }

            if (new[] { 610, 611, 612, 616, 670 }.Contains(retType))
            {
                double hmm = GetAt(props, 1, depth) * 1000.0;
                double bfmm = GetAt(props, 2, width) * 1000.0;
                double twmm = GetAt(props, 4, tw) * 1000.0;
                double tfmm = GetAt(props, 3, tf) * 1000.0;

                string iName = SectionNamingService.GenerateSectionName(
                    eFramePropType.I,
                    hmm, hmm, twmm, bfmm, tfmm, bfmm, tfmm, 0);

                if (!string.IsNullOrWhiteSpace(iName))
                    return iName;
            }

            return originalName;
        }

        private static string GetTypeName(int retType, string secName)
        {
            if (retType == 680) return "Tapered";
            if (new[] { 610, 611, 612, 616, 670, 690, 698 }.Contains(retType)) return "I-Section";
            if (new[] { 620, 673, 694, 61100 }.Contains(retType)) return "Tee";
            if (new[] { 630, 631, 691 }.Contains(retType)) return "Channel";
            if (new[] { 640, 692 }.Contains(retType)) return "Angle";
            if (retType == 693) return "2L-Angle";
            if (new[] { 650, 654, 675, 696 }.Contains(retType)) return "Box/Tube";
            if (retType == 672) return "Rectangular";
            if (new[] { 655, 660, 661, 695 }.Contains(retType)) return "Pipe";
            if (retType == 671) return "Circle";

            string n = (secName ?? "").Trim().ToUpperInvariant();
            if (n.Contains("PIPE") || n.Contains("CHS") || n.StartsWith("PIP")) return "Pipe";
            if (n.Contains("BOX") || n.Contains("TUBE") || n.Contains("RHS") || n.Contains("SHS") || n.StartsWith("HSS")) return "Box/Tube";
            if (n.StartsWith("2L") || n.Contains("DOUBLE ANGLE")) return "2L-Angle";
            if (n.StartsWith("L")) return "Angle";
            if (n.StartsWith("MC") || n.StartsWith("C") || n.StartsWith("UPN") || n.StartsWith("UPE")) return "Channel";
            if (n.StartsWith("T")) return "Tee";
            if (n.StartsWith("I") || n.StartsWith("W") || n.StartsWith("H") || n.StartsWith("UB") || n.StartsWith("UC") || n.StartsWith("HE") || n.StartsWith("IP")) return "I-Section";
            return "Other";
        }

        private static double GetDensityKgM3(object prop, string materialName, Action<string> addError)
        {
            double raw = 0;
            string err1 = null;
            string err2 = null;
            bool ok = TryGetMaterialDensityRaw(prop, materialName, true, out raw, out err1) ||
                      TryGetMaterialDensityRaw(prop, materialName, false, out raw, out err2);
            if (ok)
            {
                if (raw >= 500) return raw;
                if (raw >= 40) return raw * 1000.0 / 9.80665;
                if (raw >= 1.2) return raw * 1000.0;
                if (raw > 0) return raw * 27679904.710191;
            }

            string n = (materialName ?? "").ToUpperInvariant();
            if (n.Contains("CONC")) return 2400;
            if (n.Contains("ALUM")) return 2700;
            if (n.Contains("WOOD") || n.Contains("TIMBER")) return 600;

            if (!string.IsNullOrWhiteSpace(materialName))
            {
                string err = err1 ?? err2;
                if (!string.IsNullOrWhiteSpace(err))
                    addError?.Invoke($"Material '{materialName}': could not read density from STAAD ({err}). Using fallback 7850 kg/m3.");
            }
            return 7850;
        }

        private static bool TryGetMaterialDensityRaw(object prop, string materialName, bool extended, out double density, out string error)
        {
            density = 0;
            error = null;

            if (OpenStaadPropertyAdapter.TryGetMaterialPropertyRaw(prop, materialName, extended, out double[] values, out _, out error))
            {
                density = values.Length > 2 ? values[2] : 0.0;
                return density > 0;
            }

            return false;
        }

        internal static bool IsSteelForMassBreakdown(SectionStat stat)
        {
            if (stat == null)
                return false;

            string material = (stat.Material ?? string.Empty).Trim().ToUpperInvariant();

            // Material name is more reliable than the density fallback. When an
            // OpenSTAAD density call fails, GetDensityKgM3 deliberately returns
            // 7850 kg/m3; that fallback must not turn known non-steel materials
            // into steel in Mass Breakdown.
            if (material.Contains("CONC") ||
                material.Contains("ALUM") ||
                material.Contains("WOOD") ||
                material.Contains("TIMBER"))
            {
                return false;
            }

            if (material.Contains("STEEL"))
                return true;

            // Keep custom steel grades such as Q345, S355, and A992 working.
            return stat.Density >= 3000;
        }

        private static ASTMA123Category GetCategoryFromType(string sectionType)
        {
            switch (sectionType)
            {
                case "I-Section":
                case "Tapered":
                    return ASTMA123Category.Plate;
                case "Pipe":
                case "Box/Tube":
                case "Rectangular":
                    return ASTMA123Category.PipeTubing;
                default:
                    return ASTMA123Category.StructuralShapes;
            }
        }

        private static string CategoryToString(ASTMA123Category cat)
        {
            switch (cat)
            {
                case ASTMA123Category.Plate: return "Plate";
                case ASTMA123Category.PipeTubing: return "Pipe & Tubing";
                default: return "Structural Shapes";
            }
        }

        private static int GetASTMA123Grade(ASTMA123Category category, double thicknessMM)
        {
            switch (category)
            {
                case ASTMA123Category.Plate:
                    if (thicknessMM < 1.6) return 45;
                    if (thicknessMM < 3.2) return 65;
                    if (thicknessMM < 16.0) return 75;
                    return 100;
                case ASTMA123Category.PipeTubing:
                    if (thicknessMM < 3.2) return 45;
                    return 75;
                default:
                    if (thicknessMM < 1.6) return 45;
                    if (thicknessMM < 3.2) return 65;
                    if (thicknessMM < 6.4) return 75;
                    return 100;
            }
        }

        private static double GetCoatingWeight(int grade)
        {
            switch (grade)
            {
                case 45: return 320;
                case 65: return 460;
                case 75: return 530;
                case 100: return 705;
                default: return 530;
            }
        }

        private static int[] GetAllBeamIds(object geometry, Action<string> addError)
        {
            if (OpenStaadGeometryAdapter.TryGetBeamList(geometry, out int[] beamIds, out string error))
                return beamIds.Where(x => x > 0).Distinct().ToArray();

            if (!string.IsNullOrWhiteSpace(error))
            {
                addError?.Invoke("GetAllBeamIds failed: " + error);
            }

            return new int[0];
        }

        private static int[] ToIntArray(object obj)
        {
            if (obj is int[] i) return i;
            if (!(obj is Array arr)) return new int[0];
            var list = new List<int>(arr.Length);
            foreach (var v in arr) list.Add(ToInt(v));
            return list.ToArray();
        }

        private static double[] ToDoubleArray(object obj, int minLen)
        {
            var list = new List<double>();
            if (obj is double[] d) list.AddRange(d);
            else if (obj is Array arr) foreach (var v in arr) list.Add(ToDouble(v));
            while (list.Count < minLen) list.Add(0);
            return list.ToArray();
        }

        private static int ToInt(object v)
        {
            TryToInt(v, out int value);
            return value;
        }

        private static double ToDouble(object v)
        {
            TryToDouble(v, out double value);
            return value;
        }

        private static bool TryToInt(object v, out int value)
        {
            try
            {
                value = Convert.ToInt32(v, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                value = 0;
                return false;
            }
        }

        private static bool TryToDouble(object v, out double value)
        {
            try
            {
                value = Convert.ToDouble(v, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                value = 0;
                return false;
            }
        }

        private static double GetAt(double[] arr, int idx, double fallback = 0)
        {
            if (arr == null || idx < 0 || idx >= arr.Length) return fallback;
            return arr[idx] > 0 ? arr[idx] : fallback;
        }

        private static int GetSectionSortPriority(SectionStat s)
        {
            if (s == null) return 99;
            string t = (s.Type ?? string.Empty).Trim();

            // Requested order:
            // Non-tapered section -> Tapered -> Steel shapes -> Pipe -> Tube -> Angle -> Circle -> Others
            if (t.Equals("I-Section", StringComparison.OrdinalIgnoreCase)) return 0;
            if (t.Equals("Tapered", StringComparison.OrdinalIgnoreCase)) return 1;

            if (t.Equals("Channel", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("Tee", StringComparison.OrdinalIgnoreCase))
                return 2;

            if (t.Equals("Pipe", StringComparison.OrdinalIgnoreCase)) return 3;

            if (t.Equals("Box/Tube", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("Rectangular", StringComparison.OrdinalIgnoreCase))
                return 4;

            if (t.Equals("Angle", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("2L-Angle", StringComparison.OrdinalIgnoreCase))
                return 5;

            if (t.Equals("Circle", StringComparison.OrdinalIgnoreCase)) return 6;
            return 7;
        }
    }
}
