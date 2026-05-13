using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using StaadSteelApp.Core;
using StaadSteelApp.AISC360;
using StaadSteelApp.Core.Check;

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
                CheckCalculationResult calculation = CheckCalculationService.Calculate(new CheckCalculationRequest
                {
                    OpenStaad = _os,
                    MemberNo = memberNo,
                    AllForces = _allForces,
                    IsBuiltUp = chkIsBuiltUp.Checked
                });

                if (calculation.Results == null && EnsureForcesForMember(memberNo))
                {
                    calculation = CheckCalculationService.Calculate(new CheckCalculationRequest
                    {
                        OpenStaad = _os,
                        MemberNo = memberNo,
                        AllForces = _allForces,
                        IsBuiltUp = chkIsBuiltUp.Checked
                    });
                }

                _currentResults = calculation.Results;
                _currentSteelInfo = ToFormDesignResult(calculation.SteelInfo);
                _currentGoverningForce = calculation.GoverningForce;
                _currentManualSource = calculation.ManualSource;
                _currentForceEnvelope = ToFormEnvelopeEntries(calculation.ForceEnvelope);
                _staadDesignRatio = calculation.StaadDesignRatio;
                _staadCriticalLoadCase = calculation.StaadCriticalLoadCase;

                // Update UI Labels
                if (_currentResults != null)
                {
                    lblSection.Text = $"Section: {_currentResults.Member.SectionName ?? "Unknown"}"; // Stacked Mapper doesn't set Name yet?
                    lblMaterial.Text = $"Material: {TryGetMaterialName(memberNo)}";
                    int comboLc = _staadCriticalLoadCase ?? _currentGoverningForce?.LoadCase ?? 0;
                    lblCombo.Text = comboLc > 0 ? $"Combo: {comboLc}" : "Combo: --";

                    if (_currentGoverningForce != null)
                    {
                        lblForces.Text = $"P={_currentGoverningForce.Fx:F1} kN   M3={_currentGoverningForce.Mz:F1} kN-m   M2={_currentGoverningForce.My:F1} kN-m   V2={_currentGoverningForce.Fy:F1} kN   V3={_currentGoverningForce.Fz:F1} kN";
                    }
                }

                 PopulateSummaryGrid();
                 UpdateComparison();

                 if (calculation.MapperWarningCount > 0)
                 {
                     TopMostMessageBox.ShowWarning(calculation.MapperWarningSummary, "STAAD Mapping Warning");
                 }

             }
             catch (Exception ex)
             {
                TopMostMessageBox.ShowError(ex.Message, "Check Error");
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
                TopMostMessageBox.ShowWarning(
                    "Target D/C must be a number between 0 and 1.5 (recommended <= 1.0).",
                    "Invalid Target");
                return;
            }

            int memberNo = int.Parse(cboFrame.SelectedItem.ToString());
            if (!TryGetMemberSteelDesignResults(memberNo, out DesignResultInfo steelInfo))
            {
                TopMostMessageBox.ShowWarning(
                    "Could not extract governing steel design result from STAAD for this member.",
                    "Sizing Error");
                return;
            }

            var governingForce = GetCriticalSectionForces(memberNo, steelInfo);
            if (governingForce == null)
            {
                TopMostMessageBox.ShowWarning(
                    "Could not extract critical-section forces for sizing.",
                    "Sizing Error");
                return;
            }

             if (!TryGetTaperedSectionData(memberNo, out CheckTaperedSectionInfo currentTapered))
            {
                TopMostMessageBox.ShowInfo(
                    "This sizing tool currently supports tapered/built-up sections only.",
                    "Sizing Scope");
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
             var candidatesFull = CheckTaperedSizingService.OptimizeDiscrete(baseMember, targetRatio, steelInfo.CriticalSection, currentTapered, keepDepthFixed: false);
             var candidatesKeepDepth = CheckTaperedSizingService.OptimizeDiscrete(baseMember, targetRatio, steelInfo.CriticalSection, currentTapered, keepDepthFixed: true);

             var topFull = CheckTaperedSizingService.SelectDiverseCandidates(candidatesFull, 3);
             var topKeepDepth = CheckTaperedSizingService.SelectDiverseCandidates(candidatesKeepDepth, 3);

            if (topFull.Count == 0 && topKeepDepth.Count == 0)
            {
                 TopMostMessageBox.ShowWarning("Could not find any sizing solution within the Standard Geometry Limits.", "Sizing Failed");
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
                string sectionText = CheckTaperedSizingService.FormatSuggestedTaperedSection(opt.Section);
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
                string sectionText = CheckTaperedSizingService.FormatSuggestedTaperedSection(opt.Section);
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

            sb.AppendLine();
            sb.AppendLine(txtDetails.Text);
            txtDetails.Text = sb.ToString();
        }

    }
}
