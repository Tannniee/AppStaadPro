using System;
using System.Collections.Generic;
using System.Globalization;

namespace StaadSteelApp.Core.Interop
{
    internal static partial class OpenStaadSelectionAdapter
    {
        private static int[] NormalizeIds(int[] rawIds, out int invalidCount)
        {
            invalidCount = 0;
            if (rawIds == null || rawIds.Length == 0)
                return Array.Empty<int>();

            var seen = new HashSet<int>();
            var valid = new List<int>(rawIds.Length);
            foreach (int id in rawIds)
            {
                if (id <= 0)
                {
                    invalidCount++;
                    continue;
                }

                if (seen.Add(id))
                    valid.Add(id);
            }

            return valid.ToArray();
        }

        private static int[] FilterValidNodeIds(object geometry, int[] rawNodeIds, out int invalidCount)
        {
            invalidCount = 0;
            if (rawNodeIds == null || rawNodeIds.Length == 0)
                return Array.Empty<int>();

            HashSet<int> existingNodeIds = TryGetExistingNodeIds(geometry);
            var seen = new HashSet<int>();
            var valid = new List<int>(rawNodeIds.Length);

            foreach (int nodeId in rawNodeIds)
            {
                if (nodeId <= 0)
                {
                    invalidCount++;
                    continue;
                }

                if (!seen.Add(nodeId))
                    continue;

                if (existingNodeIds != null && !existingNodeIds.Contains(nodeId))
                {
                    invalidCount++;
                    continue;
                }

                if (TryGetNodeCoordinates(geometry, nodeId, out _, out _, out _, out _))
                    valid.Add(nodeId);
                else
                    invalidCount++;
            }

            return valid.ToArray();
        }

        private static int[] FilterValidBeamIds(object geometry, int[] rawBeamIds, out int invalidCount)
        {
            invalidCount = 0;
            if (rawBeamIds == null || rawBeamIds.Length == 0)
                return Array.Empty<int>();

            var seen = new HashSet<int>();
            var valid = new List<int>(rawBeamIds.Length);
            foreach (int beamId in rawBeamIds)
            {
                if (beamId <= 0 || !seen.Add(beamId))
                {
                    if (beamId <= 0)
                        invalidCount++;
                    continue;
                }

                if (TryGetBeamIncidence(geometry, beamId, out int nodeA, out int nodeB) && nodeA > 0 && nodeB > 0)
                    valid.Add(beamId);
                else
                    invalidCount++;
            }

            return valid.ToArray();
        }

        private static HashSet<int> TryGetExistingNodeIds(object geometry)
        {
            try
            {
                object countRaw = ComInvoke.Invoke(geometry, "GetNodeCount");
                int count = Convert.ToInt32(countRaw, CultureInfo.InvariantCulture);
                if (count <= 0)
                    return new HashSet<int>();

                object[] args = new object[] { new int[count] };
                ComInvoke.Invoke(geometry, "GetNodeList", args, ComInvoke.ByRefModifiers(1, 0));
                return new HashSet<int>(ComResult.ToIntArray(args[0]));
            }
            catch
            {
                return null;
            }
        }

        private static bool TryGetNodeCoordinates(object geometry, int nodeNo, out double x, out double y, out double z, out string error)
        {
            x = 0;
            y = 0;
            z = 0;
            error = string.Empty;

            try
            {
                object[] args = new object[] { nodeNo, 0.0, 0.0, 0.0 };
                ComInvoke.Invoke(geometry, "GetNodeCoordinates", args, ComInvoke.ByRefModifiers(4, 1, 2, 3));
                x = Convert.ToDouble(args[1], CultureInfo.InvariantCulture);
                y = Convert.ToDouble(args[2], CultureInfo.InvariantCulture);
                z = Convert.ToDouble(args[3], CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool TryGetBeamIncidence(object geometry, int beamId, out int nodeA, out int nodeB)
        {
            nodeA = 0;
            nodeB = 0;

            try
            {
                object[] args = new object[] { beamId, 0, 0 };
                ComInvoke.Invoke(geometry, "GetMemberIncidence", args, ComInvoke.ByRefModifiers(3, 1, 2));
                nodeA = Convert.ToInt32(args[1], CultureInfo.InvariantCulture);
                nodeB = Convert.ToInt32(args[2], CultureInfo.InvariantCulture);
                return nodeA > 0 && nodeB > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
