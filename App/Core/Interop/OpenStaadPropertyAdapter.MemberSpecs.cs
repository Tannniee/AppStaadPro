using System;
using System.Threading;

namespace StaadSteelApp.Core.Interop
{
    internal static partial class OpenStaadPropertyAdapter
    {
        internal static int CreateMemberSpec(object propertyObj, string methodName, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(methodName))
            {
                error = "Create member spec method name is empty.";
                return -1;
            }

            if (!ComInvoke.TryInvoke(propertyObj, methodName, out object result, out error))
                return -1;

            return result == null ? 0 : ComResult.ToInt32(result);
        }

        internal static int AssignMemberSpecToBeam(object propertyObj, int[] beams, int specId, out string error)
        {
            error = string.Empty;
            if (!ComInvoke.TryInvoke(propertyObj, "AssignMemberSpecToBeam", out object result, out error, new object[] { beams, specId }))
                return -1;

            return result == null ? 0 : ComResult.ToInt32(result);
        }

        internal static bool TryGetMemberSpecCode(object propertyObj, int beamId, out int specCode, out string error)
        {
            specCode = -999;
            error = string.Empty;

            object[] args = new object[] { beamId, 0 };
            if (!ComInvoke.TryInvoke(propertyObj, "GetMemberSpecCode", out object result, out error, args, ComInvoke.ByRefModifiers(2, 1)))
                return false;

            int code = result == null ? 0 : ComResult.ToInt32(result);
            if (code != 1)
            {
                error = "GetMemberSpecCode returned " + code + ".";
                return false;
            }

            specCode = ComResult.ToInt32(args[1]);
            return true;
        }

        internal static bool TryRemoveMemberSpec(object propertyObj, string methodName, int beamId, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(methodName))
            {
                error = "Remove member spec method name is empty.";
                return false;
            }

            if (!ComInvoke.TryInvoke(propertyObj, methodName, out object result, out error, new object[] { beamId }))
                return false;

            int code = result == null ? 0 : ComResult.ToInt32(result);
            return code == 0 || code == 1;
        }

        internal static int CreateMemberReleaseSpec(object propertyObj, int location, int[] dof, double[] spring, out string error)
        {
            error = string.Empty;
            object[] args = new object[] { location, dof, spring };
            if (!ComInvoke.TryInvoke(propertyObj, "CreateMemberReleaseSpec", out object result, out error, args, ComInvoke.ByRefModifiers(3, 1, 2)))
                return -1;

            return result == null ? 0 : ComResult.ToInt32(result);
        }

        internal static bool TryRemoveMemberReleaseSpecFromBeam(object propertyObj, int beamId, int location, out string error)
        {
            error = string.Empty;
            return ComInvoke.TryInvoke(propertyObj, "RemoveMemberReleaseSpecFromBeam", out _, out error, new object[] { beamId, location });
        }

        internal static int AssignBetaAngle(object propertyObj, int[] beams, double angle, out string error)
        {
            error = string.Empty;
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                if (ComInvoke.TryInvoke(propertyObj, "AssignBetaAngle", out object result, out error, new object[] { beams, angle }))
                    return result == null ? 0 : ComResult.ToInt32(result);

                Thread.Sleep(75);
            }

            return -1;
        }

        internal static bool TryGetBetaAngle(object propertyObj, int beamId, out double angle, out string error)
        {
            angle = 0.0;
            error = string.Empty;

            if (!ComInvoke.TryInvoke(propertyObj, "GetBetaAngle", out object result, out error, new object[] { beamId }))
                return false;

            return ComResult.TryToDouble(result, out angle);
        }
    }
}
