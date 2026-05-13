using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core.Misc
{
    internal static class MemberRenumberService
    {
        private const string ResultTitle = "Renumber";

        internal static ServiceOperationResult RenumberMembersByPropertyRef()
        {
            if (!MiscOpenStaadService.TryGetOpenStaad(out object openStaad, out string connectionError))
                return ServiceOperationResult.Error(ResultTitle, connectionError);

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Geometry", out object geometry, out string geometryError))
                return ServiceOperationResult.Error(ResultTitle, geometryError);

            if (!OpenStaadPropertyAdapter.TryGetPropertyObject(openStaad, out object propertyObj, out string propertyError))
                return ServiceOperationResult.Error(ResultTitle, propertyError);

            if (!OpenStaadGeometryAdapter.TryGetBeamList(geometry, out int[] beamIds, out string beamError))
                return ServiceOperationResult.Error(ResultTitle, "Could not read member list: " + beamError);

            beamIds = NormalizeIds(beamIds);
            if (beamIds.Length == 0)
                return ServiceOperationResult.Info(ResultTitle, "No analytical members found in STAAD.");

            if (!TryBuildMemberGroups(propertyObj, beamIds, out List<MemberRefItem> members, out string groupError))
                return ServiceOperationResult.Error(ResultTitle, groupError);

            List<RenumberPair> finalPairs = BuildFinalPairs(members);
            if (finalPairs.Count == 0)
                return ServiceOperationResult.Info(ResultTitle, "No members to renumber.");

            if (finalPairs.All(pair => pair.OldId == pair.NewId))
            {
                return ServiceOperationResult.Info(
                    ResultTitle,
                    $"Members are already ordered by property Ref ID. Checked {finalPairs.Count} members across {members.Select(item => item.PropertyRefNo).Distinct().Count()} groups.");
            }

            if (!TryRenumberInTwoPasses(geometry, finalPairs, out string renumberError))
                return ServiceOperationResult.Error(ResultTitle, renumberError);

            int groupCount = members.Select(item => item.PropertyRefNo).Distinct().Count();
            return ServiceOperationResult.Success(
                ResultTitle,
                $"Renumbered {finalPairs.Count} members across {groupCount} property Ref ID groups. New range: 1 To {finalPairs.Count}.");
        }

        private static bool TryBuildMemberGroups(
            object propertyObj,
            int[] beamIds,
            out List<MemberRefItem> members,
            out string error)
        {
            members = new List<MemberRefItem>(beamIds.Length);
            var errors = new StringBuilder();

            foreach (int beamId in beamIds)
            {
                if (OpenStaadPropertyAdapter.TryGetBeamSectionPropertyRefNoFromProperty(
                        propertyObj,
                        beamId,
                        out int propertyRefNo,
                        out string refError))
                {
                    members.Add(new MemberRefItem(beamId, propertyRefNo));
                    continue;
                }

                errors.AppendLine($"Member {beamId}: {refError}");
            }

            error = errors.ToString().Trim();
            if (members.Count == beamIds.Length)
                return true;

            if (string.IsNullOrWhiteSpace(error))
                error = "Could not read property Ref ID for every member.";

            return false;
        }

        private static List<RenumberPair> BuildFinalPairs(IEnumerable<MemberRefItem> members)
        {
            var pairs = new List<RenumberPair>();
            int nextId = 1;

            foreach (MemberRefItem member in members
                         .GroupBy(item => item.PropertyRefNo)
                         .OrderBy(group => group.Key)
                         .SelectMany(group => group.OrderBy(item => item.MemberId)))
            {
                pairs.Add(new RenumberPair(member.MemberId, nextId++));
            }

            return pairs;
        }

        private static bool TryRenumberInTwoPasses(object geometry, List<RenumberPair> finalPairs, out string error)
        {
            error = string.Empty;

            int tempStart = finalPairs.Max(pair => pair.OldId) + 1;
            var tempPairs = finalPairs
                .Select((pair, index) => new RenumberPair(pair.OldId, tempStart + index))
                .ToList();

            var movedToTemp = new List<RenumberPair>();
            foreach (RenumberPair pair in tempPairs)
            {
                if (OpenStaadGeometryAdapter.TryRenumberBeam(geometry, pair.OldId, pair.NewId, out string stepError))
                {
                    movedToTemp.Add(pair);
                    continue;
                }

                TryRollbackTempPass(geometry, movedToTemp, out string rollbackError);
                error = BuildFailureMessage("Temporary renumber pass failed.", pair, stepError, rollbackError);
                return false;
            }

            var tempToFinal = new List<RenumberPair>(finalPairs.Count);
            for (int i = 0; i < finalPairs.Count; i++)
                tempToFinal.Add(new RenumberPair(tempPairs[i].NewId, finalPairs[i].NewId));

            var movedToFinal = new List<RenumberPair>();
            foreach (RenumberPair pair in tempToFinal)
            {
                if (OpenStaadGeometryAdapter.TryRenumberBeam(geometry, pair.OldId, pair.NewId, out string stepError))
                {
                    movedToFinal.Add(pair);
                    continue;
                }

                TryRollbackFinalPass(geometry, movedToFinal, tempPairs, out string rollbackError);
                error = BuildFailureMessage("Final renumber pass failed.", pair, stepError, rollbackError);
                return false;
            }

            return true;
        }

        private static void TryRollbackFinalPass(
            object geometry,
            List<RenumberPair> movedToFinal,
            List<RenumberPair> tempPairs,
            out string error)
        {
            var errors = new StringBuilder();

            for (int i = movedToFinal.Count - 1; i >= 0; i--)
            {
                RenumberPair pair = movedToFinal[i];
                if (!OpenStaadGeometryAdapter.TryRenumberBeam(geometry, pair.NewId, pair.OldId, out string finalRollbackError))
                    errors.AppendLine($"{pair.NewId} -> {pair.OldId}: {finalRollbackError}");
            }

            TryRollbackTempPass(geometry, tempPairs, out string tempRollbackError);
            if (!string.IsNullOrWhiteSpace(tempRollbackError))
                errors.AppendLine(tempRollbackError);

            error = errors.ToString().Trim();
        }

        private static void TryRollbackTempPass(object geometry, List<RenumberPair> movedToTemp, out string error)
        {
            var errors = new StringBuilder();

            for (int i = movedToTemp.Count - 1; i >= 0; i--)
            {
                RenumberPair pair = movedToTemp[i];
                if (!OpenStaadGeometryAdapter.TryRenumberBeam(geometry, pair.NewId, pair.OldId, out string rollbackError))
                    errors.AppendLine($"{pair.NewId} -> {pair.OldId}: {rollbackError}");
            }

            error = errors.ToString().Trim();
        }

        private static string BuildFailureMessage(string header, RenumberPair pair, string stepError, string rollbackError)
        {
            string message = $"{header}\nPair: {pair.OldId} -> {pair.NewId}\n{stepError}";
            if (!string.IsNullOrWhiteSpace(rollbackError))
                message += "\nRollback issues:\n" + rollbackError;

            return message;
        }

        private static int[] NormalizeIds(IEnumerable<int> ids)
        {
            return ids == null
                ? Array.Empty<int>()
                : ids.Where(id => id > 0).Distinct().OrderBy(id => id).ToArray();
        }

        private sealed class MemberRefItem
        {
            internal MemberRefItem(int memberId, int propertyRefNo)
            {
                MemberId = memberId;
                PropertyRefNo = propertyRefNo;
            }

            internal int MemberId { get; }
            internal int PropertyRefNo { get; }
        }

        private sealed class RenumberPair
        {
            internal RenumberPair(int oldId, int newId)
            {
                OldId = oldId;
                NewId = newId;
            }

            internal int OldId { get; }
            internal int NewId { get; }
        }
    }
}
