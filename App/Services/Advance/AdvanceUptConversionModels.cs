using System;
using System.Collections.Generic;
using System.Linq;

namespace StaadSteelApp.Core.Advance
{
    public sealed class AdvanceUptConversionPreview
    {
        private readonly List<AdvanceUptConversionGroup> _groups = new List<AdvanceUptConversionGroup>();

        public IReadOnlyList<AdvanceUptConversionGroup> Groups => _groups;
        public int TotalMemberCount { get; internal set; }
        public int AlreadyUptMemberCount { get; internal set; }
        public int NotPipeTubeMemberCount { get; internal set; }
        public int UnreadableMemberCount { get; internal set; }
        public int InvalidDimensionMemberCount { get; internal set; }
        public string WarningText { get; internal set; } = string.Empty;

        public int EligibleMemberCount => _groups.Sum(group => group.MemberIds.Count);
        public int EligibleGroupCount => _groups.Count;

        internal void AddGroup(AdvanceUptConversionGroup group)
        {
            if (group != null)
                _groups.Add(group);
        }
    }

    public sealed class AdvanceUptConversionGroup
    {
        public string DisplayName { get; internal set; } = string.Empty;
        public string UptPropertyName { get; internal set; } = string.Empty;
        public AdvanceUptConversionShape Shape { get; internal set; }
        public double DepthMm { get; internal set; }
        public double WidthMm { get; internal set; }
        public double ThicknessMm { get; internal set; }
        public double InnerDiameterMm { get; internal set; }
        public IReadOnlyList<int> MemberIds { get; internal set; } = Array.Empty<int>();
        public IReadOnlyList<int> LegacyPropertyRefIds { get; internal set; } = Array.Empty<int>();
    }

    public enum AdvanceUptConversionShape
    {
        Pipe,
        Tube
    }
}
