using System;
using System.Collections.Generic;
using System.Linq;

namespace StaadSteelApp.Core.Advance
{
    public sealed class AdvanceLoadFragmentPreview
    {
        private readonly List<AdvanceLoadFragmentGroup> _groups = new List<AdvanceLoadFragmentGroup>();

        public IReadOnlyList<AdvanceLoadFragmentGroup> Groups => _groups;
        public int TotalPrimaryLoadCases { get; internal set; }
        public int TotalUniformLoadItems { get; internal set; }
        public int UniqueUniformLoadGroups { get; internal set; }
        public int FragmentedGroupCount => _groups.Count;
        public int FragmentedItemCount => _groups.Sum(group => group.Items.Count);

        internal void AddGroup(AdvanceLoadFragmentGroup group)
        {
            if (group != null)
                _groups.Add(group);
        }
    }

    public sealed class AdvanceLoadFragmentGroup
    {
        public int LoadCaseNumber { get; internal set; }
        public string LoadCaseTitle { get; internal set; } = string.Empty;
        public int TypeCode { get; internal set; }
        public string TypeName { get; internal set; } = string.Empty;
        public int Direction { get; internal set; }
        public string DirectionText { get; internal set; } = string.Empty;
        public double RepresentativeForce { get; internal set; }
        public double MinForce { get; internal set; }
        public double MaxForce { get; internal set; }
        public int DistancePatternCount { get; internal set; }
        public IReadOnlyList<int> MemberIds { get; internal set; } = Array.Empty<int>();
        public IReadOnlyList<AdvanceLoadFragmentItem> Items { get; internal set; } = Array.Empty<AdvanceLoadFragmentItem>();
    }

    public sealed class AdvanceLoadFragmentItem
    {
        public int LoadItemIndex { get; internal set; }
        public int TypeIndex { get; internal set; }
        public double Force { get; internal set; }
        public IReadOnlyList<double> Distances { get; internal set; } = Array.Empty<double>();
        public IReadOnlyList<int> Assignments { get; internal set; } = Array.Empty<int>();
    }
}
