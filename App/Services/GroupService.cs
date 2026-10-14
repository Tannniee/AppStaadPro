using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static class GroupService
    {
        private const int MemberGroupType = 2;
        private const int UpdateReplace = 0;
        private const int UpdateRemove = 1;
        private const int UpdateAdd = 2;

        public static IReadOnlyList<GroupEntry> GetMemberGroups()
        {
            object openStaad = GetOpenStaadOrThrow();
            return GetMemberGroups(openStaad);
        }

        internal static IReadOnlyList<GroupEntry> GetMemberGroups(object openStaad)
        {
            if (!OpenStaadGeometryAdapter.TryGetGroupNames(openStaad, MemberGroupType, out string[] names, out string error))
                throw new InvalidOperationException("Could not read member groups: " + error);

            var entries = new List<GroupEntry>();
            foreach (string name in names.OrderBy(x => x, NaturalGroupNameComparer.Instance))
            {
                int entityCount = GetGroupEntityCount(openStaad, name);
                entries.Add(new GroupEntry
                {
                    Name = name,
                    EntityIds = Array.Empty<int>(),
                    EntityCount = entityCount
                });
            }

            return entries;
        }

        public static GroupEntry GetMemberGroup(string groupName)
        {
            object openStaad = GetOpenStaadOrThrow();
            int[] entities = GetGroupEntities(openStaad, groupName);
            return new GroupEntry
            {
                Name = groupName,
                EntityIds = entities,
                EntityCount = entities.Length
            };
        }

        public static void HighlightMemberGroup(string groupName)
        {
            HighlightMemberGroups(new[] { groupName });
        }

        public static void HighlightMemberGroups(IEnumerable<string> groupNames)
        {
            object openStaad = GetOpenStaadOrThrow();
            HighlightMemberGroups(openStaad, groupNames);
        }

        internal static void HighlightMemberGroups(object openStaad, IEnumerable<string> groupNames)
        {
            string[] names = (groupNames ?? Enumerable.Empty<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(NormalizeGroupName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (names.Length == 0)
                throw new InvalidOperationException("Please select at least one group.");

            int[] entities = names
                .SelectMany(name => GetGroupEntities(openStaad, name))
                .Where(id => id > 0)
                .Distinct()
                .OrderBy(id => id)
                .ToArray();

            if (entities.Length == 0)
                throw new InvalidOperationException("The selected group(s) do not contain any members.");

            HighlightMembers(openStaad, entities);
        }

        public static string CreateMemberGroupFromSelection(string groupName)
        {
            object openStaad = GetOpenStaadOrThrow();
            return CreateMemberGroupFromSelection(openStaad, groupName);
        }

        internal static string CreateMemberGroupFromSelection(object openStaad, string groupName)
        {
            string normalizedName = NormalizeNewGroupName(groupName);
            int[] selectedMembers = GetSelectedMembers(openStaad);

            if (MemberGroupExists(openStaad, normalizedName))
                throw new InvalidOperationException("Group '" + normalizedName + "' already exists.");

            if (!OpenStaadGeometryAdapter.TryCreateGroup(openStaad, MemberGroupType, normalizedName, selectedMembers, out string error))
                throw new InvalidOperationException(error);

            int[] createdMembers = GetGroupEntities(openStaad, normalizedName);
            if (createdMembers.Length == 0)
                throw new InvalidOperationException("Group '" + normalizedName + "' was created, but STAAD returned no members for it.");

            return normalizedName;
        }

        public static void DeleteGroup(string groupName)
        {
            object openStaad = GetOpenStaadOrThrow();
            DeleteGroup(openStaad, groupName);
        }

        internal static void DeleteGroup(object openStaad, string groupName)
        {
            string normalizedName = NormalizeGroupName(groupName);

            if (!OpenStaadGeometryAdapter.TryDeleteGroup(openStaad, normalizedName, out string error))
                throw new InvalidOperationException(error);
        }

        public static void AddSelectedMembersToGroup(string groupName)
        {
            UpdateMemberGroupFromSelection(GetOpenStaadOrThrow(), groupName, UpdateAdd);
        }

        internal static void AddSelectedMembersToGroup(object openStaad, string groupName)
        {
            UpdateMemberGroupFromSelection(openStaad, groupName, UpdateAdd);
        }

        public static void RemoveSelectedMembersFromGroup(string groupName)
        {
            UpdateMemberGroupFromSelection(GetOpenStaadOrThrow(), groupName, UpdateRemove);
        }

        internal static void RemoveSelectedMembersFromGroup(object openStaad, string groupName)
        {
            UpdateMemberGroupFromSelection(openStaad, groupName, UpdateRemove);
        }

        public static void ReplaceGroupWithSelectedMembers(string groupName)
        {
            UpdateMemberGroupFromSelection(GetOpenStaadOrThrow(), groupName, UpdateReplace);
        }

        private static void UpdateMemberGroupFromSelection(object openStaad, string groupName, int updateFlag)
        {
            string normalizedName = NormalizeGroupName(groupName);
            int[] selectedMembers = GetSelectedMembers(openStaad);

            if (!OpenStaadGeometryAdapter.TryUpdateGroup(openStaad, normalizedName, updateFlag, selectedMembers, out string error))
                throw new InvalidOperationException(error);
        }

        private static int[] GetSelectedMembers(object openStaad)
        {
            if (!OpenStaadSelectionAdapter.TryGetSelectedBeamIds(
                    openStaad,
                    validateIncidence: true,
                    includeHiddenEntities: false,
                    sortFlags: new[] { 0, 1 },
                    out int[] selectedMembers,
                    out string error))
            {
                throw new InvalidOperationException("Could not read selected members: " + error);
            }

            if (selectedMembers.Length == 0)
                throw new InvalidOperationException("Please select member(s) in STAAD.");

            return selectedMembers;
        }

        private static int[] GetGroupEntities(object openStaad, string groupName)
        {
            string normalizedName = NormalizeGroupName(groupName);
            if (!OpenStaadGeometryAdapter.TryGetGroupEntities(openStaad, normalizedName, out int[] entities, out string error))
                throw new InvalidOperationException("Could not read group entities: " + error);

            return entities ?? Array.Empty<int>();
        }

        private static int GetGroupEntityCount(object openStaad, string groupName)
        {
            string normalizedName = NormalizeGroupName(groupName);
            if (!OpenStaadGeometryAdapter.TryGetGroupEntityCount(openStaad, normalizedName, out int count, out string error))
                throw new InvalidOperationException("Could not read group entity count: " + error);

            return count;
        }

        private static void HighlightMembers(object openStaad, IReadOnlyList<int> members)
        {
            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Geometry", out object geometry, out string geometryError))
                throw new InvalidOperationException(geometryError);

            if (!OpenStaadGeometryAdapter.TryHighlightBeams(geometry, members, out string highlightError))
                throw new InvalidOperationException("Could not highlight group members: " + highlightError);
        }

        private static object GetOpenStaadOrThrow()
        {
            if (StaadManager.TryGetOpenSTAAD(out object openStaad, out string error))
                return openStaad;

            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? "STAAD is not available." : error);
        }

        private static bool MemberGroupExists(object openStaad, string groupName)
        {
            if (!OpenStaadGeometryAdapter.TryGetGroupNames(openStaad, MemberGroupType, out string[] names, out _))
                return false;

            return names.Any(name => string.Equals(name, groupName, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeGroupName(string groupName)
        {
            string normalized = (groupName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                throw new InvalidOperationException("Please enter a group name.");

            return normalized;
        }

        private static string NormalizeNewGroupName(string groupName)
        {
            string rawName = NormalizeGroupName(groupName);
            string body = rawName.StartsWith("_", StringComparison.Ordinal) ? rawName.Substring(1) : rawName;
            var safeName = new StringBuilder(body.Length + 1);

            foreach (char character in body)
            {
                if (char.IsLetterOrDigit(character))
                {
                    safeName.Append(char.ToUpperInvariant(character));
                }
            }

            if (safeName.Length == 0)
                throw new InvalidOperationException("Please enter a valid group name.");

            return "_" + safeName;
        }

        private sealed class NaturalGroupNameComparer : IComparer<string>
        {
            internal static readonly NaturalGroupNameComparer Instance = new NaturalGroupNameComparer();

            public int Compare(string left, string right)
            {
                if (ReferenceEquals(left, right))
                    return 0;

                if (left == null)
                    return -1;

                if (right == null)
                    return 1;

                string leftText = TrimGroupPrefix(left);
                string rightText = TrimGroupPrefix(right);
                int leftIndex = 0;
                int rightIndex = 0;

                while (leftIndex < leftText.Length && rightIndex < rightText.Length)
                {
                    char leftChar = leftText[leftIndex];
                    char rightChar = rightText[rightIndex];

                    if (char.IsDigit(leftChar) && char.IsDigit(rightChar))
                    {
                        int numberCompare = CompareNumberSegment(leftText, ref leftIndex, rightText, ref rightIndex);
                        if (numberCompare != 0)
                            return numberCompare;

                        continue;
                    }

                    int charCompare = char.ToUpperInvariant(leftChar).CompareTo(char.ToUpperInvariant(rightChar));
                    if (charCompare != 0)
                        return charCompare;

                    leftIndex++;
                    rightIndex++;
                }

                int lengthCompare = (leftText.Length - leftIndex).CompareTo(rightText.Length - rightIndex);
                if (lengthCompare != 0)
                    return lengthCompare;

                return StringComparer.CurrentCultureIgnoreCase.Compare(left, right);
            }

            private static string TrimGroupPrefix(string value)
            {
                string text = (value ?? string.Empty).Trim();
                return text.StartsWith("_", StringComparison.Ordinal) ? text.Substring(1) : text;
            }

            private static int CompareNumberSegment(string left, ref int leftIndex, string right, ref int rightIndex)
            {
                int leftStart = leftIndex;
                int rightStart = rightIndex;

                while (leftIndex < left.Length && char.IsDigit(left[leftIndex]))
                    leftIndex++;

                while (rightIndex < right.Length && char.IsDigit(right[rightIndex]))
                    rightIndex++;

                string leftNumber = left.Substring(leftStart, leftIndex - leftStart).TrimStart('0');
                string rightNumber = right.Substring(rightStart, rightIndex - rightStart).TrimStart('0');

                if (leftNumber.Length == 0)
                    leftNumber = "0";

                if (rightNumber.Length == 0)
                    rightNumber = "0";

                int lengthCompare = leftNumber.Length.CompareTo(rightNumber.Length);
                if (lengthCompare != 0)
                    return lengthCompare;

                int valueCompare = string.CompareOrdinal(leftNumber, rightNumber);
                if (valueCompare != 0)
                    return valueCompare;

                int rawLengthCompare = (leftIndex - leftStart).CompareTo(rightIndex - rightStart);
                return rawLengthCompare;
            }
        }
    }
}
