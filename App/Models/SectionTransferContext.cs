using System;
using System.Linq;

namespace StaadSteelApp.Core
{
    /// <summary>
    /// Metadata captured with a copied section. Property reference numbers are
    /// model-local, so the source model key and raw property values are kept to
    /// resolve an equivalent property safely in the destination model.
    /// </summary>
    public sealed class SectionTransferContext
    {
        public string SourceModelKey { get; set; }
        public SectionPropertyFingerprint SourceFingerprint { get; set; }

        public SectionTransferContext Clone()
        {
            return new SectionTransferContext
            {
                SourceModelKey = SourceModelKey ?? string.Empty,
                SourceFingerprint = SourceFingerprint?.Clone()
            };
        }
    }

    public sealed class SectionPropertyFingerprint
    {
        public int TypeCode { get; set; }
        public double[] Values { get; set; } = Array.Empty<double>();

        public SectionPropertyFingerprint Clone()
        {
            return new SectionPropertyFingerprint
            {
                TypeCode = TypeCode,
                Values = Values?.ToArray() ?? Array.Empty<double>()
            };
        }
    }
}
