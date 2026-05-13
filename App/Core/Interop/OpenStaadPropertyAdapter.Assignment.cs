using System;

namespace StaadSteelApp.Core.Interop
{
    internal static partial class OpenStaadPropertyAdapter
    {
        internal static bool TrySetMaterialName(object propertyObj, string materialName, out string error)
        {
            error = string.Empty;
            return ComInvoke.TryInvoke(propertyObj, "SetMaterialName", out _, out error, new object[] { materialName });
        }

        internal static bool TryAssignMaterialToMember(object propertyObj, string materialName, int[] beamIds, out string error)
        {
            error = string.Empty;
            if (!ComInvoke.TryInvoke(propertyObj, "AssignMaterialToMember", out object result, out error, new object[] { materialName, beamIds }))
                return false;

            return IsAssignSuccess(result);
        }

        internal static bool TryAssignMaterialToMember(object propertyObj, string materialName, int beamId, out string error)
        {
            error = string.Empty;
            if (!ComInvoke.TryInvoke(propertyObj, "AssignMaterialToMember", out object result, out error, new object[] { materialName, beamId }))
                return false;

            return IsAssignSuccess(result);
        }

        internal static bool TryAssignBeamProperty(object propertyObj, int[] beamIds, int propertyRefNo, out string error)
        {
            error = string.Empty;
            if (!ComInvoke.TryInvoke(propertyObj, "AssignBeamProperty", out object result, out error, new object[] { beamIds, propertyRefNo }))
                return false;

            return IsZeroStatusSuccess(result, out error);
        }

        internal static bool TryAssignBeamProperty(object propertyObj, int beamId, int propertyRefNo, out string error)
        {
            error = string.Empty;
            if (!ComInvoke.TryInvoke(propertyObj, "AssignBeamProperty", out object result, out error, new object[] { beamId, propertyRefNo }))
                return false;

            return IsZeroStatusSuccess(result, out error);
        }
    }
}
