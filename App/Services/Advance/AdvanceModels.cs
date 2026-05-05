using System;

namespace StaadSteelApp.Core.Advance
{
    internal static class AdvanceConstants
    {
        internal const int BaseUnitEnglish = 1;
        internal const int BaseUnitMetric = 2;
        internal const double AxisEpsilon = 1e-9;
        internal const double CoordEpsilon = 1e-6;
    }

    internal struct AdvancePoint3D
    {
        internal AdvancePoint3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        internal double X;
        internal double Y;
        internal double Z;
    }

    internal sealed class AdvanceNodeData
    {
        internal int Id { get; set; }
        internal AdvancePoint3D Point { get; set; }
    }

    internal readonly struct AdvanceEdgeKey : IEquatable<AdvanceEdgeKey>
    {
        internal AdvanceEdgeKey(int nodeA, int nodeB)
        {
            if (nodeA <= nodeB)
            {
                NodeA = nodeA;
                NodeB = nodeB;
            }
            else
            {
                NodeA = nodeB;
                NodeB = nodeA;
            }
        }

        internal int NodeA { get; }
        internal int NodeB { get; }

        public bool Equals(AdvanceEdgeKey other)
        {
            return NodeA == other.NodeA && NodeB == other.NodeB;
        }

        public override bool Equals(object obj)
        {
            return obj is AdvanceEdgeKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (NodeA * 397) ^ NodeB;
            }
        }
    }
}
