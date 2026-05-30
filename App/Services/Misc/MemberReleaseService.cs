using System;
using System.Diagnostics;
using StaadSteelApp.Core.Interop;
using Microsoft.Office.Interop.Excel;

namespace StaadSteelApp.Core.Misc
{
    internal static class MemberReleaseService
    {
        private const string CELL_AXIAL_START = "D15";
        private const string CELL_AXIAL_END = "E15";
        private const string CELL_SHEAR_Y_START = "D16";
        private const string CELL_SHEAR_Y_END = "E16";
        private const string CELL_SHEAR_Z_START = "D17";
        private const string CELL_SHEAR_Z_END = "E17";
        private const string CELL_TORSION_START = "D18";
        private const string CELL_TORSION_END = "E18";
        private const string CELL_MOM_Y_START = "D19";
        private const string CELL_MOM_Y_END = "E19";
        private const string CELL_MOM_Z_START = "D20";
        private const string CELL_MOM_Z_END = "E20";

        internal static ServiceOperationResult ApplyReleasesSelected(Worksheet ws)
        {
            int[] startDof = new int[6];
            startDof[0] = BoolToInt(IsMark(ws.Range[CELL_AXIAL_START].Value));
            startDof[1] = BoolToInt(IsMark(ws.Range[CELL_SHEAR_Y_START].Value));
            startDof[2] = BoolToInt(IsMark(ws.Range[CELL_SHEAR_Z_START].Value));
            startDof[3] = BoolToInt(IsMark(ws.Range[CELL_TORSION_START].Value));
            startDof[4] = BoolToInt(IsMark(ws.Range[CELL_MOM_Y_START].Value));
            startDof[5] = BoolToInt(IsMark(ws.Range[CELL_MOM_Z_START].Value));

            int[] endDof = new int[6];
            endDof[0] = BoolToInt(IsMark(ws.Range[CELL_AXIAL_END].Value));
            endDof[1] = BoolToInt(IsMark(ws.Range[CELL_SHEAR_Y_END].Value));
            endDof[2] = BoolToInt(IsMark(ws.Range[CELL_SHEAR_Z_END].Value));
            endDof[3] = BoolToInt(IsMark(ws.Range[CELL_TORSION_END].Value));
            endDof[4] = BoolToInt(IsMark(ws.Range[CELL_MOM_Y_END].Value));
            endDof[5] = BoolToInt(IsMark(ws.Range[CELL_MOM_Z_END].Value));

            return ApplyReleasesSelected(startDof, endDof);
        }

        internal static ServiceOperationResult ApplyReleasesSelected(int[] startDofInput, int[] endDofInput)
        {
            if (!MiscOpenStaadService.TryGetProperty(out object propObj, out string propertyError))
            {
                Debug.WriteLine(propertyError);
                return ServiceOperationResult.Error("Member Releases", propertyError);
            }

            if (!MiscSelectionService.TryGetSelectedBeamIds(out int[] beamIds, out string selectionError))
            {
                string message = string.IsNullOrWhiteSpace(selectionError)
                    ? "Could not read STAAD selection."
                    : selectionError;
                return ServiceOperationResult.Info("Member Releases", message);
            }

            int beamCount = beamIds.Length;
            if (beamCount == 0)
            {
                string message = string.IsNullOrWhiteSpace(selectionError)
                    ? "Please select beams in STAAD."
                    : "Could not read STAAD selection: " + selectionError;
                return ServiceOperationResult.Info("Member Releases", message);
            }

            int[] startDof = NormalizeReleaseDof(startDofInput);
            int[] endDof = NormalizeReleaseDof(endDofInput);

            bool startOk = HasRelease(startDof)
                ? TryApplyReleaseSpec(propObj, beamIds, 0, startDof, out string startError)
                : TryRemoveRelease(propObj, beamIds, 0, out startError);
            if (!startOk)
            {
                Debug.WriteLine("Apply start releases failed: " + startError);
                return ServiceOperationResult.Error("Member Releases", "Apply start releases failed: " + startError);
            }

            bool endOk = HasRelease(endDof)
                ? TryApplyReleaseSpec(propObj, beamIds, 1, endDof, out string endError)
                : TryRemoveRelease(propObj, beamIds, 1, out endError);
            if (!endOk)
            {
                Debug.WriteLine("Apply end releases failed: " + endError);
                return ServiceOperationResult.Error("Member Releases", "Apply end releases failed: " + endError);
            }

            return ServiceOperationResult.Success("Member Releases", $"Releases applied to {beamCount} beams.");
        }

        internal static ServiceOperationResult ResetDefaultSelected()
        {
            if (!MiscOpenStaadService.TryGetProperty(out object propObj, out string propertyError))
            {
                Debug.WriteLine(propertyError);
                return ServiceOperationResult.Error("Reset Members", propertyError);
            }

            if (!MiscSelectionService.TryGetSelectedBeamIds(out int[] beamIds, out string _selectionError))
                return ServiceOperationResult.Info("Reset Members", "Could not read STAAD selection.");

            if (beamIds.Length == 0)
                return ServiceOperationResult.Info("Reset Members", "Please select beams in STAAD.");

            bool clearStart = TryRemoveRelease(propObj, beamIds, 0, out string startError);
            bool clearEnd = TryRemoveRelease(propObj, beamIds, 1, out string endError);

            if (clearStart && clearEnd)
            {
                return ServiceOperationResult.Success(
                    "Reset Members",
                    "Member releases reset. Local axes beta is not changed (clear manually in STAAD).");
            }

            string message = "Reset completed with issues:";
            if (!clearStart) message += "\n- Start release: " + startError;
            if (!clearEnd) message += "\n- End release: " + endError;
            return ServiceOperationResult.Warning("Reset Members", message);
        }

