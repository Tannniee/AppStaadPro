using System;
using System.Collections.Generic;

namespace StaadSteelApp.Core.Interop
{
    internal static partial class OpenStaadGeometryAdapter
    {
        private const int SingleSelectionFallbackLimit = 200;

        internal static bool TryGetBeamLength(object openStaad, int beamId, out double length, out string error)
        {
            length = 0.0;
            error = string.Empty;

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Geometry", out object geometry, out error))
                return false;

            return TryGetBeamLengthFromGeometry(geometry, beamId, out length, out error);
        }

        internal static bool TryGetBeamLengthFromGeometry(object geometry, int beamId, out double length, out string error)
        {
            length = 0.0;
            error = string.Empty;

            if (geometry == null)
            {
                error = "OpenSTAAD Geometry object is not available.";
                return false;
            }

            if (!ComInvoke.TryInvoke(geometry, "GetBeamLength", out object result, out error, new object[] { beamId }))
                return false;

            if (!ComResult.TryToDouble(result, out length) || length <= 0.0)
            {
                error = "OpenSTAAD returned invalid beam length.";
                return false;
            }

            return true;
        }

        internal static bool TryAddNode(object geometry, double x, double y, double z, out int nodeNo, out string error)
        {
            nodeNo = 0;
            error = string.Empty;

            if (geometry == null)
            {
                error = "OpenSTAAD Geometry object is not available.";
                return false;
            }

            if (!ComInvoke.TryInvoke(geometry, "AddNode", out object result, out error, new object[] { x, y, z }))
            {
                error = "AddNode failed: " + error;
                return false;
            }

            nodeNo = ComResult.ToInt32(result);
            if (nodeNo <= 0)
            {
                error = "OpenSTAAD returned invalid node number while creating node.";
                return false;
            }

            return true;
        }

        internal static bool TrySetNodeCoordinate(object geometry, int nodeNo, double x, double y, double z, out string error)
        {
            error = string.Empty;

            if (geometry == null)
            {
                error = "OpenSTAAD Geometry object is not available.";
                return false;
            }

            return ComInvoke.TryInvoke(
                geometry,
                "SetNodeCoordinate",
                out _,
                out error,
                new object[] { nodeNo, x, y, z });
        }

        internal static bool TryAddBeam(object geometry, int nodeA, int nodeB, out int beamId, out string error)
        {
            beamId = 0;
            error = string.Empty;

            if (geometry == null)
            {
                error = "OpenSTAAD Geometry object is not available.";
                return false;
            }

            if (!ComInvoke.TryInvoke(geometry, "AddBeam", out object result, out error, new object[] { nodeA, nodeB }))
                return false;

            beamId = ComResult.ToInt32(result);
            return beamId > 0;
        }

        internal static bool TryGetBeamsConnectedAtNode(object geometry, int nodeId, out int[] beamIds, out string error)
        {
            beamIds = Array.Empty<int>();
            error = string.Empty;

            if (geometry == null)
            {
                error = "OpenSTAAD Geometry object is not available.";
                return false;
            }

            if (!ComInvoke.TryInvoke(
                    geometry,
                    "GetNoOfBeamsConnectedAtNode",
                    out object countRaw,
                    out error,
                    new object[] { nodeId }))
            {
                return false;
            }

            int count = ComResult.ToInt32(countRaw);
            if (count <= 0)
                return true;

            object[] args = new object[] { nodeId, new int[count] };
            if (!ComInvoke.TryInvoke(
                    geometry,
                    "GetBeamsConnectedAtNode",
                    out _,
                    out error,
                    args,
                    ComInvoke.ByRefModifiers(2, 1)))
            {
                return false;
            }

            beamIds = NormalizePositiveIds(ComResult.ToIntArray(args[1]));
            return true;
        }

        internal static bool TryGetMemberIncidence(object geometry, int beamId, out int nodeA, out int nodeB, out string error)
        {
            nodeA = 0;
            nodeB = 0;
            error = string.Empty;

            if (geometry == null)
            {
                error = "OpenSTAAD Geometry object is not available.";
                return false;
            }

            object[] args = new object[] { beamId, 0, 0 };
            if (!ComInvoke.TryInvoke(
                    geometry,
                    "GetMemberIncidence",
                    out _,
                    out error,
                    args,
                    ComInvoke.ByRefModifiers(3, 1, 2)))
            {
                return false;
            }

            nodeA = ComResult.ToInt32(args[1]);
            nodeB = ComResult.ToInt32(args[2]);
            return nodeA > 0 && nodeB > 0;
        }

        internal static bool TryGetNodeCoordinates(object geometry, int nodeNo, out double x, out double y, out double z, out string error)
        {
            x = 0.0;
            y = 0.0;
            z = 0.0;
            error = string.Empty;

            if (geometry == null)
            {
                error = "OpenSTAAD Geometry object is not available.";
                return false;
            }

            object[] args = new object[] { nodeNo, x, y, z };
            if (!ComInvoke.TryInvoke(geometry, "GetNodeCoordinates", out _, out error, args, ComInvoke.ByRefModifiers(4, 1, 2, 3)))
                return false;

            x = ToDouble(args[1]);
            y = ToDouble(args[2]);
            z = ToDouble(args[3]);
            return true;
        }

