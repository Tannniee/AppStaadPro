using System;

namespace StaadSteelApp.Core.Interop
{
    internal static partial class OpenStaadPropertyAdapter
    {
        internal static bool TryGetMaterialPropertyRaw(
            object propertyObj,
            string materialName,
            bool extended,
            out double[] values,
            out object result,
            out string error)
        {
            values = extended ? new double[10] : new double[5];
            result = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(materialName))
            {
                error = "Material name is empty.";
                return false;
            }

            object[] args = extended
                ? new object[] { materialName, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 }
                : new object[] { materialName, 0.0, 0.0, 0.0, 0.0, 0.0 };

            if (!ComInvoke.TryInvoke(
                    propertyObj,
                    extended ? "GetMaterialPropertyEx" : "GetMaterialProperty",
                    out result,
                    out error,
                    args,
                    ComInvoke.ByRefModifiers(args.Length, BuildByRefIndexes(args.Length))))
                return false;

            values = new double[args.Length - 1];
            for (int i = 1; i < args.Length; i++)
                values[i - 1] = ToDouble(args[i]);

            return true;
        }

        internal static bool TryGetIsotropicMaterialCount(object propertyObj, out int count, out string error)
        {
            count = 0;
            error = string.Empty;

            if (!ComInvoke.TryInvoke(propertyObj, "GetIsotropicMaterialCount", out object countRaw, out error))
                return false;

            count = Math.Max(0, ComResult.ToInt32(countRaw));
            return true;
        }

        internal static bool TryGetIsotropicMaterialName(object propertyObj, int materialIndex, out string materialName, out string error)
        {
            materialName = string.Empty;
            error = string.Empty;

            object[] args = new object[] { materialIndex, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
            if (!ComInvoke.TryInvoke(propertyObj, "GetIsotropicMaterialProperties", out object result, out error, args, ComInvoke.ByRefModifiers(7, 1, 2, 3, 4, 5, 6)))
                return false;

            materialName = Convert.ToString(result)?.Trim() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(materialName);
        }
    }
}
