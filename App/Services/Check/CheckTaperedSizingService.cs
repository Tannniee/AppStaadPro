using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StaadSteelApp.AISC360;

namespace StaadSteelApp.Core.Check
{
    internal sealed class CheckTaperedSizingCandidate
    {
        internal CheckTaperedSectionInfo Section { get; set; }
        internal double Ratio { get; set; }
        internal double Weight { get; set; }
    }

    internal static class CheckTaperedSizingService
    {
        internal static List<CheckTaperedSizingCandidate> OptimizeDiscrete(
            AnalyzedMember baseMemberTemplate,
            double targetRatio,
            double criticalLoc,
            CheckTaperedSectionInfo currentSection,
            bool keepDepthFixed)
        {
            var results = new List<CheckTaperedSizingCandidate>();
            var flanges = GetStandardFlanges();
            var validTw = GetStandardThicknesses();
            double criticalRatioAlong = ResolveCriticalRatioAlong(baseMemberTemplate, criticalLoc);

            bool requireTaper = currentSection != null && Math.Abs(currentSection.H1 - currentSection.H2) > 1e-9;
            double taperSign = requireTaper ? Math.Sign(currentSection.H1 - currentSection.H2) : 0.0;
            if (requireTaper && Math.Abs(taperSign) < 1e-9) taperSign = 1.0;
            double taperDelta = requireTaper ? Math.Abs(currentSection.H1 - currentSection.H2) : 0.0;
            if (requireTaper)
            {
                taperDelta = SnapUpToStepMm(taperDelta * 1000.0, 50.0) / 1000.0;
                taperDelta = Math.Max(0.100, taperDelta);
            }

            foreach (var fl in flanges)
            {
                double bf = fl.B;
                double tf = fl.T;
                double minHSearch = 0.300;
                double maxHSearch = 1.500;

                if (keepDepthFixed)
                {
                    if (currentSection == null) continue;

                    double h1Fixed = currentSection.H1;
                    double h2Fixed = currentSection.H2;
                    if (h1Fixed < minHSearch - 1e-9 || h1Fixed > maxHSearch + 1e-9 ||
                        h2Fixed < minHSearch - 1e-9 || h2Fixed > maxHSearch + 1e-9)
                        continue;

                    AddCandidatesForDepthPair(results, baseMemberTemplate, targetRatio, criticalRatioAlong, validTw, bf, tf, h1Fixed, h2Fixed);
                    continue;
                }

                for (double h = minHSearch; h <= maxHSearch + 1e-9; h += 0.050)
                {
                    double h1 = h;
                    double h2 = h;
                    if (requireTaper)
                    {
                        h2 = h - taperSign * taperDelta;
                        if (h2 < minHSearch - 1e-9 || h2 > maxHSearch + 1e-9)
                            continue;
                    }

                    AddCandidatesForDepthPair(results, baseMemberTemplate, targetRatio, criticalRatioAlong, validTw, bf, tf, h1, h2);
                }
            }

            results.Sort((a, b) => a.Weight.CompareTo(b.Weight));
            return results;
        }

        internal static List<CheckTaperedSizingCandidate> SelectDiverseCandidates(
            List<CheckTaperedSizingCandidate> candidates,
            int maxCount)
        {
            if (candidates == null || candidates.Count == 0 || maxCount <= 0)
                return new List<CheckTaperedSizingCandidate>();

            var sorted = candidates.OrderBy(c => c.Weight).ThenBy(c => c.Ratio).ToList();
            var selected = new List<CheckTaperedSizingCandidate>();
            var usedBfMm = new HashSet<int>();

            foreach (var candidate in sorted)
            {
                int bfMm = (int)Math.Round(candidate.Section.Bf * 1000.0);
                if (!usedBfMm.Add(bfMm)) continue;
                selected.Add(candidate);
                if (selected.Count >= maxCount) return selected;
            }

            foreach (var candidate in sorted)
            {
                if (selected.Count >= maxCount) break;
                if (!selected.Contains(candidate)) selected.Add(candidate);
            }

            return selected;
        }

        internal static string FormatSuggestedTaperedSection(CheckTaperedSectionInfo section)
        {
            return $"Tapered H1={section.H1 * 1000:F0}mm H2={section.H2 * 1000:F0}mm Bf={section.Bf * 1000:F0}mm Tf={section.Tf * 1000:F0}mm Tw={section.Tw * 1000:F0}mm Bfb={section.Bfb * 1000:F0}mm Tfb={section.Tfb * 1000:F0}mm";
        }

