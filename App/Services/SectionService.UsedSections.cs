using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        private static readonly object _usedSectionsCacheSync = new object();
        private static string _usedSectionsCacheModelKey = "";
        private static Dictionary<int, UsedSectionEntry> _usedSectionsPropertyCache = new Dictionary<int, UsedSectionEntry>();
        private static bool _usedSectionsCacheInitialized;

        public static List<UsedSectionEntry> GetUsedSections(object os)
        {
            if (os == null)
                return new List<UsedSectionEntry>();

            lock (_usedSectionsCacheSync)
            {
                EnsureUsedSectionsCache(os);
                return BuildUsedSectionsListFromCache();
            }
        }

        public static List<UsedSectionEntry> GetUsedSections(object os, bool forceRefresh)
        {
            if (os == null)
                return new List<UsedSectionEntry>();

            lock (_usedSectionsCacheSync)
            {
                if (forceRefresh)
                    RebuildUsedSectionsCache(os, GetUsedSectionsModelKey(os));
                else
                    EnsureUsedSectionsCache(os);

                return BuildUsedSectionsListFromCache();
            }
        }

        public static void UpdateUsedSectionsCacheAfterAssignment(object os, IEnumerable<int> beamIds, int targetPropertyId)
        {
            if (os == null || targetPropertyId <= 0)
                return;

            int[] assignedBeamIds = (beamIds ?? Enumerable.Empty<int>())
                .Where(x => x > 0)
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

            if (assignedBeamIds.Length == 0)
                return;

            lock (_usedSectionsCacheSync)
            {
                string modelKey = GetUsedSectionsModelKey(os);
                if (!_usedSectionsCacheInitialized || !string.Equals(_usedSectionsCacheModelKey, modelKey, StringComparison.OrdinalIgnoreCase))
                    return;

                var removeIds = new List<int>();
                foreach (var kvp in _usedSectionsPropertyCache)
                {
                    int[] remaining = (kvp.Value.BeamIds ?? Array.Empty<int>())
                        .Where(id => !assignedBeamIds.Contains(id))
                        .Distinct()
                        .OrderBy(id => id)
                        .ToArray();

                    if (remaining.Length == 0)
                    {
                        removeIds.Add(kvp.Key);
                    }
                    else
                    {
                        kvp.Value.BeamIds = remaining;
                        kvp.Value.BeamCount = remaining.Length;
                    }
                }

                foreach (int propertyId in removeIds)
                    _usedSectionsPropertyCache.Remove(propertyId);

                if (!_usedSectionsPropertyCache.TryGetValue(targetPropertyId, out UsedSectionEntry targetEntry))
                {
                    object openStaad = os;
                    if (!TryGetSectionProperty(openStaad, out object targetPropObj, out string targetPropertyError))
                    {
                        Debug.WriteLine("Property object is not available: " + targetPropertyError);
                        return;
                    }

                    targetEntry = BuildUsedSectionsCacheEntry(targetPropObj, targetPropertyId, assignedBeamIds);
                    if (targetEntry == null)
                        return;

                    _usedSectionsPropertyCache[targetPropertyId] = targetEntry;
                }
                else
                {
                    targetEntry.BeamIds = (targetEntry.BeamIds ?? Array.Empty<int>())
                        .Concat(assignedBeamIds)
                        .Where(id => id > 0)
                        .Distinct()
                        .OrderBy(id => id)
                        .ToArray();
                    targetEntry.BeamCount = targetEntry.BeamIds.Length;
                }
            }
        }

        private static void EnsureUsedSectionsCache(object os)
        {
            string modelKey = GetUsedSectionsModelKey(os);
            if (_usedSectionsCacheInitialized &&
                string.Equals(_usedSectionsCacheModelKey, modelKey, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            RebuildUsedSectionsCache(os, modelKey);
        }

        private static void RebuildUsedSectionsCache(object os, string modelKey)
        {
            var cache = new Dictionary<int, UsedSectionEntry>();
            if (os == null)
            {
                _usedSectionsPropertyCache = cache;
                _usedSectionsCacheModelKey = modelKey ?? "";
                _usedSectionsCacheInitialized = true;
                return;
            }

            object openStaad = os;
            if (!TryGetSectionProperty(openStaad, out object propObj, out string propertyError))
            {
                Debug.WriteLine("Property object is not available: " + propertyError);
                _usedSectionsPropertyCache = cache;
                _usedSectionsCacheModelKey = modelKey ?? "";
                _usedSectionsCacheInitialized = true;
                return;
            }

            int count = 0;

            if (!OpenStaadPropertyAdapter.TryGetSectionPropertyCount(propObj, out count, out string countError))
                Debug.WriteLine("GetSectionPropertyCount failed: " + countError);

            if (count > 0)
            {
                int[] propIds = GetSectionPropertyIds(propObj, count);
                foreach (int propId in propIds)
                {
                    UsedSectionEntry entry = BuildUsedSectionsCacheEntry(propObj, propId);
                    if (entry != null)
                        cache[propId] = entry;
                }
            }

            _usedSectionsPropertyCache = cache;
            _usedSectionsCacheModelKey = modelKey ?? "";
            _usedSectionsCacheInitialized = true;
        }

        private static UsedSectionEntry BuildUsedSectionsCacheEntry(object propObj, int propId, int[] beamIdsOverride = null)
        {
            if (propObj == null || propId <= 0)
                return null;

            int[] beamIds = beamIdsOverride ?? Array.Empty<int>();
            if (beamIds.Length == 0)
            {
                int beamCount = TryGetAssignedBeamCount(propObj, propId);
                if (beamCount <= 0)
                    return null;

                beamIds = TryGetAssignedBeamList(propObj, propId, beamCount);
                if (beamIds.Length == 0)
                    return null;
            }
            else
            {
                beamIds = beamIds
                    .Where(x => x > 0)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToArray();
                if (beamIds.Length == 0)
                    return null;
            }

            string rawName = "";
            TryGetSectionPropertyNameById(propObj, propId, out rawName);

            int typeCode = TryGetSectionPropertyTypeCode(propObj, propId);
            ResolveSortMetadata(typeCode, out int sortGroup, out int sortType, out string sourceLabel);
            string displayName = BuildUsedSectionDisplayName(propObj, propId, typeCode, rawName);

            return new UsedSectionEntry
            {
                PropertyId = propId,
                PropertyIds = new[] { propId },
                BeamIds = beamIds,
                Name = string.IsNullOrWhiteSpace(rawName) ? ("PROP-" + propId) : rawName.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? rawName : displayName,
                SourceLabel = sourceLabel,
                SortGroup = sortGroup,
                SortType = sortType,
                BeamCount = beamIds.Length
            };
        }

        private static List<UsedSectionEntry> BuildUsedSectionsListFromCache()
        {
            return _usedSectionsPropertyCache.Values
                .Select(CloneUsedSectionEntry)
                .GroupBy(
                    x => string.IsNullOrWhiteSpace(x.SelectionKey) ? ("PROP-" + x.PropertyId) : x.SelectionKey,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    UsedSectionEntry first = group
                        .OrderBy(x => x.SortGroup)
                        .ThenBy(x => x.SortType)
                        .ThenBy(x => x.PropertyId)
                        .First();

                    return new UsedSectionEntry
                    {
                        PropertyId = group.Min(x => x.PropertyId),
                        PropertyIds = group.SelectMany(x => x.PropertyIds ?? new[] { x.PropertyId }).Distinct().OrderBy(x => x).ToArray(),
                        BeamIds = group.SelectMany(x => x.BeamIds ?? Array.Empty<int>()).Where(x => x > 0).Distinct().OrderBy(x => x).ToArray(),
                        Name = first.Name,
                        DisplayName = first.DisplayName,
                        SourceLabel = first.SourceLabel,
                        SortGroup = first.SortGroup,
                        SortType = first.SortType,
                        BeamCount = group.SelectMany(x => x.BeamIds ?? Array.Empty<int>()).Where(x => x > 0).Distinct().Count()
                    };
                })
                .OrderBy(x => x.SortGroup)
                .ThenBy(x => x.SortType)
                .ThenBy(x => string.IsNullOrWhiteSpace(x.DisplayName) ? x.Name : x.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.PropertyId)
                .ToList();
        }

        private static UsedSectionEntry CloneUsedSectionEntry(UsedSectionEntry source)
        {
            if (source == null)
                return null;

            return new UsedSectionEntry
            {
                PropertyId = source.PropertyId,
                PropertyIds = (source.PropertyIds ?? Array.Empty<int>()).ToArray(),
                BeamIds = (source.BeamIds ?? Array.Empty<int>()).ToArray(),
                Name = source.Name,
                DisplayName = source.DisplayName,
                SourceLabel = source.SourceLabel,
                SortGroup = source.SortGroup,
                SortType = source.SortType,
                BeamCount = source.BeamCount
            };
        }

        private static string GetUsedSectionsModelKey(object os)
        {
            return StaadManager.GetModelCacheKey(os);
        }

        private static bool TryGetStaadFilePath(object os, out string staadFilePath)
        {
            staadFilePath = "";
            if (os == null)
                return false;

            foreach (object fullPathFlag in new object[] { "TRUE", true, 1 })
            {
                if (OpenStaadModelAdapter.TryGetStaadFile((object)os, fullPathFlag, out string path, out string error))
                {
                    staadFilePath = path;
                    return true;
                }
                else if (!string.IsNullOrWhiteSpace(error))
                {
                    Debug.WriteLine("GetSTAADFile failed: " + error);
                }
            }

            return false;
        }

        private static int[] GetSectionPropertyIds(object propObj, int count)
        {
            if (propObj == null || count <= 0)
                return Array.Empty<int>();

            if (OpenStaadPropertyAdapter.TryGetSectionPropertyList(propObj, out int[] propertyIds, out string error))
                return propertyIds;

            Debug.WriteLine("GetSectionPropertyList failed: " + error);

            return Array.Empty<int>();
        }

        private static int TryGetAssignedBeamCount(object propObj, int propertyId)
        {
            if (propObj == null || propertyId <= 0)
                return 0;

            if (OpenStaadPropertyAdapter.TryGetSectionPropertyAssignedBeamCount(propObj, propertyId, out int count, out string error))
                return count;

            Debug.WriteLine($"GetSectionPropertyAssignedBeamCount failed for {propertyId}: {error}");

            return 0;
        }

        private static int[] TryGetAssignedBeamList(object propObj, int propertyId, int count)
        {
            if (propObj == null || propertyId <= 0 || count <= 0)
                return Array.Empty<int>();

            if (OpenStaadPropertyAdapter.TryGetSectionPropertyAssignedBeamList(propObj, propertyId, count, out int[] beamIds, out string error))
                return beamIds;

            Debug.WriteLine($"GetSectionPropertyAssignedBeamList failed for {propertyId}: {error}");

            return Array.Empty<int>();
        }

        private static int TryGetSectionPropertyTypeCode(object propObj, int propertyId)
        {
            if (propObj == null || propertyId <= 0)
                return 0;

            if (OpenStaadPropertyAdapter.TryGetSectionPropertyType(propObj, propertyId, out int typeCode, out string error))
                return typeCode;

            Debug.WriteLine($"GetSectionPropertyType failed for {propertyId}: {error}");

            return 0;
        }


    }
}
