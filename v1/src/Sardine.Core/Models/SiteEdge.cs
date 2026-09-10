using System;
using Rhino;
using Rhino.Geometry;

namespace Sardine.Core.Models
{
    /// <summary>
    /// One rationalised edge of the working site boundary.
    /// Immutable after construction. Index order is the deterministic edge order
    /// exposed by Sardine.Site and used by the PerimeterEdges enable/disable list.
    /// </summary>
    public sealed class SiteEdge
    {
        /// <summary>Stable zero-based index in the rationalised boundary.</summary>
        public int Index { get; }

        /// <summary>The edge as a line segment, from start vertex to end vertex.</summary>
        public Line Geometry { get; }

        /// <summary>Unitised direction of the edge.</summary>
        public Vector3d Direction { get; }

        /// <summary>Edge length in metres.</summary>
        public double Length { get; }

        /// <summary>Midpoint of the edge.</summary>
        public Point3d Midpoint { get; }

        public SiteEdge(int index, Line geometry)
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
        }

        /// <summary>Shortest distance from a point to this edge segment.</summary>
        public double DistanceTo(Point3d point)
        {
            return Geometry.DistanceTo(point, true);
        }
    }
}
