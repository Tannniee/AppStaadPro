using System;

namespace StaadSteelApp.Core.Interop
{
    internal static class OpenStaadOutputAdapter
    {
        internal static bool TryGetMemberEndForces(
            object openStaad,
            int memberNo,
            int end,
            int loadCase,
            bool globalAxes,
            out double[] forces,
            out string error)
        {
            forces = new double[6];
            error = string.Empty;

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Output", out object outputObj, out error))
                return false;

            object[] args = new object[]
            {
                memberNo,
                end,
                loadCase,
                forces,
                globalAxes ? 1 : 0
            };

            if (!ComInvoke.TryInvoke(outputObj, "GetMemberEndForces", out _, out error, args, ComInvoke.ByRefModifiers(5, 3)))
                return false;

            forces = OpenStaadAdapterHelpers.NormalizeDoubleArray(args[3], 6);
            return true;
        }

        internal static bool TryGetIntermediateMemberForcesAtDistance(
            object openStaad,
            int memberNo,
            double distance,
            int loadCase,
            out double[] forces,
            out string error)
        {
            forces = new double[6];
            error = string.Empty;

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Output", out object outputObj, out error))
                return false;

            object[] args = new object[]
            {
                memberNo,
                distance,
                loadCase,
                forces
            };

            if (!ComInvoke.TryInvoke(outputObj, "GetIntermediateMemberForcesAtDistance", out _, out error, args, ComInvoke.ByRefModifiers(4, 3)))
                return false;

            forces = OpenStaadAdapterHelpers.NormalizeDoubleArray(args[3], 6);
            return true;
        }
    }
}
