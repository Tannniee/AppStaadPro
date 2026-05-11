using System;
using System.Collections.Generic;
using System.Linq;
using StaadSteelApp.AISC360;

namespace StaadSteelApp.Core.Check
{
    internal sealed class CheckCalculationRequest
    {
        internal object OpenStaad { get; set; }
        internal int MemberNo { get; set; }
        internal IReadOnlyList<MemberForceData> AllForces { get; set; }
        internal bool IsBuiltUp { get; set; }
    }

    internal sealed class CheckCalculationResult
    {
        internal DesignResults Results { get; set; }
        internal CheckSteelDesignResult SteelInfo { get; set; }
        internal MemberForceData GoverningForce { get; set; }
        internal List<CheckForceEnvelopeEntry> ForceEnvelope { get; set; } = new List<CheckForceEnvelopeEntry>();
        internal string ManualSource { get; set; } = string.Empty;
        internal double? StaadDesignRatio { get; set; }
        internal int? StaadCriticalLoadCase { get; set; }
        internal int MapperWarningCount { get; set; }
        internal string MapperWarningSummary { get; set; } = string.Empty;
    }

    internal static class CheckCalculationService
    {
        internal static CheckCalculationResult Calculate(CheckCalculationRequest request)
        {
            var result = new CheckCalculationResult();
            if (request == null || request.OpenStaad == null || request.MemberNo <= 0)
                return result;

            var mapper = new StaadToAiscMapper(request.OpenStaad);
            IReadOnlyList<MemberForceData> allForces = request.AllForces ?? Array.Empty<MemberForceData>();
            List<MemberForceData> memberForces = allForces.Where(f => f.MemberNo == request.MemberNo).ToList();

            DesignResults worstResults = null;
            double maxRatio = -1.0;
            MemberForceData governingForce = null;

            if (CheckStaadDesignService.TryGetMemberSteelDesignResult(
                    request.OpenStaad,
                    request.MemberNo,
                    out CheckSteelDesignResult steelInfo,
                    out _))
            {
                result.SteelInfo = steelInfo;
                result.ManualSource = "STAAD critical design result";
                governingForce = TryGetCriticalSectionForces(request.OpenStaad, request.MemberNo, steelInfo);
                if (governingForce != null)
                {
                    AnalyzedMember analyzedMember = MapMember(mapper, request.OpenStaad, governingForce, request.IsBuiltUp);
                    worstResults = AISC360Calculator.PerformAllChecks(analyzedMember);
                    maxRatio = worstResults.GoverningRatio;
                }

                result.StaadDesignRatio = steelInfo.CriticalRatio;
                result.StaadCriticalLoadCase = steelInfo.CriticalLoadCase;
            }
            else
            {
                result.ManualSource = "Extracted analysis envelope scan";
                foreach (MemberForceData force in memberForces)
                {
                    AnalyzedMember analyzedMember = MapMember(mapper, request.OpenStaad, force, request.IsBuiltUp);
                    DesignResults checkResults = AISC360Calculator.PerformAllChecks(analyzedMember);

                    if (checkResults.GoverningRatio > maxRatio)
                    {
                        maxRatio = checkResults.GoverningRatio;
                        worstResults = checkResults;
                        governingForce = force;
                    }
                }
            }

            result.Results = worstResults;
            result.GoverningForce = governingForce;
            result.ForceEnvelope = CheckStaadDesignService.BuildMemberForceEnvelope(
                request.OpenStaad,
                request.MemberNo,
                allForces);

            if (mapper.WarningCount > 0)
            {
                result.MapperWarningCount = mapper.WarningCount;
                result.MapperWarningSummary = mapper.GetWarningSummary(5);
            }

            return result;
        }

        private static MemberForceData TryGetCriticalSectionForces(object openStaad, int memberNo, CheckSteelDesignResult steelInfo)
        {
            return CheckStaadDesignService.TryGetCriticalSectionForces(
                    openStaad,
                    memberNo,
                    steelInfo,
                    out MemberForceData forceData,
                    out _)
                ? forceData
                : null;
        }

        private static AnalyzedMember MapMember(StaadToAiscMapper mapper, object openStaad, MemberForceData force, bool isBuiltUp)
        {
            AnalyzedMember analyzedMember = mapper.MapMember(force.MemberNo, force.LoadCase, force);
            analyzedMember.IsBuiltUp = isBuiltUp;

            if (string.IsNullOrWhiteSpace(analyzedMember.SectionName) &&
                CheckMemberInfoService.TryGetBeamSectionName(openStaad, force.MemberNo, out string sectionName, out _) &&
                !string.IsNullOrWhiteSpace(sectionName))
            {
                analyzedMember.SectionName = sectionName;
            }

            return analyzedMember;
        }
    }
}
