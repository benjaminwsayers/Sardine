using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;

namespace Sardine.Core.Geometry
{
    /// <summary>
    /// Static geometry utilities for site boundary analysis.
    /// 
    /// All methods are pure functions — no side effects, no document state.
    /// Reusable by any component in the pipeline.
    /// </summary>
    public static class SiteGeometry
    {
        /// <summary>
        /// Computes the planar area of a closed polyline.
        /// Returns 0.0 if the computation fails.
        /// </summary>
        public static double ComputeArea(Polyline polyline)
        {
            var curve = polyline.ToNurbsCurve();
            if (curve == null) return 0.0;

            var amp = AreaMassProperties.Compute(curve);
            return amp != null ? amp.Area : 0.0;
        }

        /// <summary>
        /// Computes the total perimeter length of a polyline.
        /// </summary>
        public static double ComputePerimeter(Polyline polyline)
        {
            double length = 0.0;
            for (int i = 0; i < polyline.Count - 1; i++)
                length += polyline[i].DistanceTo(polyline[i + 1]);
            return length;
        }

        /// <summary>
        /// Computes the area centroid of a closed polyline.
        /// Falls back to the geometric centre of the bounding box if mass props fail.
        /// </summary>
        public static Point3d ComputeCentroid(Polyline polyline)
        {
            var curve = polyline.ToNurbsCurve();
            if (curve != null)
            {
                var amp = AreaMassProperties.Compute(curve);
                if (amp != null) return amp.Centroid;
            }

            // Fallback: bounding box centre
            var bb = new BoundingBox(polyline);
            return bb.Center;
        }

        /// <summary>
        /// Returns the axis-aligned bounding box of a polyline.
        /// </summary>
        public static BoundingBox ComputeBoundingBox(Polyline polyline)
        {
            return new BoundingBox(polyline);
        }

        /// <summary>
        /// Fits a plane to the polyline vertices.
        /// Falls back to WorldXY if fitting fails (e.g. fewer than 3 points).
        /// </summary>
        public static Plane ComputeBoundaryPlane(Polyline polyline)
        {
            Plane plane;
            var result = Plane.FitPlaneToPoints(polyline, out plane);
            return (result == PlaneFitResult.Success) ? plane : Plane.WorldXY;
        }

        /// <summary>
        /// Returns the dominant edge direction of a set of segments,
        /// defined as the direction of the longest segment.
        /// Used as the downstream layout orientation seed.
        /// </summary>
        public static Vector3d ComputePrimaryAxis(List<Line> segments)
        {
            if (segments == null || segments.Count == 0)
                return Vector3d.XAxis;

            Line longest = segments[0];
            foreach (var seg in segments)
                if (seg.Length > longest.Length) longest = seg;

            var axis = longest.Direction;
            axis.Unitize();
            return axis;
        }

        /// <summary>
        /// Returns the angle between a vector and World X, in degrees.
        /// Clamped to [0, 180].
        /// </summary>
        public static double AngleToWorldXDeg(Vector3d axis)
        {
            double dot = Math.Abs(Vector3d.Multiply(axis, Vector3d.XAxis));
            dot = Math.Max(-1.0, Math.Min(1.0, dot));
            return RhinoMath.ToDegrees(Math.Acos(dot));
        }
    }
} 