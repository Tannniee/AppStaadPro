using System;

namespace StaadSteelApp.Core.Interop
{
    internal static partial class OpenStaadPropertyAdapter
    {
        internal static bool TryGetBeamSectionName(object openStaad, int memberNo, out string sectionName, out string error)
        {
            sectionName = string.Empty;
            error = string.Empty;

            if (!TryGetPropertyObject(openStaad, out object propertyObj, out error))
                return false;

            return TryGetBeamSectionNameFromProperty(propertyObj, memberNo, out sectionName, out error);
        }

        internal static bool TryGetBeamMaterialName(object openStaad, int memberNo, out string materialName, out string error)
        {
            materialName = string.Empty;
            error = string.Empty;

            if (!TryGetPropertyObject(openStaad, out object propertyObj, out error))
                return false;

            return TryGetBeamMaterialNameFromProperty(propertyObj, memberNo, out materialName, out error);
        }

        internal static bool TryGetBeamSectionPropertyRefNo(object openStaad, int memberNo, out int propertyRefNo, out string error)
        {
            propertyRefNo = 0;
            error = string.Empty;

            if (memberNo <= 0)
            {
                error = $"Invalid member number '{memberNo}'.";
                return false;
            }

            if (!TryGetPropertyObject(openStaad, out object propertyObj, out error))
                return false;

            return TryGetBeamSectionPropertyRefNoFromProperty(propertyObj, memberNo, out propertyRefNo, out error);
        }

        internal static bool TryGetBeamSectionNameFromProperty(object propertyObj, int memberNo, out string sectionName, out string error)
        {
            sectionName = string.Empty;
            error = string.Empty;

            if (!ComInvoke.TryInvoke(propertyObj, "GetBeamSectionName", out object result, out error, new object[] { memberNo }))
                return false;

            sectionName = Convert.ToString(result) ?? string.Empty;
            return !string.IsNullOrWhiteSpace(sectionName);
        }

        internal static bool TryGetBeamMaterialNameFromProperty(object propertyObj, int memberNo, out string materialName, out string error)
        {
            materialName = string.Empty;
            error = string.Empty;

            if (!ComInvoke.TryInvoke(propertyObj, "GetBeamMaterialName", out object result, out error, new object[] { memberNo }))
                return false;

            materialName = Convert.ToString(result) ?? string.Empty;
            return !string.IsNullOrWhiteSpace(materialName);
        }

        internal static bool TryGetBeamSectionPropertyRefNoFromProperty(object propertyObj, int memberNo, out int propertyRefNo, out string error)
        {
            propertyRefNo = 0;
            error = string.Empty;

            if (memberNo <= 0)
            {
                error = $"Invalid member number '{memberNo}'.";
                return false;
            }

            if (!ComInvoke.TryInvoke(propertyObj, "GetBeamSectionPropertyRefNo", out object result, out error, new object[] { memberNo }))
                return false;

            propertyRefNo = ComResult.ToInt32(result);
            if (propertyRefNo <= 0)
            {
                error = $"GetBeamSectionPropertyRefNo returned invalid ref '{propertyRefNo}' for member {memberNo}.";
                return false;
            }

            return true;
        }
    }
}
