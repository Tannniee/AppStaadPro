using System;
using System.Collections.Generic;
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

            if (!OpenStaadPropertyAdapter.TryGetPropertyObject(openStaad, out object propertyObj, out message))
                return false;

            List<UsedSectionEntry> usedSections = SectionService.GetUsedSections(openStaad, forceRefresh: true);
            preview = new AdvanceSectionMergePreview
            {
                TotalUsedSectionGroups = usedSections.Count
            };
            var warnings = new StringBuilder();

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

                if (!AdvanceMemberAssignmentService.TryCapture(
                        propertyObj,
                        memberIds,
                        out AdvanceMemberAssignmentSnapshot assignmentSnapshot,
                        out string materialError))
                {
                    preview.UnsafeMaterialSectionGroupCount++;
                    preview.UnsafeMaterialMemberCount += memberIds.Length;
                    warnings.AppendLine($"{entry.DisplayText}: Material could not be read safely. {materialError}");
                    continue;
                }

                int addedGroupCount = 0;
                foreach (KeyValuePair<string, int[]> materialGroup in assignmentSnapshot.GetMaterialGroups())
                {
                    int[] materialPropertyIds = materialGroup.Value
                        .Select(memberId => assignmentSnapshot.PropertyByMember[memberId])
                        .Where(propertyId => propertyId > 0)
                        .Distinct()
                        .OrderBy(propertyId => propertyId)
                        .ToArray();

                    if (materialPropertyIds.Length <= 1)
                        continue;

                    preview.AddGroup(new AdvanceSectionMergeGroup
                    {
                        DisplayName = string.IsNullOrWhiteSpace(entry.DisplayText) ? ("PROP-" + materialPropertyIds[0]) : entry.DisplayText,
                        TargetPropertyId = materialPropertyIds.Min(),
                        MaterialName = materialGroup.Key,
                        PropertyIds = materialPropertyIds,
                        MemberIds = materialGroup.Value
                    });
                    addedGroupCount++;
                }

                if (addedGroupCount == 0)
                    preview.UniqueSectionGroupCount++;
            }

            preview.WarningText = warnings.ToString().Trim();
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
                if (!TryGetMembersNotUsingTarget(propertyObj, group, out int[] sourceMembers, out string sourceError))
                {
                    errors.AppendLine($"{group.DisplayName} [{group.MaterialName}]: {sourceError}");
                    continue;
                }

                if (sourceMembers.Length == 0)
                    continue;

                if (!AdvanceMemberAssignmentService.TryReassignPropertyPreservingMaterial(
                        propertyObj,
                        sourceMembers,
                        group.TargetPropertyId,
                        group.MaterialName,
                        out _,
                        out string assignError))
                {
                    errors.AppendLine($"{group.DisplayName} [{group.MaterialName}]: {assignError}");
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

            message = $"Merged {mergedMemberCount} members across {mergedGroupCount} section groups. Material was preserved and verified.";
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
                        $"- {group.DisplayName} [{group.MaterialName}]: Ref {string.Join(", ", group.PropertyIds)} -> target Ref {group.TargetPropertyId}, {group.MemberIds.Count} members");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Skipped:");
            sb.AppendLine($"- No duplicate Ref IDs within one Material: {preview.UniqueSectionGroupCount} section groups");
            sb.AppendLine($"- Material could not be preserved safely: {preview.UnsafeMaterialSectionGroupCount} groups, {preview.UnsafeMaterialMemberCount} members");

            if (!string.IsNullOrWhiteSpace(preview.WarningText))
            {
                sb.AppendLine();
                sb.AppendLine("Details:");
                sb.AppendLine(preview.WarningText);
            }

            return sb.ToString().TrimEnd();
        }

        private static bool TryGetMembersNotUsingTarget(
            object propertyObj,
            AdvanceSectionMergeGroup group,
            out int[] sourceMemberIds,
            out string error)
        {
            sourceMemberIds = Array.Empty<int>();
            error = string.Empty;
            var result = new List<int>();
            var errors = new StringBuilder();
            var allowedPropertyIds = new HashSet<int>(group.PropertyIds ?? Array.Empty<int>());

            foreach (int memberId in NormalizeIds(group.MemberIds))
            {
                if (!OpenStaadPropertyAdapter.TryGetBeamSectionPropertyRefNoFromProperty(
                        propertyObj,
                        memberId,
                        out int currentPropertyId,
                        out string readError))
                {
                    errors.AppendLine($"Member {memberId}: could not read section Ref ID. {readError}");
                    continue;
                }

                if (!allowedPropertyIds.Contains(currentPropertyId))
                {
                    errors.AppendLine($"Member {memberId}: section changed to Ref {currentPropertyId}. Run Preview again.");
                    continue;
                }

                if (currentPropertyId != group.TargetPropertyId)
                    result.Add(memberId);
            }

            string errorText = errors.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(errorText))
            {
                error = errorText;
                return false;
            }

            sourceMemberIds = result.ToArray();
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
