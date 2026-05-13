using System;
using System.Collections.Generic;
using System.Linq;

namespace StaadSteelApp.Core.Advance
{
    public sealed class AdvanceSectionMergePreview
    {
        private readonly List<AdvanceSectionMergeGroup> _groups = new List<AdvanceSectionMergeGroup>();

        public IReadOnlyList<AdvanceSectionMergeGroup> Groups => _groups;
        public int TotalUsedSectionGroups { get; internal set; }
        public int UniqueSectionGroupCount { get; internal set; }
        public int DuplicateSectionGroupCount => _groups.Count;
        public int DuplicateMemberCount => _groups.Sum(group => group.MemberIds.Count);

        internal void AddGroup(AdvanceSectionMergeGroup group)
        {
            if (group != null)
                _groups.Add(group);
        }
    }

    public sealed class AdvanceSectionMergeGroup
    {
        public string DisplayName { get; internal set; } = string.Empty;
        public int TargetPropertyId { get; internal set; }
        public IReadOnlyList<int> PropertyIds { get; internal set; } = Array.Empty<int>();
        public IReadOnlyList<int> MemberIds { get; internal set; } = Array.Empty<int>();
    }
}
