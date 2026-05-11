namespace StaadSteelApp.Core
{
    public sealed class SectionIdentity
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public int CountryCode { get; set; }
        public int PropertyRefNo { get; set; }
        public string Material { get; set; }
    }

    public sealed class SectionGeometry
    {
        public eFramePropType PropType { get; set; }
        public double H1 { get; set; }
        public double H2 { get; set; }
        public double Tw { get; set; }
        public double Bf { get; set; }
        public double Tf { get; set; }
        public double Bfb { get; set; }
        public double Tfb { get; set; }
        public double Dis { get; set; }
    }

    public sealed class UptSectionProperties
    {
        public double Ax { get; set; }
        public double Iz { get; set; }
        public double Iy { get; set; }
        public double Ix { get; set; }
        public double Ay { get; set; }
        public double Az { get; set; }
        public double Sz { get; set; }
        public double Sy { get; set; }
        public double Pz { get; set; }
        public double Py { get; set; }
        public double Hss { get; set; }
        public double Dee { get; set; }
    }

    public sealed class LegacySectionMetadata
    {
        public string ShapeName { get; set; }
        public int TypeSpec { get; set; }
        public double Spec1 { get; set; }
        public double Spec2 { get; set; }
        public double Spec3 { get; set; }
        public double Spec4 { get; set; }
    }
}
