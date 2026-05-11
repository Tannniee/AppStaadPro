using System.Globalization;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private double Parse(string s)
        {
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                return value;

            if (double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                return value;

            return 0;
        }

        private double ParseLegacyLength(string s)
        {
            return Parse(s) / 1000.0;
        }

        private double ParseLegacyValue(string s)
        {
            return Parse(s);
        }
    }
}
