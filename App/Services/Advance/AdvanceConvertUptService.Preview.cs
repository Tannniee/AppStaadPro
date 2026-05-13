using System.Collections.Generic;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core.Advance
{
    internal static partial class AdvanceConvertUptService
    {
        private static Dictionary<int, List<int>> BuildMembersByProperty(
            object propertyObj,
            int[] beamIds,
            AdvanceUptConversionPreview preview)
        {
            var result = new Dictionary<int, List<int>>();

            foreach (int beamId in beamIds)
            {
                if (!OpenStaadPropertyAdapter.TryGetBeamSectionPropertyRefNoFromProperty(
                        propertyObj,
                        beamId,
                        out int propertyRefNo,
                        out _))
                {
                    preview.UnreadableMemberCount++;
                    continue;
                }

                if (!result.TryGetValue(propertyRefNo, out List<int> members))
                {
                    members = new List<int>();
                    result.Add(propertyRefNo, members);
                }

                members.Add(beamId);
            }

            return result;
        }
    }
}
