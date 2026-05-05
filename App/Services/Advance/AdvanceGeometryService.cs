using System;
using System.Collections.Generic;
using System.Globalization;

namespace StaadSteelApp.Core.Advance
{
    internal static class AdvanceGeometryService
    {
        internal static AdvancePoint3D ProjectPointToSegment(AdvancePoint3D point, AdvancePoint3D start, AdvancePoint3D end)
        {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double dz = end.Z - start.Z;
            double len2 = dx * dx + dy * dy + dz * dz;
            if (len2 < AdvanceConstants.AxisEpsilon)
                return start;

            double t =
                ((point.X - start.X) * dx +
                 (point.Y - start.Y) * dy +
                 (point.Z - start.Z) * dz) / len2;

            if (t < 0)
                t = 0;
            else if (t > 1)
                t = 1;

            return new AdvancePoint3D(
                start.X + t * dx,
                start.Y + t * dy,
                start.Z + t * dz);
        }

        internal static bool TryBuildRotation(
            AdvancePoint3D fromVector,
            AdvancePoint3D toVector,
            out AdvancePoint3D rotationAxis,
            out double rotationAngleRad,
            out string error)
        {
            rotationAxis = default;
            rotationAngleRad = 0;
            error = string.Empty;

            if (!TryNormalize(fromVector, out AdvancePoint3D fromUnit))
            {
                error = "Direction nodes are identical or too close. Cannot define current frame direction.";
                return false;
            }

            if (!TryNormalize(toVector, out AdvancePoint3D toUnit))
            {
                error = "Target axis is invalid.";
                return false;
            }

            double dot = Clamp(Dot(fromUnit, toUnit), -1.0, 1.0);
            AdvancePoint3D cross = Cross(fromUnit, toUnit);
            double crossLength = Length(cross);

            if (crossLength <= AdvanceConstants.AxisEpsilon)
            {
                if (dot >= 1.0 - AdvanceConstants.CoordEpsilon)
                {
                    rotationAxis = new AdvancePoint3D(1, 0, 0);
                    rotationAngleRad = 0;
                    return true;
                }

                AdvancePoint3D helper = Math.Abs(fromUnit.X) < 0.9
                    ? new AdvancePoint3D(1, 0, 0)
                    : new AdvancePoint3D(0, 1, 0);
                cross = Cross(fromUnit, helper);
                if (!TryNormalize(cross, out rotationAxis))
                {
                    error = "Could not compute rotation axis.";
                    return false;
                }

                rotationAngleRad = Math.PI;
                return true;
            }

            rotationAxis = Scale(cross, 1.0 / crossLength);
            rotationAngleRad = Math.Acos(dot);
            return true;
        }

        internal static AdvancePoint3D GetAxisVector(AlignKeepAxis axis)
        {
            switch (axis)
            {
                case AlignKeepAxis.X: return new AdvancePoint3D(1, 0, 0);
                case AlignKeepAxis.Y: return new AdvancePoint3D(0, 1, 0);
                default: return new AdvancePoint3D(0, 0, 1);
            }
        }

        internal static bool TryComputeAlignedPoint(
            AdvancePoint3D a,
            AdvancePoint3D b,
            AdvancePoint3D root,
            AlignKeepAxis keepAxis,
            out AdvancePoint3D alignedPoint,
            out string error)
        {
            alignedPoint = default;
            error = string.Empty;

            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double dz = b.Z - a.Z;

            double len2 = dx * dx + dy * dy + dz * dz;
            if (len2 < AdvanceConstants.AxisEpsilon)
            {
                error = "The two line points are identical or too close. Cannot define a line.";
                return false;
            }

            double aAxis = GetAxisValue(a, keepAxis);
            double bAxis = GetAxisValue(b, keepAxis);
            double rAxis = GetAxisValue(root, keepAxis);
            double dAxis = bAxis - aAxis;

            double t;
            if (Math.Abs(dAxis) < AdvanceConstants.AxisEpsilon)
            {
                if (Math.Abs(rAxis - aAxis) > AdvanceConstants.CoordEpsilon)
                {
                    error = $"Cannot keep {keepAxis} of root because line nodes have constant {keepAxis} and that value is different from root.";
                    return false;
                }

                t = ((root.X - a.X) * dx + (root.Y - a.Y) * dy + (root.Z - a.Z) * dz) / len2;
            }
            else
            {
                t = (rAxis - aAxis) / dAxis;
            }

            alignedPoint = new AdvancePoint3D(
                a.X + t * dx,
                a.Y + t * dy,
                a.Z + t * dz);

            return true;
        }

