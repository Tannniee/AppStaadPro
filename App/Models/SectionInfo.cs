namespace StaadSteelApp.Core
{
    public class SectionInfo
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public int CountryCode { get; set; }
        public int PropertyRefNo { get; set; }
        public bool CreateAsUPT { get; set; }
        public string FullName => string.IsNullOrEmpty(Code) || Code == "Sections" ? Name : Code + "-" + Name;
        public string Material { get; set; }

        public eFramePropType PropType { get; set; }
        public double H1 { get; set; }
        public double H2 { get; set; }
        public double Tw { get; set; }
        public double Bf { get; set; }
        public double Tf { get; set; }
        public double Bfb { get; set; }
        public double Tfb { get; set; }
        public double Dis { get; set; }

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

        public string ShapeName { get; set; }
        public int LegacyTypeSpec { get; set; }
        public double LegacySpec1 { get; set; }
        public double LegacySpec2 { get; set; }
        public double LegacySpec3 { get; set; }
        public double LegacySpec4 { get; set; }

        public SectionIdentity ToIdentity()
        {
            return new SectionIdentity
            {
                Name = Name,
                Code = Code,
                CountryCode = CountryCode,
                PropertyRefNo = PropertyRefNo,
                Material = Material
            };
        }

        public void ApplyIdentity(SectionIdentity identity)
        {
            if (identity == null) return;

            Name = identity.Name;
            Code = identity.Code;
            CountryCode = identity.CountryCode;
            PropertyRefNo = identity.PropertyRefNo;
            Material = identity.Material;
        }

        public SectionGeometry ToGeometry()
        {
            return new SectionGeometry
            {
                PropType = PropType,
                H1 = H1,
                H2 = H2,
                Tw = Tw,
                Bf = Bf,
                Tf = Tf,
                Bfb = Bfb,
                Tfb = Tfb,
                Dis = Dis
            };
        }

        public void ApplyGeometry(SectionGeometry geometry)
        {
            if (geometry == null) return;

            PropType = geometry.PropType;
            H1 = geometry.H1;
            H2 = geometry.H2;
            Tw = geometry.Tw;
            Bf = geometry.Bf;
            Tf = geometry.Tf;
            Bfb = geometry.Bfb;
            Tfb = geometry.Tfb;
            Dis = geometry.Dis;
        }

        public UptSectionProperties ToUptProperties()
        {
            return new UptSectionProperties
            {
                Ax = UptAx,
                Iz = UptIz,
                Iy = UptIy,
                Ix = UptIx,
                Ay = UptAy,
                Az = UptAz,
                Sz = UptSz,
                Sy = UptSy,
                Pz = UptPz,
                Py = UptPy,
                Hss = UptHss,
                Dee = UptDee
            };
        }

        public void ApplyUptProperties(UptSectionProperties properties)
        {
            if (properties == null) return;

            UptAx = properties.Ax;
            UptIz = properties.Iz;
            UptIy = properties.Iy;
            UptIx = properties.Ix;
            UptAy = properties.Ay;
            UptAz = properties.Az;
            UptSz = properties.Sz;
            UptSy = properties.Sy;
            UptPz = properties.Pz;
            UptPy = properties.Py;
            UptHss = properties.Hss;
            UptDee = properties.Dee;
        }

        public LegacySectionMetadata ToLegacyMetadata()
        {
            return new LegacySectionMetadata
            {
                ShapeName = ShapeName,
                TypeSpec = LegacyTypeSpec,
                Spec1 = LegacySpec1,
                Spec2 = LegacySpec2,
                Spec3 = LegacySpec3,
                Spec4 = LegacySpec4
            };
        }

        public void ApplyLegacyMetadata(LegacySectionMetadata metadata)
        {
            if (metadata == null) return;

            ShapeName = metadata.ShapeName;
            LegacyTypeSpec = metadata.TypeSpec;
            LegacySpec1 = metadata.Spec1;
            LegacySpec2 = metadata.Spec2;
            LegacySpec3 = metadata.Spec3;
            LegacySpec4 = metadata.Spec4;
        }

        public SectionInfo Clone()
        {
            var copy = new SectionInfo
            {
                CreateAsUPT = CreateAsUPT
            };
            copy.ApplyIdentity(ToIdentity());
            copy.ApplyGeometry(ToGeometry());
            copy.ApplyUptProperties(ToUptProperties());
            copy.ApplyLegacyMetadata(ToLegacyMetadata());
            return copy;
        }
    }
}
