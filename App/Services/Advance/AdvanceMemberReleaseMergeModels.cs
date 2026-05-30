using System;
using System.Collections.Generic;
using System.Linq;

namespace StaadSteelApp.Core.Advance
{
    public sealed class AdvanceMemberReleaseMergePreview
    {
        private readonly List<AdvanceMemberReleaseMergeGroup> _groups = new List<AdvanceMemberReleaseMergeGroup>();

        public IReadOnlyList<AdvanceMemberReleaseMergeGroup> Groups => _groups;
        public int TotalMemberCount { get; internal set; }
        public int MembersWithReleaseCount { get; internal set; }
        public int ReadFailureCount { get; internal set; }
        public int UnsupportedReleaseCount { get; internal set; }
        public int UniqueReleaseGroupCount { get; internal set; }
        public int MergeGroupCount => _groups.Count;
        public int MergeMemberCount => _groups.Sum(group => group.MemberIds.Count);

        internal void AddGroup(AdvanceMemberReleaseMergeGroup group)
        {
            if (group != null)
                _groups.Add(group);
        }
    }

    public sealed class AdvanceMemberReleaseMergeGroup
    {
        public string DisplayName { get; internal set; } = string.Empty;
        public int Location { get; internal set; }
        public int[] ReleaseStates { get; internal set; } = new int[6];
        public double[] SpringValues { get; internal set; } = new double[6];
        public IReadOnlyList<int> MemberIds { get; internal set; } = Array.Empty<int>();
    }
}
