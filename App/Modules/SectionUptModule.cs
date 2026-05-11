using System;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    internal static partial class SectionUptModule
    {
        internal static int GetOrCreateUPTSection(object os, SectionInfo info, Func<object, string, int> checkPropertyExists)
        {
            int tableType = 0;
            switch (info.PropType)
            {
                case eFramePropType.I: tableType = 1; break; // Wide Flange
                case eFramePropType.Channel: tableType = 2; break; // Channel
                case eFramePropType.Angle: tableType = 3; break; // Angle
                case eFramePropType.DblAngle: tableType = 4; break; // Double Angle
                case eFramePropType.T: tableType = 5; break; // Tee
                case eFramePropType.Pipe: tableType = 6; break; // Pipe
                case eFramePropType.Rectangular: tableType = 7; break; // Tube
                case eFramePropType.General: tableType = 8; break; // General
                case eFramePropType.ISection: tableType = 9; break; // ISECTION
                case eFramePropType.Prismatic: tableType = 10; break; // Prismatic
                case eFramePropType.Circle: tableType = 10; break; // Prismatic for circle
                default:
                    throw new SectionPropertyException($"Cannot create UPT for property type {info.PropType}");
            }

            int tableId = GetOrCreateUPTTableId(os, tableType);
            if (!OpenStaadPropertyAdapter.TryGetPropertyObject(os, out object propertyObj, out string propertyError))
                throw new SectionPropertyException(propertyError);

            // Check if name already exists
            int existingProperty = checkPropertyExists(os, info.Name);
            if (existingProperty > 0) return existingProperty;

            // Dimensions to meters
            double Wf = info.Bf / 1000.0;
            double Wf1 = info.Bfb > 0 ? info.Bfb / 1000.0 : Wf;
            double D = info.H1 / 1000.0;
            double D1 = info.H2 > 0 ? info.H2 / 1000.0 : D;
            double Tf = info.Tf / 1000.0;
            double Tf1 = info.Tfb > 0 ? info.Tfb / 1000.0 : Tf;
            double Tw = info.Tw / 1000.0;
            double tubeD = info.H1 / 1000.0;
            double tubeWf = info.Bf > 0 ? info.Bf / 1000.0 : (info.H2 / 1000.0);
            double generalB = info.H2 / 1000.0;
            double prismaticYD = info.H1 / 1000.0;
            double prismaticZD = info.H2 / 1000.0;
            double OD = info.H1 / 1000.0;
            double ID = info.Bf / 1000.0;
            double spc = info.Dis / 1000.0;

            object createResult = null;

            // Invoke AddUPTProperty...
            switch (tableType)
            {
                case 1: // WIDE FLANGE
                    if (D <= 0.0 || Tw <= 0.0 || Wf <= 0.0 || Tf <= 0.0)
                        throw new SectionPropertyException($"UPT WIDE FLANGE '{info.Name}' requires D/TW/WF/TF > 0.");
                    double wideWebH = Math.Max(D - (2.0 * Tf), 0.0);
                    double wideArea = (2.0 * Wf * Tf) + (wideWebH * Tw);
                    double wideIzApprox = (2.0 * Wf * Math.Pow(Tf, 3) + wideWebH * Math.Pow(Tw, 3)) / 3.0;
                    double wideIyApprox = (2.0 * Tf * Math.Pow(Wf, 3) + wideWebH * Math.Pow(Tw, 3)) / 12.0;
                    double wideIxApprox =
                        (Tw * Math.Pow(wideWebH, 3) / 12.0)
                        + 2.0 * ((Wf * Math.Pow(Tf, 3) / 12.0) + (Wf * Tf * Math.Pow((D * 0.5) - (Tf * 0.5), 2)));
                    double wideAx = PositiveOrFallback(info.UptAx, wideArea, 1e-12);
                    double wideIz = PositiveOrFallback(info.UptIz, wideIzApprox, 1e-12);
                    double wideIy = PositiveOrFallback(info.UptIy, wideIyApprox, 1e-12);
                    double wideIx = PositiveOrFallback(info.UptIx, wideIxApprox, 1e-12);
                    double wideAy = PositiveOrFallback(info.UptAy, (5.0 / 6.0) * wideAx, 1e-12);
                    double wideAz = PositiveOrFallback(info.UptAz, (5.0 / 6.0) * wideAx, 1e-12);
                    createResult = OpenStaadPropertyAdapter.AddUPTPropertyWideFlange(propertyObj, tableId, info.Name,
                        wideAx, D, Tw, Wf, Tf, wideIz, wideIy, wideIx, wideAy, wideAz, out _);
                    break;
                case 2: // CHANNEL
                    if (D <= 0.0 || Tw <= 0.0 || Wf <= 0.0 || Tf <= 0.0)
                        throw new SectionPropertyException($"UPT CHANNEL '{info.Name}' requires D/TW/WF/TF > 0.");
                    double channelWebH = Math.Max(D - (2.0 * Tf), 0.0);
                    double channelArea = (2.0 * Wf * Tf) + (channelWebH * Tw);
                    double channelIzApprox = (2.0 * Wf * Math.Pow(Tf, 3) + channelWebH * Math.Pow(Tw, 3)) / 3.0;
                    double channelIyApprox = (2.0 * Tf * Math.Pow(Wf, 3) + channelWebH * Math.Pow(Tw, 3)) / 12.0;
                    double channelIxApprox =
                        (Tw * Math.Pow(channelWebH, 3) / 12.0)
                        + 2.0 * ((Wf * Math.Pow(Tf, 3) / 12.0) + (Wf * Tf * Math.Pow((D * 0.5) - (Tf * 0.5), 2)));
                    double channelAx = PositiveOrFallback(info.UptAx, channelArea, 1e-12);
                    double channelIz = PositiveOrFallback(info.UptIz, channelIzApprox, 1e-12);
                    double channelIy = PositiveOrFallback(info.UptIy, channelIyApprox, 1e-12);
                    double channelIx = PositiveOrFallback(info.UptIx, channelIxApprox, 1e-12);
                    double channelAy = PositiveOrFallback(info.UptAy, (5.0 / 6.0) * channelAx, 1e-12);
                    double channelAz = PositiveOrFallback(info.UptAz, (5.0 / 6.0) * channelAx, 1e-12);
                    createResult = OpenStaadPropertyAdapter.AddUPTPropertyChannel(propertyObj, tableId, info.Name,
                        channelAx, D, Tw, Wf, Tf, channelIz, channelIy, channelIx, 0.0, channelAy, channelAz, out _);
                    break;
                case 3: // ANGLE
                    if (D <= 0.0 || Wf <= 0.0 || Tf <= 0.0)
                        throw new SectionPropertyException($"UPT ANGLE '{info.Name}' requires D/WF/TF > 0.");
                    double angleArea = Tf * Math.Max((D + Wf - Tf), 0.0);
                    double angleAy = PositiveOrFallback(info.UptAy, 0.5 * angleArea, 1e-12);
                    double angleAz = PositiveOrFallback(info.UptAz, 0.5 * angleArea, 1e-12);
                    createResult = OpenStaadPropertyAdapter.AddUPTPropertyAngle(propertyObj, tableId, info.Name,
                        D, Wf, Tf, 0.0, angleAy, angleAz, out _);
                    break;
                case 4: // DOUBLE ANGLE
                    if (D <= 0.0 || Wf <= 0.0 || Tf <= 0.0)
                        throw new SectionPropertyException($"UPT DOUBLE ANGLE '{info.Name}' requires D/WF/TF > 0.");
                    if (spc < 0.0)
                        throw new SectionPropertyException($"UPT DOUBLE ANGLE '{info.Name}' requires SP >= 0.");
                    double dblAngleArea = 2.0 * Tf * Math.Max((D + Wf - Tf), 0.0);
                    double dblAngleIz = info.UptIz > 0.0 ? info.UptIz : 0.0;
                    double dblAngleIy = info.UptIy > 0.0 ? info.UptIy : 0.0;
                    double dblAngleIx = info.UptIx > 0.0 ? info.UptIx : 0.0;
                    double dblAngleAy = PositiveOrFallback(info.UptAy, 0.5 * dblAngleArea, 1e-12);
                    double dblAngleAz = PositiveOrFallback(info.UptAz, 0.5 * dblAngleArea, 1e-12);
                    createResult = OpenStaadPropertyAdapter.AddUPTPropertyDoubleAngle(propertyObj, tableId, info.Name,
                        D, Wf, Tf, spc, dblAngleIz, dblAngleIy, dblAngleIx, 0.0, dblAngleAy, dblAngleAz, out _);
                    break;
                case 5: // TEE
                    if (D <= 0.0 || Wf <= 0.0 || Tf <= 0.0 || Tw <= 0.0)
                        throw new SectionPropertyException($"UPT TEE '{info.Name}' requires D/WF/TF/TW > 0.");
                    double teeWebH = Math.Max(D - Tf, 0.0);
                    double teeArea = (Wf * Tf) + (teeWebH * Tw);
                    double yFlange = D - (Tf * 0.5);
                    double yWeb = teeWebH * 0.5;
                    double yBar = ((Wf * Tf * yFlange) + (teeWebH * Tw * yWeb)) / Math.Max(teeArea, 1e-12);
                    double teeIxApprox =
                        (Wf * Math.Pow(Tf, 3) / 12.0) + (Wf * Tf * Math.Pow(yFlange - yBar, 2))
                        + (Tw * Math.Pow(teeWebH, 3) / 12.0) + (teeWebH * Tw * Math.Pow(yWeb - yBar, 2));
                    double teeIyApprox = (Tf * Math.Pow(Wf, 3) / 12.0) + (teeWebH * Math.Pow(Tw, 3) / 12.0);
                    double teeIzApprox = (Wf * Math.Pow(Tf, 3) + teeWebH * Math.Pow(Tw, 3)) / 3.0;
                    double teeAx = PositiveOrFallback(info.UptAx, teeArea, 1e-12);
                    double teeIz = PositiveOrFallback(info.UptIz, teeIzApprox, 1e-12);
                    double teeIy = PositiveOrFallback(info.UptIy, teeIyApprox, 1e-12);
                    double teeIx = PositiveOrFallback(info.UptIx, teeIxApprox, 1e-12);
                    double teeAy = PositiveOrFallback(info.UptAy, (5.0 / 6.0) * teeAx, 1e-12);
                    double teeAz = PositiveOrFallback(info.UptAz, (5.0 / 6.0) * teeAx, 1e-12);
                    createResult = OpenStaadPropertyAdapter.AddUPTPropertyTee(propertyObj, tableId, info.Name,
                        teeAx, D, Wf, Tf, Tw, teeIz, teeIy, teeIx, 0.0, teeAy, teeAz, out _);
                    break;
                case 6: // PIPE
                    if (OD <= 0.0 || ID <= 0.0 || ID >= OD)
                        throw new SectionPropertyException($"UPT PIPE '{info.Name}' requires OD > ID > 0.");
                    double pipeArea = (Math.PI / 4.0) * (OD * OD - ID * ID);
                    double pipeAy = PositiveOrFallback(info.UptAy, 0.5 * pipeArea, 1e-12);
                    double pipeAz = PositiveOrFallback(info.UptAz, 0.5 * pipeArea, 1e-12);
                    createResult = OpenStaadPropertyAdapter.AddUPTPropertyPipe(propertyObj, tableId, info.Name,
                        OD, ID, pipeAy, pipeAz, out _);
                    break;
                case 7: // TUBE
                    if (tubeD <= 0.0 || tubeWf <= 0.0 || Tf <= 0.0)
                        throw new SectionPropertyException($"UPT TUBE '{info.Name}' requires D/WF/TF > 0.");
                    if ((2.0 * Tf) >= Math.Min(tubeD, tubeWf))
                        throw new SectionPropertyException($"UPT TUBE '{info.Name}' requires TF < min(D,WF)/2.");
                    double innerD = tubeD - (2.0 * Tf);
                    double innerWf = tubeWf - (2.0 * Tf);
                    double tubeArea = (tubeD * tubeWf) - (innerD * innerWf);
                    double tubeIzApprox = ((tubeWf * Math.Pow(tubeD, 3)) - (innerWf * Math.Pow(innerD, 3))) / 12.0;
                    double tubeIyApprox = ((tubeD * Math.Pow(tubeWf, 3)) - (innerD * Math.Pow(innerWf, 3))) / 12.0;
                    double tubeIxApprox = ComputeThinWalledClosedBoxTorsionConstant(tubeD, tubeWf, Tf);
                    double tubeAx = PositiveOrFallback(info.UptAx, tubeArea, 1e-12);
                    double tubeIz = PositiveOrFallback(info.UptIz, tubeIzApprox, 1e-12);
                    double tubeIy = PositiveOrFallback(info.UptIy, tubeIyApprox, 1e-12);
                    double tubeIx = PositiveOrFallback(info.UptIx, tubeIxApprox, 1e-12);
                    double tubeAy = PositiveOrFallback(info.UptAy, 2.0 * tubeD * Tf, 1e-12);
                    double tubeAz = PositiveOrFallback(info.UptAz, 2.0 * tubeWf * Tf, 1e-12);
                    createResult = OpenStaadPropertyAdapter.AddUPTPropertyTube(propertyObj, tableId, info.Name,
                        tubeAx, tubeD, tubeWf, Tf, tubeIz, tubeIy, tubeIx, tubeAy, tubeAz, out _);
                    break;
                case 8: // GENERAL
                    if (D <= 0.0 || generalB <= 0.0 || Tw <= 0.0 || Tf <= 0.0)
                        throw new SectionPropertyException($"UPT GENERAL '{info.Name}' requires D/B/TD/TB > 0.");
                    double generalDepth = D;
                    double generalWidth = generalB;
                    double generalTd = Tw;
                    double generalTb = Tf;
                    double generalAreaRect = generalDepth * generalWidth;
                    double generalAx = PositiveOrFallback(info.UptAx, generalAreaRect, 1e-12);
                    double generalIz = PositiveOrFallback(info.UptIz, ComputeRectangularTorsionConstant(generalDepth, generalWidth), 1e-12);
                    double generalIy = PositiveOrFallback(info.UptIy, generalDepth * Math.Pow(generalWidth, 3) / 12.0, 1e-12);
                    double generalIx = PositiveOrFallback(info.UptIx, generalWidth * Math.Pow(generalDepth, 3) / 12.0, 1e-12);
                    double generalSz = PositiveOrFallback(info.UptSz, generalIx / Math.Max(generalDepth / 2.0, 1e-9), 1e-12);
                    double generalSy = PositiveOrFallback(info.UptSy, generalIy / Math.Max(generalWidth / 2.0, 1e-9), 1e-12);
                    double generalAy = PositiveOrFallback(info.UptAy, (5.0 / 6.0) * generalAx, 1e-12);
                    double generalAz = PositiveOrFallback(info.UptAz, (5.0 / 6.0) * generalAx, 1e-12);
                    double generalPz = PositiveOrFallback(info.UptPz, generalSz * 1.5, 1e-12);
                    double generalPy = PositiveOrFallback(info.UptPy, generalSy * 1.5, 1e-12);
                    double generalHss = info.UptHss >= 0.0 ? info.UptHss : 0.0;
                    double generalDee = PositiveOrFallback(info.UptDee, generalDepth, 1e-12);
                    createResult = OpenStaadPropertyAdapter.AddUPTPropertyGeneral(propertyObj, tableId, info.Name,
                        generalAx, generalDepth, generalTd, generalWidth, generalTb, generalIz, generalIy, generalIx, generalSz, generalSy,
                        generalAy, generalAz, generalPz, generalPy, generalHss, generalDee, out _);
                    break;
                case 9: // ISECTION
                    if (D <= 0.0 || Tw <= 0.0 || D1 <= 0.0 || Wf <= 0.0 || Tf <= 0.0 || Wf1 <= 0.0 || Tf1 <= 0.0)
                        throw new SectionPropertyException($"UPT ISECTION '{info.Name}' requires DWW/TWW/DWW1/BFF/TFF/BFF1/TFF1 > 0.");
                    double iArea = (Wf * Tf) + (Wf1 * Tf1) + (Math.Max(((D + D1) * 0.5) - Tf - Tf1, 0.0) * Tw);
                    double iAx = PositiveOrFallback(info.UptAx, iArea, 1e-12);
                    double iAyf = PositiveOrFallback(info.UptAy, (5.0 / 6.0) * iAx, 1e-12);
                    double iAzf = PositiveOrFallback(info.UptAz, (5.0 / 6.0) * iAx, 1e-12);
                    createResult = OpenStaadPropertyAdapter.AddUPTPropertyISection(propertyObj, tableId, info.Name,
                        D, Tw, D1, Wf, Tf, Wf1, Tf1, iAyf, iAzf, 0.0, out _);
                    break;
                case 10: // PRISMATIC
                    if (prismaticYD <= 0.0 || prismaticZD <= 0.0)
                        throw new SectionPropertyException($"UPT PRISMATIC '{info.Name}' requires YD/ZD > 0.");
                    double prismaticDepth = prismaticYD;
                    double prismaticWidth = prismaticZD;
                    double prismaticAreaRect = prismaticDepth * prismaticWidth;
                    double prismaticAx = PositiveOrFallback(info.UptAx, prismaticAreaRect, 1e-12);
                    double prismaticIz = PositiveOrFallback(info.UptIz, ComputeRectangularTorsionConstant(prismaticDepth, prismaticWidth), 1e-12);
                    double prismaticIy = PositiveOrFallback(info.UptIy, prismaticDepth * Math.Pow(prismaticWidth, 3) / 12.0, 1e-12);
                    double prismaticIx = PositiveOrFallback(info.UptIx, prismaticWidth * Math.Pow(prismaticDepth, 3) / 12.0, 1e-12);
                    double prismaticAy = PositiveOrFallback(info.UptAy, (5.0 / 6.0) * prismaticAx, 1e-12);
                    double prismaticAz = PositiveOrFallback(info.UptAz, (5.0 / 6.0) * prismaticAx, 1e-12);
                    createResult = OpenStaadPropertyAdapter.AddUPTPropertyPrismatic(propertyObj, tableId, info.Name,
                        prismaticAx, prismaticIz, prismaticIy, prismaticIx, prismaticAy, prismaticAz,
                        prismaticDepth, prismaticWidth, out _);
                    break;
            }

            int existingAfterCreate = FindPropertyIdByNameWithRetry(os, info.Name, checkPropertyExists, 2, 75);
            if (existingAfterCreate > 0) return existingAfterCreate;

            int createdPropertyId = CreatePropertyFromUptTable(propertyObj, tableId, info.Name);
            if (createdPropertyId > 0)
                return createdPropertyId;

            existingAfterCreate = FindPropertyIdByNameWithRetry(os, info.Name, checkPropertyExists, 5, 150);
            if (existingAfterCreate > 0) return existingAfterCreate;

            if (!IsUptSectionAddSuccess(createResult))
            {
                throw new SectionPropertyException($"Failed to add UPT section '{info.Name}'. OpenSTAAD returned {createResult ?? "null"}.");
            }

            throw new SectionPropertyException($"UPT section '{info.Name}' was added to table {tableId} but no section property ID could be created from it.");
        }

    }
}
