using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;
using Sardine.Core.Geometry;
using Sardine.Core.Models;

namespace Sardine.Core.Site
{
    /// <summary>
    /// Boundary rationalisation for Sardine.Site (proof-of-concept 01_SiteRationaliser).
    ///
    /// Merges consecutive boundary segments that are shorter than the minimum edge
    /// length or within the merge angle of being collinear, including the wrap-around
    /// seam at the polygon close point. Error and warning texts are part of the
    /// approved manual validation record (docs/manual-validation/Sardine.Site.md)
    /// and must not be changed casually.
    /// </summary>
    public static class SiteRationaliser
    {
        public const string ErrorNoBoundary = "No boundary provided.";
        public const string ErrorNotClosed = "Boundary must be a closed curve.";
        public const string ErrorNotPlanar = "Boundary must be planar.";
        public const string ErrorInvalidParameters = "MergeAngle and MinEdgeLength must be zero or greater.";
        public const string ErrorConversionFailed = "Could not convert boundary to polyline.";
        public const string ErrorTooFewEdges = "Boundary rationalisation produced fewer than three valid edges.";
        public const string ErrorInvalidGeometry = "Boundary rationalisation produced invalid geometry.";
        public const string ErrorDegenerate = "Boundary rationalisation produced degenerate geometry.";

        public const string WarningConverted =
            "Input was not a polyline — approximated using DivideByCount(200). " +
            "For best results supply an actual polyline from your DXF/DWG.";

        /// <summary>
        /// Rationalises <paramref name="boundary"/>. Returns the site boundary, or
        /// null with <paramref name="error"/> populated when the input is invalid.
        /// Never throws for invalid geometry.
        /// </summary>
        public static SiteBoundary Rationalise(Curve boundary, RationalisationParameters parameters, out string error)
        {
            error = null;
            if (parameters == null) parameters = new RationalisationParameters();
            var warnings = new List<string>();

            // ── Input validation ────────────────────────────────────────────
            if (boundary == null) { error = ErrorNoBoundary; return null; }
            if (!boundary.IsClosed) { error = ErrorNotClosed; return null; }
            if (!boundary.IsPlanar()) { error = ErrorNotPlanar; return null; }

            if (!IsFiniteNonNegative(parameters.MergeAngleDeg) || !IsFiniteNonNegative(parameters.MinEdgeLength))
            {
                error = ErrorInvalidParameters;
                return null;
            }

            // ── Convert to polyline ─────────────────────────────────────────
            bool converted;
            Polyline raw = SiteGeometry.ToPolyline(boundary, out converted);
            if (raw == null) { error = ErrorConversionFailed; return null; }
            if (converted) warnings.Add(WarningConverted);
            if (!raw.IsClosed) raw.Add(raw[0]);

            // ── Raw segments ────────────────────────────────────────────────
            var rawSegments = new List<Line>();
            for (int i = 0; i < raw.Count - 1; i++)
            {
                var seg = new Line(raw[i], raw[i + 1]);
                if (seg.Length > RhinoMath.ZeroTolerance) rawSegments.Add(seg);
            }

            // ── Merge short / near-parallel segments ────────────────────────
            var merged = MergeSegments(rawSegments, parameters.MergeAngleDeg, parameters.MinEdgeLength);
            if (merged.Count < 3) { error = ErrorTooFewEdges; return null; }

            // ── Rebuild the working polyline ────────────────────────────────
            var points = new List<Point3d>(merged.Count + 1);
            foreach (var seg in merged) points.Add(seg.From);
            points.Add(points[0]);
            var working = new Polyline(points);

            if (!working.IsValid || !working.IsClosed) { error = ErrorInvalidGeometry; return null; }

            double area = SiteGeometry.ComputeArea(working);
            double perimeter = SiteGeometry.ComputePerimeter(working);
            if (!IsFinitePositive(area) || !IsFinitePositive(perimeter))
            {
                error = ErrorDegenerate;
                return null;
            }

            // ── Edges ───────────────────────────────────────────────────────
            var edges = new List<SiteEdge>(merged.Count);
            for (int i = 0; i < merged.Count; i++)
            {
                if (merged[i].Length < RhinoMath.ZeroTolerance) { error = ErrorDegenerate; return null; }
                edges.Add(new SiteEdge(i, merged[i]));
            }

            return new SiteBoundary(
                boundary.DuplicateCurve(),
                working,
                edges,
                area,
                perimeter,
                SiteGeometry.ComputeCentroid(working),
                SiteGeometry.ComputeBoundingBox(working),
                converted,
                warnings);
        }

        /// <summary>
        /// Walks the segments and absorbs each one that is shorter than
        /// <paramref name="minLength"/> or within <paramref name="mergeAngleDeg"/> of
        /// the running merged direction. Then merges the last segment into the first
        /// when they are near-parallel or the last is short (polygon seam).
        /// Zero thresholds disable the corresponding rule.
        /// </summary>
        public static List<Line> MergeSegments(List<Line> segments, double mergeAngleDeg, double minLength)
        {
            var result = new List<Line>();
            if (segments == null || segments.Count == 0) return result;

            double tolRad = RhinoMath.ToRadians(mergeAngleDeg);

            Point3d currentStart = segments[0].From;
            Point3d currentEnd = segments[0].To;
            Vector3d currentDir = segments[0].Direction;
            currentDir.Unitize();

            for (int i = 1; i < segments.Count; i++)
            {
                var seg = segments[i];
                Vector3d segDir = seg.Direction;
                segDir.Unitize();

                double angle = AngleBetweenAxes(currentDir, segDir);
                bool tooShort = seg.Length < minLength;
                bool nearParallel = angle < tolRad;

                if (tooShort || nearParallel)
                {
                    currentEnd = seg.To;
                    currentDir = currentEnd - currentStart;
                    currentDir.Unitize();
                }
                else
                {
                    result.Add(new Line(currentStart, currentEnd));
                    currentStart = seg.From;
                    currentEnd = seg.To;
                    currentDir = segDir;
                }
            }

            result.Add(new Line(currentStart, currentEnd));

            // Wrap-around: merge the last segment into the first across the seam.
            if (result.Count >= 2)
            {
                var first = result[0];
                var last = result[result.Count - 1];

                Vector3d d0 = first.Direction; d0.Unitize();
                Vector3d d1 = last.Direction; d1.Unitize();

                if (AngleBetweenAxes(d0, d1) < tolRad || last.Length < minLength)
                {
                    result[0] = new Line(last.From, first.To);
                    result.RemoveAt(result.Count - 1);
                }
            }

            return result;
        }

        /// <summary>Angle in radians between two directions treated as undirected axes (0..π/2).</summary>
        private static double AngleBetweenAxes(Vector3d a, Vector3d b)
        {
            double dot = Math.Abs(a.X * b.X + a.Y * b.Y);
            return Math.Acos(Math.Max(-1.0, Math.Min(1.0, dot)));
        }

        private static bool IsFiniteNonNegative(double v)
        {
            return !double.IsNaN(v) && !double.IsInfinity(v) && v >= 0.0;
        }

        private static bool IsFinitePositive(double v)
        {
            return !double.IsNaN(v) && !double.IsInfinity(v) && v > RhinoMath.ZeroTolerance;
        }
    }
}
