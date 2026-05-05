using System;
using System.Collections.Generic;
using System.Globalization;

namespace StaadSteelApp.Core.Interop
{
    internal static class OpenStaadSupportAdapter
    {
        internal static bool TryGetSupportObject(object openStaad, out object supportObj, out string error)
        {
            return OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Support", out supportObj, out error);
        }

        internal static int CreateSupportFixed(object supportObj, out string error)
        {
            return InvokeIntMethod(supportObj, "CreateSupportFixed", null, out error);
        }

        internal static int CreateSupportPinned(object supportObj, out string error)
        {
            return InvokeIntMethod(supportObj, "CreateSupportPinned", null, out error);
        }

        internal static int CreateSupportFixedBut(object supportObj, int[] releaseSpec, double[] springSpec, out string error)
        {
            error = string.Empty;

            var releaseValues = new double[6];
            for (int i = 0; i < releaseValues.Length; i++)
                releaseValues[i] = releaseSpec != null && i < releaseSpec.Length ? releaseSpec[i] : 0;

            if (!ComInvoke.TryInvoke(
                    supportObj,
                    "CreateSupportFixedBut",
                    out object result,
                    out error,
                    new object[] { releaseValues, springSpec ?? new double[6] }))
            {
                return 0;
            }

            return ComResult.ToInt32(result);
        }

        internal static bool TryAssignSupportToNode(object supportObj, int nodeId, int supportId, out string error)
        {
            error = string.Empty;
            if (!ComInvoke.TryInvoke(supportObj, "AssignSupportToNode", out object result, out error, new object[] { nodeId, supportId }))
                return false;

            int code = ComResult.ToInt32(result);
            return code == 0 || code == 1;
        }

        internal static bool TryRemoveSupportFromNode(object supportObj, int nodeId, out string error)
        {
            error = string.Empty;
            if (!ComInvoke.TryInvoke(supportObj, "RemoveSupportFromNode", out object result, out error, new object[] { nodeId }))
                return false;

            int code = ComResult.ToInt32(result);
            return code == 0 || code == 1;
        }

        internal static bool TryGetSupportType(object supportObj, int nodeId, out int supportType, out string error)
        {
            supportType = 0;
            error = string.Empty;

            if (!ComInvoke.TryInvoke(supportObj, "GetSupportType", out object result, out error, new object[] { nodeId }))
                return false;

            supportType = ComResult.ToInt32(result);
            return supportType >= 0;
        }

        internal static bool TryGetSupportedNodeIds(object supportObj, out int[] nodeIds, out string error)
        {
            nodeIds = Array.Empty<int>();
            error = string.Empty;

            if (!ComInvoke.TryInvoke(supportObj, "GetSupportCount", out object countResult, out error))
                return false;

            int supportCount = ComResult.ToInt32(countResult);
            if (supportCount < 0)
            {
                error = "GetSupportCount returned an error.";
                return false;
            }

            if (supportCount == 0)
                return true;

            object[] args = new object[] { new int[supportCount] };
            if (!ComInvoke.TryInvoke(supportObj, "GetSupportNodes", out object result, out error, args, ComInvoke.ByRefModifiers(1, 0)))
                return false;

            int resultCount = result == null ? supportCount : ComResult.ToInt32(result);
            if (resultCount < 0)
            {
                error = "GetSupportNodes returned an error.";
                return false;
            }

            int[] raw = ComResult.ToIntArray(args[0]);
            int length = Math.Min(resultCount, raw.Length);
            var ids = new List<int>(length);
            var seen = new HashSet<int>();
            for (int i = 0; i < length; i++)
            {
                int nodeId = raw[i];
                if (nodeId > 0 && seen.Add(nodeId))
                    ids.Add(nodeId);
            }

            nodeIds = ids.ToArray();
            return true;
        }

        internal static bool TryGetSupportInformationEx(
            object supportObj,
            int nodeId,
            out int supportId,
            out int supportType,
            out int[] releaseStates,
            out double[] springSpec,
            out string error)
        {
            supportId = 0;
            supportType = 0;
            releaseStates = new int[6];
            springSpec = new double[6];
            error = string.Empty;

            object[] args = new object[]
            {
                nodeId,
                0,
                0,
                new int[6],
                new double[6]
            };

            if (!ComInvoke.TryInvoke(
                    supportObj,
                    "GetSupportInformationEx",
                    out object result,
                    out error,
                    args,
                    ComInvoke.ByRefModifiers(5, 1, 2, 3, 4)))
            {
                return false;
            }

            supportId = ComResult.ToInt32(args[1]);
            supportType = ResolveSupportTypeFromExtendedInfo(result, args[2]);
            releaseStates = NormalizeIntArray(args[3], 6);
            springSpec = NormalizeDoubleArray(args[4], 6);
            return supportId > 0 && supportType > 0;
        }

        private static int InvokeIntMethod(object target, string methodName, object[] args, out string error)
        {
            error = string.Empty;
            if (!ComInvoke.TryInvoke(target, methodName, out object result, out error, args))
                return -1;

            return result == null ? 0 : Convert.ToInt32(result, CultureInfo.InvariantCulture);
        }

        private static int ResolveSupportTypeFromExtendedInfo(object returnValue, object supportTypeArg)
        {
            int explicitType = ComResult.ToInt32(supportTypeArg);
            if (explicitType > 0)
                return explicitType;

            return ComResult.ToInt32(returnValue);
        }

        private static int[] NormalizeIntArray(object value, int expectedLength)
        {
            int[] source = ComResult.ToIntArray(value);
            if (source.Length == expectedLength)
                return source;

            var result = new int[expectedLength];
            Array.Copy(source, result, Math.Min(source.Length, result.Length));
            return result;
        }

        private static double[] NormalizeDoubleArray(object value, int expectedLength)
        {
            return OpenStaadAdapterHelpers.NormalizeDoubleArray(value, expectedLength);
        }
    }
}
