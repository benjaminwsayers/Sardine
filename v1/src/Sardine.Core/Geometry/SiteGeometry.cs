using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;

namespace Sardine.Core.Geometry
{
    /// <summary>
    /// Pure geometry helpers for site boundary analysis. No document state.
    /// </summary>
    public static class SiteGeometry
    {
        /// <summary>Planar area of a closed polyline. Returns 0.0 if the computation fails.</summary>
        public static double ComputeArea(Polyline polyline)
        {
            if (polyline == null || polyline.Count < 4) return 0.0;
            var curve = new PolylineCurve(polyline);
            var amp = AreaMassProperties.Compute(curve);
            return amp != null ? amp.Area : 0.0;
        }

        /// <summary>Total length of a polyline.</summary>
        public static double ComputePerimeter(Polyline polyline)
        {
            if (polyline == null) return 0.0;
            double length = 0.0;
            for (int i = 0; i < polyline.Count - 1; i++)
                length += polyline[i].DistanceTo(polyline[i + 1]);
            return length;
        }

        /// <summary>
        /// Area centroid of a closed polyline. Falls back to the bounding box centre
        /// when mass properties cannot be computed.
        /// </summary>
        public static Point3d ComputeCentroid(Polyline polyline)
        {
            if (polyline != null && polyline.Count >= 4)
            {
                var amp = AreaMassProperties.Compute(new PolylineCurve(polyline));
                if (amp != null) return amp.Centroid;
            }
            return new BoundingBox(polyline ?? new Polyline()).Center;
        }

        /// <summary>Area centroid of a closed curve, or the bounding box centre as a fallback.</summary>
        public static Point3d ComputeCentroid(Curve curve)
        {
            if (curve == null) return Point3d.Origin;
            var amp = AreaMassProperties.Compute(curve);
            if (amp != null) return amp.Centroid;
            return curve.GetBoundingBox(true).Center;
        }

        /// <summary>Axis-aligned bounding box of a polyline.</summary>
        public static BoundingBox ComputeBoundingBox(Polyline polyline)
        {
            return new BoundingBox(polyline);
        }

        /// <summary>
        /// Extracts a polyline from any curve. Non-polyline curves are approximated
        /// with <see cref="Tolerances.PolylineConversionDivisions"/> divisions
        /// (proof-of-concept behaviour). Returns null when conversion fails.
        /// </summary>
        public static Polyline ToPolyline(Curve curve, out bool wasConverted)
        {
            wasConverted = false;
            if (curve == null) return null;

            Polyline pl;
            if (curve.TryGetPolyline(out pl))
                return pl;

            wasConverted = true;
            Point3d[] pts;
            curve.DivideByCount(Tolerances.PolylineConversionDivisions, true, out pts);
            if (pts == null || pts.Length < 3) return null;

            var result = new Polyline(pts);
            if (!result.IsClosed) result.Add(pts[0]);
            return result;
        }

        /// <summary>
        /// Dominant edge directions of a closed polyline: edge directions are folded
        /// into the half-plane, clustered within
        /// <see cref="Tolerances.DirectionClusterAngleDeg"/> and weighted by length.
        /// Returns up to <paramref name="maxDirections"/> unit vectors, strongest first.
        /// Always returns at least one direction (World X as a last resort).
        /// </summary>
        public static List<Vector3d> DominantDirections(Polyline polyline, int maxDirections)
        {
            if (maxDirections < 1) maxDirections = 1;

            var edgeDirs = new List<Vector3d>();
            var edgeLens = new List<double>();

            if (polyline != null)
            {
                for (int i = 0; i < polyline.Count - 1; i++)
                {
                    var vec = polyline[i + 1] - polyline[i];
                    double len = vec.Length;
                    if (len < Tolerances.MinEdgeForDirection) continue;
                    vec.Unitize();

                    // Fold into [0°, 180°): opposite directions share an axis.
                    if (vec.X < -Tolerances.VectorZero ||
                        (Math.Abs(vec.X) < Tolerances.VectorZero && vec.Y < 0.0))
                        vec = -vec;

                    edgeDirs.Add(vec);
                    edgeLens.Add(len);
                }
            }

            if (edgeDirs.Count == 0)
                return new List<Vector3d> { Vector3d.XAxis };

            var clusterDirs = new List<Vector3d>();
            var clusterLens = new List<double>();

            for (int e = 0; e < edgeDirs.Count; e++)
            {
                bool merged = false;
                for (int c = 0; c < clusterDirs.Count; c++)
                {
                    double dot = Math.Max(-1.0, Math.Min(1.0,
                        edgeDirs[e].X * clusterDirs[c].X + edgeDirs[e].Y * clusterDirs[c].Y));
                    double angle = RhinoMath.ToDegrees(Math.Acos(dot));

                    if (angle < Tolerances.DirectionClusterAngleDeg)
                    {
                        var nd = clusterDirs[c] * clusterLens[c] + edgeDirs[e] * edgeLens[e];
                        nd.Unitize();
                        clusterDirs[c] = nd;
                        clusterLens[c] += edgeLens[e];
                        merged = true;
                        break;
                    }
                }
                if (!merged)
                {
                    clusterDirs.Add(edgeDirs[e]);
                    clusterLens.Add(edgeLens[e]);
                }
            }

            // Stable sort by weight descending: ties keep first-seen order.
            var order = new List<int>();
            for (int i = 0; i < clusterDirs.Count; i++) order.Add(i);
            order.Sort((a, b) =>
            {
                int cmp = clusterLens[b].CompareTo(clusterLens[a]);
                return cmp != 0 ? cmp : a.CompareTo(b);
            });

            var result = new List<Vector3d>();
            for (int i = 0; i < order.Count && i < maxDirections; i++)
                result.Add(clusterDirs[order[i]]);
            return result;
        }

        /// <summary>
        /// Dominant directions of any closed curve. Non-polyline curves are sampled
        /// with <see cref="Tolerances.DirectionSamples"/> divisions.
        /// </summary>
        public static List<Vector3d> DominantDirections(Curve curve, int maxDirections)
        {
            Polyline pl;
            if (curve != null && curve.TryGetPolyline(out pl))
                return DominantDirections(pl, maxDirections);

            if (curve == null)
                return new List<Vector3d> { Vector3d.XAxis };

            Point3d[] pts;
            curve.DivideByCount(Tolerances.DirectionSamples, true, out pts);
            if (pts == null || pts.Length < 3)
                return new List<Vector3d> { Vector3d.XAxis };

            var poly = new Polyline(pts);
            poly.Add(pts[0]);
            return DominantDirections(poly, maxDirections);
        }

        /// <summary>Angle of a vector from World X in degrees, in (-180, 180].</summary>
        public static double AngleFromWorldXDeg(Vector3d v)
        {
            return RhinoMath.ToDegrees(Math.Atan2(v.Y, v.X));
        }

        /// <summary>Rotates a vector about World Z by the given angle in degrees.</summary>
        public static Vector3d RotateInXY(Vector3d v, double degrees)
        {
            double rad = RhinoMath.ToRadians(degrees);
            double c = Math.Cos(rad);
            double s = Math.Sin(rad);
            return new Vector3d(v.X * c - v.Y * s, v.X * s + v.Y * c, 0.0);
        }
    }
}
