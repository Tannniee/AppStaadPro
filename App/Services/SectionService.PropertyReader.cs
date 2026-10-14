using System;
using System.Collections.Generic;
using System.Diagnostics;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        private static SectionInfo BuildSectionInfoFromProperty(object os, int propID, string propName, string matName)
        {
             if (os == null) throw new StaadConnectionException("Not connected to STAAD Application.");
             if (propID <= 0) throw new SectionPropertyException("Invalid property id.");

             // Get Dimensions
             int lRetType = 0;
             double[] arrProps = new double[25];
             // GetSectionPropertyValuesEx uses ByRef outputs, including the values array.
             // Start with a pre-allocated array and fall back to reflection if COM dispatch fails.
             // Note: GetSectionPropertyValuesEx params: (propID, ref lRetType, arrProps)
             // lRetType is ref. arrProps is array.

             object openStaad = os;
             if (!TryGetSectionProperty(openStaad, out object propObj, out string propertyError))
                 throw new SectionPropertyException(propertyError);

             bool propsLoaded = false;
             if (OpenStaadPropertyAdapter.TryGetSectionPropertyValuesExFromProperty(propObj, propID, out lRetType, out arrProps, out string valuesExError))
             {
                 propsLoaded = true;
             }
             else
             {
                 Debug.WriteLine("GetSectionPropertyValuesEx failed: " + valuesExError);
             }

             if (!propsLoaded)
             {
                 if (OpenStaadPropertyAdapter.TryGetSectionPropertyValuesFromProperty(propObj, propID, out double[] legacyValues, out string legacyError))
                 {
                     double[] legacyProps = new double[25];
                     legacyProps[0] = GetArrayValue(legacyValues, 2);
                     legacyProps[1] = GetArrayValue(legacyValues, 1);
                     legacyProps[2] = GetArrayValue(legacyValues, 0);
                     arrProps = legacyProps;
                 }
                 else
                 {
                     Debug.WriteLine("GetSectionPropertyValues failed: " + legacyError);
                 }
             }

             arrProps = ToDoubleArray(arrProps, 25);

             // Map to SectionInfo
             SectionInfo info = new SectionInfo();
             info.PropertyRefNo = propID;
             info.Name = propName;
             info.Material = matName;

             // Determine Source Code based on lRetType
             if (lRetType == 680) info.Code = "Tapered";
             else if (lRetType >= 690 && lRetType <= 699) info.Code = "UPT (User)";
             else if (lRetType >= 670 && lRetType <= 679) info.Code = "Prismatic";
             else if (lRetType >= 610 && lRetType <= 669) info.Code = "Legacy"; // Was Standard
             else info.Code = "Unknown";
             info.CreateAsUPT = (lRetType >= 690 && lRetType <= 699);

             // Map Dimensions based on lRetType
             switch (lRetType)
             {
                 // --- TAPERED ---
                 case 680: // scTapered
                     info.PropType = eFramePropType.I;
                     info.H1 = arrProps[0];
                     info.H2 = arrProps[2];
                     info.Tw = arrProps[1];
                     info.Bf = arrProps[3];
                     info.Tf = arrProps[4];
                     info.Bfb = arrProps[5];
                     info.Tfb = arrProps[6];
                     break;

                 // --- LEGACY PIPE (from standard table) ---
                 case 660: // scTablePipeSt
                 case 655: // scTableHSSRound
                 case 661: // scTablePipeDefine
                     info.PropType = eFramePropType.Legacy;
                     info.H1 = arrProps[1]; // OD
                     info.Tw = arrProps[2]; // Thk
                     break;

                // --- UPT PIPE ---
                case 695: // scUptPipe
                     info.PropType = eFramePropType.Pipe;
                     info.UptAx = arrProps[0];
                     info.H1 = arrProps[1]; // OD
                     info.Bf = arrProps[2]; // ID
                     info.UptAy = arrProps[3];
                     info.UptAz = arrProps[4];
                     info.UptIz = arrProps[5];
                     info.UptIy = arrProps[6];
                     info.UptIx = arrProps[7];
                     info.Tw = (arrProps[1] - arrProps[2]) / 2.0; // Tw = (OD - ID) / 2
                     break;

                 // --- PRISMATIC CIRCLE ---
                 case 671: // scPrismaticCircle
                     info.PropType = eFramePropType.Circle;
                     // Ax Iz Iy Ix YD
                     info.H1 = arrProps[4];
                     break;

                 // --- LEGACY I / W SHAPE (from standard table) ---
                 case 610: // scTableBeamSt
                 case 616: // scTableBeamD
                 case 611: // scTableBeamT
                 case 612: // scTableBeamCm
                     info.PropType = eFramePropType.Legacy;
                     info.H1 = arrProps[1];
                     info.H2 = arrProps[1];
                     info.Bf = arrProps[2];
                     info.Bfb = arrProps[2];
                     info.Tf = arrProps[3];
                     info.Tfb = arrProps[3];
                     info.Tw = arrProps[4];
                     break;

                 // --- UPT WIDE FLANGE ---
                case 690: // scUptWideFlange
                    info.PropType = eFramePropType.I;
                    info.UptAx = arrProps[0];
                    info.H1 = arrProps[1]; // D
                    info.H2 = arrProps[1]; // D
                    info.Tw = arrProps[2]; // Tw
                    info.Bf = arrProps[3]; // Wf
                    info.Tf = arrProps[4]; // Tf
                    info.UptIz = arrProps[5];
                    info.UptIy = arrProps[6];
                    info.UptIx = arrProps[7];
                    info.UptAy = arrProps[8];
                    info.UptAz = arrProps[9];
                    info.Bfb = arrProps[10] > 0.0 ? arrProps[10] : arrProps[3];
                    info.Tfb = arrProps[11] > 0.0 ? arrProps[11] : arrProps[4];
                    break;

                 // --- UPT ISECTION ---
                case 698: // scUptIsection
                    info.PropType = eFramePropType.ISection;
                    // Dww Tww Dww1 Bff Tff Bff1 Tff1 Ayf Azf Xif
                    info.H1 = arrProps[0]; // Dww
                    info.Tw = arrProps[1]; // Tww
                    info.H2 = arrProps[2]; // Dww1
                    info.Bf = arrProps[3]; // Bff
                    info.Tf = arrProps[4]; // Tff
                    info.Bfb = arrProps[5]; // Bff1
                    info.Tfb = arrProps[6]; // Tff1
                    info.UptAy = arrProps[7];
                    info.UptAz = arrProps[8];
                    info.UptXif = arrProps[9];
                    break;

                 // --- LEGACY TEE (from standard table) ---
                 case 620: // scTableTeeSt
                 case 61100: // scTableBeamTee
                     info.PropType = eFramePropType.Legacy;
                     info.H1 = arrProps[1];
                     info.Bf = arrProps[2];
                     info.Tf = arrProps[3];
                     info.Tw = arrProps[4];
                     break;

                 // --- UPT TEE ---
                case 694: // scUptTee
                    info.PropType = eFramePropType.T;
                    info.UptAx = arrProps[0];
                    info.H1 = arrProps[1]; // D
                    info.Bf = arrProps[2]; // Wf
                    info.Tf = arrProps[3]; // Tf
                    info.Tw = arrProps[4]; // Tw
                    info.UptIz = arrProps[5];
                    info.UptIy = arrProps[6];
                    info.UptIx = arrProps[7];
                    info.UptCy = arrProps[8];
                    info.UptAy = arrProps[9];
                    info.UptAz = arrProps[10];
                    break;

                 // --- PRISMATIC TEE ---
                 case 673: // scPrismaticTee
                     info.PropType = eFramePropType.T;
                     // Ax Iz Iy Ix YD ZD YB ZB
                     info.H1 = arrProps[4]; // Total depth
                     info.Bf = arrProps[5]; // Flange width
                     info.Tf = Math.Max(0.0, arrProps[4] - arrProps[6]); // Flange thickness
                     info.Tw = arrProps[7]; // Stem width
                     break;

                 // --- LEGACY CHANNEL ---
                 case 630: // scTableChannelSt
                 case 631: // scTableChannelD (Double Channel)
                     info.PropType = eFramePropType.Legacy;
                     info.H1 = arrProps[1];
                     info.Bf = arrProps[2];
                     info.Tf = arrProps[3];
                     info.Tw = arrProps[4];
                     break;

                 // --- UPT CHANNEL ---
                case 691: // scUptChannel
                    info.PropType = eFramePropType.Channel;
                    info.UptAx = arrProps[0];
                    info.H1 = arrProps[1]; // D
                    info.Tw = arrProps[2]; // Tw
                    info.Bf = arrProps[3]; // Wf
                    info.Tf = arrProps[4]; // Tf
                    info.UptIz = arrProps[5];
                    info.UptIy = arrProps[6];
                    info.UptIx = arrProps[7];
                    info.UptCz = arrProps[8];
                    info.UptAy = arrProps[9];
                    info.UptAz = arrProps[10];
                    break;

                 // --- LEGACY ANGLE ---
                 case 640: // scTableAngleSt
                     info.PropType = eFramePropType.Legacy;
                     info.H1 = arrProps[1];
                     info.Bf = arrProps[2];
                     info.Tf = arrProps[3];
                     info.Tw = arrProps[3];
                     break;

                 // --- UPT ANGLE ---
                case 692: // scUptAngle
                    info.PropType = eFramePropType.Angle;
                    info.H1 = arrProps[1]; // D
                    info.Bf = arrProps[2]; // Wf
                    info.Tf = arrProps[3]; // Tf
                    info.Tw = arrProps[3]; // Tw
                    info.UptR = arrProps[4];
                    info.UptAy = arrProps[5];
                    info.UptAz = arrProps[6];
                    break;

                 // --- LEGACY TUBE (from standard table) ---
                 case 650: // scTableTubeSt
                 case 654: // scTableHSSRectangle
                     info.PropType = eFramePropType.Legacy;
                     info.H1 = arrProps[1]; // TH (Depth)
                     info.H2 = arrProps[2]; // WT (Width)
                     info.Tf = arrProps[3]; // DT (Thickness)
                     info.Tw = arrProps[3];
                     break;

                 // --- UPT TUBE ---
                case 696: // scUptTube
                     info.PropType = eFramePropType.Rectangular;
                     info.UptAx = arrProps[0];
                     info.H1 = arrProps[1]; // D
                     info.Bf = arrProps[2]; // Wf
                     info.H2 = arrProps[2]; // keep width mirror for backward compatibility
                     info.Tf = arrProps[3]; // Tf
                     info.Tw = arrProps[3]; // Assume Tw = Tf or use other prop
                     info.UptIz = arrProps[4];
                     info.UptIy = arrProps[5];
                     info.UptIx = arrProps[6];
                     info.UptAy = arrProps[7];
                     info.UptAz = arrProps[8];
                     break;

                 // --- UPT GENERAL ---
                case 697: // scUptGeneral
                    // Ax D Td B Tb Iz Iy Ix Sz Sy Ay Az Pz Py Hss Dee
                    info.PropType = eFramePropType.General;
                    info.UptAx = arrProps[0];
                    info.H1 = arrProps[1]; // D
                    info.Tw = arrProps[2]; // Td
                    info.H2 = arrProps[3]; // B
                    info.Tf = arrProps[4]; // Tb
                    info.UptIz = arrProps[5];
                    info.UptIy = arrProps[6];
                    info.UptIx = arrProps[7];
                    info.UptSz = arrProps[8];
                    info.UptSy = arrProps[9];
                    info.UptAy = arrProps[10];
                    info.UptAz = arrProps[11];
                    info.UptPz = arrProps[12];
                    info.UptPy = arrProps[13];
                    info.UptHss = arrProps[14];
                    info.UptDee = arrProps[15];
                    break;

                // --- UPT PRISMATIC ---
                case 699: // scUptPrismatic
                    // Ax Iz Iy Ix Ay Az YD ZD
                    info.PropType = eFramePropType.Prismatic;
                    info.UptAx = arrProps[0];
                    info.UptIz = arrProps[1];
                    info.UptIy = arrProps[2];
                    info.UptIx = arrProps[3];
                    info.UptAy = arrProps[4];
                    info.UptAz = arrProps[5];
                    info.H1 = arrProps[6]; // YD
                    info.H2 = arrProps[7]; // ZD
                    break;

                 // --- PRISMATIC RECTANGLE / TAPERED TUBE ---
                 case 672: // scPrismaticRectangle
                     info.PropType = eFramePropType.Rectangular;
                     // Ax Iz Iy Ix YD ZD
                     info.H1 = arrProps[4];
                     info.H2 = arrProps[5];
                     break;

                 case 674: // scPrismaticTrapezoidal
                     info.PropType = eFramePropType.Trapezoidal;
                     // Ax Iz Iy Ix YD ZD ZB
                     info.H1 = arrProps[4];
                     info.H2 = arrProps[5]; // Top width
                     info.Bf = arrProps[6]; // Bottom width
                     break;

                 case 675: // scTaperedTube
                     info.PropType = eFramePropType.Pipe;
                     // Ax Iz Iy Ix D1 D2 TH SECTION_TYPE
                     info.H1 = arrProps[4]; // d1
                     info.H2 = arrProps[5]; // d2
                     info.Tw = arrProps[6]; // th
                     int taperedTubeSectionType = (int)Math.Round(arrProps[7]);
                     info.TaperedTubeShape = GetTaperedTubeShapeFromReadType(taperedTubeSectionType);
                     break;

                 case 676: // scPrismaticGeneral
                     info.PropType = eFramePropType.General;
                     // Ax Ay Az Ix Iy Iz YD ZD YB ZB
                     info.UptAx = arrProps[0];
                     info.UptAy = arrProps[1];
                     info.UptAz = arrProps[2];
                     info.UptIx = arrProps[3];
                     info.UptIy = arrProps[4];
                     info.UptIz = arrProps[5];
                     info.H1 = arrProps[6];
                     info.H2 = arrProps[7];
                     info.Tw = arrProps[8];
                     info.Bf = arrProps[9];
                     break;

                 // --- DOUBLE ANGLE ---
                case 693: // scUptDoubleAngle
                    info.PropType = eFramePropType.DblAngle;
                    info.H1 = arrProps[1];
                    info.Bf = arrProps[2];
                    info.Tf = arrProps[3];
                    info.Dis = arrProps[4];
                    info.UptIz = arrProps[5];
                    info.UptIy = arrProps[6];
                    info.UptIx = arrProps[7];
                    info.UptCy = arrProps[8];
                    info.UptAy = arrProps[9];
                    info.UptAz = arrProps[10];
                    break;

                 default:
                     // GENERIC FALLBACK
                     // If this is a Legacy/Standard table section, mark it as Legacy
                     if (info.Code == "Legacy")
                     {
                         info.PropType = eFramePropType.Legacy;
                     }
                     // Otherwise try to infer PropType from Name
                     else if (propName.StartsWith("I") || propName.StartsWith("W") || propName.StartsWith("H"))
                         info.PropType = eFramePropType.I;
                     else if (propName.StartsWith("C")) info.PropType = eFramePropType.Channel;
                     else if (propName.StartsWith("L")) info.PropType = eFramePropType.Angle;
                     else if (propName.StartsWith("T")) info.PropType = eFramePropType.T;
                     else if (propName.Contains("PIP")) info.PropType = eFramePropType.Pipe;
                     else if (propName.Contains("TUB") || propName.Contains("HSS") || propName.Contains("SHS") || propName.Contains("RHS")) info.PropType = eFramePropType.Rectangular;

                     info.H1 = arrProps[1];
                     info.H2 = arrProps[1];
                     info.Bf = arrProps[2];
                     info.Bfb = arrProps[2];
                     info.Tf = arrProps[3];
                     info.Tfb = arrProps[3];
                     info.Tw = arrProps[4];
                     break;
             }

             // Convert Meters to Millimeters
             info.H1 *= 1000;
             info.H2 *= 1000;
             info.Tw *= 1000;
             info.Bf *= 1000;
             info.Tf *= 1000;
             info.Bfb *= 1000;
             info.Tfb *= 1000;
             info.Dis *= 1000;

             if (lRetType == 680)
             {
                 string generated = SectionNamingService.GenerateSectionName(
                     eFramePropType.I,
                     info.H1,
                     info.H2,
                     info.Tw,
                     info.Bf,
                     info.Tf,
                     info.Bfb,
                     info.Tfb,
                     info.Dis);
                 if (!string.IsNullOrWhiteSpace(generated))
                     info.Name = generated;
             }

             return info;
        }

        internal static TaperedTubeShape GetTaperedTubeShapeFromReadType(int sectionType)
        {
            // GetSectionPropertyValuesEx is one-based: 1 = Round, 6 = Square.
            if (sectionType == 1)
                return TaperedTubeShape.Round;

            if (sectionType == 6)
                return TaperedTubeShape.Square;

            throw new SectionPropertyException(
                $"Tapered tube subtype {sectionType} is not supported. " +
                "Only Round and Square are available in this app.");
        }

    }
}
