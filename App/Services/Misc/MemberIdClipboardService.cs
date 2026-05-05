using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace StaadSteelApp.Core.Misc
{
    internal static class MemberIdClipboardService
    {
        internal static void CopySelectedMemberIds()
        {
            if (!MiscSelectionService.TryGetSelectedBeamIds(out int[] beamIds, out string selectionError))
            {
                string message = string.IsNullOrWhiteSpace(selectionError)
                    ? "Could not read STAAD selection."
                    : selectionError;
                TopMostMessageBox.ShowInfo(message, "ID MEMBER");
                return;
            }

            if (beamIds == null || beamIds.Length == 0)
            {
                TopMostMessageBox.ShowInfo("Please select beams in STAAD.", "ID MEMBER");
                return;
            }

            string text = FormatMemberIdRanges(beamIds);
            Clipboard.SetText(text);
            TopMostMessageBox.ShowSuccess($"Copied {beamIds.Distinct().Count()} member IDs.", "ID MEMBER");
        }

        internal static string FormatMemberIdRanges(IEnumerable<int> memberIds)
        {
            var ids = memberIds
                .Distinct()
                .OrderBy(item => item)
                .ToArray();

            if (ids.Length == 0)
                return string.Empty;

            var parts = new List<string>();
            int start = ids[0];
            int previous = ids[0];

            for (int i = 1; i < ids.Length; i++)
            {
                int current = ids[i];
                if (current == previous + 1)
                {
                    previous = current;
                    continue;
                }

                AppendRange(parts, start, previous);
                start = current;
                previous = current;
            }

            AppendRange(parts, start, previous);
            return string.Join(" ", parts);
        }

        private static void AppendRange(ICollection<string> parts, int start, int end)
        {
            if (start == end)
            {
                parts.Add(start.ToString());
                return;
            }

            parts.Add($"{start} To {end}");
        }
    }
}
