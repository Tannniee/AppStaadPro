using System;
using System.Collections.Generic;

namespace StaadSteelApp.Core.Interop
{
    internal static class OpenStaadViewAdapter
    {
        internal static bool TryGetVisibleMembers(object openStaad, out int[] memberIds, out string error)
        {
            memberIds = Array.Empty<int>();
            error = string.Empty;

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "View", out object view, out error))
                return false;

            if (!ComInvoke.TryInvoke(view, "GetNoOfBeamsInView", out object countRaw, out error))
                return false;

            int count = Math.Max(0, ComResult.ToInt32(countRaw));
            if (count <= 0)
                return true;

            object[] args = new object[] { new int[count] };
            if (!ComInvoke.TryInvoke(view, "GetBeamsInView", out _, out error, args, ComInvoke.ByRefModifiers(1, 0)))
                return false;

            memberIds = NormalizePositiveIds(ComResult.ToIntArray(args[0]));
            return true;
        }

        internal static bool TrySetVisibleMembers(object openStaad, IReadOnlyList<int> memberIds, out string error)
        {
            error = string.Empty;
            int[] ids = NormalizePositiveIds(memberIds);

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "View", out object view, out error))
                return false;

            if (!TryHideAllMembers(view, out error))
                return false;

            if (ids.Length > 0 && !TryShowMembersCore(view, ids, out error))
            {
                string showError = error;
                ComInvoke.TryInvoke(view, "ShowAllMembers", out _, out _);
                TryRefreshView(view);
                error = showError;
                return false;
            }

            TryRefreshView(view);
            return true;
        }

        internal static bool TryHideMembers(object openStaad, IReadOnlyList<int> memberIds, out string error)
        {
            error = string.Empty;
            int[] ids = NormalizePositiveIds(memberIds);
            if (ids.Length == 0)
            {
                error = "No valid beam IDs to hide.";
                return false;
            }

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "View", out object view, out error))
                return false;

            if (!ComInvoke.TryInvoke(view, "HideMembers", out _, out error, new object[] { ids.Length, ids }) &&
                !TryHideMembersOneByOne(view, ids, out error))
            {
                return false;
            }

            TryRefreshView(view);
            return true;
        }

        internal static bool TryShowMembers(object openStaad, IReadOnlyList<int> memberIds, out string error)
        {
            error = string.Empty;
            int[] ids = NormalizePositiveIds(memberIds);
            if (ids.Length == 0)
            {
                error = "No valid beam IDs to show.";
                return false;
            }

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "View", out object view, out error))
                return false;

            if (!TryShowMembersCore(view, ids, out error))
                return false;

            TryRefreshView(view);
            return true;
        }

        internal static bool TryShowAllMembers(object openStaad, out string error)
        {
            error = string.Empty;
            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "View", out object view, out error))
                return false;

            if (!ComInvoke.TryInvoke(view, "ShowAllMembers", out _, out error))
                return false;

            TryRefreshView(view);
            return true;
        }

        private static void TryRefreshView(object view)
        {
            ComInvoke.TryInvoke(view, "RefreshView", out _, out _);
        }

        private static bool TryHideAllMembers(object view, out string error)
        {
            error = string.Empty;
            return ComInvoke.TryInvoke(view, "HideAllMembers", out _, out error);
        }

        private static bool TryShowMembersCore(object view, int[] ids, out string error)
        {
            error = string.Empty;
            if (ComInvoke.TryInvoke(view, "ShowMembers", out _, out error, new object[] { ids.Length, ids }))
                return true;

            return TryShowMembersOneByOne(view, ids, out error);
        }

        private static bool TryHideMembersOneByOne(object view, int[] ids, out string error)
        {
            error = string.Empty;
            string lastError = string.Empty;
            int hidden = 0;

            foreach (int id in ids)
            {
                if (ComInvoke.TryInvoke(view, "HideMember", out _, out string memberError, new object[] { id }))
                {
                    hidden++;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(memberError))
                    lastError = memberError;
            }

            if (hidden > 0)
                return true;

            error = string.IsNullOrWhiteSpace(lastError)
                ? "STAAD did not accept HideMembers or HideMember."
                : lastError;
            return false;
        }

        private static bool TryShowMembersOneByOne(object view, int[] ids, out string error)
        {
            error = string.Empty;
            string lastError = string.Empty;
            int shown = 0;

            foreach (int id in ids)
            {
                if (ComInvoke.TryInvoke(view, "ShowMember", out _, out string memberError, new object[] { id }))
                {
                    shown++;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(memberError))
                    lastError = memberError;
            }

            if (shown > 0)
                return true;

            error = string.IsNullOrWhiteSpace(lastError)
                ? "STAAD did not accept ShowMembers or ShowMember."
                : lastError;
            return false;
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
    }
}
