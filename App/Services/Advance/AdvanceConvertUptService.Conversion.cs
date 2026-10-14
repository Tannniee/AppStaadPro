using System;
using System.Diagnostics;
using System.Linq;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core.Advance
{
    internal static partial class AdvanceConvertUptService
    {
        private static bool TryValidatePreviewGroup(
            object propertyObj,
            AdvanceUptConversionGroup group,
            out string error)
        {
            error = string.Empty;
            int[] memberIds = NormalizeIds(group?.MemberIds);
            if (group == null || memberIds.Length == 0 ||
                group.SourcePropertyRefByMember == null ||
                group.SourcePropertyRefByMember.Count != memberIds.Length)
            {
                error = "Preview source data is incomplete. Run Preview again.";
                return false;
            }

            foreach (int memberId in memberIds)
            {
                if (!group.SourcePropertyRefByMember.TryGetValue(memberId, out int expectedPropertyRef))
                {
                    error = $"Member {memberId} is missing its Preview source Ref ID. Run Preview again.";
                    return false;
                }

                if (!OpenStaadPropertyAdapter.TryGetBeamSectionPropertyRefNoFromProperty(
                        propertyObj,
                        memberId,
                        out int currentPropertyRef,
                        out string propertyError))
                {
                    error = $"Could not recheck member {memberId}: {propertyError}";
                    return false;
                }

                if (currentPropertyRef != expectedPropertyRef)
                {
                    error = $"Member {memberId} changed from Ref {expectedPropertyRef} to Ref {currentPropertyRef} after Preview. Run Preview again.";
                    return false;
                }
            }

            foreach (int propertyRef in group.SourcePropertyRefByMember.Values.Distinct().OrderBy(value => value))
            {
                if (!OpenStaadPropertyAdapter.TryGetSectionPropertyValuesExFromProperty(
                        propertyObj,
                        propertyRef,
                        out int typeCode,
                        out double[] props,
                        out string valuesError))
                {
                    error = $"Could not recheck Ref {propertyRef}: {valuesError}";
                    return false;
                }

                if (!TryBuildCandidate(
                        typeCode,
                        props,
                        out ConversionCandidate currentCandidate,
                        out _,
                        out string candidateError))
                {
                    error = $"Ref {propertyRef} is no longer convertible. {candidateError} Run Preview again.";
                    return false;
                }

                if (!string.Equals(currentCandidate.Key, group.SourceCandidateKey, StringComparison.OrdinalIgnoreCase))
                {
                    error = $"Ref {propertyRef} dimensions changed after Preview. Run Preview again.";
                    return false;
                }
            }

            return true;
        }

        private static SectionInfo BuildSectionInfo(AdvanceUptConversionGroup group)
        {
            var info = new SectionInfo
            {
                CreateAsUPT = true,
                Name = group.UptPropertyName,
                Material = group.MaterialName
            };

            if (group.Shape == AdvanceUptConversionShape.Pipe)
            {
                info.PropType = eFramePropType.Pipe;
                info.H1 = group.DepthMm;
                info.Bf = group.InnerDiameterMm;
                info.Tw = group.ThicknessMm;
                return info;
            }

            info.PropType = eFramePropType.Rectangular;
            info.H1 = group.DepthMm;
            info.H2 = group.WidthMm;
            info.Bf = group.WidthMm;
            info.Tf = group.ThicknessMm;
            info.Tw = group.ThicknessMm;
            return info;
        }

        private static int CheckPropertyExists(object os, string name)
        {
            if (os == null || string.IsNullOrWhiteSpace(name))
                return 0;

            if (!OpenStaadPropertyAdapter.TryGetPropertyObject(os, out object propertyObj, out string propertyError))
            {
                Debug.WriteLine("Property object is not available: " + propertyError);
                return 0;
            }

            if (!OpenStaadPropertyAdapter.TryGetSectionPropertyList(propertyObj, out int[] propertyIds, out string listError))
            {
                Debug.WriteLine("GetSectionPropertyList failed: " + listError);
                return 0;
            }

            string targetName = NormalizeUptPropertyName(name);
            foreach (int propertyId in propertyIds)
            {
                if (!OpenStaadPropertyAdapter.TryGetSectionPropertyName(propertyObj, propertyId, out string propertyName, out _))
                    continue;

                if (string.Equals(NormalizeUptPropertyName(propertyName), targetName, StringComparison.OrdinalIgnoreCase))
                    return propertyId;
            }

            return 0;
        }

        private static string NormalizeUptPropertyName(string name)
        {
            string normalized = (name ?? string.Empty).Trim();
            while (normalized.StartsWith("UPT ", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(4).TrimStart();

            return normalized;
        }
    }
}
