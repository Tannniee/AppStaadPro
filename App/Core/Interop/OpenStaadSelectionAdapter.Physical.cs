using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace StaadSteelApp.Core.Interop
{
    internal static partial class OpenStaadSelectionAdapter
    {
        private static int[] ExpandPhysicalSelectionToAnalyticalMembers(object geometry, int[] physicalMembers)
        {
            if (geometry == null || physicalMembers == null || physicalMembers.Length == 0)
                return Array.Empty<int>();

            var members = new HashSet<int>();

            foreach (int physicalId in physicalMembers)
            {
                if (physicalId <= 0)
                    continue;

                int analyticalCount = TryGetAnalyticalMemberCountForPhysicalMember(geometry, physicalId);
                if (analyticalCount <= 0)
                    continue;

                int[] analyticalMembers = TryGetAnalyticalMembersForPhysicalMember(geometry, physicalId, analyticalCount);
                foreach (int memberNo in analyticalMembers)
                {
                    if (memberNo > 0)
                        members.Add(memberNo);
                }
            }

            if (members.Count == 0)
                return Array.Empty<int>();

            int[] result = new int[members.Count];
            members.CopyTo(result);
            Array.Sort(result);
            return result;
        }

        private static int TryGetAnalyticalMemberCountForPhysicalMember(object geometry, int physicalId)
        {
            if (geometry == null || physicalId <= 0)
                return 0;

            if (ComInvoke.TryInvoke(
                    geometry,
                    "GetAnalyticalMemberCountForPhysicalMember",
                    out object ret,
                    out string error,
                    new object[] { physicalId }))
            {
                return Math.Max(0, ComResult.ToInt32(ret));
            }

            Debug.WriteLine($"GetAnalyticalMemberCountForPhysicalMember({physicalId}) reflection failed: {error}");

            try
            {
                dynamic dynGeometry = geometry;
                return Math.Max(0, Convert.ToInt32(dynGeometry.GetAnalyticalMemberCountForPhysicalMember(physicalId), CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetAnalyticalMemberCountForPhysicalMember({physicalId}) dynamic failed: {ex.Message}");
                return 0;
            }
        }

        private static int[] TryGetAnalyticalMembersForPhysicalMember(object geometry, int physicalId, int analyticalCount)
        {
            if (geometry == null || physicalId <= 0 || analyticalCount <= 0)
                return Array.Empty<int>();

            try
            {
                object[] args = new object[] { physicalId, analyticalCount, new int[analyticalCount] };
                object ret = ComInvoke.Invoke(
                    geometry,
                    "GetAnalyticalMembersForPhysicalMember",
                    args,
                    ComInvoke.ByRefModifiers(3, 2));

                int code = ComResult.ToInt32(ret);
                if (code != 0)
                    Debug.WriteLine($"GetAnalyticalMembersForPhysicalMember({physicalId}) returned code {code}.");

                int[] ids = ComResult.ToIntArray(args[2]);
                if (ids.Length > 0)
                    return ids;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetAnalyticalMembersForPhysicalMember({physicalId}) reflection failed: {ex.Message}");
            }

            try
            {
                dynamic dynGeometry = geometry;
                dynamic dynList = new int[analyticalCount];
                object ret = dynGeometry.GetAnalyticalMembersForPhysicalMember(physicalId, analyticalCount, ref dynList);

                int code = ComResult.ToInt32(ret);
                if (code != 0)
                    Debug.WriteLine($"GetAnalyticalMembersForPhysicalMember({physicalId}) dynamic returned code {code}.");

                return ComResult.ToIntArray(dynList);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetAnalyticalMembersForPhysicalMember({physicalId}) dynamic failed: {ex.Message}");
                return Array.Empty<int>();
            }
        }
    }
}
