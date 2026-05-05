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
        private bool ApplyDesignParameters(int memberNo, AnalyzedMember am)
        {
            // Defaults if design parameters are not available.
            am.Lx = am.Length;
            am.Ly = am.Length;
            am.Lb = am.Length;

            if (!TryGetMemberDesignParameters(memberNo, out var parameters) || parameters.Count == 0)
                return false;

            // Design method can differ between briefs (LRFD/ASD).
            if (TryGetStringParam(parameters, "METHOD", out string method))
            {
                if (method.IndexOf("ASD", StringComparison.OrdinalIgnoreCase) >= 0)
                    am.Method = DesignMethod.ASD;
                else if (method.IndexOf("LRFD", StringComparison.OrdinalIgnoreCase) >= 0)
                    am.Method = DesignMethod.LRFD;
            }

            // Prefer material values from design parameters when present (units: kN/m2).
            if (TryGetPositiveDoubleParamWarn(parameters, memberNo, "FYLD", out double fyKnPerM2))
                am.Fy = fyKnPerM2 * 0.001; // kN/m2 -> MPa
            if (TryGetPositiveDoubleParamWarn(parameters, memberNo, "FU", out double fuKnPerM2))
                am.Fu = fuKnPerM2 * 0.001; // kN/m2 -> MPa

            // Compression effective lengths/K-factors.
            // STAAD local-z is typically major bending axis for steel members.
            if (TryGetPositiveLengthParamMmWarn(parameters, memberNo, "LZ", out double lzMm))
                am.Lx = lzMm;
            else if (TryGetPositiveLengthParamMmWarn(parameters, memberNo, "LX", out double lxMm))
                am.Lx = lxMm;

            if (TryGetPositiveLengthParamMmWarn(parameters, memberNo, "LY", out double lyMm))
                am.Ly = lyMm;

            if (TryGetPositiveDoubleParamWarn(parameters, memberNo, "KZ", out double kz))
                am.Kx = kz;
            else if (TryGetPositiveDoubleParamWarn(parameters, memberNo, "KX", out double kx))
                am.Kx = kx;

            if (TryGetPositiveDoubleParamWarn(parameters, memberNo, "KY", out double ky))
                am.Ky = ky;

            if (am.Lx <= 0) am.Lx = am.Length;
            if (am.Ly <= 0) am.Ly = am.Length;
            if (am.Kx <= 0) am.Kx = 1.0;
            if (am.Ky <= 0) am.Ky = 1.0;

            // Flexural unbraced length for major-axis LTB.
            // Prefer UNL; otherwise use top/bottom flange bracing lengths.
            bool hasUnl = TryGetPositiveLengthParamMmWarn(parameters, memberNo, "UNL", out double unlMm);
            bool hasUnt = TryGetPositiveLengthParamMmWarn(parameters, memberNo, "UNT", out double untMm);
            bool hasUnb = TryGetPositiveLengthParamMmWarn(parameters, memberNo, "UNB", out double unbMm);

            am.ParamUNL = hasUnl ? unlMm : 0.0;
            am.ParamUNT = hasUnt ? untMm : 0.0;
            am.ParamUNB = hasUnb ? unbMm : 0.0;

            if (hasUnl)
            {
                am.Lb = unlMm;
                am.LbSource = "UNL";
            }
            else if (hasUnt && hasUnb)
            {
                am.Lb = Math.Max(untMm, unbMm);
                am.LbSource = "max(UNT,UNB)";
            }
            else if (hasUnt)
            {
                am.Lb = untMm;
                am.LbSource = "UNT";
            }
            else if (hasUnb)
            {
                am.Lb = unbMm;
                am.LbSource = "UNB";
            }

            if (am.Lb <= 0)
            {
                am.Lb = am.Length;
                am.LbSource = "Length(default)";
            }

            bool hasCb = false;
            if (TryGetPositiveDoubleParamWarn(parameters, memberNo, "CB", out double cb))
            {
                am.Cb = cb;
                hasCb = true;
            }

            return hasCb;
        }

        private bool TryGetMemberDesignParameters(int memberNo, out Dictionary<string, string> parameters)
        {
            if (_memberDesignParamCache.TryGetValue(memberNo, out parameters))
                return parameters != null && parameters.Count > 0;

            parameters = null;
            if (!TryGetAiscDesignBriefRefs(out List<int> briefRefs) || briefRefs.Count == 0)
            {
                WarnOnce($"AiscBriefRefs:{memberNo}", $"Member {memberNo}: could not read AISC Design Brief references from STAAD. Design parameters will use defaults.");
                _memberDesignParamCache[memberNo] = null;
                return false;
            }

            if (!OpenStaadDesignAdapter.TryGetDesignObject(_os, out object design, out string designError))
            {
                WarnOnce($"OpenStaad.Design:{memberNo}", $"Member {memberNo}: could not access OpenSTAAD 'Design' object. Design parameters will use defaults. {designError}");
                _memberDesignParamCache[memberNo] = null;
                return false;
            }

            string lastError = string.Empty;
            foreach (int briefRef in briefRefs)
            {
                if (OpenStaadDesignAdapter.TryGetMemberDesignParameters(design, briefRef, memberNo, out var parsed, out string parameterError))
                {
                    if (parsed.Count > 0)
                    {
                        _memberDesignParamCache[memberNo] = parsed;
                        parameters = parsed;
                        return true;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(parameterError))
                {
                    lastError = parameterError;
                }
            }

            WarnOnce($"MemberDesignParams:{memberNo}", string.IsNullOrWhiteSpace(lastError)
                ? $"Member {memberNo}: could not read STAAD member design parameters. Defaults will be used."
                : $"Member {memberNo}: could not read STAAD member design parameters. Defaults will be used. {lastError}");
            _memberDesignParamCache[memberNo] = null;
            return false;
        }

        private bool TryGetAiscDesignBriefRefs(out List<int> briefRefs)
        {
            if (_aiscBriefRefs != null && _aiscBriefRefs.Count > 0)
            {
                briefRefs = _aiscBriefRefs;
                return true;
            }

            briefRefs = null;
            string designError;
            if (!OpenStaadDesignAdapter.TryGetDesignObject(_os, out object design, out designError))
                return false;

            var preferred = new List<int>(); // 1067 first
            var fallback = new List<int>();  // older AISC codes

            for (int i = 0; i < 100; i++)
            {
                if (!OpenStaadDesignAdapter.TryGetDesignBriefCode(design, i, out int code, out _))
                    continue;

                if (code == 1067)
                    preferred.Add(i);
                else if (code == 1061 || code == 1045 || code == 1011 || code == 1002)
                    fallback.Add(i);
            }

            preferred.AddRange(fallback);
            if (preferred.Count == 0)
                return false;

            _aiscBriefRefs = preferred;
            briefRefs = _aiscBriefRefs;
            return true;
        }

        private object GetOpenStaadSubObject(string propertyName)
        {
            if (_os == null || string.IsNullOrWhiteSpace(propertyName)) return null;

            if (ComInvoke.TryGetProperty(_os, propertyName, out object result, out string error))
                return result;

            if (!string.IsNullOrWhiteSpace(error))
            {
                WarnOnce("OpenStaadSubObject:" + propertyName, $"Could not access OpenSTAAD sub-object '{propertyName}'. {error}");
            }

            return null;
        }

    }
}
