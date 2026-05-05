using System.Collections.Generic;
using StaadSteelApp.Core;
using StaadSteelApp.Core.Check;

namespace StaadSteelApp
{
    public partial class CheckForm
    {
        private bool TryGetMemberSteelDesignResults(int memberNo, out DesignResultInfo info)
        {
            info = null;
            if (_os == null)
                return false;

            object openStaad = _os;
            if (!CheckStaadDesignService.TryGetMemberSteelDesignResult(
                    openStaad,
                    memberNo,
                    out CheckSteelDesignResult result,
                    out string error))
            {
                WarnOnce($"SteelDesignResults:{memberNo}", $"Member {memberNo}: failed to read STAAD steel design results. {error}");
                return false;
            }

            info = ToFormDesignResult(result);
            return true;
        }

        private MemberForceData GetCriticalSectionForces(int memberNo, DesignResultInfo info)
        {
            if (_os == null || info == null)
                return null;

            object openStaad = _os;
            if (!CheckStaadDesignService.TryGetCriticalSectionForces(
                    openStaad,
                    memberNo,
                    ToServiceDesignResult(info),
                    out MemberForceData forceData,
                    out string error))
            {
                WarnOnce($"CriticalForces:{memberNo}", $"Member {memberNo}: failed to read critical-section forces from STAAD. {error}");
                return null;
            }

            return forceData;
        }

        private List<ForceEnvelopeEntry> BuildMemberForceEnvelope(int memberNo)
        {
            object openStaad = _os;
            List<CheckForceEnvelopeEntry> entries = CheckStaadDesignService.BuildMemberForceEnvelope(openStaad, memberNo, _allForces);
            return ToFormEnvelopeEntries(entries);
        }

        private static DesignResultInfo ToFormDesignResult(CheckSteelDesignResult result)
        {
            if (result == null)
                return null;

            return new DesignResultInfo
            {
                DesignCode = result.DesignCode,
                DesignStatus = result.DesignStatus,
                CriticalRatio = result.CriticalRatio,
                AllowableRatio = result.AllowableRatio,
                CriticalLoadCase = result.CriticalLoadCase,
                CriticalSection = result.CriticalSection,
                CriticalClause = result.CriticalClause,
                DesignSection = result.DesignSection,
                DesignForces = result.DesignForces ?? new double[3],
                KLByR = result.KLByR
            };
        }

        private static CheckSteelDesignResult ToServiceDesignResult(DesignResultInfo info)
        {
            if (info == null)
                return null;

            return new CheckSteelDesignResult
            {
                DesignCode = info.DesignCode,
                DesignStatus = info.DesignStatus,
                CriticalRatio = info.CriticalRatio,
                AllowableRatio = info.AllowableRatio,
                CriticalLoadCase = info.CriticalLoadCase,
                CriticalSection = info.CriticalSection,
                CriticalClause = info.CriticalClause,
                DesignSection = info.DesignSection,
                DesignForces = info.DesignForces ?? new double[3],
                KLByR = info.KLByR
            };
        }

        private static List<ForceEnvelopeEntry> ToFormEnvelopeEntries(IEnumerable<CheckForceEnvelopeEntry> entries)
        {
            var result = new List<ForceEnvelopeEntry>();
            if (entries == null)
                return result;

            foreach (CheckForceEnvelopeEntry entry in entries)
            {
                result.Add(new ForceEnvelopeEntry
                {
                    Label = entry.Label,
                    Value = entry.Value,
                    LoadCase = entry.LoadCase,
                    Station = entry.Station,
                    Unit = entry.Unit,
                    IsSampled = entry.IsSampled
                });
            }

            return result;
        }
    }
}
