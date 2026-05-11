using System;
using System.Diagnostics;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core.Misc
{
    internal static class MemberSpecService
    {
        internal static ServiceOperationResult ApplyMemberSpecSelected(MiscMemberSpecType specType)
        {
            if (!MiscOpenStaadService.TryGetProperty(out object propObj, out string propertyError))
            {
                Debug.WriteLine(propertyError);
                return ServiceOperationResult.Error("Member Spec", propertyError);
            }

            if (!MiscSelectionService.TryGetSelectedBeamIds(out int[] beamIds, out string selectionError))
            {
                string message = string.IsNullOrWhiteSpace(selectionError)
                    ? "Could not read STAAD selection."
                    : selectionError;
                return ServiceOperationResult.Info("Member Spec", message);
            }

            int beamCount = beamIds.Length;
            if (beamCount == 0)
            {
                string message = string.IsNullOrWhiteSpace(selectionError)
                    ? "Please select beams in STAAD."
                    : "Could not read STAAD selection: " + selectionError;
                return ServiceOperationResult.Info("Member Spec", message);
            }

            try
            {
                if (specType == MiscMemberSpecType.Normal)
                {
                    int cleared = 0;
                    foreach (int beam in beamIds)
                    {
                        bool ok = true;
                        ok &= TryRemoveMemberSpec(propObj, "RemoveMemberTrussSpecFromBeam", beam);
                        ok &= TryRemoveMemberSpec(propObj, "RemoveMemberTensionSpecFromBeam", beam);
                        ok &= TryRemoveMemberSpec(propObj, "RemoveMemberCompressionSpecFromBeam", beam);
                        if (ok)
                            cleared++;
                    }

                    return ServiceOperationResult.Success(
                        "Member Spec",
                        $"Normal member spec applied to {cleared}/{beamCount} beams.");
                }

                string createMethod = GetCreateMethodName(specType);
                int specId = InvokeCreateMemberSpec(propObj, createMethod, out string createError);
                if (specId < 0)
                {
                    string message = string.IsNullOrEmpty(createError)
                        ? $"Could not create member spec via {createMethod}."
                        : createError;
                    return ServiceOperationResult.Error("Member Spec", message);
                }

                foreach (int beam in beamIds)
                {
                    if (specType != MiscMemberSpecType.Truss)
                        TryRemoveMemberSpec(propObj, "RemoveMemberTrussSpecFromBeam", beam);
                    if (specType != MiscMemberSpecType.Tension)
                        TryRemoveMemberSpec(propObj, "RemoveMemberTensionSpecFromBeam", beam);
                    if (specType != MiscMemberSpecType.Compression)
                        TryRemoveMemberSpec(propObj, "RemoveMemberCompressionSpecFromBeam", beam);
                }

                if (!TryAssignMemberSpec(propObj, beamIds, specId, specType, out string assignError))
                {
                    return ServiceOperationResult.Error("Member Spec", "Assign member spec failed: " + assignError);
                }

                return ServiceOperationResult.Success(
                    "Member Spec",
                    $"{GetSpecTypeLabel(specType)} member spec applied to {beamCount} beams.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ApplyMemberSpecSelected failed: " + ex);
                AppLogger.Exception("Member spec failed.", ex);
                return ServiceOperationResult.Error("Member Spec", "Member spec failed: " + ex.Message);
            }
        }

        private static int AssignMemberSpecToBeam(object propObj, int[] beams, int specId)
        {
            return OpenStaadPropertyAdapter.AssignMemberSpecToBeam(propObj, beams, specId, out _);
        }

        private static bool TryAssignMemberSpec(object propObj, int[] beams, int specId, MiscMemberSpecType specType, out string errorLog)
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
                if (DoAllBeamsHaveExpectedSpec(propObj, beams, specType, out string verifyError))
                {
                    errorLog = string.Empty;
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(verifyError))
                    sb.AppendLine(verifyError);
            }

            sb.AppendLine("Bulk assign returned code " + bulkCode);

            int success = 0;
            foreach (int beam in beams)
            {
                try
                {
                    int code = AssignMemberSpecToBeam(propObj, new[] { beam }, specId);
                    string verifyError = string.Empty;
                    bool verified = BeamHasExpectedSpec(propObj, beam, specType, out verifyError);

                    if (IsAssignMemberSpecSuccess(code) && verified)
                    {
                        success++;
                        continue;
                    }

                    string suffix = string.IsNullOrWhiteSpace(verifyError) ? string.Empty : $" ({verifyError})";
                    sb.AppendLine($"Beam {beam}: return code {code}{suffix}");
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

        private static bool DoAllBeamsHaveExpectedSpec(object propObj, int[] beams, MiscMemberSpecType specType, out string errorLog)
        {
            var sb = new System.Text.StringBuilder();
            foreach (int beam in beams)
            {
                if (BeamHasExpectedSpec(propObj, beam, specType, out string error))
                    continue;

                if (sb.Length > 0)
                    sb.AppendLine();
                sb.Append("Beam ").Append(beam).Append(": ").Append(error);
            }

            errorLog = sb.ToString();
            return string.IsNullOrWhiteSpace(errorLog);
        }

        private static bool BeamHasExpectedSpec(object propObj, int beam, MiscMemberSpecType specType, out string error)
        {
            error = string.Empty;
            if (!TryGetMemberSpecCode(propObj, beam, out int specCode, out string specError))
            {
                error = string.IsNullOrWhiteSpace(specError)
                    ? "spec was not applied."
                    : specError;
                return false;
            }

            int expectedCode = GetExpectedMemberSpecCode(specType);
            if (expectedCode < 0)
            {
                error = "unknown expected member spec type.";
                return false;
            }

            if (specCode != expectedCode)
            {
                error = $"expected spec code {expectedCode}, actual {specCode}.";
                return false;
            }

            return true;
        }

        private static bool TryGetMemberSpecCode(object propObj, int beam, out int specCode, out string error)
        {
            specCode = -999;
            error = string.Empty;

            return OpenStaadPropertyAdapter.TryGetMemberSpecCode(propObj, beam, out specCode, out error);
        }

        private static int GetExpectedMemberSpecCode(MiscMemberSpecType specType)
        {
            switch (specType)
            {
                case MiscMemberSpecType.Truss:
                    return 0;
                case MiscMemberSpecType.Tension:
                    return 1;
                case MiscMemberSpecType.Compression:
                    return 2;
                default:
                    return -1;
            }
        }

        private static string GetCreateMethodName(MiscMemberSpecType specType)
        {
            switch (specType)
            {
                case MiscMemberSpecType.Truss:
                    return "CreateMemberTrussSpec";
                case MiscMemberSpecType.Tension:
                    return "CreateMemberTensionSpec";
                case MiscMemberSpecType.Compression:
                    return "CreateMemberCompressionSpec";
                default:
                    return string.Empty;
            }
        }

        private static string GetSpecTypeLabel(MiscMemberSpecType specType)
        {
            switch (specType)
            {
                case MiscMemberSpecType.Truss:
                    return "Truss";
                case MiscMemberSpecType.Tension:
                    return "Tension";
                case MiscMemberSpecType.Compression:
                    return "Compression";
                default:
                    return "Member";
            }
        }

        private static int InvokeCreateMemberSpec(object propObj, string methodName, out string error)
        {
            return OpenStaadPropertyAdapter.CreateMemberSpec(propObj, methodName, out error);
        }

        private static bool TryRemoveMemberSpec(object propObj, string methodName, int beam)
        {
            if (OpenStaadPropertyAdapter.TryRemoveMemberSpec(propObj, methodName, beam, out string error))
                return true;

            if (!string.IsNullOrWhiteSpace(error))
            {
                Debug.WriteLine($"{methodName} failed for beam {beam}: {error}");
            }

            return false;
        }
    }
}
