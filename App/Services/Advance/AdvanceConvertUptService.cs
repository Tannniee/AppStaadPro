using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core.Advance
{
    internal static partial class AdvanceConvertUptService
    {
        private const double DimensionToleranceMm = 0.001;
        private const int TaperedTubeTypeCode = 675;
        private const int TaperedTubeRoundReadType = 1;
        private const int TaperedTubeSquareReadType = 6;

        internal static bool TryPreviewLegacyPipeTubeConversion(out AdvanceUptConversionPreview preview, out string message)
        {
            preview = null;
            message = string.Empty;

            if (!AdvanceOpenStaadService.TryGetOpenStaad(out object openStaad, out message))
                return false;

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Geometry", out object geometry, out message))
                return false;

            if (!OpenStaadPropertyAdapter.TryGetPropertyObject(openStaad, out object propertyObj, out message))
                return false;

            if (!OpenStaadGeometryAdapter.TryGetBeamList(geometry, out int[] beamIds, out string beamError))
            {
                message = "Could not read member list: " + beamError;
                return false;
            }

            beamIds = NormalizeIds(beamIds);
            preview = new AdvanceUptConversionPreview { TotalMemberCount = beamIds.Length };
            if (beamIds.Length == 0)
            {
                message = "No analytical members found in STAAD.";
                return true;
            }

            Dictionary<int, List<int>> membersByProperty = BuildMembersByProperty(propertyObj, beamIds, preview);
            var buckets = new Dictionary<string, PreviewBucket>(StringComparer.OrdinalIgnoreCase);
            var warnings = new StringBuilder();

            foreach (KeyValuePair<int, List<int>> item in membersByProperty.OrderBy(pair => pair.Key))
            {
                int propertyRefNo = item.Key;
                List<int> members = item.Value;
                int memberCount = members.Count;

                if (!OpenStaadPropertyAdapter.TryGetSectionPropertyValuesExFromProperty(
                        propertyObj,
                        propertyRefNo,
                        out int typeCode,
                        out double[] props,
                        out string valuesError))
                {
                    preview.UnreadableMemberCount += memberCount;
                    warnings.AppendLine($"Ref {propertyRefNo}: {valuesError}");
                    continue;
                }

                if (IsExistingUptPipeOrTube(typeCode))
                {
                    preview.AlreadyUptMemberCount += memberCount;
                    continue;
                }

                if (!TryBuildCandidate(
                        typeCode,
                        props,
                        out ConversionCandidate candidate,
                        out CandidateBuildFailure failure,
                        out string dimensionError))
                {
                    if (failure == CandidateBuildFailure.NonUniformTapered)
                    {
                        preview.NonUniformTaperedMemberCount += memberCount;
                        warnings.AppendLine($"Ref {propertyRefNo}: {dimensionError}");
                    }
                    else if (failure == CandidateBuildFailure.UnsupportedTaperedSubtype)
                    {
                        preview.UnsupportedTaperedSubtypeMemberCount += memberCount;
                        warnings.AppendLine($"Ref {propertyRefNo}: {dimensionError}");
                    }
                    else if (IsConvertiblePipeOrTube(typeCode))
                    {
                        preview.InvalidDimensionMemberCount += memberCount;
                        warnings.AppendLine($"Ref {propertyRefNo}: {dimensionError}");
                    }
                    else
                    {
                        preview.NotPipeTubeMemberCount += memberCount;
                    }

                    continue;
                }

                if (!AdvanceMemberAssignmentService.TryCapture(
                        propertyObj,
                        members,
                        out AdvanceMemberAssignmentSnapshot assignmentSnapshot,
                        out string materialError))
                {
                    preview.UnsafeMaterialMemberCount += memberCount;
                    warnings.AppendLine($"Ref {propertyRefNo}: Material could not be read safely. {materialError}");
                    continue;
                }

                foreach (KeyValuePair<string, int[]> materialGroup in assignmentSnapshot.GetMaterialGroups())
                {
                    string bucketKey = candidate.Key + "|MAT|" + materialGroup.Key;
                    if (!buckets.TryGetValue(bucketKey, out PreviewBucket bucket))
                    {
                        bucket = new PreviewBucket(candidate, materialGroup.Key);
                        buckets.Add(bucketKey, bucket);
                    }

                    bucket.PropertyRefIds.Add(propertyRefNo);
                    bucket.SourceDescriptions.Add(candidate.SourceDescription);
                    bucket.MemberIds.AddRange(materialGroup.Value);
                    foreach (int memberId in materialGroup.Value)
                        bucket.SourcePropertyRefByMember[memberId] = propertyRefNo;
                }
            }

            foreach (PreviewBucket bucket in buckets.Values.OrderBy(item => item.Candidate.SortKey))
                preview.AddGroup(bucket.ToGroup());

            preview.WarningText = warnings.ToString().Trim();
            message = BuildPreviewText(preview);
            return true;
        }

        internal static bool TryConvertLegacyPipeTubeToUpt(AdvanceUptConversionPreview preview, out int convertedMemberCount, out string message)
        {
            convertedMemberCount = 0;
            message = string.Empty;

            if (preview == null || preview.Groups.Count == 0)
            {
                message = "Run Preview first. No Pipe/Tube groups are ready to convert.";
                return false;
            }

            if (!AdvanceOpenStaadService.TryGetOpenStaad(out object openStaad, out message))
                return false;

            if (!OpenStaadPropertyAdapter.TryGetPropertyObject(openStaad, out object propertyObj, out message))
                return false;

            var errors = new StringBuilder();
            int convertedGroups = 0;

            foreach (AdvanceUptConversionGroup group in preview.Groups)
            {
                int[] memberIds = NormalizeIds(group.MemberIds);
                if (memberIds.Length == 0)
                    continue;

                try
                {
                    if (!TryValidatePreviewGroup(propertyObj, group, out string validationError))
                    {
                        errors.AppendLine($"{group.DisplayName}: {validationError}");
                        continue;
                    }

                    SectionInfo info = BuildSectionInfo(group);
                    int propertyId = SectionUptModule.GetOrCreateUPTSection(openStaad, info, CheckPropertyExists);
                    if (propertyId <= 0)
                    {
                        errors.AppendLine($"{group.DisplayName}: could not create UPT property.");
                        continue;
                    }

                    if (!AdvanceMemberAssignmentService.TryReassignPropertyPreservingMaterial(
                            propertyObj,
                            memberIds,
                            propertyId,
                            group.MaterialName,
                            out _,
                            out string assignError))
                    {
                        errors.AppendLine($"{group.DisplayName}: {assignError}");
                        continue;
                    }

                    SectionService.UpdateUsedSectionsCacheAfterAssignment(openStaad, memberIds, propertyId);
                    convertedGroups++;
                    convertedMemberCount += memberIds.Length;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Convert UPT failed for {group.DisplayName}: {ex}");
                    errors.AppendLine($"{group.DisplayName}: {ex.Message}");
                }
            }

            string errorText = errors.ToString().Trim();
            if (convertedMemberCount == 0)
            {
                message = string.IsNullOrWhiteSpace(errorText)
                    ? "No members were converted."
                    : errorText;
                return false;
            }

            message = $"Converted {convertedMemberCount} members across {convertedGroups} groups. Section and Material assignments were verified.";
            if (!string.IsNullOrWhiteSpace(errorText))
                message += "\n\nIssues:\n" + errorText;

            return string.IsNullOrWhiteSpace(errorText);
        }

        internal static string BuildPreviewText(AdvanceUptConversionPreview preview)
        {
            if (preview == null)
                return "No preview data.";

            var sb = new StringBuilder();
            sb.AppendLine("Eligible sections:");
            if (preview.Groups.Count == 0)
            {
                sb.AppendLine("- None");
            }
            else
            {
                foreach (AdvanceUptConversionGroup group in preview.Groups)
                {
                    sb.AppendLine(
                        $"- {group.SourceDescription} -> {group.DisplayName} [{group.MaterialName}]: {group.MemberIds.Count} members, {group.LegacyPropertyRefIds.Count} source Ref IDs");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Skipped:");
            sb.AppendLine($"- Already UPT: {preview.AlreadyUptMemberCount} members");
            sb.AppendLine($"- Tapered D1/D2 differ by more than {DimensionToleranceMm:0.###} mm: {preview.NonUniformTaperedMemberCount} members");
            sb.AppendLine($"- Unsupported tapered subtype: {preview.UnsupportedTaperedSubtypeMemberCount} members");
            sb.AppendLine($"- Not a supported Pipe/Tube source: {preview.NotPipeTubeMemberCount} members");
            sb.AppendLine($"- Unreadable or invalid dimensions: {preview.UnreadableMemberCount + preview.InvalidDimensionMemberCount} members");
            sb.AppendLine($"- Material could not be preserved safely: {preview.UnsafeMaterialMemberCount} members");

            if (!string.IsNullOrWhiteSpace(preview.WarningText))
            {
                sb.AppendLine();
                sb.AppendLine("Details:");
                sb.AppendLine(preview.WarningText);
            }

            return sb.ToString().TrimEnd();
        }
    }
}
