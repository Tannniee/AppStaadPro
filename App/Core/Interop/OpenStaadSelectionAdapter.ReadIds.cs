using System;
using System.Diagnostics;

namespace StaadSteelApp.Core.Interop
{
    internal static partial class OpenStaadSelectionAdapter
    {
        private static int[] TryGetSelectedIds(object geometry, string methodName, int count, int[] sortFlags)
        {
            if (geometry == null || count <= 0 || string.IsNullOrWhiteSpace(methodName))
                return Array.Empty<int>();

            foreach (int sortFlag in sortFlags ?? new[] { 0, 1 })
            {
                if (TryGetSelectedIdsDynamic(geometry, methodName, count, sortFlag, sortedByRef: false, out int[] ids) && ids.Length > 0)
                    return ids;

                if (TryGetSelectedIdsDynamic(geometry, methodName, count, sortFlag, sortedByRef: true, out ids) && ids.Length > 0)
                    return ids;

                if (TryGetSelectedIdsReflection(geometry, methodName, count, sortFlag, sortedByRef: false, out ids) && ids.Length > 0)
                    return ids;

                if (TryGetSelectedIdsReflection(geometry, methodName, count, sortFlag, sortedByRef: true, out ids) && ids.Length > 0)
                    return ids;
            }

            return Array.Empty<int>();
        }

        private static bool TryGetSelectedIdsDynamic(object geometry, string methodName, int count, int sortFlag, bool sortedByRef, out int[] ids)
        {
            ids = Array.Empty<int>();
            try
            {
                dynamic dynGeometry = geometry;
                dynamic idsVar = new int[count];

                if (sortedByRef)
                {
                    dynamic sorted = sortFlag;
                    if (string.Equals(methodName, "GetSelectedNodes", StringComparison.Ordinal))
                        dynGeometry.GetSelectedNodes(ref idsVar, ref sorted);
                    else if (string.Equals(methodName, "GetSelectedBeams", StringComparison.Ordinal))
                        dynGeometry.GetSelectedBeams(ref idsVar, ref sorted);
                    else if (string.Equals(methodName, "GetSelectedPhysicalMembers", StringComparison.Ordinal))
                        dynGeometry.GetSelectedPhysicalMembers(ref idsVar, ref sorted);
                    else
                        return false;
                }
                else
                {
                    if (string.Equals(methodName, "GetSelectedNodes", StringComparison.Ordinal))
                        dynGeometry.GetSelectedNodes(ref idsVar, sortFlag);
                    else if (string.Equals(methodName, "GetSelectedBeams", StringComparison.Ordinal))
                        dynGeometry.GetSelectedBeams(ref idsVar, sortFlag);
                    else if (string.Equals(methodName, "GetSelectedPhysicalMembers", StringComparison.Ordinal))
                        dynGeometry.GetSelectedPhysicalMembers(ref idsVar, sortFlag);
                    else
                        return false;
                }

                ids = ComResult.ToIntArray(idsVar);
                return true;
            }
            catch (Exception ex)
            {
                string sortedText = sortedByRef ? "ref" : "val";
                Debug.WriteLine($"{methodName}(ref,{sortedText},sorted={sortFlag}) failed: {ex.Message}");
                return false;
            }
        }

        private static bool TryGetSelectedIdsReflection(object geometry, string methodName, int count, int sortFlag, bool sortedByRef, out int[] ids)
        {
            ids = Array.Empty<int>();
            try
            {
                object[] args = new object[] { new int[count], sortFlag };
                int[] byRefIndexes = sortedByRef ? new[] { 0, 1 } : new[] { 0 };
                ComInvoke.Invoke(geometry, methodName, args, ComInvoke.ByRefModifiers(2, byRefIndexes));
                ids = ComResult.ToIntArray(args[0]);
                return true;
            }
            catch (Exception ex)
            {
                string sortedText = sortedByRef ? "ByRef,ByRef" : "ByRef,val";
                Debug.WriteLine($"{methodName} reflection({sortedText},sorted={sortFlag}) failed: {ex.Message}");
                return false;
            }
        }
    }
}
