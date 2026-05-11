using System;
using System.Collections.Generic;

namespace StaadSteelApp.Core.Interop
{
    internal static partial class OpenStaadGeometryAdapter
    {
        internal static bool TryGetGroupNames(object openStaad, int groupType, out string[] names, out string error)
        {
            names = Array.Empty<string>();
            error = string.Empty;

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Geometry", out object geometry, out error))
                return false;

            if (!ComInvoke.TryInvoke(geometry, "GetGroupCount", out object countRaw, out error, new object[] { groupType }))
                return false;

            int count = ComResult.ToInt32(countRaw);
            if (count <= 0)
                return true;

            object[] args = new object[] { groupType, new string[count] };
            if (!ComInvoke.TryInvoke(geometry, "GetGroupNames", out _, out error, args, ComInvoke.ByRefModifiers(2, 1)))
                return false;

            names = NormalizeStrings(args[1] as Array);
            return true;
        }

        internal static bool TryGetGroupEntities(object openStaad, string groupName, out int[] entities, out string error)
        {
            entities = Array.Empty<int>();
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(groupName))
                return true;

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Geometry", out object geometry, out error))
                return false;

            if (!ComInvoke.TryInvoke(geometry, "GetGroupEntityCount", out object countRaw, out error, new object[] { groupName }))
                return false;

            int count = ComResult.ToInt32(countRaw);
            if (count <= 0)
                return true;

            object[] args = new object[] { groupName, new int[count] };
            if (!ComInvoke.TryInvoke(geometry, "GetGroupEntities", out _, out error, args, ComInvoke.ByRefModifiers(2, 1)))
                return false;

            entities = NormalizePositiveIds(ComResult.ToIntArray(args[1]));
            return true;
        }

        internal static bool TryGetGroupEntityCount(object openStaad, string groupName, out int count, out string error)
        {
            count = 0;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(groupName))
                return true;

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Geometry", out object geometry, out error))
                return false;

            if (!ComInvoke.TryInvoke(geometry, "GetGroupEntityCount", out object countRaw, out error, new object[] { groupName }))
                return false;

            count = Math.Max(0, ComResult.ToInt32(countRaw));
            return true;
        }

        internal static bool TryCreateGroup(object openStaad, int groupType, string groupName, IReadOnlyList<int> entities, out string error)
        {
            error = string.Empty;

            int[] ids = NormalizePositiveIds(entities);
            if (ids.Length == 0)
            {
                error = "No valid entity IDs to create group.";
                return false;
            }

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Geometry", out object geometry, out error))
                return false;

            if (!ComInvoke.TryInvoke(
                    geometry,
                    "CreateGroupEx",
                    out object result,
                    out error,
                    new object[] { groupType, groupName, ids.Length, ids }))
            {
                return false;
            }

            return IsGroupCommandSuccess(result, "CreateGroupEx", out error);
        }

        internal static bool TryDeleteGroup(object openStaad, string groupName, out string error)
        {
            error = string.Empty;

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Geometry", out object geometry, out error))
                return false;

            if (!ComInvoke.TryInvoke(geometry, "DeleteGroup", out object result, out error, new object[] { groupName }))
                return false;

            return IsGroupCommandSuccess(result, "DeleteGroup", out error);
        }

        internal static bool TryUpdateGroup(object openStaad, string groupName, int updateFlag, IReadOnlyList<int> entities, out string error)
        {
            error = string.Empty;

            int[] ids = NormalizePositiveIds(entities);
            if (ids.Length == 0)
            {
                error = "No valid entity IDs to update group.";
                return false;
            }

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Geometry", out object geometry, out error))
                return false;

            object[] args = new object[] { groupName, updateFlag, ids.Length, ids };
            if (!ComInvoke.TryInvoke(
                    geometry,
                    "UpdateGroup",
                    out object result,
                    out error,
                    args,
                    ComInvoke.ByRefModifiers(4, 3)))
            {
                return false;
            }

            return IsGroupCommandSuccess(result, "UpdateGroup", out error);
        }

        private static string[] NormalizeStrings(Array values)
        {
            if (values == null || values.Length == 0)
                return Array.Empty<string>();

            var result = new List<string>(values.Length);
            foreach (object value in values)
            {
                string text = Convert.ToString(value);
                if (!string.IsNullOrWhiteSpace(text))
                    result.Add(text.Trim());
            }

            return result.ToArray();
        }

        private static bool IsGroupCommandSuccess(object result, string methodName, out string error)
        {
            error = string.Empty;
            if (result == null)
                return true;

            if (result is bool boolResult)
            {
                if (boolResult)
                    return true;

                error = methodName + " returned false.";
                return false;
            }

            if (!ComResult.TryToInt32(result, out int code))
            {
                error = methodName + " returned an unrecognized result.";
                return false;
            }

            if (code == 0)
                return true;

            error = methodName + " returned " + code + ": " + GetGroupErrorText(code);
            return false;
        }

        private static string GetGroupErrorText(int code)
        {
            switch (code)
            {
                case -1: return "General error.";
                case -100: return "Invalid argument.";
                case -107: return "Array of integer expected.";
                case -110: return "No beam, plate, or solid has been selected.";
                case -2005: return "No node has been selected.";
                case -3005: return "No member has been selected.";
                case -4005: return "No plate has been selected.";
                case -5005: return "No solid has been selected.";
                case -7001: return "Group already exists.";
                default: return "Unknown OpenSTAAD group error.";
            }
        }
    }
}
