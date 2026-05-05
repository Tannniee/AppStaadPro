using System;

namespace StaadSteelApp.Core.Interop
{
    internal static partial class OpenStaadPropertyAdapter
    {
        internal static bool TryGetSectionPropertyValuesEx(
            object openStaad,
            int propertyRefNo,
            out int sectionType,
            out double[] values,
            out string error)
        {
            sectionType = 0;
            values = new double[25];
            error = string.Empty;

            if (propertyRefNo <= 0)
            {
                error = $"Invalid property ref '{propertyRefNo}'.";
                return false;
            }

            if (!TryGetPropertyObject(openStaad, out object propertyObj, out error))
                return false;

            object[] args = new object[] { propertyRefNo, sectionType, values };
            if (!ComInvoke.TryInvoke(propertyObj, "GetSectionPropertyValuesEx", out _, out error, args, ComInvoke.ByRefModifiers(3, 1, 2)))
                return false;

            sectionType = ComResult.ToInt32(args[1]);
            values = OpenStaadAdapterHelpers.NormalizeDoubleArray(args[2], 25);
            return true;
        }

        internal static bool TryGetSectionPropertyValues(
            object openStaad,
            int propertyRefNo,
            out double[] values,
            out string error)
        {
            values = new double[10];
            error = string.Empty;

            if (propertyRefNo <= 0)
            {
                error = $"Invalid property ref '{propertyRefNo}'.";
                return false;
            }

            if (!TryGetPropertyObject(openStaad, out object propertyObj, out error))
                return false;

            object[] args = new object[]
            {
                propertyRefNo,
                0.0, // width
                0.0, // depth
                0.0, // Ax
                0.0, // Ay
                0.0, // Az
                0.0, // Ix
                0.0, // Iy
                0.0, // Iz
                0.0, // Tf
                0.0  // Tw
            };

            if (!ComInvoke.TryInvoke(propertyObj, "GetSectionPropertyValues", out _, out error, args, ComInvoke.ByRefModifiers(11, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10)))
                return false;

            values = new[]
            {
                ToDouble(args[1]),
                ToDouble(args[2]),
                ToDouble(args[3]),
                ToDouble(args[4]),
                ToDouble(args[5]),
                ToDouble(args[6]),
                ToDouble(args[7]),
                ToDouble(args[8]),
                ToDouble(args[9]),
                ToDouble(args[10])
            };

            return true;
        }

        internal static bool TryGetSectionPropertyValuesExFromProperty(
            object propertyObj,
            int propertyRefNo,
            out int sectionType,
            out double[] values,
            out string error)
        {
            sectionType = 0;
            values = new double[25];
            error = string.Empty;

            if (propertyRefNo <= 0)
            {
                error = $"Invalid property ref '{propertyRefNo}'.";
                return false;
            }

            object[] args = new object[] { propertyRefNo, sectionType, values };
            if (!ComInvoke.TryInvoke(propertyObj, "GetSectionPropertyValuesEx", out _, out error, args, ComInvoke.ByRefModifiers(3, 1, 2)))
                return false;

            sectionType = ComResult.ToInt32(args[1]);
            values = OpenStaadAdapterHelpers.NormalizeDoubleArray(args[2], 25);
            return true;
        }

        internal static bool TryGetSectionPropertyValuesFromProperty(
            object propertyObj,
            int propertyRefNo,
            out double[] values,
            out string error)
        {
            values = new double[10];
            error = string.Empty;

            if (propertyRefNo <= 0)
            {
                error = $"Invalid property ref '{propertyRefNo}'.";
                return false;
            }

            object[] args = new object[]
            {
                propertyRefNo,
                0.0,
                0.0,
                0.0,
                0.0,
                0.0,
                0.0,
                0.0,
                0.0,
                0.0,
                0.0
            };

            if (!ComInvoke.TryInvoke(propertyObj, "GetSectionPropertyValues", out _, out error, args, ComInvoke.ByRefModifiers(11, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10)))
                return false;

            values = new[]
            {
                ToDouble(args[1]),
                ToDouble(args[2]),
                ToDouble(args[3]),
                ToDouble(args[4]),
                ToDouble(args[5]),
                ToDouble(args[6]),
                ToDouble(args[7]),
                ToDouble(args[8]),
                ToDouble(args[9]),
                ToDouble(args[10])
            };

            return true;
        }

        internal static bool TryGetTaperedSectionDataForMember(
            object openStaad,
            int memberNo,
            out OpenStaadTaperedSectionData data,
            out string error)
        {
            data = null;

            if (!TryGetBeamSectionPropertyRefNo(openStaad, memberNo, out int propertyRefNo, out error))
                return false;

            if (!TryGetSectionPropertyValuesEx(openStaad, propertyRefNo, out int sectionType, out double[] values, out error))
                return false;

            if (sectionType != TaperedSectionTypeCode || values.Length < 7)
            {
                error = $"Property {propertyRefNo} is not a tapered section.";
                return false;
            }

            data = new OpenStaadTaperedSectionData
            {
                H1 = values[0],
                Tw = values[1],
                H2 = values[2],
                Bf = values[3],
                Tf = values[4],
                Bfb = values[5],
                Tfb = values[6]
            };

            if (data.H1 <= 0 || data.H2 <= 0 || data.Tw <= 0 || data.Bf <= 0 || data.Tf <= 0 || data.Bfb <= 0 || data.Tfb <= 0)
            {
                error = "Tapered section dimensions are incomplete or invalid.";
                return false;
            }

            return true;
        }
    }
}
