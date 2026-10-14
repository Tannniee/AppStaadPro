using System;
using StaadSteelApp.Core.Advance;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core.Misc
{
    internal static class MiscSelectionService
    {
        internal static bool TryGetSelectedBeamIds(out int[] beamIds, out string error)
        {
            beamIds = Array.Empty<int>();
            error = string.Empty;

            if (!MiscOpenStaadService.TryGetOpenStaad(out object openStaad, out error))
                return false;

            return TryGetSelectedBeamIds(openStaad, out beamIds, out error);
        }

        internal static bool TryGetSelectedBeamIds(object openStaad, out int[] beamIds, out string error)
        {
            beamIds = Array.Empty<int>();
            error = string.Empty;

            return OpenStaadSelectionAdapter.TryGetSelectedBeamIds(
                openStaad,
                true,
                true,
                out beamIds,
                out error);
        }

        internal static bool TryGetSelectedNodeIds(out int[] nodeIds, out string error)
        {
            nodeIds = Array.Empty<int>();
            error = string.Empty;

            if (!AdvanceService.TryGetSelectedNodes(out nodeIds, out string selectionError))
            {
                error = string.IsNullOrWhiteSpace(selectionError)
                    ? "Could not read STAAD node selection."
                    : selectionError;
                return false;
            }

            if (nodeIds == null || nodeIds.Length == 0)
            {
                error = "Please select nodes in STAAD.";
                return false;
            }

            return true;
        }

        internal static bool TryGetSelectedNodeIds(object openStaad, out int[] nodeIds, out string error)
        {
            nodeIds = Array.Empty<int>();
            error = string.Empty;

            if (!OpenStaadSelectionAdapter.TryGetSelectedNodeIds(
                    openStaad,
                    true,
                    true,
                    out nodeIds,
                    out int invalidCount,
                    out error))
            {
                return false;
            }

            if (nodeIds == null || nodeIds.Length == 0)
            {
                error = invalidCount > 0
                    ? "Please select valid nodes in STAAD. Invalid or deleted node IDs were ignored."
                    : "Please select nodes in STAAD.";
                return false;
            }

            return true;
        }
    }
}
