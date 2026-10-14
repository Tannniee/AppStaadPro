using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class SectionManagerService
    {
        private static List<SectionStat> ScanModel(object os, int[] selectedBeamIds, out int errorCount, out List<string> errorSamples)
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
            if (!OpenStaadAdapterHelpers.TryGetSubObject(os, "Geometry", out object geometry, out string geometryError))
            {
                AddError(geometryError);
                errorCount = localErrorCount;
                errorSamples = localErrorSamples;
                return result.Values.ToList();
            }

            if (!OpenStaadAdapterHelpers.TryGetSubObject(os, "Property", out object prop, out string propertyError))
            {
                AddError(propertyError);
                errorCount = localErrorCount;
                errorSamples = localErrorSamples;
                return result.Values.ToList();
            }

            var beamIds = selectedBeamIds ?? GetAllBeamIds(geometry, AddError);
            var sectionCache = new Dictionary<int, (string Name, string Type, double Area, double Perimeter, double Thickness, List<SectionPlateComponentSpec> PlateComponents, SectionCoatingDimensions CoatingDimensions)>();
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
                if (OpenStaadGeometryAdapter.TryGetBeamLengthFromGeometry(geometry, beamId, out length, out string lengthError))
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
                    info = ResolveSection(prop, propId, secName, AddError);
                    sectionCache[propId] = info;
                }

                if (!densityCache.TryGetValue(matName, out double density))
                {
                    density = GetDensityKgM3(prop, matName, AddError);
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
                        Density = density,
                        CoatingDimensions = info.CoatingDimensions
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
                AddPlateMass(stat, info.PlateComponents, length, stat.Area, stat.Density);

                double coatArea = stat.Perimeter * length;
                stat.ZincMass += coatArea * stat.CoatingWeight / 1000.0;
            }

            errorCount = localErrorCount;
            errorSamples = localErrorSamples;
            return result.Values.ToList();
        }

        private static (string Name, string Type, double Area, double Perimeter, double Thickness, List<SectionPlateComponentSpec> PlateComponents, SectionCoatingDimensions CoatingDimensions) ResolveSection(
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
            var plateComponents = new List<SectionPlateComponentSpec>();
            var coatingDimensions = new SectionCoatingDimensions();

            if (retType == 680)
            {
                double h1 = GetAt(p, 0), web = GetAt(p, 1), h2 = GetAt(p, 2), bf = GetAt(p, 3), tft = GetAt(p, 4), bfb = GetAt(p, 5), tfb = GetAt(p, 6);
                double a1 = bf * tft + bfb * tfb + Math.Max(0, h1 - tft - tfb) * web;
                double a2 = bf * tft + bfb * tfb + Math.Max(0, h2 - tft - tfb) * web;
                if (a1 > 0 && a2 > 0) area = (a1 + a2) / 2.0;
                perimeter = Math.Max(0, (2 * (bf + bfb) + 2 * h1 - 2 * web + 2 * (bf + bfb) + 2 * h2 - 2 * web) / 2.0);
                thickness = Math.Max(0, Math.Min(tft, tfb));
                if (thickness <= 0) thickness = Math.Max(0, web);
                AddISectionPlateComponents(plateComponents, (h1 + h2) / 2.0, web, bf, tft, bfb, tfb);
                SetISectionCoatingDimensions(coatingDimensions, (h1 + h2) / 2.0, web, (bf + bfb) / 2.0, (tft + tfb) / 2.0);
            }
            else if (retType == 698)
            {
                double h1 = GetAt(p, 0), web = GetAt(p, 1), h2 = GetAt(p, 2, h1);
                double bf = GetAt(p, 3), tft = GetAt(p, 4), bfb = GetAt(p, 5, bf), tfb = GetAt(p, 6, tft);
                double averageDepth = (h1 + h2) / 2.0;
                double averageFlangeWidth = (bf + bfb) / 2.0;
                double averageFlangeThickness = (tft + tfb) / 2.0;
                perimeter = Math.Max(0, 2 * averageDepth + 4 * averageFlangeWidth - 2 * web);
                thickness = Math.Max(0, Math.Min(web, Math.Min(tft, tfb)));
                AddISectionPlateComponents(plateComponents, averageDepth, web, bf, tft, bfb, tfb);
                SetISectionCoatingDimensions(coatingDimensions, averageDepth, web, averageFlangeWidth, averageFlangeThickness);
            }
            else if (retType == 690)
            {
                double h = GetAt(p, 1, depth), web = GetAt(p, 2, tw);
                double bf = GetAt(p, 3, width), tft = GetAt(p, 4, tf);
                double bfb = GetAt(p, 10, bf), tfb = GetAt(p, 11, tft);
                double averageFlangeWidth = (bf + bfb) / 2.0;
                double averageFlangeThickness = (tft + tfb) / 2.0;
                perimeter = Math.Max(0, 2 * h + 4 * averageFlangeWidth - 2 * web);
                thickness = Math.Max(0, Math.Min(web, Math.Min(tft, tfb)));
                AddISectionPlateComponents(plateComponents, h, web, bf, tft, bfb, tfb);
                SetISectionCoatingDimensions(coatingDimensions, h, web, averageFlangeWidth, averageFlangeThickness);
            }
            else if (type == "Pipe")
            {
                double od = GetAt(p, 1, depth > 0 ? depth : width);
                if (od <= 0 && area > 0) od = 2.0 * Math.Sqrt(area / Math.PI);
                double pipeThickness = retType == 695
                    ? Math.Max(0, (od - GetAt(p, 2)) / 2.0)
                    : Math.Max(0, GetAt(p, 2, tw > 0 ? tw : tf));
                perimeter = Math.Max(0, Math.PI * od);
                thickness = pipeThickness;
                coatingDimensions.PipeOutsideDiameterMm = od * 1000.0;
                coatingDimensions.PipeThicknessMm = pipeThickness * 1000.0;
            }
            else if (type == "Box/Tube" || type == "Rectangular")
            {
                double h = GetAt(p, 1, depth);
                double b = GetAt(p, 2, width);
                double boxThickness = Math.Max(0, GetAt(p, 3, tw > 0 ? tw : tf));
                perimeter = Math.Max(0, 2 * (h + b));
                thickness = boxThickness;
                coatingDimensions.BoxAMm = h * 1000.0;
                coatingDimensions.BoxBMm = b * 1000.0;
                coatingDimensions.BoxThicknessAMm = boxThickness * 1000.0;
                coatingDimensions.BoxThicknessBMm = boxThickness * 1000.0;
            }
            else if (type == "I-Section" || type == "Channel")
            {
                double h = GetAt(p, 1, depth);
                double b = GetAt(p, 2, width);
                double tff = GetAt(p, 3, tf);
                double tww = GetAt(p, 4, tw);
                perimeter = Math.Max(0, 4 * b + 2 * h - 2 * tww);
                thickness = Math.Max(0, Math.Min(tff > 0 ? tff : tww, tww > 0 ? tww : tff));
                AddISectionPlateComponents(plateComponents, h, tww, b, tff, b, tff);
                if (type == "I-Section")
                    SetISectionCoatingDimensions(coatingDimensions, h, tww, b, tff);
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
            return (generatedName, type, Math.Max(0, area), Math.Max(0, perimeter), Math.Max(0, thickness * 1000.0), plateComponents, coatingDimensions);
        }

        private static void SetISectionCoatingDimensions(
            SectionCoatingDimensions dimensions,
            double webDepthM,
            double webThicknessM,
            double flangeWidthM,
            double flangeThicknessM)
        {
            dimensions.WebDepthMm = Math.Max(0, webDepthM * 1000.0);
            dimensions.FlangeWidthMm = Math.Max(0, flangeWidthM * 1000.0);
            dimensions.WebThicknessMm = Math.Max(0, webThicknessM * 1000.0);
            dimensions.FlangeThicknessMm = Math.Max(0, flangeThicknessM * 1000.0);
        }

        private static void AddISectionPlateComponents(
            List<SectionPlateComponentSpec> components,
            double depth,
            double webThickness,
            double topFlangeWidth,
            double topFlangeThickness,
            double bottomFlangeWidth,
            double bottomFlangeThickness)
        {
            if (components == null)
                return;

            double webHeight = Math.Max(0, depth - topFlangeThickness - bottomFlangeThickness);
            AddPlateComponent(components, webThickness, 1, 1.0, webHeight * webThickness);
            AddPlateComponent(components, topFlangeThickness, 1, 1.0, topFlangeWidth * topFlangeThickness);
            AddPlateComponent(components, bottomFlangeThickness, 1, 1.0, bottomFlangeWidth * bottomFlangeThickness);
        }

        private static void AddPlateComponent(
            List<SectionPlateComponentSpec> components,
            double thicknessM,
            int count,
            double lengthMultiplier,
            double areaPerLength)
        {
            if (thicknessM <= 0 || count <= 0 || lengthMultiplier <= 0 || areaPerLength <= 0)
                return;

            double thicknessMm = thicknessM * 1000.0;
            var existing = components.FirstOrDefault(x => Math.Abs(x.Thickness - thicknessMm) <= 0.001);
            if (existing == null)
            {
                components.Add(new SectionPlateComponentSpec
                {
                    Thickness = thicknessMm,
                    Count = count,
                    LengthMultiplier = lengthMultiplier,
                    AreaPerLength = areaPerLength
                });
                return;
            }

            existing.Count += count;
            existing.LengthMultiplier += lengthMultiplier;
            existing.AreaPerLength += areaPerLength;
        }

        private static void AddPlateMass(
            SectionStat stat,
            List<SectionPlateComponentSpec> components,
            double memberLength,
            double sectionArea,
            double density)
        {
            if (stat == null || components == null || components.Count == 0 || memberLength <= 0 || density <= 0)
                return;

            double targetMass = memberLength * Math.Max(0, sectionArea) * density;
            double rawMass = components.Sum(x => memberLength * x.AreaPerLength * density);
            double scale = rawMass > 1e-9 && targetMass > 0 ? targetMass / rawMass : 1.0;

            foreach (var component in components)
            {
                double mass = memberLength * component.AreaPerLength * density * scale;
                double totalLength = memberLength * component.LengthMultiplier;
                if (mass <= 0 || totalLength <= 0)
                    continue;

                var bucket = stat.PlateMasses.FirstOrDefault(x => Math.Abs(x.Thickness - component.Thickness) <= 0.001);
                if (bucket == null)
                {
                    bucket = new SectionPlateMass { Thickness = component.Thickness };
                    stat.PlateMasses.Add(bucket);
                }

                bucket.Count += component.Count;
                bucket.TotalLength += totalLength;
                bucket.Mass += mass;
            }
        }

    }
}
