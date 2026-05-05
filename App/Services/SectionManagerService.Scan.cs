using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using StaadSteelApp.Core.Interop;
using Excel = Microsoft.Office.Interop.Excel;

namespace StaadSteelApp.Core
{
    public static partial class SectionManagerService
    {
        private static List<SectionStat> ScanModel(object os, out int errorCount, out List<string> errorSamples)
        {
            int localErrorCount = 0;
            var localErrorSamples = new List<string>();
            void AddError(string msg)
            {
                localErrorCount++;
                if (localErrorSamples.Count < MaxScanErrorSamples)
                    localErrorSamples.Add(msg);
            }

            var result = new Dictionary<string, SectionStat>(StringComparer.OrdinalIgnoreCase);
            dynamic osDyn = os;
            dynamic geometry = osDyn.Geometry;
            dynamic prop = osDyn.Property;

            var beamIds = GetAllBeamIds((object)geometry, AddError);
            var sectionCache = new Dictionary<int, (string Name, string Type, double Area, double Perimeter, double Thickness)>();
            var densityCache = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (int beamId in beamIds)
            {
                int propId = 0;
                if (OpenStaadPropertyAdapter.TryGetBeamSectionPropertyRefNo(os, beamId, out propId, out string propIdError))
                {
                    if (propId <= 0) continue; // No property assigned
                }
                else
                {
                    AddError($"Beam {beamId}: GetBeamSectionPropertyRefNo failed: {propIdError}");
                    continue;
                }

                string secName;
                if (!OpenStaadPropertyAdapter.TryGetBeamSectionName(os, beamId, out secName, out string sectionNameError))
                {
                    secName = "PROP-" + propId;
                    AddError($"Beam {beamId} / PROP {propId}: GetBeamSectionName failed: {sectionNameError}");
                }

                string matName;
                if (!OpenStaadPropertyAdapter.TryGetBeamMaterialName(os, beamId, out matName, out string materialNameError))
                {
                    matName = "Unknown";
                    AddError($"Beam {beamId} / PROP {propId}: GetBeamMaterialName failed: {materialNameError}");
                }

                double length = 0;
                if (OpenStaadGeometryAdapter.TryGetBeamLengthFromGeometry((object)geometry, beamId, out length, out string lengthError))
                {
                    if (length < 0) length = 0;
                }
                else
                {
                    AddError($"Beam {beamId}: GetBeamLength failed: {lengthError}");
                }

                if (length < 0) length = 0;

                if (!sectionCache.TryGetValue(propId, out var info))
                {
                    info = ResolveSection((object)prop, propId, secName, AddError);
                    sectionCache[propId] = info;
                }

                if (!densityCache.TryGetValue(matName, out double density))
                {
                    density = GetDensityKgM3((object)prop, matName, AddError);
                    densityCache[matName] = density;
                }

                string sectionDisplayName = string.IsNullOrWhiteSpace(info.Name) ? secName : info.Name;
                string key = sectionDisplayName + "|" + matName + "|P" + propId.ToString(CultureInfo.InvariantCulture);
                if (!result.TryGetValue(key, out SectionStat stat))
                {
                    stat = new SectionStat
                    {
                        Name = sectionDisplayName,
                        Type = info.Type,
                        Material = matName,
                        Area = info.Area,
                        Perimeter = info.Perimeter,
                        Thickness = info.Thickness,
                        Density = density
                    };

                    var cat = GetCategoryFromType(stat.Type);
                    stat.ASTMA123Category = CategoryToString(cat);
                    stat.CoatingGrade = GetASTMA123Grade(cat, stat.Thickness);
                    stat.CoatingWeight = GetCoatingWeight(stat.CoatingGrade);
                    result[key] = stat;
                }

                stat.Count++;
                stat.TotalLength += length;
                stat.TotalWeight += length * stat.Area * stat.Density;

                double coatArea = stat.Perimeter * length;
                stat.ZincMass += coatArea * stat.CoatingWeight / 1000.0;
            }

            errorCount = localErrorCount;
            errorSamples = localErrorSamples;
            return result.Values.ToList();
        }

