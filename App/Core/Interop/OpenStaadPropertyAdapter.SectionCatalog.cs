using System;

namespace StaadSteelApp.Core.Interop
{
    internal static partial class OpenStaadPropertyAdapter
    {
        internal static bool TryGetSectionPropertyName(object propertyObj, int propertyRefNo, out string sectionName, out string error)
        {
            sectionName = string.Empty;
            error = string.Empty;

            object[] args = new object[] { propertyRefNo, string.Empty };
            if (!ComInvoke.TryInvoke(propertyObj, "GetSectionPropertyName", out object result, out error, args, ComInvoke.ByRefModifiers(2, 1)))
                return false;

            string nameFromOut = Convert.ToString(args[1]) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(nameFromOut))
            {
                sectionName = nameFromOut.Trim();
                return true;
            }

            string nameFromRet = Convert.ToString(result) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(nameFromRet))
            {
                sectionName = nameFromRet.Trim();
                return true;
            }

            return false;
        }

        internal static bool TryGetSectionPropertyList(object propertyObj, out int[] propertyIds, out string error)
        {
            propertyIds = Array.Empty<int>();
            error = string.Empty;

            if (!ComInvoke.TryInvoke(propertyObj, "GetSectionPropertyCount", out object countRaw, out error))
                return false;

            int count = ComResult.ToInt32(countRaw);
            if (count <= 0)
                return true;

            object[] args = new object[] { new int[count] };
            if (!ComInvoke.TryInvoke(propertyObj, "GetSectionPropertyList", out _, out error, args, ComInvoke.ByRefModifiers(1, 0)))
                return false;

            propertyIds = ComResult.ToIntArray(args[0]);
            return true;
        }

        internal static bool TryGetSectionPropertyCount(object propertyObj, out int count, out string error)
        {
            count = 0;
            error = string.Empty;

            if (!ComInvoke.TryInvoke(propertyObj, "GetSectionPropertyCount", out object countRaw, out error))
                return false;

            count = Math.Max(0, ComResult.ToInt32(countRaw));
            return true;
        }

        internal static bool TryGetSectionPropertyAssignedBeamCount(object propertyObj, int propertyRefNo, out int count, out string error)
        {
            count = 0;
            error = string.Empty;

            if (propertyRefNo <= 0)
            {
                error = $"Invalid property ref '{propertyRefNo}'.";
                return false;
            }

            if (!ComInvoke.TryInvoke(propertyObj, "GetSectionPropertyAssignedBeamCount", out object countRaw, out error, new object[] { propertyRefNo }))
                return false;

            count = Math.Max(0, ComResult.ToInt32(countRaw));
            return true;
        }

        internal static bool TryGetSectionPropertyAssignedBeamList(object propertyObj, int propertyRefNo, int count, out int[] beamIds, out string error)
        {
            beamIds = Array.Empty<int>();
            error = string.Empty;

            if (propertyRefNo <= 0 || count <= 0)
                return true;

            object[] args = new object[] { propertyRefNo, new int[count] };
            if (!ComInvoke.TryInvoke(propertyObj, "GetSectionPropertyAssignedBeamList", out _, out error, args, ComInvoke.ByRefModifiers(2, 1)))
                return false;

            beamIds = ComResult.ToIntArray(args[1]);
            return true;
        }

        internal static bool TryGetSectionPropertyType(object propertyObj, int propertyRefNo, out int typeCode, out string error)
        {
            typeCode = 0;
            error = string.Empty;

            if (propertyRefNo <= 0)
            {
                error = $"Invalid property ref '{propertyRefNo}'.";
                return false;
            }

            if (ComInvoke.TryInvoke(propertyObj, "GetSectionPropertyType", out object result, out error, new object[] { propertyRefNo }))
            {
                typeCode = ComResult.ToInt32(result);
                if (typeCode > 0)
                    return true;
            }

            if (TryGetSectionPropertyValuesExFromProperty(propertyObj, propertyRefNo, out typeCode, out _, out error))
                return typeCode > 0;

            return false;
        }
    }
}
