using System;
using Rhino;
using Rhino.Geometry;

namespace Sardine.Core.Models
{
    /// <summary>
    /// Represents a single rationalised edge of a site boundary.
    /// Immutable after construction.
    /// </summary>
    public class SiteEdge
    {
        /// <summary>Stable zero-based index in the rationalised boundary.</summary>
        public int Index { get; private set; }

        /// <summary>The edge as a Line segment.</summary>
        public Line Geometry { get; private set; }

        /// <summary>Unitised direction vector of the edge.</summary>
        public Vector3d Direction { get; private set; }

        /// <summary>Edge length in model units.</summary>
        public double Length { get; private set; }

        /// <summary>Midpoint of the edge.</summary>
        public Point3d Midpoint { get; private set; }

        /// <summary>
        /// Angle between this edge direction and the site primary axis, in degrees.
        /// </summary>
        public double AngleToPrimaryAxisDeg { get; private set; }

        /// <summary>
        /// Constructs a SiteEdge and computes all derived properties.
        /// </summary>
        /// <param name="index">Zero-based edge index.</param>
        /// <param name="geometry">The edge line segment.</param>
        /// <param name="primaryAxis">Site primary axis for angle computation.</param>
        public SiteEdge(int index, Line geometry, Vector3d primaryAxis)
        {
            if (geometry.Length < RhinoMath.ZeroTolerance)
                throw new ArgumentException(
                    string.Format("SiteEdge {0}: geometry has zero or near-zero length.", index));

            Index = index;
            Geometry = geometry;
            Length = geometry.Length;
            Midpoint = geometry.PointAt(0.5);
            var dir = geometry.Direction;
            dir.Unitize();
            Direction = dir;

            AngleToPrimaryAxisDeg = ComputeAngleToPrimaryAxis(dir, primaryAxis);
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private static double ComputeAngleToPrimaryAxis(Vector3d edgeDir, Vector3d primaryAxis)
        {
            // Use Abs so we treat antiparallel edges the same as parallel
            double dot = Math.Abs(Vector3d.Multiply(edgeDir, primaryAxis));
            dot = Math.Max(-1.0, Math.Min(1.0, dot)); // guard float drift
            return RhinoMath.ToDegrees(Math.Acos(dot));
        }
    }
}