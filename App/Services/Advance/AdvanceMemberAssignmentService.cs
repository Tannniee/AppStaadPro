using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core.Advance
{
    internal sealed class AdvanceMemberAssignmentSnapshot
    {
        internal AdvanceMemberAssignmentSnapshot(
            IDictionary<int, int> propertyByMember,
            IDictionary<int, string> materialByMember)
        {
            PropertyByMember = new Dictionary<int, int>(propertyByMember ?? new Dictionary<int, int>());
            MaterialByMember = new Dictionary<int, string>(materialByMember ?? new Dictionary<int, string>());
        }

        internal IReadOnlyDictionary<int, int> PropertyByMember { get; }
        internal IReadOnlyDictionary<int, string> MaterialByMember { get; }

        internal IReadOnlyDictionary<string, int[]> GetMaterialGroups()
        {
            return MaterialByMember
                .GroupBy(item => item.Value, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(item => item.Key).OrderBy(id => id).ToArray(),
                    StringComparer.OrdinalIgnoreCase);
        }

        internal string BuildMaterialSummary()
        {
            return string.Join(
                ", ",
                GetMaterialGroups().Select(group => $"{group.Key} ({group.Value.Length})"));
        }

        internal bool TryGetUniformMaterial(out string materialName)
        {
            string[] materials = MaterialByMember.Values
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            materialName = materials.Length == 1 ? materials[0] : string.Empty;
            return materials.Length == 1;
        }
    }

    internal static class AdvanceMemberAssignmentService
    {
        internal static bool TryCapture(
            object propertyObj,
            IEnumerable<int> memberIds,
            out AdvanceMemberAssignmentSnapshot snapshot,
            out string error)
        {
            snapshot = null;
            error = string.Empty;

            if (propertyObj == null)
            {
                error = "STAAD property object is not available.";
                return false;
            }

            int[] ids = NormalizeIds(memberIds);
            if (ids.Length == 0)
            {
                error = "No members were provided.";
                return false;
            }

            var propertyByMember = new Dictionary<int, int>();
            var materialByMember = new Dictionary<int, string>();
            var errors = new StringBuilder();

            foreach (int memberId in ids)
            {
                if (!OpenStaadPropertyAdapter.TryGetBeamSectionPropertyRefNoFromProperty(
                        propertyObj,
                        memberId,
                        out int propertyId,
                        out string propertyError))
                {
                    errors.AppendLine($"Member {memberId}: could not read section Ref ID. {propertyError}");
                    continue;
                }

                if (!TryReadMaterial(propertyObj, memberId, out string materialName, out string materialError))
                {
                    errors.AppendLine($"Member {memberId}: could not read Material. {materialError}");
                    continue;
                }

                propertyByMember[memberId] = propertyId;
                materialByMember[memberId] = materialName;
            }

            if (propertyByMember.Count != ids.Length || materialByMember.Count != ids.Length)
            {
                error = errors.ToString().Trim();
                if (string.IsNullOrWhiteSpace(error))
                    error = $"Could not capture section and Material for all {ids.Length} members.";
                return false;
            }

            snapshot = new AdvanceMemberAssignmentSnapshot(propertyByMember, materialByMember);
            return true;
        }

        internal static bool TryReassignPropertyPreservingMaterial(
            object propertyObj,
            IEnumerable<int> memberIds,
            int targetPropertyId,
            string expectedMaterial,
            out string materialSummary,
            out string error)
        {
            materialSummary = string.Empty;
            error = string.Empty;

            if (targetPropertyId <= 0)
            {
                error = "Target section Ref ID is invalid.";
                return false;
            }

            if (!TryCapture(propertyObj, memberIds, out AdvanceMemberAssignmentSnapshot snapshot, out error))
                return false;

            materialSummary = snapshot.BuildMaterialSummary();
            if (!string.IsNullOrWhiteSpace(expectedMaterial))
            {
                if (!snapshot.TryGetUniformMaterial(out string currentMaterial) ||
                    !MaterialsMatch(currentMaterial, expectedMaterial))
                {
                    error = $"Material changed after Preview. Expected {expectedMaterial}; found {materialSummary}. Run Preview again.";
                    return false;
                }
            }

            int[] ids = NormalizeIds(memberIds);
            if (!TryAssignPropertyGroup(propertyObj, ids, targetPropertyId, out string propertyError))
            {
                string rollback = TryRollback(propertyObj, snapshot);
                error = "Could not assign the target section. " + propertyError + rollback;
                return false;
            }

            if (!TryRestoreMaterials(propertyObj, snapshot, out string materialError))
            {
                string rollback = TryRollback(propertyObj, snapshot);
                error = "The section changed, but Material verification failed. " + materialError + rollback;
                return false;
            }

            return true;
        }

        private static string TryRollback(object propertyObj, AdvanceMemberAssignmentSnapshot snapshot)
        {
            var issues = new List<string>();

            foreach (IGrouping<int, KeyValuePair<int, int>> group in snapshot.PropertyByMember
                         .GroupBy(item => item.Value)
                         .OrderBy(group => group.Key))
            {
                int[] memberIds = group.Select(item => item.Key).OrderBy(id => id).ToArray();
                if (!TryAssignPropertyGroup(propertyObj, memberIds, group.Key, out string propertyError))
                    issues.Add($"section Ref {group.Key}: {propertyError}");
            }

            if (!TryRestoreMaterials(propertyObj, snapshot, out string materialError))
                issues.Add("Material: " + materialError);

            return issues.Count == 0
                ? " Original section and Material assignments were restored."
                : " Rollback was incomplete: " + string.Join(" | ", issues);
        }

        private static bool TryAssignPropertyGroup(
            object propertyObj,
            int[] memberIds,
            int propertyId,
            out string error)
        {
            error = string.Empty;
            if (memberIds == null || memberIds.Length == 0)
                return true;

            string commandError = string.Empty;

            bool bulkAccepted = OpenStaadPropertyAdapter.TryAssignBeamProperty(
                propertyObj,
                memberIds,
                propertyId,
                out string bulkError);

            if (!bulkAccepted)
            {
                var fallbackErrors = new StringBuilder();

                foreach (int memberId in memberIds)
                {
                    if (!OpenStaadPropertyAdapter.TryAssignBeamProperty(propertyObj, memberId, propertyId, out string singleError))
                        fallbackErrors.AppendLine($"Member {memberId}: {singleError}");
                }

                if (fallbackErrors.Length > 0)
                {
                    commandError = string.IsNullOrWhiteSpace(bulkError)
                        ? fallbackErrors.ToString().Trim()
                        : "Bulk assignment failed: " + bulkError + Environment.NewLine + fallbackErrors.ToString().Trim();
                }
            }

            var verificationErrors = new StringBuilder();
            foreach (int memberId in memberIds)
            {
                if (!OpenStaadPropertyAdapter.TryGetBeamSectionPropertyRefNoFromProperty(
                        propertyObj,
                        memberId,
                        out int actualPropertyId,
                        out string readError) ||
                    actualPropertyId != propertyId)
                {
                    verificationErrors.AppendLine($"Member {memberId}: expected section Ref {propertyId}, found {actualPropertyId}. {readError}");
                }
            }

            if (verificationErrors.Length == 0)
                return true;

            error = AppendError(commandError, verificationErrors.ToString().Trim());
            return false;
        }

        private static bool TryRestoreMaterials(
            object propertyObj,
            AdvanceMemberAssignmentSnapshot snapshot,
            out string error)
        {
            error = string.Empty;
            var assignmentErrors = new StringBuilder();

            foreach (KeyValuePair<string, int[]> group in snapshot.GetMaterialGroups())
            {
                string materialName = group.Key;
                int[] memberIds = group.Value;

                if (string.Equals(materialName, "None", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (int memberId in memberIds)
                    {
                        if (TryReadMaterial(propertyObj, memberId, out string currentMaterial, out _) &&
                            MaterialsMatch(currentMaterial, "None"))
                        {
                            continue;
                        }

                        if (!OpenStaadPropertyAdapter.TryRemoveMaterialFromBeam(propertyObj, memberId, out string removeError))
                            assignmentErrors.AppendLine($"Member {memberId}: could not restore Material None. {removeError}");
                    }
                    continue;
                }

                OpenStaadPropertyAdapter.TrySetMaterialName(propertyObj, materialName, out _);

                bool bulkAccepted = OpenStaadPropertyAdapter.TryAssignMaterialToMember(
                    propertyObj,
                    materialName,
                    memberIds,
                    out string bulkError);

                if (bulkAccepted)
                    continue;

                var fallbackErrors = new StringBuilder();

                foreach (int memberId in memberIds)
                {
                    if (!OpenStaadPropertyAdapter.TryAssignMaterialToMember(
                            propertyObj,
                            materialName,
                            memberId,
                            out string singleError))
                    {
                        fallbackErrors.AppendLine($"Member {memberId}: could not assign Material {materialName}. {singleError}");
                    }
                }

                if (fallbackErrors.Length > 0)
                {
                    if (!string.IsNullOrWhiteSpace(bulkError))
                        assignmentErrors.AppendLine($"Material {materialName}: bulk assignment failed. {bulkError}");
                    assignmentErrors.Append(fallbackErrors);
                }
            }

            var verificationErrors = new StringBuilder();
            foreach (KeyValuePair<int, string> item in snapshot.MaterialByMember.OrderBy(item => item.Key))
            {
                if (!TryReadMaterial(propertyObj, item.Key, out string actualMaterial, out string readError) ||
                    !MaterialsMatch(actualMaterial, item.Value))
                {
                    verificationErrors.AppendLine($"Member {item.Key}: expected Material {item.Value}, found {actualMaterial}. {readError}");
                }
            }

            if (verificationErrors.Length == 0)
                return true;

            error = AppendError(assignmentErrors.ToString().Trim(), verificationErrors.ToString().Trim());
            return false;
        }

        private static bool TryReadMaterial(
            object propertyObj,
            int memberId,
            out string materialName,
            out string error)
        {
            materialName = string.Empty;
            error = string.Empty;

            bool read = OpenStaadPropertyAdapter.TryGetBeamMaterialNameFromProperty(
                propertyObj,
                memberId,
                out string rawMaterial,
                out error);

            if (read && !string.IsNullOrWhiteSpace(rawMaterial))
            {
                materialName = NormalizeMaterial(rawMaterial);
                return true;
            }

            if (string.IsNullOrWhiteSpace(error))
            {
                materialName = "None";
                return true;
            }

            return false;
        }

        private static bool MaterialsMatch(string left, string right)
        {
            return string.Equals(
                NormalizeMaterial(left),
                NormalizeMaterial(right),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeMaterial(string materialName)
        {
            string value = (materialName ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(value) ||
                   string.Equals(value, "None", StringComparison.OrdinalIgnoreCase)
                ? "None"
                : value;
        }

        private static string AppendError(string current, string next)
        {
            if (string.IsNullOrWhiteSpace(current))
                return next ?? string.Empty;
            if (string.IsNullOrWhiteSpace(next))
                return current;
            return current + Environment.NewLine + next;
        }

        private static int[] NormalizeIds(IEnumerable<int> memberIds)
        {
            return memberIds == null
                ? Array.Empty<int>()
                : memberIds.Where(id => id > 0).Distinct().OrderBy(id => id).ToArray();
        }
    }
}
