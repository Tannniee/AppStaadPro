using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core.Advance
{
    internal static class AdvanceSectionMergeService
    {
        internal static bool TryPreviewSectionMerge(out AdvanceSectionMergePreview preview, out string message)
        {
            preview = null;
            message = string.Empty;

            if (!AdvanceOpenStaadService.TryGetOpenStaad(out object openStaad, out message))
                return false;

            List<UsedSectionEntry> usedSections = SectionService.GetUsedSections(openStaad, forceRefresh: true);
            preview = new AdvanceSectionMergePreview
            {
                TotalUsedSectionGroups = usedSections.Count
            };

            foreach (UsedSectionEntry entry in usedSections.OrderBy(item => item.DisplayText, StringComparer.OrdinalIgnoreCase))
            {
                int[] propertyIds = NormalizeIds(entry.PropertyIds ?? new[] { entry.PropertyId });
                if (propertyIds.Length <= 1)
                {
                    preview.UniqueSectionGroupCount++;
                    continue;
                }

                int[] memberIds = NormalizeIds(entry.BeamIds);
                if (memberIds.Length == 0)
                {
                    preview.UniqueSectionGroupCount++;
                    continue;
                }

                preview.AddGroup(new AdvanceSectionMergeGroup
                {
                    DisplayName = string.IsNullOrWhiteSpace(entry.DisplayText) ? ("PROP-" + propertyIds[0]) : entry.DisplayText,
                    TargetPropertyId = propertyIds.Min(),
                    PropertyIds = propertyIds,
                    MemberIds = memberIds
                });
            }

            message = BuildPreviewText(preview);
            return true;
        }

        internal static bool TryMergeDuplicateSections(AdvanceSectionMergePreview preview, out int mergedMemberCount, out string message)
        {
            mergedMemberCount = 0;
            message = string.Empty;

            if (preview == null || preview.Groups.Count == 0)
            {
                message = "Run Preview first. No duplicate section groups are ready to merge.";
                return false;
            }

            if (!AdvanceOpenStaadService.TryGetOpenStaad(out object openStaad, out message))
                return false;

            if (!OpenStaadPropertyAdapter.TryGetPropertyObject(openStaad, out object propertyObj, out message))
                return false;

            var errors = new StringBuilder();
            int mergedGroupCount = 0;

            foreach (AdvanceSectionMergeGroup group in preview.Groups)
            {
                int[] sourceMembers = GetMembersNotUsingTarget(propertyObj, group.MemberIds, group.TargetPropertyId);
                if (sourceMembers.Length == 0)
                    continue;

                if (!TryAssignAndVerify(propertyObj, sourceMembers, group.TargetPropertyId, out string assignError))
                {
                    errors.AppendLine($"{group.DisplayName}: {assignError}");
                    continue;
                }

                SectionService.UpdateUsedSectionsCacheAfterAssignment(openStaad, sourceMembers, group.TargetPropertyId);
                mergedGroupCount++;
                mergedMemberCount += sourceMembers.Length;
            }

            string errorText = errors.ToString().Trim();
            if (mergedMemberCount == 0)
            {
                message = string.IsNullOrWhiteSpace(errorText)
                    ? "No members needed reassignment."
                    : errorText;
                return false;
            }

            message = $"Merged {mergedMemberCount} members across {mergedGroupCount} section groups.";
            if (!string.IsNullOrWhiteSpace(errorText))
                message += "\n\nIssues:\n" + errorText;

            return string.IsNullOrWhiteSpace(errorText);
        }

        internal static string BuildPreviewText(AdvanceSectionMergePreview preview)
        {
            if (preview == null)
                return "No preview data.";

            var sb = new StringBuilder();
            sb.AppendLine("Duplicate sections:");

            if (preview.Groups.Count == 0)
            {
                sb.AppendLine("- None");
            }
            else
            {
                foreach (AdvanceSectionMergeGroup group in preview.Groups)
                {
                    sb.AppendLine(
                        $"- {group.DisplayName}: Ref {string.Join(", ", group.PropertyIds)} -> target Ref {group.TargetPropertyId}, {group.MemberIds.Count} members");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Skipped:");
            sb.AppendLine($"- Unique sections: {preview.UniqueSectionGroupCount} groups");
            return sb.ToString().TrimEnd();
        }

        private static int[] GetMembersNotUsingTarget(object propertyObj, IEnumerable<int> memberIds, int targetPropertyId)
        {
            var result = new List<int>();

            foreach (int memberId in NormalizeIds(memberIds))
            {
                if (!OpenStaadPropertyAdapter.TryGetBeamSectionPropertyRefNoFromProperty(
                        propertyObj,
                        memberId,
                        out int currentPropertyId,
                        out string error))
                {
                    Debug.WriteLine($"GetBeamSectionPropertyRefNo failed for member {memberId}: {error}");
                    continue;
                }

                if (currentPropertyId != targetPropertyId)
                    result.Add(memberId);
            }

            return result.ToArray();
        }

        private static bool TryAssignAndVerify(object propertyObj, int[] memberIds, int propertyId, out string error)
        {
            error = string.Empty;
            bool bulkAccepted = OpenStaadPropertyAdapter.TryAssignBeamProperty(propertyObj, memberIds, propertyId, out string bulkError);
            if (bulkAccepted && VerifyAssignedProperty(propertyObj, memberIds, propertyId))
                return true;

            int success = 0;
            var errors = new StringBuilder();
            if (!bulkAccepted && !string.IsNullOrWhiteSpace(bulkError))
                errors.AppendLine("Bulk AssignBeamProperty failed: " + bulkError);

            foreach (int memberId in memberIds)
            {
                if (OpenStaadPropertyAdapter.TryAssignBeamProperty(propertyObj, memberId, propertyId, out string singleError))
                {
                    success++;
                    continue;
                }

                errors.AppendLine($"Member {memberId}: {singleError}");
            }

            if (success == memberIds.Length && VerifyAssignedProperty(propertyObj, memberIds, propertyId))
                return true;

            error = errors.ToString().Trim();
            if (string.IsNullOrWhiteSpace(error))
                error = $"Assignment verification failed for property {propertyId}.";

            return false;
        }

        private static bool VerifyAssignedProperty(object propertyObj, int[] memberIds, int propertyId)
        {
            foreach (int memberId in memberIds.Take(Math.Min(memberIds.Length, 20)))
            {
                if (!OpenStaadPropertyAdapter.TryGetBeamSectionPropertyRefNoFromProperty(
                        propertyObj,
                        memberId,
                        out int actualPropertyId,
                        out _) ||
                    actualPropertyId != propertyId)
                {
                    return false;
                }
            }

            return true;
        }

        private static int[] NormalizeIds(IEnumerable<int> ids)
        {
            return ids == null
                ? Array.Empty<int>()
                : ids.Where(id => id > 0).Distinct().OrderBy(id => id).ToArray();
        }
    }
}
