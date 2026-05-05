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
            if (source == null) return null;

            return new SectionInfo
            {
                Name = source.Name,
                Code = source.Code,
                CountryCode = source.CountryCode,
                PropertyRefNo = source.PropertyRefNo,
                CreateAsUPT = source.CreateAsUPT,
                Material = source.Material,
                PropType = source.PropType,
                H1 = source.H1,
                H2 = source.H2,
                Tw = source.Tw,
                Bf = source.Bf,
                Tf = source.Tf,
                Bfb = source.Bfb,
                Tfb = source.Tfb,
                Dis = source.Dis,
                UptAx = source.UptAx,
                UptIz = source.UptIz,
                UptIy = source.UptIy,
                UptIx = source.UptIx,
                UptAy = source.UptAy,
                UptAz = source.UptAz,
                UptSz = source.UptSz,
                UptSy = source.UptSy,
                UptPz = source.UptPz,
                UptPy = source.UptPy,
                UptHss = source.UptHss,
                UptDee = source.UptDee,
                ShapeName = source.ShapeName,
                LegacyTypeSpec = source.LegacyTypeSpec,
                LegacySpec1 = source.LegacySpec1,
                LegacySpec2 = source.LegacySpec2,
                LegacySpec3 = source.LegacySpec3,
                LegacySpec4 = source.LegacySpec4
            };
        }
    }
}
