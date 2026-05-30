using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core.Advance
{
    internal static class AdvanceMemberReleaseMergeService
    {
        private const double SpringTolerance = 0.0000001;
        private static readonly string[] DofLabels = { "FX", "FY", "FZ", "MX", "MY", "MZ" };

        internal static bool TryPreviewMemberReleaseMerge(out AdvanceMemberReleaseMergePreview preview, out string message)
        {
            preview = null;
            message = string.Empty;

            if (!TryGetPropertyAndGeometry(out object propertyObj, out object geometry, out message))
                return false;

            if (!OpenStaadGeometryAdapter.TryGetBeamList(geometry, out int[] memberIds, out message))
                return false;

            preview = new AdvanceMemberReleaseMergePreview
            {
                TotalMemberCount = memberIds.Length
            };

            var groups = new Dictionary<MemberReleaseKey, List<int>>();
            foreach (int memberId in memberIds)
            {
                for (int location = 0; location <= 1; location++)
                {
                    if (!OpenStaadPropertyAdapter.TryGetMemberReleaseSpec(
                            propertyObj,
                            memberId,
                            location,
                            out int[] releaseStates,
                            out double[] springValues,
                            out string releaseError))
                    {
                        preview.ReadFailureCount++;
                        Debug.WriteLine($"GetMemberReleaseSpec failed for member {memberId}, loc {location}: {releaseError}");
                        continue;
                    }

                    var key = MemberReleaseKey.From(location, releaseStates, springValues);
                    if (!key.HasRelease)
                        continue;

                    if (!key.IsSimpleRelease)
                    {
                        preview.UnsupportedReleaseCount++;
                        continue;
                    }

                    if (!groups.TryGetValue(key, out List<int> groupMembers))
                    {
                        groupMembers = new List<int>();
                        groups.Add(key, groupMembers);
                    }

                    groupMembers.Add(memberId);
                }
            }

            preview.MembersWithReleaseCount = groups.Values.SelectMany(item => item).Distinct().Count();
            foreach (KeyValuePair<MemberReleaseKey, List<int>> pair in groups
                         .OrderBy(item => item.Key.Location)
                         .ThenBy(item => item.Key.DisplayText, StringComparer.OrdinalIgnoreCase))
            {
                int[] normalizedMembers = NormalizeIds(pair.Value);
                if (normalizedMembers.Length <= 1)
                {
                    preview.UniqueReleaseGroupCount++;
                    continue;
                }

                preview.AddGroup(new AdvanceMemberReleaseMergeGroup
                {
                    DisplayName = pair.Key.DisplayText,
                    Location = pair.Key.Location,
                    ReleaseStates = pair.Key.ReleaseStates.ToArray(),
                    SpringValues = pair.Key.SpringValues.ToArray(),
                    MemberIds = normalizedMembers
                });
            }

            message = BuildPreviewText(preview);
            return true;
        }

        internal static bool TryMergeMemberReleases(
            AdvanceMemberReleaseMergePreview preview,
            out int mergedMemberCount,
            out string message)
        {
            mergedMemberCount = 0;
            message = string.Empty;

            if (preview == null || preview.Groups.Count == 0)
            {
                message = "Run Preview first. No duplicate member releases are ready to merge.";
                return false;
            }

            if (!TryGetPropertyAndGeometry(out object propertyObj, out _, out message))
                return false;

            var errors = new StringBuilder();
            int mergedGroupCount = 0;

            foreach (AdvanceMemberReleaseMergeGroup group in preview.Groups)
            {
                int[] memberIds = NormalizeIds(group.MemberIds);
                if (memberIds.Length == 0)
                    continue;

                int specId = OpenStaadPropertyAdapter.CreateMemberReleaseSpec(
                    propertyObj,
                    group.Location,
                    group.ReleaseStates,
                    group.SpringValues,
                    out string createError);
                if (specId < 0)
                {
                    errors.AppendLine($"{group.DisplayName}: CreateMemberReleaseSpec failed ({createError}).");
                    continue;
                }

                if (!TryReplaceGroupRelease(propertyObj, group, specId, memberIds, out string replaceError))
                {
                    errors.AppendLine($"{group.DisplayName}: {replaceError}");
                    continue;
                }

                mergedGroupCount++;
                mergedMemberCount += memberIds.Length;
            }

            string errorText = errors.ToString().Trim();
            if (mergedMemberCount == 0)
            {
                message = string.IsNullOrWhiteSpace(errorText)
                    ? "No member releases needed merging."
                    : errorText;
                return false;
            }

            message = $"Merged {mergedMemberCount} member release assignments across {mergedGroupCount} release groups.";
            if (!string.IsNullOrWhiteSpace(errorText))
                message += "\n\nIssues:\n" + errorText;

            return string.IsNullOrWhiteSpace(errorText);
        }

        internal static string BuildPreviewText(AdvanceMemberReleaseMergePreview preview)
        {
            if (preview == null)
                return "No preview data.";

            var sb = new StringBuilder();
            sb.AppendLine("Duplicate member releases:");

            if (preview.Groups.Count == 0)
            {
                sb.AppendLine("- None");
            }
            else
            {
                foreach (AdvanceMemberReleaseMergeGroup group in preview.Groups)
                    sb.AppendLine($"- {group.DisplayName}: {group.MemberIds.Count} members");
            }

            sb.AppendLine();
            sb.AppendLine("Skipped:");
            sb.AppendLine($"- Single-member release groups: {preview.UniqueReleaseGroupCount}");
            sb.AppendLine($"- Members without release: {Math.Max(0, preview.TotalMemberCount - preview.MembersWithReleaseCount)}");
            if (preview.UnsupportedReleaseCount > 0)
                sb.AppendLine($"- Unsupported spring/partial/MP release assignments: {preview.UnsupportedReleaseCount}");
            if (preview.ReadFailureCount > 0)
                sb.AppendLine($"- Read failures: {preview.ReadFailureCount}");

            return sb.ToString().TrimEnd();
        }

        private static bool TryGetPropertyAndGeometry(out object propertyObj, out object geometry, out string message)
        {
            propertyObj = null;
            geometry = null;

            if (!AdvanceOpenStaadService.TryGetOpenStaad(out object openStaad, out message))
                return false;

            if (!OpenStaadPropertyAdapter.TryGetPropertyObject(openStaad, out propertyObj, out message))
                return false;

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Geometry", out geometry, out message))
                return false;

            return true;
        }

        private static bool TryReplaceGroupRelease(
            object propertyObj,
            AdvanceMemberReleaseMergeGroup group,
            int specId,
            int[] memberIds,
            out string error)
        {
            error = string.Empty;
            var errors = new StringBuilder();
            var removedMembers = new List<int>(memberIds.Length);

            foreach (int memberId in memberIds)
            {
                if (TryRemoveRelease(propertyObj, memberId, group.Location, out string removeError))
                {
                    removedMembers.Add(memberId);
                    continue;
                }

                errors.AppendLine($"Member {memberId}: remove failed ({removeError}).");
            }

            int[] assignTargets = removedMembers.ToArray();
            if (assignTargets.Length == 0)
            {
                error = errors.ToString().Trim();
                if (string.IsNullOrWhiteSpace(error))
                    error = "No members could be cleared for reassignment.";
                return false;
            }

            if (!TryAssignMemberSpec(propertyObj, assignTargets, specId, out string assignError))
                errors.AppendLine("Assign failed: " + assignError);

            if (!VerifyGroupRelease(propertyObj, group, assignTargets, out string verifyError))
                errors.AppendLine("Verification failed: " + verifyError);

            error = errors.ToString().Trim();
            return string.IsNullOrWhiteSpace(error);
        }

        private static bool TryRemoveRelease(object propertyObj, int memberId, int location, out string error)
        {
            if (OpenStaadPropertyAdapter.TryRemoveMemberReleaseSpecFromBeam(propertyObj, memberId, location, out error))
                return true;

            if (OpenStaadPropertyAdapter.TryDeleteMemberReleaseSpec(propertyObj, memberId, location, out error))
                return true;

            return false;
        }

        private static bool TryAssignMemberSpec(object propertyObj, int[] memberIds, int specId, out string errorLog)
        {
            var errors = new StringBuilder();
            int bulkCode = OpenStaadPropertyAdapter.AssignMemberSpecToBeam(propertyObj, memberIds, specId, out string bulkError);
            if (IsAssignSuccess(bulkCode))
            {
                errorLog = string.Empty;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(bulkError))
                errors.AppendLine("Bulk AssignMemberSpecToBeam failed: " + bulkError);
            else
                errors.AppendLine("Bulk AssignMemberSpecToBeam returned code " + bulkCode);

            int success = 0;
            foreach (int memberId in memberIds)
            {
                int code = OpenStaadPropertyAdapter.AssignMemberSpecToBeam(propertyObj, new[] { memberId }, specId, out string singleError);
                if (IsAssignSuccess(code))
                {
                    success++;
                    continue;
                }

                errors.AppendLine(string.IsNullOrWhiteSpace(singleError)
                    ? $"Member {memberId}: return code {code}"
                    : $"Member {memberId}: {singleError}");
            }

            errorLog = errors.ToString().Trim();
            return success == memberIds.Length;
        }

        private static bool VerifyGroupRelease(
            object propertyObj,
            AdvanceMemberReleaseMergeGroup group,
            int[] memberIds,
            out string error)
        {
            var errors = new StringBuilder();
            foreach (int memberId in memberIds.Take(Math.Min(memberIds.Length, 40)))
            {
                if (!OpenStaadPropertyAdapter.TryGetMemberReleaseSpec(
                        propertyObj,
                        memberId,
                        group.Location,
                        out int[] releaseStates,
                        out double[] springValues,
                        out string readError))
                {
                    errors.AppendLine($"Member {memberId}: {readError}");
                    continue;
                }

                if (!MemberReleaseKey.Matches(group.Location, group.ReleaseStates, group.SpringValues, releaseStates, springValues))
                    errors.AppendLine($"Member {memberId}: release values changed.");
            }

            error = errors.ToString().Trim();
            return string.IsNullOrWhiteSpace(error);
        }

        private static bool IsAssignSuccess(int code)
        {
            return code == 0 || code == 1;
        }

        private static int[] NormalizeIds(IEnumerable<int> ids)
        {
            return ids == null
                ? Array.Empty<int>()
                : ids.Where(id => id > 0).Distinct().OrderBy(id => id).ToArray();
        }

        private struct MemberReleaseKey : IEquatable<MemberReleaseKey>
        {
            internal int Location;
            internal int[] ReleaseStates;
            internal double[] SpringValues;
            internal bool HasRelease;
            internal bool IsSimpleRelease;
            internal string DisplayText;

            internal static MemberReleaseKey From(int location, int[] releaseStates, double[] springValues)
            {
                int[] normalizedRelease = NormalizeReleaseStates(releaseStates);
                double[] normalizedSpring = NormalizeSpringValues(springValues);
                bool hasRelease = normalizedRelease.Any(state => state != 0) ||
                    normalizedSpring.Any(value => Math.Abs(value) > SpringTolerance);
                bool isSimpleRelease = normalizedRelease.All(state => state == 0 || state == 1) &&
                    normalizedSpring.All(value => Math.Abs(value) <= SpringTolerance);

                return new MemberReleaseKey
                {
                    Location = location,
                    ReleaseStates = normalizedRelease,
                    SpringValues = normalizedSpring,
                    HasRelease = hasRelease,
                    IsSimpleRelease = isSimpleRelease,
                    DisplayText = FormatDisplayText(location, normalizedRelease, normalizedSpring)
                };
            }

            internal static bool Matches(
                int location,
                int[] expectedRelease,
                double[] expectedSpring,
                int[] actualRelease,
                double[] actualSpring)
            {
                MemberReleaseKey expected = From(location, expectedRelease, expectedSpring);
                MemberReleaseKey actual = From(location, actualRelease, actualSpring);
                return expected.Equals(actual);
            }

            public bool Equals(MemberReleaseKey other)
            {
                return Location == other.Location &&
                    ReleaseStates.SequenceEqual(other.ReleaseStates) &&
                    SpringValues.SequenceEqual(other.SpringValues);
            }

            public override bool Equals(object obj)
            {
                return obj is MemberReleaseKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = Location;
                    foreach (int state in ReleaseStates)
                        hash = (hash * 397) ^ state;
                    foreach (double value in SpringValues)
                        hash = (hash * 397) ^ value.GetHashCode();
                    return hash;
                }
            }

            private static int[] NormalizeReleaseStates(int[] values)
            {
                var result = new int[6];
                if (values == null)
                    return result;

                for (int i = 0; i < result.Length && i < values.Length; i++)
                    result[i] = values[i];
                return result;
            }

            private static double[] NormalizeSpringValues(double[] values)
            {
                var result = new double[6];
                if (values == null)
                    return result;

                for (int i = 0; i < result.Length && i < values.Length; i++)
                    result[i] = Math.Abs(values[i]) <= SpringTolerance ? 0.0 : Math.Round(values[i], 7);
                return result;
            }

            private static string FormatDisplayText(int location, int[] releaseStates, double[] springValues)
            {
                string prefix = location == 0 ? "START" : "END";
                var parts = new List<string>();
                for (int i = 0; i < DofLabels.Length; i++)
                {
                    bool released = i < releaseStates.Length && releaseStates[i] != 0;
                    bool spring = i < springValues.Length && Math.Abs(springValues[i]) > SpringTolerance;
                    if (released || spring)
                        parts.Add(DofLabels[i]);
                }

                return parts.Count == 0 ? prefix : prefix + " " + string.Join(" ", parts);
            }
        }
    }
}
