namespace StaadSteelApp.Core
{
    public enum eFramePropType
    {
        I = 1,
        Channel = 2,
        T = 3,
        Angle = 4,
        DblAngle = 5,
        Box = 6,
        Pipe = 7,
        Rectangular = 8,
        Circle = 9,
        General = 10,
        DbChannel = 11,
        AutoSelect = 12,
        Variable = 14,
        ISection = 15,
        Prismatic = 16,
        Legacy = 99
    }


    public class SectionInfo
    {
        public string Name {get;set;}
        public string Code {get;set;}
        public int CountryCode {get;set;}
        // STAAD Property reference number (e.g. R29). 0 when not sourced from an existing member property.
        public int PropertyRefNo { get; set; }
        public bool CreateAsUPT { get; set; }

        public string FullName => string.IsNullOrEmpty(Code) || Code == "Sections" ? Name : Code + "-" + Name; // Fallback
        public string Material {get;set;}
        public eFramePropType PropType {get;set;}
        public double H1 {get;set;}
        public double H2 {get;set;}
        public double Tw {get;set;}
        public double Bf {get;set;}
        public double Tf {get;set;}
        public double Bfb {get;set;}
        public double Tfb {get;set;}
        public double Dis {get;set;}
        // UPT engineering properties (OpenSTAAD units, typically SI based).
        public double UptAx { get; set; }
        public double UptIz { get; set; }
        public double UptIy { get; set; }
        public double UptIx { get; set; }
        public double UptAy { get; set; }
        public double UptAz { get; set; }
        public double UptSz { get; set; }
        public double UptSy { get; set; }
        public double UptPz { get; set; }
        public double UptPy { get; set; }
        public double UptHss { get; set; }
        public double UptDee { get; set; }
        public string ShapeName {get;set;} // For Legacy sections: "Pipe", "Wide Flange", etc.
        public int LegacyTypeSpec {get;set;}
        public double LegacySpec1 {get;set;}
        public double LegacySpec2 {get;set;}
        public double LegacySpec3 {get;set;}
        public double LegacySpec4 {get;set;}
    }
}
