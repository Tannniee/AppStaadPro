using System;

namespace StaadSteelApp.Core.Interop
{
    internal static partial class OpenStaadPropertyAdapter
    {
        internal static int CreateTaperedIProperty(object propertyObj, double[] dimensions, out string error)
        {
            return InvokeInt(propertyObj, "CreateTaperedIProperty", out error, dimensions);
        }

        internal static int CreatePrismaticRectangleProperty(object propertyObj, double yDepth, double zDepth, out string error)
        {
            return InvokeInt(propertyObj, "CreatePrismaticRectangleProperty", out error, yDepth, zDepth);
        }

        internal static int CreatePrismaticCircleProperty(object propertyObj, double diameter, out string error)
        {
            return InvokeInt(propertyObj, "CreatePrismaticCircleProperty", out error, diameter);
        }

        internal static int CreatePrismaticTeeProperty(
            object propertyObj,
            double totalDepth,
            double flangeWidth,
            double stemDepth,
            double stemWidth,
            out string error)
        {
            return InvokeInt(
                propertyObj,
                "CreatePrismaticTeeProperty",
                out error,
                totalDepth,
                flangeWidth,
                stemDepth,
                stemWidth);
        }

        internal static int CreatePrismaticTrapezoidalProperty(
            object propertyObj,
            double depth,
            double topWidth,
            double bottomWidth,
            out string error)
        {
            return InvokeInt(
                propertyObj,
                "CreatePrismaticTrapezoidalProperty",
                out error,
                depth,
                topWidth,
                bottomWidth);
        }

        internal static int CreateTaperedTubeProperty(
            object propertyObj,
            int tubeType,
            double startDepth,
            double endDepth,
            double thickness,
            out string error)
        {
            return InvokeInt(
                propertyObj,
                "CreateTaperedTubeProperty",
                out error,
                tubeType,
                startDepth,
                endDepth,
                thickness);
        }

        internal static int CreatePipePropertyFromTable(object propertyObj, int countryCode, string sectionName, out string error)
        {
            return InvokeInt(propertyObj, "CreatePipePropertyFromTable", out error, countryCode, sectionName, 0, 0.0, 0.0);
        }

        internal static int CreateTubePropertyFromTable(object propertyObj, int countryCode, string sectionName, out string error)
        {
            return InvokeInt(propertyObj, "CreateTubePropertyFromTable", out error, countryCode, sectionName, 0, 0.0, 0.0, 0.0);
        }

        internal static int CreateUserDefinedTubePropertyFromTable(
            object propertyObj,
            int countryCode,
            double thickness,
            double width,
            double depth,
            out string error)
        {
            // OSAPP: TypeSpec -1 creates a user-defined tube. Additional
            // specifications are TH/WT/DT: thickness, width and depth.
            return InvokeInt(
                propertyObj,
                "CreateTubePropertyFromTable",
                out error,
                countryCode,
                string.Empty,
                -1,
                thickness,
                width,
                depth);
        }

        internal static int CreateAnglePropertyFromTable(object propertyObj, int countryCode, string sectionName, int typeSpec, double spacing, out string error)
        {
            return InvokeInt(propertyObj, "CreateAnglePropertyFromTable", out error, countryCode, sectionName, typeSpec, spacing);
        }

        internal static int CreateChannelPropertyFromTable(object propertyObj, int countryCode, string sectionName, int typeSpec, double spacing, out string error)
        {
            return InvokeInt(propertyObj, "CreateChannelPropertyFromTable", out error, countryCode, sectionName, typeSpec, spacing);
        }

        internal static int CreateTeePropertyFromTable(object propertyObj, int countryCode, string sectionName, out string error)
        {
            return InvokeInt(propertyObj, "CreateTeePropertyFromTable", out error, countryCode, sectionName, 0);
        }

        internal static int CreateWideFlangePropertyFromTable(object propertyObj, int countryCode, string sectionName, int typeSpec, object specs, out string error)
        {
            return InvokeInt(propertyObj, "CreateWideFlangePropertyFromTable", out error, countryCode, sectionName, typeSpec, specs);
        }

        internal static int CreateBeamPropertyFromTable(object propertyObj, int countryCode, string sectionName, out string error)
        {
            return InvokeInt(propertyObj, "CreateBeamPropertyFromTable", out error, countryCode, sectionName, 0, 0.0, 0.0);
        }
    }
}
