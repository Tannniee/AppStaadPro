using System;
using System.Collections.Generic;
using System.Diagnostics;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        private static SectionInfo BuildSectionInfoFromProperty(dynamic os, int propID, string propName, string matName)
        {
             if (os == null) throw new Exception("Not connected to STAAD Application.");
             if (propID <= 0) throw new Exception("Invalid property id.");

             // Get Dimensions
             int lRetType = 0;
             double[] arrProps = new double[25];
             // GetSectionPropertyValuesEx uses ByRef outputs, including the values array.
             // Start with a pre-allocated array and fall back to reflection if dynamic dispatch fails.
             // Note: GetSectionPropertyValuesEx params: (propID, ref lRetType, arrProps)
             // lRetType is ref. arrProps is array.

             object propObj = os.Property;
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
                     info.Tw = (arrProps[1] - arrProps[2]) / 2.0; // Tw = (OD - ID) / 2
                     break;

                 // --- PRISMATIC CIRCLE ---
                 case 671: // scPrismaticCircle
                     info.PropType = eFramePropType.Circle;
                     // arrProps[0] is Area (AX), calculate diameter: D = 2 * sqrt(A/PI)
                     double area = arrProps[0];
                     if (area > 0)
                         info.H1 = 2.0 * Math.Sqrt(area / Math.PI);
                     else
                         info.H1 = arrProps[1]; // Fallback
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
                    info.Bfb = arrProps[3];
                    info.Tfb = arrProps[4];
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
                    info.UptAy = arrProps[9];
                    info.UptAz = arrProps[10];
                    break;

                 // --- PRISMATIC TEE ---
                 case 673: // scPrismaticTee
                     info.PropType = eFramePropType.T;
                     // Prismatic Tee stores: arrProps[0]=AX, arrProps[1]=IX, arrProps[2]=IY, arrProps[3]=IZ
                     // Tee dimensions are complex to reverse-calculate from properties
                     // For now, use placeholder - user should input new dimensions
                     // TODO: Implement proper reverse calculation if needed
                     double teeArea = arrProps[0];
                     info.H1 = Math.Sqrt(teeArea) * 2; // Approximate depth
                     info.Bf = Math.Sqrt(teeArea) * 2; // Approximate width
                     info.Tw = Math.Sqrt(teeArea) * 0.1; // Approximate stem
                     info.Tf = Math.Sqrt(teeArea) * 0.1; // Approximate flange
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
                    info.UptAy = arrProps[4];
                    info.UptAz = arrProps[5];
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
                     // STAAD names Prismatic Rectangle as "Rect YDxZD" (e.g., "Rect 0.30x0.30")
                     // Parse dimensions from name instead of calculating from I values
                     double parsedYD = 0, parsedZD = 0;
                     if (!string.IsNullOrEmpty(propName))
                     {
                         // Try to parse "Rect 0.30x0.30" format
                         var match = System.Text.RegularExpressions.Regex.Match(
                             propName, @"(\d+\.?\d*)\s*[xX]\s*(\d+\.?\d*)");
                         if (match.Success)
                         {
                             double.TryParse(match.Groups[1].Value, out parsedYD);
                             double.TryParse(match.Groups[2].Value, out parsedZD);
                         }
                     }

                     if (parsedYD > 0 && parsedZD > 0)
                     {
                         info.H1 = parsedYD; // Already in meters from name
                         info.H2 = parsedZD;
                     }
                     else
                     {
                         // Fallback: use sqrt(Area) for both (assume square)
                         double rectArea = arrProps[0];
                         info.H1 = info.H2 = Math.Sqrt(rectArea > 0 ? rectArea : 0);
                     }
                     break;

                 case 675: // scTaperedTube
                     info.PropType = eFramePropType.Rectangular;
                     info.H1 = arrProps[1]; // d1
                     info.H2 = arrProps[2]; // d2
                     info.Tw = arrProps[3]; // th
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
                    info.UptAy = arrProps[8];
                    info.UptAz = arrProps[9];
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

        private static string TryGetMaterialNameFromProperty(dynamic os, int propertyId)
        {
            if (os == null || propertyId <= 0)
                return "";

            try
            {
                object propObj = os.Property;
                int count = TryGetAssignedBeamCount(propObj, propertyId);
                if (count <= 0)
                    return "";

                int firstBeamId = 0;
                foreach (int beamId in TryGetAssignedBeamList(propObj, propertyId, count))
                {
                    if (beamId > 0)
                    {
                        firstBeamId = beamId;
                        break;
                    }
                }
                if (firstBeamId <= 0)
                    return "";

                object openStaad = os;
                return OpenStaadPropertyAdapter.TryGetBeamMaterialName(openStaad, firstBeamId, out string materialName, out _)
                    ? materialName
                    : "";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TryGetMaterialNameFromProperty failed for prop {propertyId}: {ex.Message}");
                return "";
            }
        }

        private static int[] ToIntArray(object raw)
        {
            if (raw == null) return Array.Empty<int>();

            if (raw is int singleInt)
                return singleInt > 0 ? new[] { singleInt } : Array.Empty<int>();

            if (raw is int[] typed)
            {
                var cleaned = new List<int>(typed.Length);
                var seen = new HashSet<int>();
                foreach (int v in typed)
                {
                    if (v <= 0) continue;
                    if (seen.Add(v))
                        cleaned.Add(v);
                }
                return cleaned.ToArray();
            }

            if (raw is Array arr)
            {
                var cleaned = new List<int>(arr.Length);
                var seen = new HashSet<int>();
                foreach (object item in arr)
                {
                    int v;
                    try
                    {
                        v = Convert.ToInt32(item);
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    if (v <= 0) continue;
                    if (seen.Add(v))
                        cleaned.Add(v);
                }
                return cleaned.ToArray();
            }

            return Array.Empty<int>();
        }

        private static double[] ToDoubleArray(object raw, int minLength)
        {
            var values = new List<double>();

            if (raw is double[] doubles)
            {
                values.AddRange(doubles);
            }
            else if (raw is Array arr)
            {
                foreach (object item in arr)
                {
                    try
                    {
                        values.Add(Convert.ToDouble(item));
                    }
                    catch (Exception)
                    {
                        values.Add(0.0);
                    }
                }
            }

            while (values.Count < minLength)
                values.Add(0.0);

            return values.ToArray();
        }

        public static List<string> GetIsotropicMaterialNames(dynamic os)
        {
            var materials = new List<string>();
            if (os == null) return materials;

            object propObj = os.Property;
            if (!OpenStaadPropertyAdapter.TryGetIsotropicMaterialCount(propObj, out int count, out string countError))
            {
                WarnOnce("IsotropicMaterialCount", "Could not read isotropic material count from STAAD. Material dropdown may be incomplete. " + countError);
                return materials;
            }

            if (count <= 0) return materials;

            var uniqueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // OpenSTAAD doc: varMatNo is zero-based index.
            void ReadMaterials(int startInclusive, int endExclusive)
            {
                for (int i = startInclusive; i < endExclusive; i++)
                {
                    if (OpenStaadPropertyAdapter.TryGetIsotropicMaterialName(propObj, i, out string name, out string readError))
                    {
                        if (!string.IsNullOrWhiteSpace(name) && uniqueNames.Add(name))
                            materials.Add(name);
                    }
                    else
                    {
                        // Many STAAD builds throw for out-of-range indices; only warn if nothing loads at all.
                        if (materials.Count == 0 && i <= startInclusive)
                            WarnOnce("IsotropicMaterials:Read", "Could not read isotropic material properties from STAAD. Material dropdown may be incomplete. " + readError);
                    }
                }
            }

            ReadMaterials(0, count);

            // Compatibility fallback for versions that unexpectedly behave as one-based.
            if (materials.Count == 0)
            {
                ReadMaterials(1, count + 1);
                if (materials.Count == 0)
                    WarnOnce("IsotropicMaterials:Empty", "STAAD reported isotropic materials, but none could be read. Material dropdown may be incomplete.");
            }

            return materials;
        }

        private static double GetArrayValue(double[] values, int index)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : 0.0;
        }
    }
}
