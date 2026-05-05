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
        private void PopulateSummaryGrid()
        {
            dgvSummary.Rows.Clear();
            _checkMap.Clear();

            if (_currentResults == null) return;

            var checks = GetAllChecks();

            foreach (var check in checks)
            {
                string unit = check.Name.Contains("Shear") ? " kN" : 
                              check.Name.Contains("Flex") ? " kN-m" : 
                              check.Name.Contains("Axial") ? " kN" : "";

                int rowIdx = dgvSummary.Rows.Add(
                    check.Name,
                    check.Section,
                    $"{check.Capacity:F1}{unit}",
                    $"{check.Demand:F1}{unit}",
                    check.Ratio.ToString("F3"),
                    check.GetStatusSymbol()
                );

                 var ratioCell = dgvSummary.Rows[rowIdx].Cells["colRatio"];
                 if (check.Status == CheckStatus.Pass)
                     ratioCell.Style.ForeColor = Color.FromArgb(0, 150, 100); 
                 else
                     ratioCell.Style.ForeColor = Color.FromArgb(200, 50, 50);

                 _checkMap[check.Name] = check;
            }

            int overallIdx = dgvSummary.Rows.Add(
                "OVERALL",
                "",
                "",
                "",
                _currentResults.GoverningRatio.ToString("F3"),
                _currentResults.OverallStatus == CheckStatus.Pass ? "OK" : "NG"
            );
            dgvSummary.Rows[overallIdx].DefaultCellStyle.Font = new Font(dgvSummary.Font, FontStyle.Bold);

            dgvSummary.ClearSelection();
            ShowSummaryReport();
        }

        private void dgvSummary_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvSummary.SelectedRows.Count == 0)
            {
                ShowSummaryReport();
                return;
            }

            string checkName = dgvSummary.SelectedRows[0].Cells["colCheck"].Value?.ToString();
            if (string.IsNullOrEmpty(checkName) || checkName == "OVERALL") 
            {
                ShowOverallDetails();
                return;
            }

            if (_checkMap.TryGetValue(checkName, out CheckDetail check))
            {
                ShowCheckDetails(check);
            }
        }

        private void ShowSummaryReport()
        {
            if (_currentResults == null) return;
            txtDetails.Text = BuildSummaryReportText();
        }

        private void ShowCheckDetails(CheckDetail check)
        {
            txtDetails.Text = BuildCheckDetailText(check);
        }

        private void ShowOverallDetails()
        {
            if (_currentResults == null) return;
            txtDetails.Text = BuildOverallDetailsText();
        }

        private void UpdateComparison()
        {
             if (_currentResults == null)
             {
                 lblCompare.Text = "@ LC --";
                 lblCompareSub.Visible = false;
                 lblCompareSub.Text = string.Empty;
                 lblCompareExtra.Visible = false;
                 lblCompareExtra.Text = string.Empty;
                 return;
             }

             CheckDetail overallCheck = FindGoverningCheckDetail();
             CheckDetail staadClauseCheck = ResolveStaadClauseCheck();
             string staadClauseText = NormalizeClauseText(_currentSteelInfo?.CriticalClause);
             string lcText = _currentGoverningForce != null
                 ? $"@ LC{_currentGoverningForce.LoadCase}/{ShortStationText(_currentGoverningForce.Node)}"
                 : "@ LC --";

             var lines = new List<string> { lcText };

             if (_staadDesignRatio.HasValue)
             {
                 string staadLine = $"STAAD: {_staadDesignRatio.Value:F3}";
                 if (!string.IsNullOrWhiteSpace(staadClauseText))
                     staadLine += $" ({staadClauseText})";
                 lines.Add(staadLine);
             }
             else
             {
                 lines.Add("STAAD: --");
             }

             if (staadClauseCheck != null)
             {
                 string appClauseCode = GetPrimaryCheckCode(staadClauseCheck);
                 string appClauseLine = $"App: {staadClauseCheck.Ratio:F3}";
                 if (!string.IsNullOrWhiteSpace(appClauseCode))
                     appClauseLine += $" ({appClauseCode})";
                 lines.Add(appClauseLine);
             }
             else
             {
                 lines.Add("App: --");
             }

             lines.Add("@ Overall");

             string overallCode = GetPrimaryCheckCode(overallCheck);
             if (string.IsNullOrWhiteSpace(overallCode))
                 overallCode = NormalizeClauseText(_currentResults.GoverningCheck);

             string overallLabel = "AppGoverning";
             double overallRatio = _currentResults.GoverningRatio;
             string governingCode = overallCode;

             if (_staadDesignRatio.HasValue && _staadDesignRatio.Value > overallRatio)
             {
                 overallLabel = "StaadGoverning";
                 overallRatio = _staadDesignRatio.Value;
                 governingCode = staadClauseText;
             }

             string governingLine = $"{overallLabel}: {overallRatio:F3}";
             if (!string.IsNullOrWhiteSpace(governingCode))
                 governingLine += $" ({governingCode})";
             lines.Add(governingLine);

             string diffLine = "Diff: --";
             if (_staadDesignRatio.HasValue && staadClauseCheck != null && _staadDesignRatio.Value > 1e-9)
             {
                 double diffPercent = (staadClauseCheck.Ratio - _staadDesignRatio.Value) / _staadDesignRatio.Value * 100.0;
                 diffLine = $"Diff: {diffPercent:+0.#;-0.#;0.#}%";
             }
             lines.Add(diffLine);

             lblCompare.Text = string.Join(Environment.NewLine, lines.Take(3));
             lblCompareSub.Text = string.Join(Environment.NewLine, lines.Skip(3));
             lblCompareSub.Visible = true;
             double? slendernessValue;
             lblCompareExtra.Text = BuildSlendernessLine(out slendernessValue);
             lblCompareExtra.ForeColor = GetSlendernessColor(slendernessValue);
             lblCompareExtra.Visible = true;
        }

        private CheckDetail FindGoverningCheckDetail()
        {
            if (_currentResults == null) return null;

            return GetAllChecks()
                .OrderByDescending(c => c?.Ratio ?? double.MinValue)
                .FirstOrDefault();
        }

        private CheckDetail ResolveStaadClauseCheck()
        {
            string clause = NormalizeClauseText(_currentSteelInfo?.CriticalClause);
            if (string.IsNullOrWhiteSpace(clause))
                return null;

            if (ClauseContainsToken(clause, "H3"))
                return _currentResults?.TorsionCheck ?? _currentResults?.InteractionCheck;
            if (ClauseContainsToken(clause, "H1"))
                return _currentResults?.InteractionCheck;
            if (ClauseContainsToken(clause, "F2") ||
                ClauseContainsToken(clause, "F3") ||
                ClauseContainsToken(clause, "F4") ||
                ClauseContainsToken(clause, "F5"))
                return _currentResults?.FlexureCheckMajor;
            if (ClauseContainsToken(clause, "F6"))
                return _currentResults?.FlexureCheckMinor;
            if (ClauseContainsToken(clause, "F7") ||
                ClauseContainsToken(clause, "F8"))
                return (_currentResults?.FlexureCheckMajor?.Ratio ?? 0) >= (_currentResults?.FlexureCheckMinor?.Ratio ?? 0)
                    ? _currentResults?.FlexureCheckMajor
                    : _currentResults?.FlexureCheckMinor;
            if (ClauseContainsToken(clause, "G2") ||
                ClauseContainsToken(clause, "G4") ||
                ClauseContainsToken(clause, "G5"))
                return _currentResults?.ShearCheckMajor;
            if (ClauseContainsToken(clause, "G6") ||
                ClauseContainsToken(clause, "G7"))
                return _currentResults?.ShearCheckMinor;
            if (ClauseContainsToken(clause, "E"))
                return _currentResults?.CompressionCheck;
            if (ClauseContainsToken(clause, "D"))
                return _currentResults?.TensionCheck;

            return null;
        }

        private static string NormalizeClauseText(string clause)
        {
            if (string.IsNullOrWhiteSpace(clause))
                return string.Empty;

            string text = clause.Trim();
            bool changed;
            do
            {
                changed = false;
                if (text.StartsWith("Cl.", StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Substring(3).Trim();
                    changed = true;
                }
                if (text.StartsWith("Eq.", StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Substring(3).Trim();
                    changed = true;
                }
                if (text.StartsWith("Req.", StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Substring(4).Trim();
                    changed = true;
                }
            } while (changed);

            return text.Trim();
        }

        private static bool ClauseContainsToken(string clause, string token)
        {
            if (string.IsNullOrWhiteSpace(clause) || string.IsNullOrWhiteSpace(token))
                return false;

            string normalizedClause = NormalizeClauseText(clause).ToUpperInvariant();
            string normalizedToken = token.Trim().ToUpperInvariant();
            int idx = normalizedClause.IndexOf(normalizedToken, StringComparison.Ordinal);
            if (idx < 0)
                return false;

            if (idx > 0)
            {
                char prev = normalizedClause[idx - 1];
                if (char.IsLetterOrDigit(prev))
                    return false;
            }

            return true;
        }

        private static string GetPrimaryCheckCode(CheckDetail check)
        {
            if (check == null)
                return string.Empty;

            return NormalizeClauseText(check.Section);
        }

        private static string ShortStationText(string station)
        {
            if (string.IsNullOrWhiteSpace(station))
                return "--";

            string text = station.Trim();
            if (text.StartsWith("At ", StringComparison.OrdinalIgnoreCase))
                return text.Substring(3).Trim();

            return text;
        }

        private string BuildSlendernessLine(out double? slendernessValue)
        {
            double? klr = null;

            if (_currentSteelInfo != null && _currentSteelInfo.KLByR > 0)
            {
                klr = _currentSteelInfo.KLByR;
            }
            else if (_currentResults?.Member != null)
            {
                var m = _currentResults.Member;
                double klrx = (m.rx > 0) ? (m.Kx * m.Lx / m.rx) : 0.0;
                double klry = (m.ry > 0) ? (m.Ky * m.Ly / m.ry) : 0.0;
                double max = Math.Max(klrx, klry);
                if (max > 0)
                    klr = max;
            }

            slendernessValue = klr;
            if (!klr.HasValue)
                return "Slenderness: --";

            string status = klr.Value <= 200.0 ? "OK" : "> 200";
            return $"Slenderness: KL/r = {klr.Value:F1} ({status})";
        }

        private static Color GetSlendernessColor(double? slendernessValue)
        {
            if (!slendernessValue.HasValue)
                return Color.FromArgb(120, 90, 20);

            if (slendernessValue.Value < 170.0)
                return Color.FromArgb(0, 150, 100);

            if (slendernessValue.Value < 200.0)
                return Color.FromArgb(120, 90, 20);

            return Color.FromArgb(200, 50, 50);
        }
        
        private void BtnCopy_Click(object sender, EventArgs e)
        {
            if (_currentResults == null) return;
            Clipboard.SetText(BuildDetailedCopyText());
            MessageBox.Show("Check detail copied to clipboard!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnCopyDetail_Click(object sender, EventArgs e)
        {
            if (_currentResults == null) return;
            Clipboard.SetText(BuildDetailedCopyText());
            MessageBox.Show("Check detail copied to clipboard!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private List<CheckDetail> GetAllChecks()
        {
            var checks = new List<CheckDetail>();
            if (_currentResults?.CompressionCheck != null) checks.Add(_currentResults.CompressionCheck);
            if (_currentResults?.TensionCheck != null) checks.Add(_currentResults.TensionCheck);
            if (_currentResults?.FlexureCheckMajor != null) checks.Add(_currentResults.FlexureCheckMajor);
            if (_currentResults?.FlexureCheckMinor != null) checks.Add(_currentResults.FlexureCheckMinor);
            if (_currentResults?.ShearCheckMajor != null) checks.Add(_currentResults.ShearCheckMajor);
            if (_currentResults?.ShearCheckMinor != null) checks.Add(_currentResults.ShearCheckMinor);
            if (_currentResults?.InteractionCheck != null) checks.Add(_currentResults.InteractionCheck);
            if (_currentResults?.TorsionCheck != null) checks.Add(_currentResults.TorsionCheck);
            if (_currentResults?.ServiceabilityCheck != null) checks.Add(_currentResults.ServiceabilityCheck);
            return checks;
        }

        private string BuildSummaryReportText()
        {
            if (_currentResults == null) return string.Empty;

            string frame = string.IsNullOrWhiteSpace(cboFrame.Text) ? "--" : cboFrame.Text;
            string staadRatioText = _staadDesignRatio.HasValue ? _staadDesignRatio.Value.ToString("F3") : "--";
            string staadComboText = _staadCriticalLoadCase.HasValue ? _staadCriticalLoadCase.Value.ToString() : "--";

            var sb = new StringBuilder();
            sb.AppendLine("AISC 360-16 Design Check Report");
            sb.AppendLine($"Frame: {frame}");
            sb.AppendLine(lblSection.Text);
            sb.AppendLine(lblMaterial.Text);
            sb.AppendLine(lblCombo.Text);
            sb.AppendLine($"STAAD Design Ratio: {staadRatioText} (LC: {staadComboText})");
            sb.AppendLine();
            sb.AppendLine(lblForces.Text);
            sb.AppendLine();
            sb.AppendLine("=== FUNCTION CHECK TRACE ===");
            sb.AppendLine($"Manual Source: {_currentManualSource}");
            if (_currentGoverningForce != null)
                sb.AppendLine($"Manual Governing Station: LC {_currentGoverningForce.LoadCase} @ {_currentGoverningForce.Node}");
            if (_currentSteelInfo != null)
            {
                sb.AppendLine($"STAAD Status: {_currentSteelInfo.DesignStatus}");
                sb.AppendLine($"STAAD Clause: {_currentSteelInfo.CriticalClause}");
                sb.AppendLine($"STAAD Section: {_currentSteelInfo.DesignSection}");
                sb.AppendLine($"STAAD Critical Section: {_currentSteelInfo.CriticalSection:F3} m");
                if (_currentSteelInfo.KLByR > 0)
                    sb.AppendLine($"STAAD KL/r: {_currentSteelInfo.KLByR:F2}");
            }

            AppendForceEnvelope(sb);
            sb.AppendLine();
            sb.AppendLine("=== CHECK SUMMARY ===");

            foreach (var check in GetAllChecks())
            {
                sb.AppendLine($"{check.Name,-25} {check.Section,-8} Capacity:{check.Capacity,8:F1}  Demand:{check.Demand,8:F1}  D/C:{check.Ratio,6:F3}  {check.Status}");
            }

            sb.AppendLine();
            sb.AppendLine($"Governing: {_currentResults.GoverningRatio:F3} ({_currentResults.GoverningCheck})");
            sb.AppendLine($"STAAD:     {staadRatioText}");
            return sb.ToString();
        }

        private void AppendForceEnvelope(StringBuilder sb)
        {
            if (sb == null) return;

            sb.AppendLine("Extracted Max Internal Forces:");
            if (_currentForceEnvelope == null || _currentForceEnvelope.Count == 0)
            {
                sb.AppendLine("  No force envelope available.");
                return;
            }

            bool anySampled = _currentForceEnvelope.Any(e => e.IsSampled);
            sb.AppendLine(anySampled
                ? "  Source: sampled intermediate forces along member."
                : "  Source: extracted start/end member forces only.");

            foreach (var entry in _currentForceEnvelope)
            {
                sb.AppendLine($"  {entry.Label,-2} = {entry.Value,10:F3} {entry.Unit,-5} @ LC {entry.LoadCase,-4} / {entry.Station}");
            }
        }

        private string BuildCheckDetailText(CheckDetail check)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== {check.Name.ToUpperInvariant()} ({check.Section}) ===");
            sb.AppendLine();

            foreach (var step in check.Details)
            {
                sb.AppendLine(step.ToString());
            }

            sb.AppendLine();
            sb.AppendLine($"Capacity: {check.Capacity:F2}");
            sb.AppendLine($"Demand:   {check.Demand:F2}");
            sb.AppendLine($"D/C:      {check.Ratio:F3}");
            sb.AppendLine($"Status:   {check.Status} {check.GetStatusSymbol()}");

            if (check.SubChecks != null && check.SubChecks.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("--- Alternative / Reference Checks ---");
                foreach (var sub in check.SubChecks)
                {
                    sb.AppendLine();
                    sb.AppendLine($"- {sub.Name} ({sub.Section})");
                    sb.AppendLine($"  Capacity: {sub.Capacity:F2}");
                    sb.AppendLine($"  Demand:   {sub.Demand:F2}");
                    sb.AppendLine($"  D/C:      {sub.Ratio:F3}");
                    sb.AppendLine($"  Status:   {sub.Status} {sub.GetStatusSymbol()}");
                }
            }

            if (check.Messages != null && check.Messages.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Notes:");
                foreach (var msg in check.Messages)
                {
                    sb.AppendLine($"- {msg}");
                }
            }

            return sb.ToString();
        }

        private string BuildOverallDetailsText()
        {
            if (_currentResults == null) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("=== OVERALL SUMMARY ===");
            sb.AppendLine();
            sb.AppendLine($"Governing Check: {_currentResults.GoverningCheck}");
            sb.AppendLine($"Governing Ratio: {_currentResults.GoverningRatio:F3}");
            sb.AppendLine($"Overall Status:  {_currentResults.OverallStatus}");

            if (_currentResults.Member != null)
            {
                var m = _currentResults.Member;
                sb.AppendLine();
                sb.AppendLine("--- Calculated Section Properties ---");
                sb.AppendLine($"  d   = {m.d:F1} mm");
                sb.AppendLine($"  bf  = {m.bf:F1} mm");
                sb.AppendLine($"  tf  = {m.tf:F1} mm");
                sb.AppendLine($"  tw  = {m.tw:F1} mm");
                sb.AppendLine($"  A   = {m.Ag:F0} mm2");
                sb.AppendLine($"  Ix  = {m.Ix:E3} mm4");
                sb.AppendLine($"  Iy  = {m.Iy:E3} mm4");
                sb.AppendLine($"  Sx  = {m.Sx:F0} mm3");
                sb.AppendLine($"  Sy  = {m.Sy:F0} mm3");
                sb.AppendLine($"  Zx  = {m.Zx:F0} mm3");
                sb.AppendLine($"  Zy  = {m.Zy:F0} mm3");
                sb.AppendLine($"  rx  = {m.rx:F1} mm");
                sb.AppendLine($"  ry  = {m.ry:F1} mm");
                sb.AppendLine($"  J   = {m.J:E3} mm4");
                sb.AppendLine($"  Cw  = {m.Cw:E3} mm6");
                sb.AppendLine();
                sb.AppendLine("--- Material Properties ---");
                sb.AppendLine($"  E   = {m.E:F0} MPa");
                sb.AppendLine($"  Fy  = {m.Fy:F0} MPa");
                sb.AppendLine();
                sb.AppendLine("--- Design Parameters ---");
                sb.AppendLine($"  L   = {m.Length:F0} mm");
                sb.AppendLine($"  Lb  = {m.Lb:F0} mm");
                if (!string.IsNullOrWhiteSpace(m.LbSource))
                    sb.AppendLine($"  LbSrc = {m.LbSource}");
                if (m.ParamUNL > 0 || m.ParamUNT > 0 || m.ParamUNB > 0)
                    sb.AppendLine($"  UNL/UNT/UNB = {m.ParamUNL:F0}/{m.ParamUNT:F0}/{m.ParamUNB:F0} mm");
                sb.AppendLine($"  Kx  = {m.Kx:F2}");
                sb.AppendLine($"  Ky  = {m.Ky:F2}");
                sb.AppendLine($"  Cb  = {m.Cb:F2}");
            }

            if (_currentResults.FlexureClass != null)
            {
                sb.AppendLine();
                sb.AppendLine("--- Section Classification (Flexure) ---");
                foreach (var step in _currentResults.FlexureClass.Details)
                {
                    sb.AppendLine(step.ToString());
                }
            }

            return sb.ToString();
        }

        private string BuildDetailedCopyText()
        {
            if (_currentResults == null) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine(BuildSummaryReportText().TrimEnd());

            foreach (var check in GetAllChecks())
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine(BuildCheckDetailText(check).TrimEnd());
            }

            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine(BuildOverallDetailsText().TrimEnd());
            return sb.ToString();
        }
    }
}