        private static void AddCandidatesForDepthPair(
            List<CheckTaperedSizingCandidate> results,
            AnalyzedMember baseMemberTemplate,
            double targetRatio,
            double criticalRatioAlong,
            double[] validTw,
            double bf,
            double tf,
            double h1,
            double h2)
        {
            double minTwChart = Math.Max(GetMinTwForDepth(h1), GetMinTwForDepth(h2));
            double bfMm = bf * 1000.0;
            double tfMm = tf * 1000.0;
            if (tfMm + 1e-9 < GetMinimumFlangeThicknessMm(bfMm))
                return;

            foreach (double tw in validTw)
            {
                if (tw < minTwChart) continue;

                var section = new CheckTaperedSectionInfo
                {
                    H1 = h1,
                    H2 = h2,
                    Bf = bf,
                    Tf = tf,
                    Bfb = bf,
                    Tfb = tf,
                    Tw = tw / 1000.0
                };
                if (!SatisfiesPracticalPlateProportion(section))
                    continue;

                var checkMember = CloneMember(baseMemberTemplate);
                ApplyTaperedCandidateProperties(checkMember, section, criticalRatioAlong);
                var checkResult = AISC360Calculator.PerformAllChecks(checkMember);

                if (checkResult.GoverningRatio <= targetRatio)
                {
                    if (ExceedsKlrLimit(checkMember, 180.0))
                        continue;

                    results.Add(new CheckTaperedSizingCandidate
                    {
                        Section = section,
                        Ratio = checkResult.GoverningRatio,
                        Weight = CalculateSectionArea(section)
                    });
                    break;
                }
            }
        }

        private static List<(double B, double T)> GetStandardFlanges()
        {
            var widths = new double[] { 125, 150, 165, 180, 200, 215, 250, 285, 300, 330, 400 };
            var thicknesses = GetStandardThicknesses();
            var flanges = new List<(double, double)>();

            foreach (var width in widths)
            {
                double minT;
                if (width == 125) minT = 5;
                else if (width == 150) minT = 5;
                else if (width == 165) minT = 6;
                else if (width == 180) minT = 6;
                else if (width == 200) minT = 6;
                else if (width == 215) minT = 8;
                else if (width == 250) minT = 8;
                else if (width == 285) minT = 10;
                else if (width == 300) minT = 10;
                else if (width == 330) minT = 10;
                else if (width == 400) minT = 12;
                else minT = 5;

                foreach (var thickness in thicknesses)
                {
                    if (thickness >= minT)
                        flanges.Add((width / 1000.0, thickness / 1000.0));
                }
            }

            return flanges;
        }

        private static double GetMinTwForDepth(double hM)
        {
            double hMm = hM * 1000.0;
            if (hMm >= 1200.0 - 1e-9) return 10.0;
            if (hMm > 900.0 + 1e-9) return 8.0;
            if (hMm > 500.0 + 1e-9) return 6.0;
            return 5.0;
        }

        private static double CalculateSectionArea(CheckTaperedSectionInfo section)
        {
            if (section == null) return double.MaxValue;

            double hAvg = (section.H1 + section.H2) * 1000.0 / 2.0;
            double bf = section.Bf * 1000.0;
            double tf = section.Tf * 1000.0;
            double bfb = section.Bfb * 1000.0;
            double tfb = section.Tfb * 1000.0;
            double tw = section.Tw * 1000.0;
            double hWeb = Math.Max(0.0, hAvg - tf - tfb);

            return (bf * tf) + (bfb * tfb) + (hWeb * tw);
        }

        private static double SnapUpToStepMm(double valueMm, double stepMm)
        {
            if (stepMm <= 0) return valueMm;
            return Math.Ceiling(valueMm / stepMm) * stepMm;
        }

        private static double[] GetStandardThicknesses()
        {
            return new double[] { 5, 6, 8, 10, 12, 14, 16, 18, 20, 25, 30, 40, 50 };
        }

        private static double ResolveCriticalRatioAlong(AnalyzedMember member, double criticalLoc)
        {
            if (criticalLoc >= 0.0 && criticalLoc <= 1.0)
                return criticalLoc;

            if (member != null && member.Length > 1e-9)
            {
                double ratioFromDistance = (criticalLoc * 1000.0) / member.Length;
                if (ratioFromDistance >= 0.0 && ratioFromDistance <= 1.0)
                    return ratioFromDistance;
            }

            return 0.5;
        }

        private static bool SatisfiesPracticalPlateProportion(CheckTaperedSectionInfo section)
        {
            if (section == null) return false;
            double twMm = section.Tw * 1000.0;
            double tfAvgMm = 0.5 * (section.Tf + section.Tfb) * 1000.0;
            return twMm + 1e-9 >= 0.4 * tfAvgMm;
        }

