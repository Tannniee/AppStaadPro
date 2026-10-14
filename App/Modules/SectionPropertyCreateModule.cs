using System;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    internal static class SectionPropertyCreateModule
    {
        internal static int GetOrCreateProperty(
            object os,
            SectionInfo info,
            Func<object, SectionInfo, int> getOrCreateUPTSection,
            Func<object, string, int> checkPropertyExists)
        {
            // 0. UPT Handling
            if (info.CreateAsUPT)
            {
                return getOrCreateUPTSection(os, info);
            }

            if (!OpenStaadPropertyAdapter.TryGetPropertyObject(os, out object propertyObj, out string propertyError))
                throw new SectionPropertyException(propertyError);

            // 1. Tapered I
            if (info.PropType == eFramePropType.I)
            {
                if (info.H1 <= 0 || info.Tw <= 0)
                {
                    return 0;
                }

                double[] dims = new double[7];
                dims[0] = info.H1 / 1000.0;
                dims[1] = info.Tw / 1000.0;
                dims[2] = info.H2 / 1000.0;
                dims[3] = info.Bf / 1000.0;
                dims[4] = info.Tf / 1000.0;
                dims[5] = info.Bfb / 1000.0;
                dims[6] = info.Tfb / 1000.0;

                return OpenStaadPropertyAdapter.CreateTaperedIProperty(propertyObj, dims, out _);
            }

            // 2. Prismatic Rectangle
            if (info.PropType == eFramePropType.Rectangular)
            {
                return OpenStaadPropertyAdapter.CreatePrismaticRectangleProperty(propertyObj, info.H1 / 1000.0, info.H2 / 1000.0, out _);
            }

            // 3. Prismatic Circle
            if (info.PropType == eFramePropType.Circle)
            {
                return OpenStaadPropertyAdapter.CreatePrismaticCircleProperty(propertyObj, info.H1 / 1000.0, out _);
            }

            // 4. Prismatic Tee
            if (info.PropType == eFramePropType.T)
            {
                double stemDepth = info.H1 - info.Tf;
                if (stemDepth <= 0.0 || info.Tw <= 0.0)
                    return 0;

                return OpenStaadPropertyAdapter.CreatePrismaticTeeProperty(
                    propertyObj,
                    info.H1 / 1000.0,
                    info.Bf / 1000.0,
                    stemDepth / 1000.0,
                    info.Tw / 1000.0,
                    out _
                );
            }

            // 5. Prismatic Trapezoidal
            if (info.PropType == eFramePropType.Trapezoidal)
            {
                return OpenStaadPropertyAdapter.CreatePrismaticTrapezoidalProperty(
                    propertyObj,
                    info.H1 / 1000.0,
                    info.H2 / 1000.0,
                    info.Bf / 1000.0,
                    out _);
            }

            // 6. Tapered Pipe / Tapered Tube
            if (info.PropType == eFramePropType.Pipe)
            {
                if (info.H1 <= 0.0 || info.H2 <= 0.0 || info.Tw <= 0.0 ||
                    (2.0 * info.Tw) >= Math.Min(info.H1, info.H2))
                {
                    return 0;
                }

                int tubeType = GetTaperedTubeCreateType(info.TaperedTubeShape);

                return OpenStaadPropertyAdapter.CreateTaperedTubeProperty(
                    propertyObj,
                    tubeType,
                    info.H1 / 1000.0,
                    info.H2 / 1000.0,
                    info.Tw / 1000.0,
                    out _);
            }

            if (info.PropType == eFramePropType.General)
            {
                throw new SectionPropertyException(
                    "Prismatic General requires Ax, Ay, Az, Ix, Iy, Iz, YD, ZD, YB, and ZB. " +
                    "Use Create as UPT > GENERAL, or reuse an existing equivalent property.");
            }

            bool hasTypeSpec = info.LegacyTypeSpec != 0;

            // 7. Standard Table Logic (for Legacy only)
            if (!hasTypeSpec)
            {
                int existingID = checkPropertyExists(os, info.Name);
                if (existingID > 0) return existingID;
            }

            int countryCode = info.CountryCode > 0 ? info.CountryCode : 1;
            string shapeName = info.ShapeName ?? "";

            if (IsPipeShape(shapeName, info.Name))
            {
                int newID_Pipe = TryCreateWithFallback(
                    name => OpenStaadPropertyAdapter.CreatePipePropertyFromTable(propertyObj, countryCode, name, out _),
                    info.Name
                );
                if (newID_Pipe > 0) return newID_Pipe;
            }
            else if (IsTubeShape(shapeName, info.Name))
            {
                int newID_Tube = TryCreateWithFallback(
                    name => OpenStaadPropertyAdapter.CreateTubePropertyFromTable(propertyObj, countryCode, name, out _),
                    info.Name
                );
                if (newID_Tube > 0) return newID_Tube;

                double tubeThicknessMm = info.Tf > 0.0 ? info.Tf : info.Tw;
                double tubeDepthMm = info.H1;
                double tubeWidthMm = info.H2 > 0.0 ? info.H2 : info.Bf;
                if (tubeThicknessMm > 0.0 && tubeDepthMm > 0.0 && tubeWidthMm > 0.0 &&
                    (2.0 * tubeThicknessMm) < Math.Min(tubeDepthMm, tubeWidthMm))
                {
                    int userDefinedTubeId = OpenStaadPropertyAdapter.CreateUserDefinedTubePropertyFromTable(
                        propertyObj,
                        countryCode,
                        tubeThicknessMm / 1000.0,
                        tubeWidthMm / 1000.0,
                        tubeDepthMm / 1000.0,
                        out _);
                    if (userDefinedTubeId > 0) return userDefinedTubeId;
                }
            }
            else if (IsAngleShape(shapeName, info.Name))
            {
                int angleTypeSpec = info.LegacyTypeSpec;
                double angleSpacing = info.LegacySpec1;
                int newID_Angle = TryCreateWithFallback(
                    name => OpenStaadPropertyAdapter.CreateAnglePropertyFromTable(propertyObj, countryCode, name, angleTypeSpec, angleSpacing, out _),
                    info.Name
                );
                if (newID_Angle > 0) return newID_Angle;
                if (hasTypeSpec) return newID_Angle;
            }
            else if (IsChannelShape(shapeName, info.Name))
            {
                int channelTypeSpec = info.LegacyTypeSpec;
                double channelSpacing = info.LegacySpec1;
                int newID_Channel = TryCreateWithFallback(
                    name => OpenStaadPropertyAdapter.CreateChannelPropertyFromTable(propertyObj, countryCode, name, channelTypeSpec, channelSpacing, out _),
                    info.Name
                );
                if (newID_Channel > 0) return newID_Channel;
                if (hasTypeSpec) return newID_Channel;
            }
            else if (IsTeeShape(shapeName, info.Name))
            {
                int newID_Tee = TryCreateWithFallback(
                    name => OpenStaadPropertyAdapter.CreateTeePropertyFromTable(propertyObj, countryCode, name, out _),
                    info.Name
                );
                if (newID_Tee > 0) return newID_Tee;
            }
            else if (IsWideFlangeShape(shapeName, info.Name))
            {
                int wideTypeSpec = info.LegacyTypeSpec;
                object specs = new double[]
                {
                    info.LegacySpec1,
                    info.LegacySpec2,
                    info.LegacySpec3,
                    info.LegacySpec4
                };
                int newID_Wide = TryCreateWithFallback(
                    name => OpenStaadPropertyAdapter.CreateWideFlangePropertyFromTable(propertyObj, countryCode, name, wideTypeSpec, specs, out _),
                    info.Name
                );
                if (newID_Wide > 0) return newID_Wide;
                if (hasTypeSpec) return newID_Wide;
            }

            if (hasTypeSpec) return 0;

            int newID_Std = TryCreateWithFallback(
                name => OpenStaadPropertyAdapter.CreateBeamPropertyFromTable(propertyObj, countryCode, name, out _),
                info.Name
            );
            if (newID_Std > 0) return newID_Std;

            return 0;
        }

        internal static int GetTaperedTubeCreateType(TaperedTubeShape shape)
        {
            if (shape == TaperedTubeShape.Round)
                return 0;

            if (shape == TaperedTubeShape.Square)
                return 5;

            throw new SectionPropertyException("Select Tapered Pipe or Tapered Tube before creating the section.");
        }

        private static int TryCreateWithFallback(Func<string, int> create, string name)
        {
            if (string.IsNullOrEmpty(name)) return 0;

            int id = create(name);
            if (id > 0) return id;

            int lastResult = id;
            if (name.EndsWith(".0", StringComparison.Ordinal))
            {
                string fixedName = name.Substring(0, name.Length - 2);
                lastResult = create(fixedName);
                if (lastResult > 0) return lastResult;
            }

            return lastResult;
        }

        private static bool IsPipeShape(string shapeName, string sectionName)
        {
            string shape = shapeName ?? "";
            if (shape.IndexOf("pipe", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("chs", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("hss round", StringComparison.OrdinalIgnoreCase) >= 0) return true;

            if (sectionName.StartsWith("PIP", StringComparison.OrdinalIgnoreCase)) return true;
            if (sectionName.StartsWith("CHS", StringComparison.OrdinalIgnoreCase)) return true;
            return sectionName.StartsWith("PIPE", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTubeShape(string shapeName, string sectionName)
        {
            string shape = shapeName ?? "";
            if (shape.IndexOf("tube", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("hss", StringComparison.OrdinalIgnoreCase) >= 0
                && shape.IndexOf("round", StringComparison.OrdinalIgnoreCase) < 0) return true;
            if (shape.IndexOf("rhs", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("shs", StringComparison.OrdinalIgnoreCase) >= 0) return true;

            if (sectionName.StartsWith("TUB", StringComparison.OrdinalIgnoreCase)) return true;
            if (sectionName.StartsWith("HSS", StringComparison.OrdinalIgnoreCase)) return true;
            if (sectionName.StartsWith("RHS", StringComparison.OrdinalIgnoreCase)) return true;
            return sectionName.StartsWith("SHS", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAngleShape(string shapeName, string sectionName)
        {
            string shape = shapeName ?? "";
            if (shape.IndexOf("angle", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return sectionName.StartsWith("L", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsChannelShape(string shapeName, string sectionName)
        {
            string shape = shapeName ?? "";
            if (shape.IndexOf("channel", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.StartsWith("UP ", StringComparison.OrdinalIgnoreCase)) return true;
            if (shape.StartsWith("UPN", StringComparison.OrdinalIgnoreCase)) return true;
            if (shape.StartsWith("UPE", StringComparison.OrdinalIgnoreCase)) return true;
            if (shape.StartsWith("UAP", StringComparison.OrdinalIgnoreCase)) return true;
            return sectionName.StartsWith("C", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTeeShape(string shapeName, string sectionName)
        {
            string shape = shapeName ?? "";
            if (shape.IndexOf("tee", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("t shape", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("ct shape", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return sectionName.StartsWith("T", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsWideFlangeShape(string shapeName, string sectionName)
        {
            string shape = shapeName ?? "";
            if (shape.IndexOf("shape", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("ipe", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("ipn", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("jo", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("ub", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("uc", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("wb", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("wc", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("hp", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("he", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("hl", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("hd", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("i shape", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("h shape", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("w shape", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("m shape", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("s shape", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (shape.IndexOf("b shape", StringComparison.OrdinalIgnoreCase) >= 0) return true;

            return sectionName.StartsWith("W", StringComparison.OrdinalIgnoreCase)
                || sectionName.StartsWith("S", StringComparison.OrdinalIgnoreCase)
                || sectionName.StartsWith("M", StringComparison.OrdinalIgnoreCase)
                || sectionName.StartsWith("B", StringComparison.OrdinalIgnoreCase)
                || sectionName.StartsWith("HP", StringComparison.OrdinalIgnoreCase)
                || sectionName.StartsWith("UB", StringComparison.OrdinalIgnoreCase)
                || sectionName.StartsWith("UC", StringComparison.OrdinalIgnoreCase)
                || sectionName.StartsWith("IPE", StringComparison.OrdinalIgnoreCase)
                || sectionName.StartsWith("IPN", StringComparison.OrdinalIgnoreCase)
                || sectionName.StartsWith("HE", StringComparison.OrdinalIgnoreCase)
                || sectionName.StartsWith("HL", StringComparison.OrdinalIgnoreCase)
                || sectionName.StartsWith("HD", StringComparison.OrdinalIgnoreCase)
                || sectionName.StartsWith("JO", StringComparison.OrdinalIgnoreCase);
        }
    }
}
