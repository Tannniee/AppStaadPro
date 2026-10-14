using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Excel = Microsoft.Office.Interop.Excel;

namespace StaadSteelApp.Core
{
    public class SectionStat
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Material { get; set; }
        public int Count { get; set; }
        public double TotalLength { get; set; } // m
        public double Area { get; set; } // m2
        public double Density { get; set; } // kg/m3
        public double TotalWeight { get; set; } // kg
        public double Perimeter { get; set; } // m
        public double Thickness { get; set; } // mm
        public string ASTMA123Category { get; set; }
        public int CoatingGrade { get; set; }
        public double CoatingWeight { get; set; } // g/m2
        public double ZincMass { get; set; } // kg
        public double FirePaintThickness { get; set; } // mm
        public double FirePaintVolume { get; set; } // L
        public double FirePaintMass { get; set; } // kg
        internal SectionCoatingDimensions CoatingDimensions { get; set; } = new SectionCoatingDimensions();
        public List<SectionPlateMass> PlateMasses { get; } = new List<SectionPlateMass>();
    }

    internal sealed class SectionCoatingDimensions
    {
        public double WebDepthMm { get; set; }
        public double FlangeWidthMm { get; set; }
        public double WebThicknessMm { get; set; }
        public double FlangeThicknessMm { get; set; }
        public double PipeOutsideDiameterMm { get; set; }
        public double PipeThicknessMm { get; set; }
        public double BoxAMm { get; set; }
        public double BoxBMm { get; set; }
        public double BoxThicknessAMm { get; set; }
        public double BoxThicknessBMm { get; set; }
    }

    public class SectionPlateMass
    {
        public double Thickness { get; set; } // mm
        public int Count { get; set; }
        public double TotalLength { get; set; } // m
        public double Mass { get; set; } // kg
    }

    public sealed class SectionMassBreakdownOptions
    {
        private readonly HashSet<int> _rolledSectionIds;

        public SectionMassBreakdownOptions(IEnumerable<int> rolledSectionIds)
        {
            _rolledSectionIds = new HashSet<int>(rolledSectionIds ?? Enumerable.Empty<int>());
        }

        public static SectionMassBreakdownOptions Empty { get; } = new SectionMassBreakdownOptions(null);

        public bool IsRolled(SectionStat stat)
        {
            return stat != null && _rolledSectionIds.Contains(stat.Id);
        }
    }

    internal sealed class SectionPlateComponentSpec
    {
        public double Thickness { get; set; } // mm
        public int Count { get; set; }
        public double LengthMultiplier { get; set; }
        public double AreaPerLength { get; set; } // m2
    }

    public enum ASTMA123Category
    {
        Plate,
        PipeTubing,
        StructuralShapes
    }

    public static partial class SectionManagerService
    {
        public enum CoatingExportMode { Galvanizing, MassBreakdown }
        private const int MaxScanErrorSamples = 200;

    }
}
