using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using StaadSteelApp.AISC360;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public partial class StaadToAiscMapper
    {
        private const double KnToN = 1000.0;
        private const double MToMm = 1000.0;
        private const double KnmToNmm = 1000000.0;
        private const double M2ToMm2 = 1000000.0;
        private const double M4ToMm4 = 1000000000000.0;

        private readonly object _os;
        private readonly Dictionary<int, Dictionary<string, string>> _memberDesignParamCache =
            new Dictionary<int, Dictionary<string, string>>();
        private List<int> _aiscBriefRefs;

        private const int MaxWarningSamples = 200;
        private int _warningCount;
        private readonly List<string> _warningSamples = new List<string>();
        private readonly HashSet<string> _warnOnceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public int WarningCount => _warningCount;
        public IReadOnlyList<string> WarningSamples => _warningSamples;

        public StaadToAiscMapper(object openStaad)
        {
            _os = openStaad;
        }

        public string GetWarningSummary(int maxLines = 10)
        {
            int lines = Math.Max(0, maxLines);
            var sb = new System.Text.StringBuilder();
            sb.Append("STAAD mapping warnings: ").Append(_warningCount);

            int take = Math.Min(lines, _warningSamples.Count);
            for (int i = 0; i < take; i++)
            {
                sb.AppendLine();
                sb.Append(_warningSamples[i]);
            }

            if (_warningCount > _warningSamples.Count)
            {
                sb.AppendLine();
                sb.Append("... (more not shown)");
            }

            return sb.ToString();
        }

        private void Warn(string message, Exception ex = null)
        {
            _warningCount++;
            if (_warningSamples.Count < MaxWarningSamples)
            {
                string msg = ex == null ? message : (message + " | " + ex.Message);
                _warningSamples.Add(msg);
            }

            if (ex == null)
                Debug.WriteLine("STAAD mapper warning: " + message);
            else
                Debug.WriteLine("STAAD mapper warning: " + message + " | " + ex);
        }

        private void WarnOnce(string key, string message, Exception ex = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                Warn(message, ex);
                return;
            }

            if (_warnOnceKeys.Add(key))
                Warn(message, ex);
        }
 
        public AnalyzedMember MapMember(int memberNo, int loadCase, MemberForceData forces)
        {
            var am = new AnalyzedMember();
            am.FrameName = memberNo.ToString();
            am.Method = DesignMethod.LRFD; // Default
            am.SectionName = TryGetBeamSectionName(memberNo);
            if (string.IsNullOrWhiteSpace(am.SectionName))
                WarnOnce($"SectionNameBlank:{memberNo}", $"Member {memberNo}: STAAD section name is blank/unknown. Section mapping may be incomplete.");

            // 1. Geography & Length
            double lengthMm = 0;
            if (OpenStaadGeometryAdapter.TryGetBeamLength(_os, memberNo, out double lengthM, out string lengthError))
            {
                lengthMm = lengthM * MToMm; // m -> mm
            }
            else
            {
                WarnOnce($"BeamLength:{memberNo}", $"Member {memberNo}: failed to read length from STAAD. Results may be incorrect. {lengthError}");
                lengthMm = 0;
            }

            am.Length = lengthMm;
            if (am.Length <= 0 || double.IsNaN(am.Length) || double.IsInfinity(am.Length))
                WarnOnce($"BeamLength:{memberNo}", $"Member {memberNo}: STAAD returned an invalid length ({am.Length}). Results may be incorrect.");

            // 2. Parse Distance for Interpolation
            double dist = 0;
            // Node format expected: "Start", "End", or "At X.XXX"
            if (forces.Node == "End")
            {
                dist = am.Length;
            }
            else if (forces.Node != null && forces.Node.StartsWith("At "))
            {
                // Try parse "At 1.500"
                string distStr = forces.Node.Substring(3).Trim();
                if (TryParseDouble(distStr, out double dVal))
                {
                    dist = dVal * MToMm; // m -> mm
                }
                else
                {
                    WarnOnce($"ForceNodeParse:{memberNo}:{forces.Node}",
                        $"Member {memberNo}: could not parse force station '{forces.Node}'. Using Start location for interpolation.");
                }
            }

            // 3. Map Forces (Input ForceData is in Base Units: kN, m)
            // Internal AISC engine convention:
            //   force = N, moment = N-mm, length = mm, stress = MPa.
            // Sign convention:
            //   compression < 0, tension > 0 (same convention used by AISC360Calculator).
            am.Pr = forces.Fx * KnToN;
            am.Mrx = forces.Mz * KnmToNmm;
            am.Mry = forces.My * KnmToNmm;
            am.Tr = forces.Mx * KnmToNmm;
            am.Vrx = forces.Fy * KnToN;
            am.Vry = forces.Fz * KnToN;

            // 4. Section Properties (will convert to mm and interpolate if Tapered)
            MapSectionProperties(memberNo, am, dist);
            if (string.IsNullOrWhiteSpace(am.SectionName))
                am.SectionName = TryGetBeamSectionName(memberNo);
            ApplySectionTypeFlags(am);

            // 5. Material Properties (will convert to MPa)
            MapMaterialProperties(memberNo, am);

            // 6. Prefer STAAD design-brief parameters (K/L/Cb/METHOD/Fy/Fu) when available.
            // This keeps the manual check aligned with STAAD's own steel design setup.
            bool hasDesignCb = ApplyDesignParameters(memberNo, am);

            // If Cb is not explicitly provided by STAAD design parameters,
            // estimate it from the member moment gradient.
            if (!hasDesignCb)
                MapMomentGradientParameters(memberNo, loadCase, am);

            if (am.Ag <= 0 || am.Ix <= 0 || am.Iy <= 0)
                WarnOnce($"SectionPropsInvalid:{memberNo}", $"Member {memberNo}: mapped section properties look invalid (Ag/Ix/Iy <= 0). Results may be meaningless.");
            if (am.Fy <= 0 || am.E <= 0)
                WarnOnce($"MaterialPropsInvalid:{memberNo}", $"Member {memberNo}: mapped material properties look invalid (Fy/E <= 0). Defaults may be wrong for this member.");

            return am;
        }

    }
}