        internal static bool TryGetNodeList(object geometry, out int[] nodeIds, out string error)
        {
            nodeIds = Array.Empty<int>();
            error = string.Empty;

            if (geometry == null)
            {
                error = "OpenSTAAD Geometry object is not available.";
                return false;
            }

            if (!ComInvoke.TryInvoke(geometry, "GetNodeCount", out object countRaw, out error))
                return false;

            int count = ComResult.ToInt32(countRaw);
            if (count <= 0)
                return true;

            object[] args = new object[] { new int[count] };
            if (!ComInvoke.TryInvoke(geometry, "GetNodeList", out _, out error, args, ComInvoke.ByRefModifiers(1, 0)))
                return false;

            nodeIds = NormalizePositiveIds(ComResult.ToIntArray(args[0]));
            return true;
        }

        internal static bool TryGetBeamList(object geometry, out int[] beamIds, out string error)
        {
            beamIds = Array.Empty<int>();
            error = string.Empty;

            if (geometry == null)
            {
                error = "OpenSTAAD Geometry object is not available.";
                return false;
            }

            if (!ComInvoke.TryInvoke(geometry, "GetMemberCount", out object countRaw, out error))
                return false;

            int count = ComResult.ToInt32(countRaw);
            if (count <= 0)
                return true;

            object[] args = new object[] { new int[count] };
            if (!ComInvoke.TryInvoke(geometry, "GetBeamList", out _, out error, args, ComInvoke.ByRefModifiers(1, 0)))
                return false;

            beamIds = NormalizePositiveIds(ComResult.ToIntArray(args[0]));
            return true;
        }

        internal static bool TryClearMemberSelection(object geometry, out string error)
        {
            return TryClearSelection(geometry, "ClearMemberSelection", out error);
        }

        internal static bool TryClearNodeSelection(object geometry, out string error)
        {
            return TryClearSelection(geometry, "ClearNodeSelection", out error);
        }

        internal static bool TryHighlightBeams(object geometry, IReadOnlyList<int> beamIds, out string error)
        {
            error = string.Empty;
            int[] ids = NormalizePositiveIds(beamIds);
            if (ids.Length == 0)
            {
                error = "No valid beam IDs to highlight.";
                return false;
            }

            TryClearMemberSelection(geometry, out _);
            return TrySelectMany(geometry, "SelectMultipleBeams", "SelectBeam", ids, out error);
        }

        internal static bool TryHighlightNodes(object geometry, IReadOnlyList<int> nodeIds, out string error)
        {
            error = string.Empty;
            int[] ids = NormalizePositiveIds(nodeIds);
            if (ids.Length == 0)
            {
                error = "No valid node IDs to highlight.";
                return false;
            }

            TryClearNodeSelection(geometry, out _);
            return TrySelectMany(geometry, "SelectMultipleNodes", "SelectNode", ids, out error);
        }

        private static bool TryClearSelection(object geometry, string methodName, out string error)
        {
            error = string.Empty;
            if (geometry == null)
            {
                error = "OpenSTAAD Geometry object is not available.";
                return false;
            }

            return ComInvoke.TryInvoke(geometry, methodName, out _, out error);
        }

        private static bool TrySelectMany(object geometry, string multipleMethodName, string singleMethodName, int[] ids, out string error)
        {
            error = string.Empty;
            if (geometry == null)
            {
                error = "OpenSTAAD Geometry object is not available.";
                return false;
            }

            if (TrySelectManyDynamic(geometry, multipleMethodName, ids, out object result, out string dynamicError) &&
                IsSelectionSuccess(result))
            {
                return true;
            }

            if (ComInvoke.TryInvoke(geometry, multipleMethodName, out result, out string multipleError, new object[] { ids }) &&
                IsSelectionSuccess(result))
            {
                return true;
            }

            if (ids.Length > SingleSelectionFallbackLimit)
            {
                error = !string.IsNullOrWhiteSpace(multipleError)
                    ? multipleError
                    : (!string.IsNullOrWhiteSpace(dynamicError)
                        ? dynamicError
                        : $"STAAD bulk selection failed for {ids.Length} items.");
                return false;
            }

            int success = 0;
            string lastError = !string.IsNullOrWhiteSpace(multipleError) ? multipleError : (dynamicError ?? string.Empty);
            foreach (int id in ids)
            {
                if (ComInvoke.TryInvoke(geometry, singleMethodName, out result, out string singleError, new object[] { id }) &&
                    IsSelectionSuccess(result))
                {
                    success++;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(singleError))
                    lastError = singleError;
            }

            if (success > 0)
                return true;

            error = string.IsNullOrWhiteSpace(lastError)
                ? "STAAD selection could not be updated."
                : lastError;
            return false;
        }

        private static bool TrySelectManyDynamic(object geometry, string multipleMethodName, int[] ids, out object result, out string error)
        {
            result = null;
            error = string.Empty;
            if (geometry == null || ids == null || ids.Length == 0)
                return false;

            try
            {
                dynamic dynGeometry = geometry;

                if (string.Equals(multipleMethodName, "SelectMultipleBeams", StringComparison.Ordinal))
                    result = dynGeometry.SelectMultipleBeams(ids);
                else if (string.Equals(multipleMethodName, "SelectMultipleNodes", StringComparison.Ordinal))
                    result = dynGeometry.SelectMultipleNodes(ids);
                else
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool IsSelectionSuccess(object result)
        {
            return result == null || ComResult.IsTrue(result);
        }

        private static int[] NormalizePositiveIds(IReadOnlyList<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return Array.Empty<int>();

            var seen = new HashSet<int>();
            var result = new List<int>(ids.Count);
            foreach (int id in ids)
            {
                if (id > 0 && seen.Add(id))
                    result.Add(id);
            }

            return result.ToArray();
        }

        private static double ToDouble(object value)
        {
            return ComResult.TryToDouble(value, out double result) ? result : 0.0;
        }
    }
}
