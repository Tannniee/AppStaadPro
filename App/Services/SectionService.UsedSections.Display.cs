using System;
using System.Diagnostics;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        private static string BuildUsedSectionDisplayName(object propObj, int propertyId, int typeCode, string rawName)
        {
            if (propObj == null || propertyId <= 0)
                return rawName ?? "";

            ReadSectionShapeData(propObj, propertyId, out int actualTypeCode, out double[] props, out double width, out double depth, out double tf, out double tw);
            if (actualTypeCode > 0)
                typeCode = actualTypeCode;

            double h1mm;
            double h2mm;
            double twmm;
            double bfmm;
            double tfmm;
            double bfbmm;
            double tfbmm;

            switch (typeCode)
            {
                case 680:
                    h1mm = GetAt(props, 0) * 1000.0;
                    h2mm = GetAt(props, 2) * 1000.0;
                    twmm = GetAt(props, 1) * 1000.0;
                    bfmm = GetAt(props, 3) * 1000.0;
                    tfmm = GetAt(props, 4) * 1000.0;
                    bfbmm = GetAt(props, 5, GetAt(props, 3)) * 1000.0;
                    tfbmm = GetAt(props, 6, GetAt(props, 4)) * 1000.0;
                    return FirstNonEmpty(
                        SectionNamingService.GenerateSectionName(eFramePropType.I, h1mm, h2mm, twmm, bfmm, tfmm, bfbmm, tfbmm, 0),
                        rawName);

                case 610:
                case 611:
                case 612:
                case 616:
                case 670:
                    h1mm = GetAt(props, 1, depth) * 1000.0;
                    bfmm = GetAt(props, 2, width) * 1000.0;
                    twmm = GetAt(props, 4, tw) * 1000.0;
                    tfmm = GetAt(props, 3, tf) * 1000.0;
                    return FirstNonEmpty(
                        SectionNamingService.GenerateSectionName(eFramePropType.I, h1mm, h1mm, twmm, bfmm, tfmm, bfmm, tfmm, 0),
                        rawName);

                case 698: // UPT ISECTION: Dww, Tww, Dww1, Bff, Tff, Bff1, Tff1, ...
                    return BuildUptISectionDisplayName(props, rawName);

                case 620:
                case 673:
                case 694:
                case 61100:
                    h1mm = GetAt(props, 1, depth) * 1000.0;
                    bfmm = GetAt(props, 2, width) * 1000.0;
                    twmm = GetAt(props, 4, tw) * 1000.0;
                    tfmm = GetAt(props, 3, tf) * 1000.0;
                    return FirstNonEmpty(
                        "T-" + SectionNamingService.FmtNum(h1mm) + "x" + SectionNamingService.FmtNum(bfmm) + "x" + SectionNamingService.FmtNum(twmm) + "x" + SectionNamingService.FmtNum(tfmm),
                        rawName);

                case 630:
                case 631:
                case 691:
                    h1mm = GetAt(props, 1, depth) * 1000.0;
                    bfmm = GetAt(props, 2, width) * 1000.0;
                    twmm = GetAt(props, 4, tw) * 1000.0;
                    tfmm = GetAt(props, 3, tf) * 1000.0;
                    return FirstNonEmpty(
                        SectionNamingService.GenerateSectionName(eFramePropType.Channel, h1mm, 0, twmm, bfmm, tfmm, 0, 0, 0),
                        rawName);

                case 640:
                case 692:
                    h1mm = GetAt(props, 1, depth) * 1000.0;
                    bfmm = GetAt(props, 2, width) * 1000.0;
                    tfmm = GetAt(props, 3, tf > 0 ? tf : tw) * 1000.0;
                    return FirstNonEmpty(
                        SectionNamingService.GenerateSectionName(eFramePropType.Angle, h1mm, 0, 0, bfmm, tfmm, 0, 0, 0),
                        rawName);

                case 693:
                    h1mm = GetAt(props, 1, depth) * 1000.0;
                    bfmm = GetAt(props, 2, width) * 1000.0;
                    tfmm = GetAt(props, 3, tf > 0 ? tf : tw) * 1000.0;
                    return FirstNonEmpty(
                        SectionNamingService.GenerateSectionName(eFramePropType.DblAngle, h1mm, 0, 0, bfmm, tfmm, 0, 0, 0),
                        rawName);

                case 650:
                case 654:
                    h1mm = GetAt(props, 1, depth) * 1000.0;
                    h2mm = GetAt(props, 2, width) * 1000.0;
                    tfmm = GetAt(props, 3, tf > 0 ? tf : tw) * 1000.0;
                    twmm = GetAt(props, 4, tw > 0 ? tw : tf) * 1000.0;
                    return FirstNonEmpty(
                        SectionNamingService.GenerateSectionName(eFramePropType.Rectangular, h1mm, h2mm, twmm, 0, tfmm, 0, 0, 0),
                        rawName);

                case 675:
                    h1mm = GetAt(props, 4) * 1000.0;
                    h2mm = GetAt(props, 5) * 1000.0;
                    twmm = GetAt(props, 6) * 1000.0;
                    int taperedTubeSectionType = (int)Math.Round(GetAt(props, 7));
                    string taperedPrefix = taperedTubeSectionType == 1 ? "Pipe" :
                        taperedTubeSectionType == 6 ? "Tube" : "TaperedTube";
                    string taperedDepth = Math.Abs(h1mm - h2mm) > 0.1
                        ? "(" + SectionNamingService.FmtNum(h1mm) + "~" + SectionNamingService.FmtNum(h2mm) + ")"
                        : SectionNamingService.FmtNum(h1mm);
                    return taperedPrefix + "-" + taperedDepth + "x" + SectionNamingService.FmtNum(twmm);

                case 696:
                    h1mm = GetAt(props, 1, depth) * 1000.0;
                    h2mm = GetAt(props, 2, width) * 1000.0;
                    tfmm = GetAt(props, 3, tf > 0 ? tf : tw) * 1000.0;
                    return FirstNonEmpty(
                        SectionNamingService.GenerateSectionName(eFramePropType.Rectangular, h1mm, h2mm, tfmm, 0, tfmm, 0, 0, 0),
                        rawName);

                case 672:
                    h1mm = GetAt(props, 1, depth) * 1000.0;
                    h2mm = GetAt(props, 2, width) * 1000.0;
                    if (h1mm > 0 && h2mm > 0)
                        return "Rect-" + SectionNamingService.FmtNum(h1mm) + "x" + SectionNamingService.FmtNum(h2mm);
                    return rawName ?? "";

                case 655:
                case 660:
                case 661:
                    h1mm = GetAt(props, 1, depth > 0 ? depth : width) * 1000.0;
                    double tmm = GetAt(props, 2, tw > 0 ? tw : tf) * 1000.0;
                    return FirstNonEmpty(
                        SectionNamingService.GenerateSectionName(eFramePropType.Pipe, h1mm, 0, tmm, 0, 0, 0, 0, 0),
                        rawName);

                case 695:
                    h1mm = GetAt(props, 1, depth > 0 ? depth : width) * 1000.0;
                    double idmm = GetAt(props, 2, tw > 0 ? tw : tf) * 1000.0;
                    double uptPipeThickness = (h1mm - idmm) / 2.0;
                    return FirstNonEmpty(
                        SectionNamingService.GenerateSectionName(eFramePropType.Pipe, h1mm, 0, uptPipeThickness, 0, 0, 0, 0, 0),
                        rawName);

                case 671:
                    h1mm = GetAt(props, 1, depth) * 1000.0;
                    if (h1mm > 0)
                        return "Circle-" + SectionNamingService.FmtNum(h1mm);
                    return rawName ?? "";

                default:
                    return rawName ?? "";
            }
        }

        private static void ReadSectionShapeData(object propObj, int propertyId, out int typeCode, out double[] props, out double width, out double depth, out double tf, out double tw)
        {
            typeCode = 0;
            props = new double[25];
            width = 0;
            depth = 0;
            tf = 0;
            tw = 0;

            if (propObj == null || propertyId <= 0)
                return;

            if (!OpenStaadPropertyAdapter.TryGetSectionPropertyValuesExFromProperty(propObj, propertyId, out typeCode, out props, out string exError))
                Debug.WriteLine($"GetSectionPropertyValuesEx failed for property {propertyId}: {exError}");

            if (OpenStaadPropertyAdapter.TryGetSectionPropertyValuesFromProperty(propObj, propertyId, out double[] values, out string valuesError))
            {
                width = GetAt(values, 0);
                depth = GetAt(values, 1);
                tf = GetAt(values, 8);
                tw = GetAt(values, 9);
            }
            else
            {
                Debug.WriteLine($"GetSectionPropertyValues failed for property {propertyId}: {valuesError}");
            }
        }

        private static string FirstNonEmpty(string primary, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(primary))
                return primary;

            return fallback ?? "";
        }

        private static string BuildUptISectionDisplayName(double[] props, string rawName)
        {
            double h1mm = GetAt(props, 0) * 1000.0;
            double twmm = GetAt(props, 1) * 1000.0;
            double h2mm = GetAt(props, 2, GetAt(props, 0)) * 1000.0;
            double bfmm = GetAt(props, 3) * 1000.0;
            double tfmm = GetAt(props, 4) * 1000.0;
            double bfbmm = GetAt(props, 5, GetAt(props, 3)) * 1000.0;
            double tfbmm = GetAt(props, 6, GetAt(props, 4)) * 1000.0;

            return FirstNonEmpty(
                SectionNamingService.GenerateSectionName(
                    eFramePropType.I,
                    h1mm,
                    h2mm,
                    twmm,
                    bfmm,
                    tfmm,
                    bfbmm,
                    tfbmm,
                    0),
                rawName);
        }

        private static double ToDouble(object value)
        {
            try
            {
                return Convert.ToDouble(value);
            }
            catch
            {
                return 0.0;
            }
        }

        private static double GetAt(double[] values, int index, double fallback = 0)
        {
            if (values != null && index >= 0 && index < values.Length)
            {
                double value = values[index];
                if (Math.Abs(value) > 0.0000001)
                    return value;
            }

            return fallback;
        }

        private static void ResolveSortMetadata(int typeCode, out int sortGroup, out int sortType, out string sourceLabel)
        {
            sortGroup = 3;
            sortType = 9;
            sourceLabel = "Other";

            if (typeCode == 680)
            {
                sortGroup = 0;
                sortType = 0;
                sourceLabel = "Tapered I";
                return;
            }

            if (typeCode >= 670 && typeCode <= 679)
            {
                sortGroup = 0;
                sortType = 1;
                sourceLabel = "Prismatic";
                return;
            }

            if (typeCode >= 610 && typeCode <= 669)
            {
                sortGroup = 1;
                sortType = 0;
                sourceLabel = "Legacy";
                return;
            }

            if (typeCode >= 690 && typeCode <= 699)
            {
                sortGroup = 2;
                sortType = 0;
                sourceLabel = "UPT";
            }
        }
    }
}
