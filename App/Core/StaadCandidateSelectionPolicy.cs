using System;
using System.Collections.Generic;
using System.Linq;

namespace StaadSteelApp.Core
{
    internal enum StaadCandidateSelectionOutcome
    {
        None,
        Selected,
        Ambiguous
    }

    internal static class StaadCandidateSelectionPolicy
    {
        internal static StaadCandidateSelectionOutcome SelectIndex(
            IReadOnlyList<int> candidateProcessIds,
            int activeProcessId,
            IReadOnlyCollection<int> runningStaadProcessIds,
            out int selectedIndex)
        {
            selectedIndex = -1;
            IReadOnlyList<int> candidates = candidateProcessIds ?? Array.Empty<int>();
            int runningCount = runningStaadProcessIds?.Where(processId => processId > 0).Distinct().Count() ?? 0;

            if (candidates.Count == 0)
                return runningCount > 1
                    ? StaadCandidateSelectionOutcome.Ambiguous
                    : StaadCandidateSelectionOutcome.None;

            if (activeProcessId > 0)
            {
                int[] matchingIndexes = Enumerable.Range(0, candidates.Count)
                    .Where(index => candidates[index] == activeProcessId)
                    .ToArray();
                if (matchingIndexes.Length == 1)
                {
                    selectedIndex = matchingIndexes[0];
                    return StaadCandidateSelectionOutcome.Selected;
                }

                if (matchingIndexes.Length > 1)
                    return StaadCandidateSelectionOutcome.Ambiguous;
            }

            if (candidates.Count == 1 && runningCount <= 1)
            {
                selectedIndex = 0;
                return StaadCandidateSelectionOutcome.Selected;
            }

            return StaadCandidateSelectionOutcome.Ambiguous;
        }
    }
}