        private static (string Name, string Type, double Area, double Perimeter, double Thickness) ResolveSection(
            object prop,
            int propId,
            string secName,
            Action<string> addError)
        {
            int retType = 0;
            double[] p = new double[25];
            bool gotEx = false;
            bool gotSimple = false;
            if (OpenStaadPropertyAdapter.TryGetSectionPropertyValuesExFromProperty(prop, propId, out retType, out p, out string exError))
            {
                gotEx = true;
            }
            else
            {
                addError?.Invoke($"PROP {propId} '{secName}': GetSectionPropertyValuesEx failed: {exError}");
            }

            double width = 0, depth = 0, area = 0, tf = 0, tw = 0;
            if (OpenStaadPropertyAdapter.TryGetSectionPropertyValuesFromProperty(prop, propId, out double[] sectionValues, out string valuesError))
            {
                width = GetAt(sectionValues, 0);
                depth = GetAt(sectionValues, 1);
                area = GetAt(sectionValues, 2);
                tf = GetAt(sectionValues, 8);
                tw = GetAt(sectionValues, 9);
                gotSimple = true;
            }
            else
            {
                addError?.Invoke($"PROP {propId} '{secName}': GetSectionPropertyValues failed: {valuesError}");
            }

            if (gotEx)
            {
                bool any = false;
                for (int i = 0; i < p.Length; i++)
                {
                    if (p[i] > 0) { any = true; break; }
                }
                if (retType == 0 && !any)
                    addError?.Invoke($"PROP {propId} '{secName}': GetSectionPropertyValuesEx returned no usable data (type/props are zero).");
            }

            if (gotSimple && width <= 0 && depth <= 0 && area <= 0 && tf <= 0 && tw <= 0)
                addError?.Invoke($"PROP {propId} '{secName}': GetSectionPropertyValues returned all zeros.");

            if (!gotEx && !gotSimple)
            {
                addError?.Invoke($"PROP {propId} '{secName}': could not read section properties from STAAD. Computed weights may be zero/incorrect.");
            }

            string type = GetTypeName(retType, secName);
            if (retType == 680)
            {
                double h1 = GetAt(p, 0);
                double h2 = GetAt(p, 2);
                // Some STAAD models store prismatic I-sections under Variable/Tapered type.
                // If start/end depths are effectively equal, treat as non-tapered I-section.
                if (h1 > 0 && h2 > 0 && Math.Abs(h1 - h2) <= 0.001) // 0.001 m = 1 mm
                    type = "I-Section";
            }
            string generatedName = BuildSectionDisplayName(retType, p, secName, propId, width, depth, tf, tw);
            double perimeter = 0;
            double thickness = 0;

            if (retType == 680)
            {
                double h1 = GetAt(p, 0), web = GetAt(p, 1), h2 = GetAt(p, 2), bf = GetAt(p, 3), tft = GetAt(p, 4), bfb = GetAt(p, 5), tfb = GetAt(p, 6);
                double a1 = bf * tft + bfb * tfb + Math.Max(0, h1 - tft - tfb) * web;
                double a2 = bf * tft + bfb * tfb + Math.Max(0, h2 - tft - tfb) * web;
                if (a1 > 0 && a2 > 0) area = (a1 + a2) / 2.0;
                perimeter = Math.Max(0, (2 * (bf + bfb) + 2 * h1 - 2 * web + 2 * (bf + bfb) + 2 * h2 - 2 * web) / 2.0);
                thickness = Math.Max(0, Math.Min(tft, tfb));
                if (thickness <= 0) thickness = Math.Max(0, web);
            }
            else if (type == "Pipe")
            {
                double od = GetAt(p, 1, depth > 0 ? depth : width);
                if (od <= 0 && area > 0) od = 2.0 * Math.Sqrt(area / Math.PI);
                perimeter = Math.Max(0, Math.PI * od);
                thickness = Math.Max(0, GetAt(p, 2, tw > 0 ? tw : tf));
            }
            else if (type == "Box/Tube" || type == "Rectangular")
            {
                double h = GetAt(p, 1, depth);
                double b = GetAt(p, 2, width);
                perimeter = Math.Max(0, 2 * (h + b));
                thickness = Math.Max(0, GetAt(p, 3, tw > 0 ? tw : tf));
            }
            else if (type == "I-Section" || type == "Channel")
            {
                double h = GetAt(p, 1, depth);
                double b = GetAt(p, 2, width);
                double tff = GetAt(p, 3, tf);
                double tww = GetAt(p, 4, tw);
                perimeter = Math.Max(0, 4 * b + 2 * h - 2 * tww);
                thickness = Math.Max(0, Math.Min(tff > 0 ? tff : tww, tww > 0 ? tww : tff));
            }
            else if (type == "Angle")
            {
                double h = GetAt(p, 1, depth);
                double b = GetAt(p, 2, width);
                double t = GetAt(p, 3, tf > 0 ? tf : tw);
                perimeter = Math.Max(0, 2 * (h + b));
                thickness = Math.Max(0, t);
            }
            else if (type == "2L-Angle")
            {
                double h = GetAt(p, 1, depth);
                double b = GetAt(p, 2, width);
                double t = GetAt(p, 3, tf > 0 ? tf : tw);
                perimeter = Math.Max(0, 4 * (h + b));
                thickness = Math.Max(0, t);
            }
            else
            {
                perimeter = (width > 0 && depth > 0) ? 2 * (width + depth) : 0;
                thickness = Math.Max(0, Math.Min(tf > 0 ? tf : tw, tw > 0 ? tw : tf));
            }

            if (area <= 0 && width > 0 && depth > 0) area = width * depth;
            return (generatedName, type, Math.Max(0, area), Math.Max(0, perimeter), Math.Max(0, thickness * 1000.0));
        }

    }
}
