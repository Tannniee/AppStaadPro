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

                if (!TryBuildCandidate(typeCode, props, out ConversionCandidate candidate, out string dimensionError))
                {
                    if (IsLegacyPipeOrTube(typeCode))
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

                if (!buckets.TryGetValue(candidate.Key, out PreviewBucket bucket))
                {
                    bucket = new PreviewBucket(candidate);
                    buckets.Add(candidate.Key, bucket);
                }

                bucket.PropertyRefIds.Add(propertyRefNo);
                bucket.MemberIds.AddRange(members);
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
                message = "Run Preview first. No legacy Pipe/Tube groups are ready to convert.";
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
                    SectionInfo info = BuildSectionInfo(group);
                    int propertyId = SectionUptModule.GetOrCreateUPTSection(openStaad, info, CheckPropertyExists);
                    if (propertyId <= 0)
                    {
                        errors.AppendLine($"{group.DisplayName}: could not create UPT property.");
                        continue;
                    }

                    if (!TryAssignAndVerify(propertyObj, memberIds, propertyId, out string assignError))
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

            message = $"Converted {convertedMemberCount} members across {convertedGroups} groups.";
            if (!string.IsNullOrWhiteSpace(errorText))
                message += "\n\nIssues:\n" + errorText;

            return string.IsNullOrWhiteSpace(errorText);
        }

        internal static string BuildPreviewText(AdvanceUptConversionPreview preview)
        {
            if (preview == null)
                return "No preview data.";

            var sb = new StringBuilder();
            sb.AppendLine("Eligible legacy sections:");
            if (preview.Groups.Count == 0)
            {
                sb.AppendLine("- None");
            }
            else
            {
                foreach (AdvanceUptConversionGroup group in preview.Groups)
                {
                    sb.AppendLine(
                        $"- {group.DisplayName}: {group.MemberIds.Count} members, {group.LegacyPropertyRefIds.Count} legacy Ref IDs");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Skipped:");
            sb.AppendLine($"- Already UPT: {preview.AlreadyUptMemberCount} members");
            sb.AppendLine($"- Not Pipe/Tube: {preview.NotPipeTubeMemberCount} members");
            sb.AppendLine($"- Could not read dimensions: {preview.UnreadableMemberCount + preview.InvalidDimensionMemberCount} members");

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
