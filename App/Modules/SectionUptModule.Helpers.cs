using System;
using System.Collections.Generic;
using System.Threading;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    internal static partial class SectionUptModule
    {
        private static int GetOrCreateUPTTableId(object os, int targetType)
        {
            if (!OpenStaadPropertyAdapter.TryGetPropertyObject(os, out object propObjForTables, out string propertyError))
                throw new SectionPropertyException(propertyError);
            int tableCount = OpenStaadPropertyAdapter.GetUserProvidedTableCount(propObjForTables, out _);
            HashSet<int> existingIds = new HashSet<int>();
            int preferredTableId = GetPreferredUPTTableId(targetType);

            if (tableCount > 0)
            {
                int[] existingTables = OpenStaadPropertyAdapter.TryGetUserProvidedTableList(propObjForTables, tableCount, out int[] tableIds, out _)
                    ? tableIds
                    : Array.Empty<int>();

                foreach (int tableId in existingTables)
                {
                    existingIds.Add(tableId);
                    if (TryGetUPTTableSectionType(propObjForTables, tableId, out int currentType) && currentType == targetType)
                        return tableId;

                    if (preferredTableId > 0 && tableId == preferredTableId)
                        return tableId;
                }
            }

            if (preferredTableId > 0 && !existingIds.Contains(preferredTableId))
            {
                OpenStaadPropertyAdapter.TryCreateUPTTableEx(propObjForTables, preferredTableId, targetType, out _);
                return preferredTableId;
            }

            for (int newId = 1; newId <= 99; newId++)
            {
                if (!existingIds.Contains(newId))
                {
                    OpenStaadPropertyAdapter.TryCreateUPTTableEx(propObjForTables, newId, targetType, out _);
                    return newId;
                }
            }

            throw new SectionPropertyException("Maximum Number of UPT Tables (99) reached.");
        }

        private static int GetPreferredUPTTableId(int targetType)
        {
            if (targetType >= 1 && targetType <= 10)
                return targetType;

            return 0;
        }

        private static int GetUPTTableSectionType(object property, int tableId)
        {
            if (OpenStaadPropertyAdapter.TryGetUserProvidedTableSectionType(property, tableId, out int sectionType, out string error))
                return sectionType;

            throw new StaadInteropException(error);
        }

        private static bool TryGetUPTTableSectionType(object property, int tableId, out int sectionType)
        {
            sectionType = 0;

            try
            {
                sectionType = GetUPTTableSectionType(property, tableId);
                return sectionType > 0;
            }
            catch (Exception)
            {
                sectionType = 0;
                return false;
            }
        }

        private static bool IsUptSectionAddSuccess(object ret)
        {
            if (ret == null) return false;
            if (ret is bool b) return !b;
            if (ret is int i) return i == 0 || i == -6045;
            if (ret is short s) return s == 0 || s == -6045;
            try
            {
                int value = Convert.ToInt32(ret);
                return value == 0 || value == -6045;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static int FindPropertyIdByNameWithRetry(
            object os,
            string propertyName,
            Func<object, string, int> checkPropertyExists,
            int attempts,
            int delayMs)
        {
            if (os == null || string.IsNullOrWhiteSpace(propertyName) || checkPropertyExists == null)
                return 0;

            string target = propertyName.Trim();
            for (int i = 0; i < attempts; i++)
            {
                int id = 0;
                try
                {
                    id = checkPropertyExists(os, target);
                }
                catch (Exception)
                {
                    id = 0;
                }

                if (id > 0) return id;

                if (i < attempts - 1)
                    Thread.Sleep(delayMs);
            }

            return 0;
        }

        private static int CreatePropertyFromUptTable(object property, int tableId, string sectionName)
        {
            if (property == null || tableId <= 0 || string.IsNullOrWhiteSpace(sectionName))
                return 0;

            string baseName = sectionName.Trim();
            string[] candidateNames = new[]
            {
                baseName,
                "UPT " + baseName
            };

            foreach (string candidate in candidateNames)
            {
                int propId = OpenStaadPropertyAdapter.CreatePropertyFromUPTTable(property, tableId, candidate, out _);
                if (propId > 0) return propId;
            }

            return 0;
        }

        private static double PositiveOrFallback(double value, double fallback, double minimum)
        {
            if (value > minimum) return value;
            if (fallback > minimum) return fallback;
            return minimum;
        }

        private static double ComputeRectangularTorsionConstant(double depth, double width)
        {
            if (depth <= 0.0 || width <= 0.0)
                return 0.0;

            double a = Math.Max(depth, width);
            double b = Math.Min(depth, width);
            double ratio = b / a;
            double term = (1.0 / 3.0) - (0.21 * ratio * (1.0 - (Math.Pow(ratio, 4) / 12.0)));
            if (term <= 0.0) term = 1.0 / 12.0;

            return a * Math.Pow(b, 3) * term;
        }

        private static double ComputeThinWalledClosedBoxTorsionConstant(double depth, double width, double thickness)
        {
            if (depth <= 0.0 || width <= 0.0 || thickness <= 0.0)
                return 0.0;

            double meanDepth = depth - thickness;
            double meanWidth = width - thickness;
            if (meanDepth <= 0.0 || meanWidth <= 0.0)
                return 0.0;

            double meanArea = meanDepth * meanWidth;
            double meanPerimeter = 2.0 * (meanDepth + meanWidth);
            if (meanPerimeter <= 0.0)
                return 0.0;

            return (4.0 * meanArea * meanArea * thickness) / meanPerimeter;
        }
    }
}
