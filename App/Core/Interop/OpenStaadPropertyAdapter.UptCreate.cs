using System;

namespace StaadSteelApp.Core.Interop
{
    internal static partial class OpenStaadPropertyAdapter
    {
        internal static int AddUPTPropertyWideFlange(object propertyObj, int tableId, string name, double ax, double d, double tw, double wf, double tf, double iz, double iy, double ix, double ay, double az, out string error)
        {
            return InvokeInt(propertyObj, "AddUPTPropertyWIDEFLANGE", out error, tableId, name, ax, d, tw, wf, tf, iz, iy, ix, ay, az);
        }

        internal static int AddUPTPropertyWideFlangeUnequal(object propertyObj, int tableId, string name, double[] properties, out string error)
        {
            return InvokeInt(propertyObj, "AddUPTPropertyWIDEFLANGEUNEQUAL", out error, tableId, name, properties);
        }

        internal static int AddUPTPropertyChannel(object propertyObj, int tableId, string name, double ax, double d, double tw, double wf, double tf, double iz, double iy, double ix, double cz, double ay, double az, out string error)
        {
            return InvokeInt(propertyObj, "AddUPTPropertyCHANNEL", out error, tableId, name, ax, d, tw, wf, tf, iz, iy, ix, cz, ay, az);
        }

        internal static int AddUPTPropertyAngle(object propertyObj, int tableId, string name, double d, double wf, double tf, double r, double ay, double az, out string error)
        {
            return InvokeInt(propertyObj, "AddUPTPropertyANGLE", out error, tableId, name, d, wf, tf, r, ay, az);
        }

        internal static int AddUPTPropertyDoubleAngle(object propertyObj, int tableId, string name, double d, double wf, double tf, double spacing, double iz, double iy, double ix, double cy, double ay, double az, out string error)
        {
            return InvokeInt(propertyObj, "AddUPTPropertyDOUBLEANGLE", out error, tableId, name, d, wf, tf, spacing, iz, iy, ix, cy, ay, az);
        }

        internal static int AddUPTPropertyTee(object propertyObj, int tableId, string name, double ax, double d, double wf, double tf, double tw, double iz, double iy, double ix, double cy, double ay, double az, out string error)
        {
            return InvokeInt(propertyObj, "AddUPTPropertyTEE", out error, tableId, name, ax, d, wf, tf, tw, iz, iy, ix, cy, ay, az);
        }

        internal static int AddUPTPropertyPipe(object propertyObj, int tableId, string name, double od, double id, double ay, double az, out string error)
        {
            return InvokeInt(propertyObj, "AddUPTPropertyPIPE", out error, tableId, name, od, id, ay, az);
        }

        internal static int AddUPTPropertyTube(object propertyObj, int tableId, string name, double ax, double d, double wf, double tf, double iz, double iy, double ix, double ay, double az, out string error)
        {
            return InvokeInt(propertyObj, "AddUPTPropertyTUBE", out error, tableId, name, ax, d, wf, tf, iz, iy, ix, ay, az);
        }

        internal static int AddUPTPropertyGeneral(object propertyObj, int tableId, string name, double ax, double d, double td, double b, double tb, double iz, double iy, double ix, double sz, double sy, double ay, double az, double pz, double py, double hss, double dee, out string error)
        {
            return InvokeInt(propertyObj, "AddUPTPropertyGENERAL", out error, tableId, name, ax, d, td, b, tb, iz, iy, ix, sz, sy, ay, az, pz, py, hss, dee);
        }

        internal static int AddUPTPropertyISection(object propertyObj, int tableId, string name, double d, double tw, double d1, double wf, double tf, double wf1, double tf1, double ay, double az, double xi, out string error)
        {
            return InvokeInt(propertyObj, "AddUPTPropertyISECTION", out error, tableId, name, d, tw, d1, wf, tf, wf1, tf1, ay, az, xi);
        }

        internal static int AddUPTPropertyPrismatic(object propertyObj, int tableId, string name, double ax, double iz, double iy, double ix, double ay, double az, double yd, double zd, out string error)
        {
            return InvokeInt(propertyObj, "AddUPTPropertyPRISMATIC", out error, tableId, name, ax, iz, iy, ix, ay, az, yd, zd);
        }

        internal static int CreatePropertyFromUPTTable(object propertyObj, int tableId, string sectionName, out string error)
        {
            return InvokeInt(propertyObj, "CreatePropertyFromUPTTable", out error, tableId, sectionName);
        }
    }
}
