using System;

namespace StaadSteelApp.Core.Interop
{
    internal static partial class OpenStaadPropertyAdapter
    {
        internal static bool TryGetUserProvidedTableList(object propertyObj, int tableCount, out int[] tableIds, out string error)
        {
            tableIds = Array.Empty<int>();
            error = string.Empty;

            if (tableCount <= 0)
                return true;

            object[] args = new object[] { new int[tableCount] };
            if (!ComInvoke.TryInvoke(propertyObj, "GetUserProvidedTableList", out _, out error, args, ComInvoke.ByRefModifiers(1, 0)))
                return false;

            tableIds = ComResult.ToIntArray(args[0]);
            return true;
        }

        internal static bool TryGetUserProvidedTableSectionType(object propertyObj, int tableId, out int sectionType, out string error)
        {
            sectionType = 0;
            error = string.Empty;

            object[] args = new object[] { tableId, 0 };
            if (!ComInvoke.TryInvoke(propertyObj, "GetUserProvidedTableSectionType", out object result, out error, args, ComInvoke.ByRefModifiers(2, 1)))
            {
                if (!ComInvoke.TryInvoke(propertyObj, "GetUserProvidedTableSectionType", out result, out error, new object[] { tableId }))
                    return false;

                sectionType = ComResult.ToInt32(result);
                return sectionType > 0;
            }

            sectionType = ComResult.ToInt32(args[1]);
            if (sectionType <= 0)
                sectionType = ComResult.ToInt32(result);

            return sectionType > 0;
        }

        internal static int GetUserProvidedTableCount(object propertyObj, out string error)
        {
            return InvokeInt(propertyObj, "GetUserProvidedTableCount", out error);
        }

        internal static bool TryCreateUPTTableEx(object propertyObj, int tableId, int sectionType, out string error)
        {
            error = string.Empty;
            if (!ComInvoke.TryInvoke(
                    propertyObj,
                    "CreateUPTTableEx",
                    out object result,
                    out error,
                    new object[] { tableId, sectionType }))
            {
                return false;
            }

            if (result is bool boolResult)
            {
                if (boolResult)
                    return true;

                error = "OpenSTAAD returned False.";
                return false;
            }

            int createdTableId = ComResult.ToInt32(result);
            if (createdTableId == tableId)
                return true;

            error = createdTableId <= 0
                ? "OpenSTAAD returned 0 instead of a created table ID."
                : $"OpenSTAAD created table {createdTableId} instead of requested table {tableId}.";
            return false;
        }
    }
}
