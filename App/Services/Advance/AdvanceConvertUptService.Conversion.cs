using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core.Advance
{
    internal static partial class AdvanceConvertUptService
    {
        private static SectionInfo BuildSectionInfo(AdvanceUptConversionGroup group)
        {
            var info = new SectionInfo
            {
                CreateAsUPT = true,
                Name = group.UptPropertyName
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

        private static bool TryAssignAndVerify(object propertyObj, int[] memberIds, int propertyId, out string error)
        {
            error = string.Empty;
            bool bulkAccepted = OpenStaadPropertyAdapter.TryAssignBeamProperty(propertyObj, memberIds, propertyId, out string bulkError);
            if (bulkAccepted && VerifyAssignedProperty(propertyObj, memberIds, propertyId))
                return true;

            int success = 0;
            var errors = new StringBuilder();
            if (!bulkAccepted && !string.IsNullOrWhiteSpace(bulkError))
                errors.AppendLine("Bulk AssignBeamProperty failed: " + bulkError);

            foreach (int memberId in memberIds)
            {
                if (OpenStaadPropertyAdapter.TryAssignBeamProperty(propertyObj, memberId, propertyId, out string singleError))
                {
                    success++;
                    continue;
                }

                errors.AppendLine($"Member {memberId}: {singleError}");
            }

            if (success == memberIds.Length && VerifyAssignedProperty(propertyObj, memberIds, propertyId))
                return true;

            error = errors.ToString().Trim();
            if (string.IsNullOrWhiteSpace(error))
                error = $"Assignment verification failed for property {propertyId}.";

            return false;
        }

        private static bool VerifyAssignedProperty(object propertyObj, int[] memberIds, int propertyId)
        {
            foreach (int memberId in memberIds.Take(Math.Min(memberIds.Length, 20)))
            {
                if (!OpenStaadPropertyAdapter.TryGetBeamSectionPropertyRefNoFromProperty(
                        propertyObj,
                        memberId,
                        out int actualPropertyId,
                        out _) ||
                    actualPropertyId != propertyId)
                {
                    return false;
                }
            }

            return true;
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

            string targetName = name.Trim();
            foreach (int propertyId in propertyIds)
            {
                if (!OpenStaadPropertyAdapter.TryGetSectionPropertyName(propertyObj, propertyId, out string propertyName, out _))
                    continue;

                if (string.Equals(propertyName, targetName, StringComparison.OrdinalIgnoreCase))
                    return propertyId;
            }

            return 0;
        }
    }
}
