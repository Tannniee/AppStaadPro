using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using StaadSteelApp.AISC360;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public partial class StaadToAiscMapper
    {
        private string TryGetBeamSectionName(int memberNo)
        {
            if (_os == null) return string.Empty;

            if (OpenStaadPropertyAdapter.TryGetBeamSectionName(_os, memberNo, out string sectionName, out string error))
            {
                return sectionName;
            }

            WarnOnce($"BeamSectionName:{memberNo}", $"Member {memberNo}: GetBeamSectionName failed. {error}");
            return string.Empty;
        }

        private string TryGetBeamMaterialName(int memberNo)
        {
            if (_os == null) return string.Empty;

            if (OpenStaadPropertyAdapter.TryGetBeamMaterialName(_os, memberNo, out string materialName, out string error))
            {
                return materialName;
            }

            WarnOnce($"BeamMaterialName:{memberNo}", $"Member {memberNo}: GetBeamMaterialName failed. {error}");
            return string.Empty;
        }

        private static void ApplySectionTypeFlags(AnalyzedMember am)
        {
            if (am == null) return;

            am.IsPipe = false;
            am.IsBoxHSS = false;
            am.IsAngle = false;
            am.IsChannel = false;
            am.IsTee = false;

            string section = (am.SectionName ?? string.Empty).Trim().ToUpperInvariant();
            if (section.Length == 0) return;

            int xCount = CountChar(section, 'X');
            bool startsWithHss = section.StartsWith("HSS", StringComparison.Ordinal);
            bool explicitPipe = section.Contains("PIPE") ||
                                section.Contains("CHS") ||
                                section.StartsWith("PIP", StringComparison.Ordinal);
            bool explicitBox = section.Contains("BOX") ||
                               section.Contains("RHS") ||
                               section.Contains("SHS") ||
                               section.Contains("TUBE") ||
                               section.StartsWith("TUB", StringComparison.Ordinal);

            bool isRoundHss = startsWithHss && xCount <= 1;
            bool isRectHss = startsWithHss && xCount >= 2;
            bool isPipe = explicitPipe || isRoundHss;
            bool isBox = explicitBox || isRectHss;

            if (isPipe && isBox)
            {
                // HSS with two dimensions (e.g., HSS8X6X...) is box; one dimension is round.
                if (isRectHss) isPipe = false;
                else isBox = false;
            }

            am.IsPipe = isPipe;
            am.IsBoxHSS = !am.IsPipe && isBox;

            if (!am.IsPipe && !am.IsBoxHSS)
            {
                am.IsAngle = section.Contains("ANGLE") ||
                             section.StartsWith("L", StringComparison.Ordinal) ||
                             section.StartsWith("2L", StringComparison.Ordinal);
                am.IsChannel = section.Contains("CHANNEL") ||
                               section.StartsWith("C", StringComparison.Ordinal) ||
                               section.StartsWith("MC", StringComparison.Ordinal) ||
                               section.StartsWith("UPN", StringComparison.Ordinal) ||
                               section.StartsWith("UPE", StringComparison.Ordinal) ||
                               section.StartsWith("PFC", StringComparison.Ordinal);
                am.IsTee = section.Contains("TEE") ||
                           section.StartsWith("WT", StringComparison.Ordinal) ||
                           section.StartsWith("MT", StringComparison.Ordinal) ||
                           section.StartsWith("ST", StringComparison.Ordinal) ||
                           (section.StartsWith("T", StringComparison.Ordinal) &&
                            !section.StartsWith("TAPER", StringComparison.Ordinal));

                if (am.IsAngle)
                {
                    am.IsChannel = false;
                    am.IsTee = false;
                }
            }

            if (am.IsPipe)
            {
                if (am.D_pipe <= 0)
                    am.D_pipe = am.d > 0 ? am.d : am.bf;

                if (am.t_pipe <= 0)
                    am.t_pipe = PickMinPositive(am.t_pipe, am.tw, am.tf, am.tfb);

                if (am.D_pipe <= 0 || am.t_pipe <= 0)
                    am.IsPipe = false;
            }
            else if (am.IsBoxHSS)
            {
                if (am.Ht <= 0) am.Ht = am.d;
                if (am.B <= 0) am.B = am.bf > 0 ? am.bf : am.bfb;
                if (am.tdes <= 0) am.tdes = PickMinPositive(am.tdes, am.tw, am.tf, am.tfb);

                if (am.Ht <= 0 || am.B <= 0 || am.tdes <= 0)
                    am.IsBoxHSS = false;
            }
        }

        private static int CountChar(string text, char c)
        {
            int count = 0;
            if (string.IsNullOrEmpty(text)) return count;
            for (int i = 0; i < text.Length; i++)
                if (text[i] == c) count++;
            return count;
        }

        private static double PickMinPositive(params double[] values)
        {
            double min = 0.0;
            if (values == null) return min;

            for (int i = 0; i < values.Length; i++)
            {
                double v = values[i];
                if (v <= 0) continue;
                if (min <= 0 || v < min) min = v;
            }

            return min;
        }

        private static bool TryGetStringParam(IReadOnlyDictionary<string, string> parameters, string key, out string value)
        {
            value = null;
            if (parameters == null || string.IsNullOrWhiteSpace(key)) return false;
            if (!parameters.TryGetValue(key, out string raw)) return false;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            value = raw.Trim();
            return true;
        }

        private static bool TryGetPositiveDoubleParam(IReadOnlyDictionary<string, string> parameters, string key, out double value)
        {
            value = 0;
            if (!TryGetStringParam(parameters, key, out string text)) return false;
            if (!TryParseDouble(text, out double parsed)) return false;
            if (parsed <= 0) return false;

            value = parsed;
            return true;
        }

        private static bool TryGetPositiveLengthParamMm(IReadOnlyDictionary<string, string> parameters, string key, out double valueMm)
        {
            valueMm = 0;
            if (!TryGetPositiveDoubleParam(parameters, key, out double valueM)) return false;

            valueMm = valueM * MToMm; // STAAD lengths are in meters
            return true;
        }

        private bool TryGetPositiveDoubleParamWarn(IReadOnlyDictionary<string, string> parameters, int memberNo, string key, out double value)
        {
            value = 0;
            if (parameters == null || string.IsNullOrWhiteSpace(key)) return false;

            if (!parameters.TryGetValue(key, out string raw) || string.IsNullOrWhiteSpace(raw))
                return false;

            string text = raw.Trim();
            if (!TryParseDouble(text, out double parsed))
            {
                WarnOnce($"DesignParamParse:{memberNo}:{key}", $"Member {memberNo}: design parameter '{key}' has invalid numeric value '{text}'.");
                return false;
            }

            if (parsed <= 0)
            {
                // Zero typically means "unassigned / use default" in STAAD — skip silently.
                // Only warn on genuinely negative values, which indicate a real problem.
                if (parsed < 0)
                    WarnOnce($"DesignParamNegative:{memberNo}:{key}", $"Member {memberNo}: design parameter '{key}' is negative ({parsed}).");
                return false;
            }

            value = parsed;
            return true;
        }

        private bool TryGetPositiveLengthParamMmWarn(IReadOnlyDictionary<string, string> parameters, int memberNo, string key, out double valueMm)
        {
            valueMm = 0;
            if (!TryGetPositiveDoubleParamWarn(parameters, memberNo, key, out double valueM))
                return false;

            valueMm = valueM * MToMm;
            return true;
        }

        private static bool TryParseDouble(string text, out double value)
        {
            const NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands;

            return double.TryParse(text, style, CultureInfo.InvariantCulture, out value) ||
                   double.TryParse(text, style, CultureInfo.CurrentCulture, out value);
        }

        private bool TryGetMaterialPropertyEx(
            string materialName,
            out double elasticity,
            out double poisson,
            out double fy,
            out double fu)
        {
            elasticity = 0;
            poisson = 0;
            fy = 0;
            fu = 0;

            if (string.IsNullOrWhiteSpace(materialName)) return false;
            object propObj = GetOpenStaadSubObject("Property");
            if (propObj == null) return false;

            if (!OpenStaadPropertyAdapter.TryGetMaterialPropertyRaw(propObj, materialName, true, out double[] values, out object ret, out string error))
            {
                WarnOnce("GetMaterialPropertyEx:" + materialName, $"Material '{materialName}': GetMaterialPropertyEx failed. Material properties may use defaults. {error}");
                return false;
            }

            elasticity = GetArrayValue(values, 0);
            poisson = GetArrayValue(values, 1);
            fy = GetArrayValue(values, 5);
            fu = GetArrayValue(values, 6);

            // Docs state TRUE/FALSE, but some COM variants can return numeric codes.
            // Accept either explicit success OR usable outputs.
            return IsTruthyResult(ret) || elasticity > 0 || fy > 0 || fu > 0;
        }

        private bool TryGetMaterialProperty(
            string materialName,
            out double elasticity,
            out double poisson)
        {
            elasticity = 0;
            poisson = 0;

            if (string.IsNullOrWhiteSpace(materialName)) return false;
            object propObj = GetOpenStaadSubObject("Property");
            if (propObj == null) return false;

            if (!OpenStaadPropertyAdapter.TryGetMaterialPropertyRaw(propObj, materialName, false, out double[] values, out object ret, out string error))
            {
                WarnOnce("GetMaterialProperty:" + materialName, $"Material '{materialName}': GetMaterialProperty failed. Material properties may use defaults. {error}");
                return false;
            }

            elasticity = GetArrayValue(values, 0);
            poisson = GetArrayValue(values, 1);

            // Docs define 0 as success for GetMaterialProperty.
            if (IsZeroSuccessResult(ret))
                return true;

            return elasticity > 0;
        }

        private static bool TryConvertStressToMpa(double rawValue, out double stressMpa)
        {
            stressMpa = 0;
            if (double.IsNaN(rawValue) || double.IsInfinity(rawValue) || rawValue <= 0) return false;

            double abs = Math.Abs(rawValue);
            if (abs >= 1e10)
            {
                // Pa -> MPa
                stressMpa = rawValue / 1_000_000.0;
            }
            else if (abs >= 1e7)
            {
                // kN/m2 -> MPa
                stressMpa = rawValue / 1000.0;
            }
            else
            {
                // Already MPa
                stressMpa = rawValue;
            }

            return stressMpa > 0 && !double.IsNaN(stressMpa) && !double.IsInfinity(stressMpa);
        }

        private static double TryToDouble(object value)
        {
            if (value == null) return 0;
            if (value is double d) return d;
            if (value is float f) return f;
            if (value is int i) return i;
            if (value is long l) return l;
            if (value is short s) return s;

            try
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                if (TryParseDouble(Convert.ToString(value) ?? string.Empty, out double parsed))
                    return parsed;
                return 0;
            }
        }

        private static bool IsTruthyResult(object ret)
        {
            if (ret is bool b) return b;
            if (ret == null) return false;
            try
            {
                return Convert.ToInt32(ret, CultureInfo.InvariantCulture) != 0;
            }
            catch
            {
                string text = Convert.ToString(ret) ?? string.Empty;
                return text.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            }
        }

        private static bool IsZeroSuccessResult(object ret)
        {
            if (ret is bool b) return b;
            if (ret == null) return false;
            try
            {
                return Convert.ToInt32(ret, CultureInfo.InvariantCulture) == 0;
            }
            catch
            {
                string text = Convert.ToString(ret) ?? string.Empty;
                return text.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            }
        }

        private static double GetArrayValue(double[] values, int index)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : 0.0;
        }

    }
}