        private static double GetMinimumFlangeThicknessMm(double bfMm)
        {
            return bfMm > 200.0 + 1e-9 ? 10.0 : 8.0;
        }

        private static bool ExceedsKlrLimit(AnalyzedMember member, double limit)
        {
            if (member == null || limit <= 0) return false;
            member.CalculateDerived();
            double rx = member.rx;
            double ry = member.ry;
            if (rx <= 1e-9 || ry <= 1e-9) return true;

            double lx = member.Lx > 1e-9 ? member.Lx : member.Length;
            double ly = member.Ly > 1e-9 ? member.Ly : member.Length;
            double kx = member.Kx > 1e-9 ? member.Kx : 1.0;
            double ky = member.Ky > 1e-9 ? member.Ky : 1.0;

            double klrX = kx * lx / rx;
            double klrY = ky * ly / ry;
            double klr = Math.Max(klrX, klrY);
            return klr > limit;
        }

        private static AnalyzedMember CloneMember(AnalyzedMember source)
        {
            var clone = new AnalyzedMember();
            var props = typeof(AnalyzedMember).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in props)
            {
                if (property.CanRead && property.CanWrite)
                    property.SetValue(clone, property.GetValue(source, null), null);
            }

            return clone;
        }

        private static void ApplyTaperedCandidateProperties(AnalyzedMember member, CheckTaperedSectionInfo section, double ratioAlong)
        {
            ratioAlong = Math.Max(0.0, Math.Min(1.0, ratioAlong));

            double d = (section.H1 + (section.H2 - section.H1) * ratioAlong) * 1000.0;
            double bf = section.Bf * 1000.0;
            double tf = section.Tf * 1000.0;
            double bfb = section.Bfb * 1000.0;
            double tfb = section.Tfb * 1000.0;
            double tw = section.Tw * 1000.0;

            if (d <= 0 || bf <= 0 || tf <= 0 || bfb <= 0 || tfb <= 0 || tw <= 0)
                return;

            double hWeb = d - tf - tfb;
            if (hWeb <= 0) return;

            member.d = d;
            member.bf = bf;
            member.tf = tf;
            member.bfb = bfb;
            member.tfb = tfb;
            member.tw = tw;
            member.IsBuiltUp = true;

            double aBottom = bfb * tfb;
            double aWeb = tw * hWeb;
            double aTop = bf * tf;
            double area = aBottom + aWeb + aTop;
            if (area <= 0) return;

            double yBottom = tfb * 0.5;
            double yWeb = tfb + hWeb * 0.5;
            double yTop = tfb + hWeb + tf * 0.5;
            double yBar = (aBottom * yBottom + aWeb * yWeb + aTop * yTop) / area;

            double ixBottom = (bfb * Math.Pow(tfb, 3) / 12.0) + aBottom * Math.Pow(yBottom - yBar, 2);
            double ixWeb = (tw * Math.Pow(hWeb, 3) / 12.0) + aWeb * Math.Pow(yWeb - yBar, 2);
            double ixTop = (bf * Math.Pow(tf, 3) / 12.0) + aTop * Math.Pow(yTop - yBar, 2);

            double iyBottom = tfb * Math.Pow(bfb, 3) / 12.0;
            double iyWeb = hWeb * Math.Pow(tw, 3) / 12.0;
            double iyTop = tf * Math.Pow(bf, 3) / 12.0;

            member.Ag = area;
            member.Ix = ixBottom + ixWeb + ixTop;
            member.Iy = iyBottom + iyWeb + iyTop;

            double cTop = d - yBar;
            double cBottom = yBar;
            double sxTop = cTop > 0 ? member.Ix / cTop : 0;
            double sxBottom = cBottom > 0 ? member.Ix / cBottom : 0;
            member.Sx = Math.Min(sxTop, sxBottom);

            double cY = 0.5 * Math.Max(bf, bfb);
            member.Sy = cY > 0 ? member.Iy / cY : 0;

            member.Zx = member.Sx * 1.10;
            member.Zy = member.Sy * 1.10;

            member.ho = d - 0.5 * (tf + tfb);
            if (member.ho <= 0) member.ho = 0.9 * d;
            member.J = (1.0 / 3.0) * (bf * Math.Pow(tf, 3) + bfb * Math.Pow(tfb, 3) + hWeb * Math.Pow(tw, 3));
            member.Cw = (member.Iy * Math.Pow(member.ho, 2)) / 4.0;

            member.rx = 0;
            member.ry = 0;
            member.rts = 0;
        }
    }
}
