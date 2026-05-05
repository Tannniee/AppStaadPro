using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using Microsoft.Win32;
using StaadSteelApp.Core;
using StaadSteelApp.AISC360;

namespace StaadSteelApp
{
    public partial class CheckForm : Form
    {
        private void PerformCalculation()
        {
            if (cboFrame.SelectedItem == null) return;
            
            try
            {
                Cursor = Cursors.WaitCursor;
                
                int memberNo = int.Parse(cboFrame.SelectedItem.ToString());
                var memberForces = _allForces?.Where(f => f.MemberNo == memberNo).ToList() ?? new List<MemberForceData>();

                // Mapper
                var mapper = new StaadToAiscMapper(_os);
                _currentSteelInfo = null;
                _currentGoverningForce = null;
                _currentManualSource = string.Empty;
                _currentForceEnvelope = new List<ForceEnvelopeEntry>();

                // Prefer STAAD steel design governing combo/section if available
                DesignResults worstResults = null;
                double maxRatio = -1.0;
                MemberForceData governingForce = null;

                if (TryGetMemberSteelDesignResults(memberNo, out DesignResultInfo steelInfo))
                {
                    _currentSteelInfo = steelInfo;
                    _currentManualSource = "STAAD critical design result";
                    governingForce = GetCriticalSectionForces(memberNo, steelInfo);
                    if (governingForce != null)
                    {
                        var analyzedMember = mapper.MapMember(governingForce.MemberNo, governingForce.LoadCase, governingForce);
                        analyzedMember.IsBuiltUp = chkIsBuiltUp.Checked;
                        if (string.IsNullOrWhiteSpace(analyzedMember.SectionName))
                            analyzedMember.SectionName = TryGetSectionName(governingForce.MemberNo);
                        worstResults = AISC360Calculator.PerformAllChecks(analyzedMember);
                        maxRatio = worstResults.GoverningRatio;
                    }

                    _staadDesignRatio = steelInfo.CriticalRatio;
                    _staadCriticalLoadCase = steelInfo.CriticalLoadCase;
                }
                else
                {
                    _staadDesignRatio = null;
                    _staadCriticalLoadCase = null;
                    _currentManualSource = "Extracted analysis envelope scan";

                    if (memberForces.Count == 0)
                        return;

                    foreach (var force in memberForces)
                    {
                        var analyzedMember = mapper.MapMember(force.MemberNo, force.LoadCase, force);
                        analyzedMember.IsBuiltUp = chkIsBuiltUp.Checked;
                        if (string.IsNullOrWhiteSpace(analyzedMember.SectionName))
                            analyzedMember.SectionName = TryGetSectionName(force.MemberNo);
                        var results = AISC360Calculator.PerformAllChecks(analyzedMember);

                        if (results.GoverningRatio > maxRatio)
                        {
                            maxRatio = results.GoverningRatio;
                            worstResults = results;
                            governingForce = force;
                        }
                    }
                }

                _currentResults = worstResults;
                _currentGoverningForce = governingForce;
                _currentForceEnvelope = BuildMemberForceEnvelope(memberNo);

                // Update UI Labels
                if (_currentResults != null)
                {
                    lblSection.Text = $"Section: {_currentResults.Member.SectionName ?? "Unknown"}"; // Stacked Mapper doesn't set Name yet?
                    lblMaterial.Text = $"Material: {TryGetMaterialName(memberNo)}";
                    int comboLc = _staadCriticalLoadCase ?? governingForce?.LoadCase ?? 0;
                    lblCombo.Text = comboLc > 0 ? $"Combo: {comboLc}" : "Combo: --";
                    
                    if (governingForce != null)
                    {
                        lblForces.Text = $"P={governingForce.Fx:F1} kN   M3={governingForce.Mz:F1} kN-m   M2={governingForce.My:F1} kN-m   V2={governingForce.Fy:F1} kN   V3={governingForce.Fz:F1} kN";
                    }
                }
                
                 PopulateSummaryGrid();
                 UpdateComparison();

                 if (mapper.WarningCount > 0)
                 {
                     TopMostMessageBox.ShowWarning(mapper.GetWarningSummary(5), "STAAD Mapping Warning");
                 }

             }
             catch (Exception ex)
             {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void RunTaperedSizing()
        {
            if (cboFrame.SelectedItem == null)
            {
                LoadSelectedBeams(true);
                if (cboFrame.SelectedItem == null)
                    return;
            }

            if (!double.TryParse(txtTargetRatio.Text, out double targetRatio) || targetRatio <= 0 || targetRatio >= 1.5)
            {
                MessageBox.Show("Target D/C must be a number between 0 and 1.5 (recommended <= 1.0).",
                    "Invalid Target", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int memberNo = int.Parse(cboFrame.SelectedItem.ToString());
            if (!TryGetMemberSteelDesignResults(memberNo, out DesignResultInfo steelInfo))
            {
                MessageBox.Show("Could not extract governing steel design result from STAAD for this member.",
                    "Sizing Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var governingForce = GetCriticalSectionForces(memberNo, steelInfo);
            if (governingForce == null)
            {
                MessageBox.Show("Could not extract critical-section forces for sizing.",
                    "Sizing Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

             if (!TryGetTaperedSectionData(memberNo, out TaperedSectionData currentTapered))
            {
                MessageBox.Show("This sizing tool currently supports tapered/built-up sections only.",
                    "Sizing Scope", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Prepare base member for checking
            var mapper = new StaadToAiscMapper(_os);
            var baseMember = mapper.MapMember(governingForce.MemberNo, governingForce.LoadCase, governingForce);
            baseMember.IsBuiltUp = true;
            if (string.IsNullOrWhiteSpace(baseMember.SectionName))
                baseMember.SectionName = TryGetSectionName(governingForce.MemberNo);

            if (mapper.WarningCount > 0)
            {
                TopMostMessageBox.ShowWarning(mapper.GetWarningSummary(5), "STAAD Mapping Warning");
            }

             // Discrete Optimization (2 modes):
             // 1) Full variable search (H1/H2/Tw/Bf/Tf all can change)
             // 2) Keep original H1/H2, vary only Tw/Bf/Tf
             var candidatesFull = OptimizeDiscrete(baseMember, governingForce, targetRatio, steelInfo.CriticalSection, currentTapered, keepDepthFixed: false);
             var candidatesKeepDepth = OptimizeDiscrete(baseMember, governingForce, targetRatio, steelInfo.CriticalSection, currentTapered, keepDepthFixed: true);

             var topFull = SelectDiverseCandidates(candidatesFull, 3);
             var topKeepDepth = SelectDiverseCandidates(candidatesKeepDepth, 3);

            if (topFull.Count == 0 && topKeepDepth.Count == 0)
            {
                 MessageBox.Show("Could not find any sizing solution within the Standard Geometry Limits.", "Sizing Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                 return;
            }

            // Update UI with options from both modes
            var sb = new StringBuilder();
            sb.AppendLine("=== TAPERED AUTO-SIZE OPTIONS (Standard Geometry) ===");
            sb.AppendLine($"Target Ratio: {targetRatio:F3}");
            sb.AppendLine("Mode A: Change H1/H2/Tw/Bf/Tf (Bfb=Bf, Tfb=Tf)");
            sb.AppendLine();

            int count = 0;
            foreach (var opt in topFull)
            {
                count++;
                string sectionText = FormatSuggestedTaperedSection(opt.Sec);
                sb.AppendLine($"OPTION {count} (Ratio: {opt.Ratio:F3}, Area: {opt.Weight:F0})");
                sb.AppendLine($"   {sectionText}");
                sb.AppendLine();
            }

            if (topFull.Count == 0)
            {
                sb.AppendLine("No valid option found for Mode A.");
                sb.AppendLine();
            }

            sb.AppendLine("Mode B: Keep H1/H2, change Tw/Bf/Tf (Bfb=Bf, Tfb=Tf)");
            sb.AppendLine();

            foreach (var opt in topKeepDepth)
            {
                count++;
                string sectionText = FormatSuggestedTaperedSection(opt.Sec);
                sb.AppendLine($"OPTION {count} (Ratio: {opt.Ratio:F3}, Area: {opt.Weight:F0})");
                sb.AppendLine($"   {sectionText}");
                sb.AppendLine();
            }

            if (topKeepDepth.Count == 0)
            {
                sb.AppendLine("No valid option found for Mode B.");
                sb.AppendLine();
            }
             
            sb.AppendLine("--- Rules applied ---");
            sb.AppendLine("- Standard Flanges: (125x5 ... 400x20) from Chart");
            sb.AppendLine("- Web Constraints: Min Tw based on Depth (Chart)");
            sb.AppendLine("- Geometry Limit: Full Table Range (300-1500mm)");
            sb.AppendLine("- Standard Tw: 5,6,8,10,12,14,16,18,20,25,30,40,50");
            
            sb.AppendLine();
            sb.AppendLine("--- Search Log ---");
            // sb.Append(log.ToString()); // Logging removed for cleanliness, can add back if needed
            
            sb.AppendLine();
            sb.AppendLine(txtDetails.Text);
            txtDetails.Text = sb.ToString();
        }

        private class OptimizationCandidate
        {
            public TaperedSectionData Sec;
            public double Ratio;
            public double Weight;
        }

        private List<OptimizationCandidate> OptimizeDiscrete(
            AnalyzedMember baseMemberTemplate,
            MemberForceData forces,
            double targetRatio,
            double criticalLoc,
            TaperedSectionData currentSection,
            bool keepDepthFixed)
        {
            var results = new List<OptimizationCandidate>();
            var flanges = GetStandardFlanges();
            var validTw = GetStandardThicknesses();
            double criticalRatioAlong = ResolveCriticalRatioAlong(baseMemberTemplate, criticalLoc);
            bool requireTaper = currentSection != null && Math.Abs(currentSection.H1 - currentSection.H2) > 1e-9;
            double taperSign = requireTaper ? Math.Sign(currentSection.H1 - currentSection.H2) : 0.0;
            if (requireTaper && Math.Abs(taperSign) < 1e-9) taperSign = 1.0;
            double taperDelta = requireTaper ? Math.Abs(currentSection.H1 - currentSection.H2) : 0.0;
            if (requireTaper)
            {
                // Preserve practical taper magnitude from input on 50 mm grid (not tiny 50 mm by default).
                taperDelta = SnapUpToStepMm(taperDelta * 1000.0, 50.0) / 1000.0;
                taperDelta = Math.Max(0.100, taperDelta);
            }

            // Search Strategy:
            // For each Standard Flange, find the Minimum Depth (H) and Minimum Web Thickness (Tw) that satisfies:
            // 1. Geometry Rules (min Tw for H, H/B limits)
            // 2. Structural Capacity (Ratio <= Target)
            
            foreach (var fl in flanges)
            {
                double bf = fl.B;
                double tf = fl.T;

                double minH_search = 0.300; // Min chart depth
                double maxH_search = 1.500; // Max chart depth

                if (keepDepthFixed)
                {
                    if (currentSection == null) continue;

                    double h1Fixed = currentSection.H1;
                    double h2Fixed = currentSection.H2;
                    if (h1Fixed < minH_search - 1e-9 || h1Fixed > maxH_search + 1e-9 ||
                        h2Fixed < minH_search - 1e-9 || h2Fixed > maxH_search + 1e-9)
                        continue;

                    AddCandidatesForDepthPair(results, baseMemberTemplate, targetRatio, criticalRatioAlong, validTw, bf, tf, h1Fixed, h2Fixed);
                    continue;
                }

                // User clarified that "Red Zone" limits (H/B constraints) are advisory only.
                // We allow the full range of Depths (300 to 1500) for ALL flange sizes.
                // Validity is determined solely by AISC design checks.
                for (double h = minH_search; h <= maxH_search + 1e-9; h += 0.050)
                {
                    double h1 = h;
                    double h2 = h;
                    if (requireTaper)
                    {
                        h2 = h - taperSign * taperDelta;
                        if (h2 < minH_search - 1e-9 || h2 > maxH_search + 1e-9)
                            continue;
                    }

                    AddCandidatesForDepthPair(results, baseMemberTemplate, targetRatio, criticalRatioAlong, validTw, bf, tf, h1, h2);
                }
            }

            // Sort results
            results.Sort((a,b) => a.Weight.CompareTo(b.Weight));
            return results;
        }

        private static void AddCandidatesForDepthPair(
            List<OptimizationCandidate> results,
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

                var sec = new TaperedSectionData
                {
                    H1 = h1,
                    H2 = h2,
                    Bf = bf,
                    Tf = tf,
                    Bfb = bf,
                    Tfb = tf,
                    Tw = tw / 1000.0
                };
                if (!SatisfiesPracticalPlateProportion(sec))
                    continue;

                var checkMember = CloneMember(baseMemberTemplate);
                ApplyTaperedCandidateProperties(checkMember, sec, criticalRatioAlong);
                var res = AISC360Calculator.PerformAllChecks(checkMember);

                if (res.GoverningRatio <= targetRatio)
                {
                    if (ExceedsKlrLimit(checkMember, 180.0))
                        continue;

                    results.Add(new OptimizationCandidate
                    {
                        Sec = sec,
                        Ratio = res.GoverningRatio,
                        Weight = CalculateSectionArea(sec)
                    });
                    break;
                }
            }
        }

        private static List<OptimizationCandidate> SelectDiverseCandidates(List<OptimizationCandidate> candidates, int maxCount)
        {
            if (candidates == null || candidates.Count == 0 || maxCount <= 0)
                return new List<OptimizationCandidate>();

            var sorted = candidates.OrderBy(c => c.Weight).ThenBy(c => c.Ratio).ToList();
            var selected = new List<OptimizationCandidate>();
            var usedBfMm = new HashSet<int>();

            foreach (var c in sorted)
            {
                int bfMm = (int)Math.Round(c.Sec.Bf * 1000.0);
                if (!usedBfMm.Add(bfMm)) continue;
                selected.Add(c);
                if (selected.Count >= maxCount) return selected;
            }

            foreach (var c in sorted)
            {
                if (selected.Count >= maxCount) break;
                if (!selected.Contains(c)) selected.Add(c);
            }

            return selected;
        }

        private static List<(double B, double T)> GetStandardFlanges()
        {
            // Standard Widths
            var widths = new double[] { 125, 150, 165, 180, 200, 215, 250, 285, 300, 330, 400 };
            
            // Standard Thicknesses
            var thicks = GetStandardThicknesses(); // 5, 6, 8, ... 50

            var list = new List<(double, double)>();

            foreach (var b in widths)
            {
                // Determine Min Thickness for this Width (based on chart)
                double minT = 0;
                if (b == 125) minT = 5;
                else if (b == 150) minT = 5;
                else if (b == 165) minT = 6;
                else if (b == 180) minT = 6;
                else if (b == 200) minT = 6;
                else if (b == 215) minT = 8;
                else if (b == 250) minT = 8;
                else if (b == 285) minT = 10;
                else if (b == 300) minT = 10;
                else if (b == 330) minT = 10;
                else if (b == 400) minT = 12; // Chart shows start at 400x12
                else minT = 5; // Fallback

                foreach (var t in thicks)
                {
                    if (t >= minT)
                    {
                        list.Add((b / 1000.0, t / 1000.0)); // Store as meters
                    }
                }
            }
            return list;
        }

        private static double GetMinTwForDepth(double hM)
        {
            // Practical shop rule:
            // H <= 500 -> 5mm, H > 500 -> 6mm, H > 900 -> 8mm, H >= 1200 -> 10mm.
            double hMm = hM * 1000.0;
            if (hMm >= 1200.0 - 1e-9) return 10.0;
            if (hMm > 900.0 + 1e-9) return 8.0;
            if (hMm > 500.0 + 1e-9) return 6.0;
            return 5.0;
        }



        private enum SizingStrategy
        {
            Balanced,   // Scale all dimensions
            DepthOnly,  // Scale H1, H2 only
            FlangeOnly  // Scale Bf, Tf, Bfb, Tfb only
        }

        private static double CalculateSectionArea(TaperedSectionData sec)
        {
            // Simple Area approximation in mm2 for weight comparison
            if (sec == null) return double.MaxValue;
            // A_flanges + A_web (using Avg depth)
            double h_avg = (sec.H1 + sec.H2) * 1000.0 / 2.0;
            double bf = sec.Bf * 1000.0;
            double tf = sec.Tf * 1000.0;
            double bfb = sec.Bfb * 1000.0;
            double tfb = sec.Tfb * 1000.0;
            double tw = sec.Tw * 1000.0;
            double hWeb = Math.Max(0.0, h_avg - tf - tfb);

             return (bf * tf) + (bfb * tfb) + (hWeb * tw);
        }


        private static double EvaluateSuggestedRatio(AnalyzedMember baseMember, TaperedSectionData suggested, double ratioAlong)
        {
            if (baseMember == null || suggested == null) return double.PositiveInfinity;
            var candidate = CloneMember(baseMember);
            ApplyTaperedCandidateProperties(candidate, suggested, ratioAlong);
            var res = AISC360Calculator.PerformAllChecks(candidate);
            return res?.GoverningRatio ?? double.PositiveInfinity;
        }

        private bool TryOptimizeSection(
            AnalyzedMember baseMember,
            TaperedSectionData baseSection,
            double criticalRatioAlong,
            double targetRatio,
            SizingStrategy strategy,
            out double scaleFactor,
            out double predictedRatio,
            out string failReason,
            out TaperedSectionData finalSection)
        {
            scaleFactor = 1.0;
            predictedRatio = double.PositiveInfinity;
            failReason = "";
            finalSection = null;

            double Evaluate(double k)
            {
                var candidateSection = ScaleTaperedSection(baseSection, k, strategy);
                candidateSection = SnapDepthToManufacturingGrid(candidateSection);
                candidateSection = SnapToAllowedFlangeSize(candidateSection);
                
                // Enforce symmetry constraints requested by user
                EnforceSymmetry(candidateSection);

                if (!SatisfiesMinimumWebThicknessRule(candidateSection))
                    return double.PositiveInfinity;
                if (!IsValidTaperedGeometryAtRatio(candidateSection, criticalRatioAlong))
                    return double.PositiveInfinity;

                var candidate = CloneMember(baseMember);
                ApplyTaperedCandidateProperties(candidate, candidateSection, criticalRatioAlong);
                var res = AISC360Calculator.PerformAllChecks(candidate);
                double ratio = res?.GoverningRatio ?? double.PositiveInfinity;
                if (double.IsNaN(ratio) || double.IsInfinity(ratio))
                    return double.PositiveInfinity;
                return ratio;
            }

            double baseRatio = Evaluate(1.0);
            if (double.IsNaN(baseRatio) || double.IsInfinity(baseRatio))
            {
                failReason = "Base section invalid.";
                return false;
            }

            // If base ratio already good, try to shrink (k < 1.0)
            // If base ratio bad, grow (k > 1.0)
            
            double low = 0.5; 
            double high = 5.0; // Allow huge range
            
            // Refine bounds based on direction
            if (baseRatio <= targetRatio)
            {
                // Passing -> Try to shrink
                high = 1.0;
            }
            else
            {
                // Failing -> Must grow
                low = 1.0;
            }

            // Binary search for optimal k where Ratio is close to Target
            // Objective: Find smallest k (if growing) or largest k (if shrinking) that satisfies Target.
            // Actually simplest is: Find k such that Ratio(k) <= Target.
            
            // Search for "Border" k
            for (int i = 0; i < 20; i++)
            {
                double mid = 0.5 * (low + high);
                double r = Evaluate(mid);
                if (double.IsNaN(r) || double.IsInfinity(r))
                {
                    // Invalid candidate => treat as unsafe/too weak.
                    low = mid;
                    continue;
                }
                
                if (r > targetRatio)
                {
                    // Too weak -> Need larger section
                    low = mid;
                }
                else
                {
                     // Strong enough -> Can we go smaller?
                     high = mid;
                }
            }
            
            // Pick 'high' which is the safe bound
            scaleFactor = high;
            predictedRatio = Evaluate(scaleFactor);
            finalSection = ScaleTaperedSection(baseSection, scaleFactor, strategy);
            finalSection = SnapDepthToManufacturingGrid(finalSection);
            finalSection = SnapToAllowedFlangeSize(finalSection);
            EnforceSymmetry(finalSection); // consisteny
            if (!SatisfiesMinimumWebThicknessRule(finalSection))
            {
                failReason = "Final section violates minimum web thickness rule.";
                return false;
            }
            if (!IsValidTaperedGeometryAtRatio(finalSection, criticalRatioAlong))
            {
                failReason = "Final section has invalid geometry at critical location.";
                return false;
            }
            
            if (double.IsNaN(predictedRatio) || double.IsInfinity(predictedRatio))
            {
                failReason = "Predicted ratio invalid.";
                return false;
            }
            if (predictedRatio > targetRatio * 1.05) // loose tolerance
            {
                failReason = $"Could not converge (Best Ratio {predictedRatio:F2})";
                  return false;
            }
            
            return true;
        }

        private static void EnforceSymmetry(TaperedSectionData sec)
        {
            if (sec == null) return;
            // Force Bottom = Top
            sec.Bfb = sec.Bf;
            sec.Tfb = sec.Tf;
        }

        private static TaperedSectionData ScaleTaperedSection(TaperedSectionData input, double k, SizingStrategy strategy)
        {
            var res = new TaperedSectionData
            {
                H1 = input.H1,
                H2 = input.H2,
                Tw = input.Tw,
                Bf = input.Bf,
                Tf = input.Tf,
                Bfb = input.Bfb,
                Tfb = input.Tfb
            };

            switch (strategy)
            {
                case SizingStrategy.Balanced:
                    res.H1 *= k;
                    res.H2 *= k;
                    res.Tw *= k;
                    res.Bf *= k;
                    res.Tf *= k;
                    res.Bfb *= k;
                    res.Tfb *= k;
                    break;
                case SizingStrategy.DepthOnly:
                    res.H1 *= k;
                    res.H2 *= k;
                    // Keep flanges fixed
                    break;
                case SizingStrategy.FlangeOnly:
                    // Keep Depth fixed
                    res.Bf *= k;
                    res.Tf *= k;
                    res.Bfb *= k;
                    res.Tfb *= k;
                    // Ideally check Tw too? Let's keep Tw fixed for FlangeOnly or scale?
                    // User said "Gi? H1 H2 hi?n t?i tang c�c Bf Tf Tw Bfb Tfb", so scale Tw too.
                    res.Tw *= k;
                    break;
            }
            return res;
        }

        private static TaperedSectionData SnapDepthToManufacturingGrid(TaperedSectionData sec)
        {
            if (sec == null) return sec;

            double h1Orig = sec.H1;
            double h2Orig = sec.H2;
            sec.H1 = SnapUpToStepMm(sec.H1 * 1000.0, 50.0) / 1000.0;
            sec.H2 = SnapUpToStepMm(sec.H2 * 1000.0, 50.0) / 1000.0;
            
            // Snap Tw to standard thicknesses
            sec.Tw = SnapToNearestSet(sec.Tw * 1000.0, GetStandardThicknesses()) / 1000.0;

            // Preserve tapered character if input was tapered but rounding makes it prismatic.
            if (Math.Abs(h1Orig - h2Orig) > 1e-6 && Math.Abs(sec.H1 - sec.H2) < 1e-9)
            {
                if (h1Orig > h2Orig)
                    sec.H1 += 0.05; // 50 mm
                else
                    sec.H2 += 0.05; // 50 mm
            }
            return sec;
        }

        private static double SnapUpToStepMm(double valueMm, double stepMm)
        {
            if (stepMm <= 0) return valueMm;
            return Math.Ceiling(valueMm / stepMm) * stepMm;
        }

        private static TaperedSectionData SnapToAllowedFlangeSize(TaperedSectionData sec)
        {
            if (sec == null) return sec;

            double bfTarget = 0.5 * (sec.Bf + sec.Bfb) * 1000.0;
            double tfTarget = 0.5 * (sec.Tf + sec.Tfb) * 1000.0;

            double bestBf = SnapToNearestSet(bfTarget, GetStandardFlangeWidths());
            double bestTf = SnapToNearestSet(tfTarget, GetStandardThicknesses());

            sec.Bf = bestBf / 1000.0;
            sec.Bfb = bestBf / 1000.0;
            sec.Tf = bestTf / 1000.0;
            sec.Tfb = bestTf / 1000.0;
            return sec;
        }

        private static double SnapToNearestSet(double target, double[] allowed)
        {
             if (allowed == null || allowed.Length == 0) return target;
             double best = allowed[0];
             double minDiff = Math.Abs(target - best);
             
             foreach(var val in allowed)
             {
                 double diff = Math.Abs(target - val);
                 if (diff < minDiff)
                 {
                     minDiff = diff;
                     best = val;
                 }
             }
             return best;
        }

        private static double[] GetStandardThicknesses()
        {
            // 5 6 8 10 12 14 16 18 20 25 30 40 50
            return new double[] { 5, 6, 8, 10, 12, 14, 16, 18, 20, 25, 30, 40, 50 };
        }

        private static double[] GetStandardFlangeWidths()
        {
             // Extracted unique widths from previous hardcoded table
             return new double[] { 125, 150, 165, 180, 200, 215, 250, 285, 300, 330, 400 };
        }

        // NOTE:
        // Red cells in the user's matrix are advisory notes (still allowed).
        // Hard restriction applied here is only minimum web thickness versus maximum web depth.
        private static bool SatisfiesMinimumWebThicknessRule(TaperedSectionData sec)
        {
            if (sec == null) return false;

            double maxDepthMm = Math.Max(sec.H1, sec.H2) * 1000.0;
            double webThkMm = sec.Tw * 1000.0;
            double minRequiredMm = GetMinimumWebThicknessMm(maxDepthMm);
            // Relax tolerance slightly for integer math
            return webThkMm + 0.1 >= minRequiredMm;
        }

        private static bool IsValidTaperedGeometryAtRatio(TaperedSectionData sec, double ratioAlong)
        {
            if (sec == null) return false;
            ratioAlong = Math.Max(0.0, Math.Min(1.0, ratioAlong));

            double d = (sec.H1 + (sec.H2 - sec.H1) * ratioAlong) * 1000.0; // m -> mm
            double tf = sec.Tf * 1000.0;
            double tfb = sec.Tfb * 1000.0;
            double tw = sec.Tw * 1000.0;
            double bf = sec.Bf * 1000.0;
            double bfb = sec.Bfb * 1000.0;

            if (d <= 0 || tf <= 0 || tfb <= 0 || tw <= 0 || bf <= 0 || bfb <= 0)
                return false;

            double hWeb = d - tf - tfb;
            return hWeb > 0;
        }

        private static double ResolveCriticalRatioAlong(AnalyzedMember member, double criticalLoc)
        {
            if (criticalLoc >= 0.0 && criticalLoc <= 1.0)
                return criticalLoc;

            if (member != null && member.Length > 1e-9)
            {
                double ratioFromDistance = (criticalLoc * 1000.0) / member.Length; // STAAD station is usually in meters.
                if (ratioFromDistance >= 0.0 && ratioFromDistance <= 1.0)
                    return ratioFromDistance;
            }

            return 0.5;
        }

        private static bool SatisfiesPracticalPlateProportion(TaperedSectionData sec)
        {
            if (sec == null) return false;
            double twMm = sec.Tw * 1000.0;
            double tfAvgMm = 0.5 * (sec.Tf + sec.Tfb) * 1000.0;
            // Avoid impractical combinations like Tf=40 with Tw=5.
            return twMm + 1e-9 >= 0.4 * tfAvgMm;
        }

        private static double GetMinimumFlangeThicknessMm(double bfMm)
        {
            // Practical rule from feedback: wide flange plates should not be too thin.
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

        private static double GetMinimumWebThicknessMm(double maxWebDepthMm)
        {
            // Keep this aligned with GetMinTwForDepth and standard depth bands.
            if (maxWebDepthMm >= 1200.0) return 10.0;
            if (maxWebDepthMm > 900.0) return 8.0;
            if (maxWebDepthMm > 500.0) return 6.0;
            return 5.0;
        }

        private static string FormatSuggestedTaperedSection(TaperedSectionData sec)
        {
            // Convert to mm for display (m -> mm)
            // Changed Tw to F0 per user request
            return $"Tapered H1={sec.H1*1000:F0}mm H2={sec.H2*1000:F0}mm Bf={sec.Bf*1000:F0}mm Tf={sec.Tf*1000:F0}mm Tw={sec.Tw*1000:F0}mm Bfb={sec.Bfb*1000:F0}mm Tfb={sec.Tfb*1000:F0}mm";
        }

        private static AnalyzedMember CloneMember(AnalyzedMember source)
        {
            var clone = new AnalyzedMember();
            var props = typeof(AnalyzedMember).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in props)
            {
                if (p.CanRead && p.CanWrite)
                    p.SetValue(clone, p.GetValue(source, null), null);
            }
            return clone;
        }

        private static void ApplyTaperedCandidateProperties(AnalyzedMember member, TaperedSectionData sec, double ratioAlong)
        {
            ratioAlong = Math.Max(0.0, Math.Min(1.0, ratioAlong));

            // Evaluate section properties at the STAAD critical section location.
            double d = (sec.H1 + (sec.H2 - sec.H1) * ratioAlong) * 1000.0; // m -> mm
            double bf = sec.Bf * 1000.0;
            double tf = sec.Tf * 1000.0;
            double bfb = sec.Bfb * 1000.0;
            double tfb = sec.Tfb * 1000.0;
            double tw = sec.Tw * 1000.0;

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
            member.Sx = Math.Min(sxTop, sxBottom); // conservative extreme-fiber modulus

            double cY = 0.5 * Math.Max(bf, bfb);
            member.Sy = cY > 0 ? member.Iy / cY : 0;

            // Conservative plastic modulus estimate for iterative sizing.
            member.Zx = member.Sx * 1.10;
            member.Zy = member.Sy * 1.10;

            member.ho = d - 0.5 * (tf + tfb);
            if (member.ho <= 0) member.ho = 0.9 * d;
            member.J = (1.0 / 3.0) * (bf * Math.Pow(tf, 3) + bfb * Math.Pow(tfb, 3) + hWeb * Math.Pow(tw, 3));
            member.Cw = (member.Iy * Math.Pow(member.ho, 2)) / 4.0;

            // Force recalculation of derived radii/rts for each candidate section.
            member.rx = 0;
            member.ry = 0;
            member.rts = 0;
        }
    }
}