        private static bool IsMark(object cellValue)
        {
            if (cellValue == null) return false;
            string text = cellValue.ToString().Trim().ToLower();
            return text == "o" || text == "x" || text == "1" || text == "true" || text == "yes";
        }

        private static int BoolToInt(bool value)
        {
            return value ? 1 : 0;
        }

        private static bool HasRelease(int[] releaseStates)
        {
            foreach (int state in releaseStates)
            {
                if (state == 1)
                    return true;
            }

            return false;
        }

        private static int[] NormalizeReleaseDof(int[] input)
        {
            int[] result = new int[6];
            if (input == null)
                return result;

            int count = Math.Min(input.Length, 6);
            for (int i = 0; i < count; i++)
                result[i] = input[i] != 0 ? 1 : 0;

            return result;
        }

        private static int CreateMemberReleaseSpec(object propObj, int location, int[] dof, double[] spring)
        {
            return OpenStaadPropertyAdapter.CreateMemberReleaseSpec(propObj, location, dof, spring, out _);
        }

        private static int AssignMemberSpecToBeam(object propObj, int[] beams, int specId)
        {
            return OpenStaadPropertyAdapter.AssignMemberSpecToBeam(propObj, beams, specId, out _);
        }

        private static bool TryRemoveRelease(object propObj, int[] beams, int location, out string errorLog)
        {
            var sb = new System.Text.StringBuilder();
            int success = 0;

            foreach (int beam in beams)
            {
                if (!HasExistingRelease(propObj, beam, location))
                {
                    success++;
                    continue;
                }

                bool removed = OpenStaadPropertyAdapter.TryRemoveMemberReleaseSpecFromBeam(propObj, beam, location, out string removeError);
                if (!removed)
                    removed = OpenStaadPropertyAdapter.TryDeleteMemberReleaseSpec(propObj, beam, location, out removeError);

                if (removed || !HasExistingRelease(propObj, beam, location))
                {
                    success++;
                    continue;
                }

                string message = $"Beam {beam}, loc {location}: {removeError}";
                sb.AppendLine(message);
                Debug.WriteLine("RemoveMemberReleaseSpecFromBeam failed: " + message);
            }

            errorLog = sb.ToString();
            return success == beams.Length;
        }

        private static bool HasExistingRelease(object propObj, int beamId, int location)
        {
            if (!OpenStaadPropertyAdapter.TryGetMemberReleaseSpec(
                    propObj,
                    beamId,
                    location,
                    out int[] releaseStates,
                    out double[] springValues,
                    out _))
            {
                return false;
            }

            if (releaseStates != null)
            {
                foreach (int state in releaseStates)
                {
                    if (state != 0)
                        return true;
                }
            }

            if (springValues != null)
            {
                foreach (double value in springValues)
                {
                    if (Math.Abs(value) > 0.0000001)
                        return true;
                }
            }

            return false;
        }

        private static bool TryApplyReleaseSpec(object propObj, int[] beams, int location, int[] dofRelease, out string errorLog)
        {
            errorLog = string.Empty;
            int[] dof = NormalizeReleaseDof(dofRelease);
            double[] spring = new double[6];

            int specId;
            try
            {
                specId = CreateMemberReleaseSpec(propObj, location, dof, spring);
            }
            catch (Exception ex)
            {
                errorLog = "CreateMemberReleaseSpec exception: " + ex.Message;
                return false;
            }

            if (specId < 0)
            {
                errorLog = "CreateMemberReleaseSpec returned code " + specId;
                return false;
            }

            return TryAssignMemberSpec(propObj, beams, specId, out errorLog);
        }

        private static bool TryAssignMemberSpec(object propObj, int[] beams, int specId, out string errorLog)
        {
            var sb = new System.Text.StringBuilder();
            int bulkCode;
            try
            {
                bulkCode = AssignMemberSpecToBeam(propObj, beams, specId);
            }
            catch (Exception ex)
            {
                bulkCode = -1;
                sb.AppendLine("Bulk assign exception: " + ex.Message);
            }

            if (IsAssignMemberSpecSuccess(bulkCode))
            {
                errorLog = string.Empty;
                return true;
            }

            sb.AppendLine("Bulk assign returned code " + bulkCode);

            int success = 0;
            foreach (int beam in beams)
            {
                try
                {
                    int code = AssignMemberSpecToBeam(propObj, new[] { beam }, specId);
                    if (IsAssignMemberSpecSuccess(code))
                    {
                        success++;
                        continue;
                    }

                    sb.AppendLine($"Beam {beam}: return code {code}");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Beam {beam}: {ex.Message}");
                }
            }

            errorLog = sb.ToString();
            return success == beams.Length;
        }

        private static bool IsAssignMemberSpecSuccess(int code)
        {
            return code == 0 || code == 1;
        }
    }
}
