using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core.Misc
{
    internal static class MemberSpecService
    {
        private static readonly object _specCacheLock = new object();
        private static readonly Dictionary<int, MiscMemberSpecType> _lastAppliedSpecByBeam = new Dictionary<int, MiscMemberSpecType>();

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
                    int cleared = ClearMemberSpecs(propObj, beamIds, null, out string clearWarning);
                    ClearCachedSpecs(beamIds);
                    if (!string.IsNullOrWhiteSpace(clearWarning))
                        Debug.WriteLine(clearWarning);

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

                ClearMemberSpecs(propObj, beamIds, specType, out string clearConflictsWarning);
                if (!string.IsNullOrWhiteSpace(clearConflictsWarning))
                    Debug.WriteLine(clearConflictsWarning);

                if (!TryAssignMemberSpec(propObj, beamIds, specId, specType, out string assignError))
                {
                    return ServiceOperationResult.Error("Member Spec", "Assign member spec failed: " + assignError);
                }

                CacheSpecForBeams(beamIds, specType);
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

        private static int ClearMemberSpecs(object propObj, int[] beams, MiscMemberSpecType? keepSpecType, out string warningLog)
        {
            var warnings = new StringBuilder();
            int cleared = 0;

            foreach (int beam in beams)
            {
                if (beam <= 0)
                    continue;

                if (TryGetCachedSpecCode(beam, out int cachedSpecCode))
                {
                    if (keepSpecType.HasValue && cachedSpecCode == GetExpectedMemberSpecCode(keepSpecType.Value))
                    {
                        cleared++;
                        continue;
                    }

                    if (TryRemoveMemberSpecByCode(propObj, beam, cachedSpecCode, out string cachedRemoveError))
                    {
                        cleared++;
                        RemoveCachedSpec(beam);
                        continue;
                    }

                    warnings.AppendLine($"Beam {beam}: cached clear failed ({cachedRemoveError}); using fallback clear.");
                }

                bool ok = true;
                if (!keepSpecType.HasValue || keepSpecType.Value != MiscMemberSpecType.Truss)
                    ok &= TryRemoveMemberSpec(propObj, "RemoveMemberTrussSpecFromBeam", beam);
                if (!keepSpecType.HasValue || keepSpecType.Value != MiscMemberSpecType.Tension)
                    ok &= TryRemoveMemberSpec(propObj, "RemoveMemberTensionSpecFromBeam", beam);
                if (!keepSpecType.HasValue || keepSpecType.Value != MiscMemberSpecType.Compression)
                    ok &= TryRemoveMemberSpec(propObj, "RemoveMemberCompressionSpecFromBeam", beam);

                if (ok)
                {
                    RemoveCachedSpec(beam);
                    cleared++;
                }
            }

            warningLog = warnings.ToString();
            return cleared;
        }

        private static void CacheSpecForBeams(int[] beams, MiscMemberSpecType specType)
        {
            if (beams == null || beams.Length == 0)
                return;

            lock (_specCacheLock)
            {
                foreach (int beam in beams)
                {
                    if (beam > 0)
                        _lastAppliedSpecByBeam[beam] = specType;
                }
            }
        }

        private static void ClearCachedSpecs(int[] beams)
        {
            if (beams == null || beams.Length == 0)
                return;

            lock (_specCacheLock)
            {
                foreach (int beam in beams)
                {
                    if (beam > 0)
                        _lastAppliedSpecByBeam.Remove(beam);
                }
            }
        }

        private static void RemoveCachedSpec(int beam)
        {
            if (beam <= 0)
                return;

            lock (_specCacheLock)
            {
                _lastAppliedSpecByBeam.Remove(beam);
            }
        }

        private static bool TryGetCachedSpecCode(int beam, out int specCode)
        {
            specCode = -1;
            if (beam <= 0)
                return false;

            lock (_specCacheLock)
            {
                if (!_lastAppliedSpecByBeam.TryGetValue(beam, out MiscMemberSpecType specType))
                    return false;

                specCode = GetExpectedMemberSpecCode(specType);
                return specCode >= 0;
            }
        }

        private static bool TryRemoveMemberSpecByCode(object propObj, int beam, int specCode, out string error)
        {
            error = string.Empty;
            string methodName = GetRemoveMethodName(specCode);
            if (string.IsNullOrWhiteSpace(methodName))
            {
                error = $"unknown member spec code {specCode}.";
                return false;
            }

            if (TryRemoveMemberSpec(propObj, methodName, beam))
                return true;

            error = $"{methodName} failed.";
            return false;
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

        private static string GetRemoveMethodName(int specCode)
        {
            switch (specCode)
            {
                case 0:
                    return "RemoveMemberTrussSpecFromBeam";
                case 1:
                    return "RemoveMemberTensionSpecFromBeam";
                case 2:
                    return "RemoveMemberCompressionSpecFromBeam";
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
