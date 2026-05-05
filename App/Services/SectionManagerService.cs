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