        internal static double GetAxisValue(AdvancePoint3D point, AlignKeepAxis axis)
        {
            switch (axis)
            {
                case AlignKeepAxis.X: return point.X;
                case AlignKeepAxis.Y: return point.Y;
                default: return point.Z;
            }
        }

        internal static string FormatCoordinate(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        internal static AdvancePoint3D RotateAroundAxis(AdvancePoint3D vector, AdvancePoint3D axisUnit, double angleRad)
        {
            if (Math.Abs(angleRad) <= AdvanceConstants.AxisEpsilon)
                return vector;

            double cos = Math.Cos(angleRad);
            double sin = Math.Sin(angleRad);
            AdvancePoint3D cross = Cross(axisUnit, vector);
            double dot = Dot(axisUnit, vector);

            return new AdvancePoint3D(
                vector.X * cos + cross.X * sin + axisUnit.X * dot * (1 - cos),
                vector.Y * cos + cross.Y * sin + axisUnit.Y * dot * (1 - cos),
                vector.Z * cos + cross.Z * sin + axisUnit.Z * dot * (1 - cos));
        }

        internal static bool TryNormalize(AdvancePoint3D vector, out AdvancePoint3D normalized)
        {
            normalized = default;
            double length = Length(vector);
            if (length <= AdvanceConstants.AxisEpsilon)
                return false;

            normalized = Scale(vector, 1.0 / length);
            return true;
        }

        internal static double Length(AdvancePoint3D vector)
        {
            return Math.Sqrt(Dot(vector, vector));
        }

        internal static AdvancePoint3D Scale(AdvancePoint3D vector, double factor)
        {
            return new AdvancePoint3D(vector.X * factor, vector.Y * factor, vector.Z * factor);
        }

        internal static AdvancePoint3D Add(AdvancePoint3D left, AdvancePoint3D right)
        {
            return new AdvancePoint3D(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        internal static AdvancePoint3D Subtract(AdvancePoint3D left, AdvancePoint3D right)
        {
            return new AdvancePoint3D(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }

        internal static double Dot(AdvancePoint3D left, AdvancePoint3D right)
        {
            return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
        }

        internal static AdvancePoint3D Cross(AdvancePoint3D left, AdvancePoint3D right)
        {
            return new AdvancePoint3D(
                left.Y * right.Z - left.Z * right.Y,
                left.Z * right.X - left.X * right.Z,
                left.X * right.Y - left.Y * right.X);
        }

        internal static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }

        internal static int CompareNodeByAxis(AdvanceNodeData left, AdvanceNodeData right, AlignKeepAxis axis)
        {
            int axisCompare = GetAxisValue(left.Point, axis).CompareTo(GetAxisValue(right.Point, axis));
            if (axisCompare != 0)
                return axisCompare;

            return left.Id.CompareTo(right.Id);
        }

        internal static List<List<AdvanceNodeData>> BuildGapGroups(IReadOnlyList<AdvanceNodeData> nodes, AlignKeepAxis gapAxis, double gapLimit)
        {
            var ordered = new List<AdvanceNodeData>(nodes);
            ordered.Sort((left, right) => CompareNodeByAxis(left, right, gapAxis));

            var groups = new List<List<AdvanceNodeData>>();
            if (ordered.Count == 0)
                return groups;

            var currentGroup = new List<AdvanceNodeData> { ordered[0] };
            double groupStartValue = GetAxisValue(ordered[0].Point, gapAxis);

            for (int i = 1; i < ordered.Count; i++)
            {
                AdvanceNodeData node = ordered[i];
                double axisValue = GetAxisValue(node.Point, gapAxis);
                if ((axisValue - groupStartValue) <= gapLimit + AdvanceConstants.CoordEpsilon)
                {
                    currentGroup.Add(node);
                    continue;
                }

                groups.Add(currentGroup);
                currentGroup = new List<AdvanceNodeData> { node };
                groupStartValue = axisValue;
            }

            groups.Add(currentGroup);
            return groups;
        }

        internal static List<AdvanceEdgeKey> BuildChainEdges(IReadOnlyList<AdvanceNodeData> nodes)
        {
            var edges = new List<AdvanceEdgeKey>(Math.Max(0, nodes.Count - 1));
            for (int i = 1; i < nodes.Count; i++)
            {
                AdvanceNodeData previous = nodes[i - 1];
                AdvanceNodeData current = nodes[i];
                if (previous.Id == current.Id)
                    continue;

                edges.Add(new AdvanceEdgeKey(previous.Id, current.Id));
            }

            return edges;
        }
    }
}
