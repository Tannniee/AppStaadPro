using System;
using System.Diagnostics;
using System.Globalization;
using StaadSteelApp.Core.Interop;
using Microsoft.Office.Interop.Excel;

namespace StaadSteelApp.Core.Misc
{
    internal static class MemberBetaService
    {
        private const string CellLocalAxesDeg = "D12";

        internal static void ApplyBetaAngleSelected(Worksheet ws)
        {
            dynamic os = StaadManager.GetOpenSTAAD();
            if (os == null) return;

            if (!MiscSelectionService.TryGetSelectedBeamIds(out int[] beamIds, out _))
                return;

            int beamCount = beamIds.Length;
            if (beamCount == 0)
                return;

            double angle;
            object raw = null;
            try
            {
                raw = ws.Range[CellLocalAxesDeg].Value;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Read beta angle from Excel failed: " + ex);
                TopMostMessageBox.ShowError("Could not read beta angle from Excel: " + ex.Message, "Beta Angle");
                return;
            }

            if (!TryParseAngle(raw, out angle))
            {
                TopMostMessageBox.ShowWarning("Angle must be a number.", "Beta Angle");
                return;
            }

            string assignError;
            OpenStaadPropertyAdapter.AssignBetaAngle(os.Property, beamIds, angle, out assignError);
            TopMostMessageBox.ShowSuccess($"Beta Angle {angle} deg applied to {beamCount} beams.", "Beta Angle");
        }

        internal static void ApplyBetaAngleSelected(double angle)
        {
            dynamic os = StaadManager.GetOpenSTAAD();
            if (os == null) return;

            if (!MiscSelectionService.TryGetSelectedBeamIds(out int[] beamIds, out string selectionError))
            {
                string message = string.IsNullOrWhiteSpace(selectionError)
                    ? "Could not read STAAD selection."
                    : selectionError;
                TopMostMessageBox.ShowInfo(message, "Info");
                return;
            }

            int beamCount = beamIds.Length;
            if (beamCount == 0)
            {
                string message = string.IsNullOrWhiteSpace(selectionError)
                    ? "Please select beams in STAAD."
                    : "Could not read STAAD selection: " + selectionError;
                TopMostMessageBox.ShowInfo(message, "Info");
                return;
            }

            if (Math.Abs(angle) < 1e-9)
            {
                TopMostMessageBox.ShowInfo(
                    "Beta angle reset to 0 is unstable via OpenSTAAD on this setup. Please clear Local Axes manually in STAAD.",
                    "Beta Angle");
                return;
            }

            bool success = TryAssignBetaAngle(os.Property, beamIds, angle, out string errorLog);
            if (success)
            {
                TopMostMessageBox.ShowSuccess($"Beta Angle {angle} deg applied to {beamCount} beams.", "Beta Angle");
                return;
            }

            string shortLog = errorLog.Length > 200 ? errorLog.Substring(0, 200) + "..." : errorLog;
            TopMostMessageBox.ShowWarning($"Beta Angle applied with some errors:\n{shortLog}", "Beta Angle Warning");
            Debug.WriteLine(errorLog);
        }

        private static bool TryParseAngle(object raw, out double angle)
        {
            if (raw == null)
            {
                angle = 0;
                return true;
            }

            if (raw is double doubleValue)
            {
                angle = doubleValue;
                return true;
            }

            if (raw is float floatValue)
            {
                angle = floatValue;
                return true;
            }

            string text = Convert.ToString(raw) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                angle = 0;
                return true;
            }

            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out angle) ||
                   double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out angle);
        }

        private static bool TryAssignBetaAngle(object propObj, int[] beams, double angle, out string errorLog)
        {
            var sb = new System.Text.StringBuilder();
            int bulkCode = InvokeAssignBetaAngle(propObj, beams, angle, out string bulkError);
            if (!IsAssignBetaSuccessCode(bulkCode) && bulkCode != 0)
            {
                string message = bulkCode < 0
                    ? $"Bulk: {bulkError}"
                    : $"Bulk: return code {bulkCode}";
                sb.AppendLine(message);
                Debug.WriteLine("AssignBetaAngle failed: " + message);
            }

            int[] remaining = GetBeamsNotMatchingAngle(propObj, beams, angle);
            if (remaining.Length == 0)
            {
                errorLog = string.Empty;
                return true;
            }

            foreach (int beam in remaining)
            {
                int code = InvokeAssignBetaAngle(propObj, new[] { beam }, angle, out string retryError);
                if (BeamMatchesBetaAngle(propObj, beam, angle))
                    continue;

                double? actual = TryGetBetaAngle(propObj, beam);
                string actualText = actual.HasValue ? actual.Value.ToString() : "N/A";
                string message = code < 0
                    ? $"Beam {beam}: {retryError} (actual={actualText})"
                    : $"Beam {beam}: return code {code} (actual={actualText})";
                sb.AppendLine(message);
                Debug.WriteLine("AssignBetaAngle failed: " + message);
            }

            errorLog = sb.ToString();
            return string.IsNullOrWhiteSpace(errorLog);
        }

        private static bool IsAssignBetaSuccessCode(int code)
        {
            return code > 0;
        }

        private static int InvokeAssignBetaAngle(object propObj, int[] beams, double angle, out string error)
        {
            error = string.Empty;
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    return OpenStaadPropertyAdapter.AssignBetaAngle(propObj, beams, angle, out error);
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
            }

            return -1;
        }

        private static double? TryGetBetaAngle(object propObj, int beam)
        {
            if (OpenStaadPropertyAdapter.TryGetBetaAngle(propObj, beam, out double angle, out string error))
                return angle;

            if (!string.IsNullOrWhiteSpace(error))
            {
                Debug.WriteLine($"TryGetBetaAngle failed for beam {beam}: {error}");
            }

            return null;
        }

        private static bool BeamMatchesBetaAngle(object propObj, int beam, double targetAngle)
        {
            double? current = TryGetBetaAngle(propObj, beam);
            return current.HasValue && AreAnglesEquivalent(current.Value, targetAngle);
        }

        private static int[] GetBeamsNotMatchingAngle(object propObj, int[] beams, double targetAngle)
        {
            var list = new System.Collections.Generic.List<int>();
            foreach (int beam in beams)
            {
                if (!BeamMatchesBetaAngle(propObj, beam, targetAngle))
                    list.Add(beam);
            }

            return list.ToArray();
        }

        private static bool AreAnglesEquivalent(double actual, double target)
        {
            double normalizedActual = NormalizeAngle(actual);
            double normalizedTarget = NormalizeAngle(target);
            return Math.Abs(normalizedActual - normalizedTarget) < 1e-6;
        }

        private static double NormalizeAngle(double angle)
        {
            double normalized = angle % 360.0;
            if (normalized < 0)
                normalized += 360.0;
            return normalized;
        }
    }
}
