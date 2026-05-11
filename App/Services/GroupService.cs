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
            if (!OpenStaadGeometryAdapter.TryGetGroupNames(openStaad, MemberGroupType, out string[] names, out string error))
                throw new InvalidOperationException("Could not read member groups: " + error);

            var entries = new List<GroupEntry>();
            foreach (string name in names.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase))
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
            object openStaad = GetOpenStaadOrThrow();
            int[] entities = GetGroupEntities(openStaad, groupName);
            if (entities.Length == 0)
                throw new InvalidOperationException("The selected group does not contain any members.");

            HighlightMembers(openStaad, entities);
        }

        public static string CreateMemberGroupFromSelection(string groupName)
        {
            string normalizedName = NormalizeNewGroupName(groupName);
            object openStaad = GetOpenStaadOrThrow();
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
            string normalizedName = NormalizeGroupName(groupName);
            object openStaad = GetOpenStaadOrThrow();

            if (!OpenStaadGeometryAdapter.TryDeleteGroup(openStaad, normalizedName, out string error))
                throw new InvalidOperationException(error);
        }

        public static void AddSelectedMembersToGroup(string groupName)
        {
            UpdateMemberGroupFromSelection(groupName, UpdateAdd);
        }

        public static void RemoveSelectedMembersFromGroup(string groupName)
        {
            UpdateMemberGroupFromSelection(groupName, UpdateRemove);
        }

        public static void ReplaceGroupWithSelectedMembers(string groupName)
        {
            UpdateMemberGroupFromSelection(groupName, UpdateReplace);
        }

        private static void UpdateMemberGroupFromSelection(string groupName, int updateFlag)
        {
            string normalizedName = NormalizeGroupName(groupName);
            object openStaad = GetOpenStaadOrThrow();
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
    }
}
