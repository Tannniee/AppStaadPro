using System.Collections.Generic;
using System.Linq;
using System.Text;
using StaadSteelApp.AISC360;

namespace StaadSteelApp
{
    public partial class CheckForm
    {
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
