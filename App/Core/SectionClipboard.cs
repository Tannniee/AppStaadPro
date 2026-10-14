using StaadSteelApp.Core;

namespace StaadSteelApp
{
    /// <summary>
    /// Static clipboard to hold copied SectionInfo within the application session.
    /// </summary>
    public static class SectionClipboard
    {
        public static SectionInfo Data { get; private set; }
        public static SectionTransferContext TransferContext { get; private set; }
        public static string DisplayText { get; private set; } = "";

        public static bool HasData => Data != null;

        public static void Set(SectionInfo info, SectionTransferContext transferContext = null)
        {
            Data = Clone(info);
            TransferContext = transferContext?.Clone();
            DisplayText = info == null ? "" : (info.FullName ?? info.Name ?? "");
        }

        public static SectionInfo GetCopy()
        {
            return Clone(Data);
        }

        public static SectionTransferContext GetTransferContextCopy()
        {
            return TransferContext?.Clone();
        }

        public static void Clear()
        {
            Data = null;
            TransferContext = null;
            DisplayText = "";
        }

        private static SectionInfo Clone(SectionInfo source)
        {
            return source?.Clone();
        }
    }
}
