using StaadSteelApp.Core;

namespace StaadSteelApp
{
    /// <summary>
    /// Static clipboard to hold copied SectionInfo within the application session.
    /// </summary>
    public static class SectionClipboard
    {
        public static SectionInfo Data { get; private set; }
        public static string DisplayText { get; private set; } = "";

        public static bool HasData => Data != null;

        public static void Set(SectionInfo info)
        {
            Data = Clone(info);
            DisplayText = info == null ? "" : (info.FullName ?? info.Name ?? "");
        }

        public static SectionInfo GetCopy()
        {
            return Clone(Data);
        }

        public static void Clear()
        {
            Data = null;
            DisplayText = "";
        }

        private static SectionInfo Clone(SectionInfo source)
        {
            return source?.Clone();
        }
    }
}
