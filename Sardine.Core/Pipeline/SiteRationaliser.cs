using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Sardine.Core.Geometry;
using Sardine.Core.Models;

namespace Sardine.Core.Pipeline
{
    /// <summary>
    /// Runs the three-stage site rationalisation pipeline against a raw input curve
    /// and produces an immutable SiteBoundary.
    /// 
    /// Merges near-parallel and short consecutive boundary segments.
    /// </summary>
    public class SiteRationaliser
    {
        private readonly RationalisationParameters _params;

        public SiteRationaliser(RationalisationParameters parameters)
        {
            _params = parameters ?? throw new ArgumentNullException("parameters");
        }

        /// <summary>
        /// Rationalises the input boundary curve and returns a populated SiteBoundary.
        /// Returns null and sets <paramref name="error"/> on invalid input.
        /// </summary>
        /// <param name="boundary">Raw input curve from the GH component.</param>
        /// <param name="error">Populated with a user-facing message on failure, null on success.</param>
        public SiteBoundary Rationalise(Curve boundary, out string error)
        {
            error = null;
            var warnings = new List<string>();

            // ── Input validation ─────────────────────────────────────────────────
            if (boundary == null)
            {
                error = "No boundary provided.";
                return null;
            }

            if (!boundary.IsClosed)
            {
                error = "Boundary must be a closed curve.";
                return null;
            }

            if (!boundary.IsPlanar())
            {
                error = "Boundary must be planar.";
                return null;
            }

            if (double.IsNaN(_params.MergeAngleDeg) || double.IsInfinity(_params.MergeAngleDeg) ||
                double.IsNaN(_params.MinEdgeLength) || double.IsInfinity(_params.MinEdgeLength) ||
                _params.MergeAngleDeg < 0.0 || _params.MinEdgeLength < 0.0)
            {
                error = "MergeAngle and MinEdgeLength must be zero or greater.";
                return null;
            }

            // ── Convert to polyline ──────────────────────────────────────────────
            bool inputWasConverted;
            Polyline raw = ToPolyline(boundary, out inputWasConverted);

            if (raw == null)
            {
                error = "Could not convert boundary to polyline.";
                return null;
            }

            if (inputWasConverted)
                warnings.Add("Input was not a polyline — approximated using DivideByCount(200). " +
                             "For best results supply an actual polyline from your DXF/DWG.");

            if (!raw.IsClosed)
                raw.Add(raw[0]);

            // Build segments for the proven merge and short-edge pass.
            var rawSegs = new List<Line>();
            for (int i = 0; i < raw.Count - 1; i++)
                rawSegs.Add(new Line(raw[i], raw[i + 1]));

            // ── R2 + R3: Collinearity merge and short edge cull ──────────────────
            var mergedSegs = MergeSegments(rawSegs, _params.MergeAngleDeg, _params.MinEdgeLength);

            if (mergedSegs.Count < 3)
            {
                error = "Boundary rationalisation produced fewer than three valid edges.";
                return null;
            }

            // ── Rebuild clean working polyline ───────────────────────────────────
            var cleanPts = new List<Point3d>();
            foreach (var seg in mergedSegs)
                cleanPts.Add(seg.From);
            cleanPts.Add(cleanPts[0]);
            var working = new Polyline(cleanPts);

            if (!working.IsValid || !working.IsClosed)
            {
                error = "Boundary rationalisation produced invalid geometry.";
                return null;
            }

            // ── Compute spatial properties ───────────────────────────────────────
            double area = SiteGeometry.ComputeArea(working);
            double perimeter = SiteGeometry.ComputePerimeter(working);

            if (double.IsNaN(area) || double.IsInfinity(area) || area <= Rhino.RhinoMath.ZeroTolerance ||
                double.IsNaN(perimeter) || double.IsInfinity(perimeter) || perimeter <= Rhino.RhinoMath.ZeroTolerance)
            {
                error = "Boundary rationalisation produced degenerate geometry.";
                return null;
            }
            Point3d centroid = SiteGeometry.ComputeCentroid(working);
            var boundingBox = SiteGeometry.ComputeBoundingBox(working);
            Plane plane = SiteGeometry.ComputeBoundaryPlane(working);

            // ── Compute orientation seed ─────────────────────────────────────────
            Vector3d primaryAxis = SiteGeometry.ComputePrimaryAxis(mergedSegs);
            double primaryAngle = SiteGeometry.AngleToWorldXDeg(primaryAxis);

            // ── Build edges ──────────────────────────────────────────────────────
            var edges = new List<SiteEdge>();
            for (int i = 0; i < mergedSegs.Count; i++)
            {
                edges.Add(new SiteEdge(i, mergedSegs[i], primaryAxis));
            }

            // ── Assemble and return ──────────────────────────────────────────────
            return new SiteBoundary.Builder()
                .SetBoundaries(boundary.DuplicateCurve(), working)
                .SetEdges(edges)
                .SetSpatialProperties(area, perimeter, centroid, boundingBox, plane)
                .SetOrientation(primaryAxis, primaryAngle)
                .SetDiagnostics(inputWasConverted, warnings)
                .Build();
        }

        /// <summary>
        /// Walks merged segments and combines consecutive segments that are either
        /// shorter than <paramref name="minLen"/> or within <paramref name="mergeAngleDeg"/>
        /// of being collinear. Handles wrap-around at the polygon close point.
        /// </summary>
        private static List<Line> MergeSegments(List<Line> segs, double mergeAngleDeg, double minLen)
        {
            if (segs.Count == 0) return segs;

            double tolRad = mergeAngleDeg * Math.PI / 180.0;
            var result = new List<Line>();

            Point3d currentStart = segs[0].From;
            Point3d currentEnd = segs[0].To;
            Vector3d currentDir = segs[0].Direction;
            currentDir.Unitize();

            for (int i = 1; i < segs.Count; i++)
            {
                var seg = segs[i];
                Vector3d segDir = seg.Direction;
                segDir.Unitize();

                double dot = Math.Abs(currentDir.X * segDir.X + currentDir.Y * segDir.Y);
                double angle = Math.Acos(Math.Max(-1.0, Math.Min(1.0, dot)));

                if (seg.Length < minLen || angle < tolRad)
                {
                    // Absorb: extend current segment to include this one
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

            // Wrap-around: merge last segment into first if near-parallel
            if (result.Count >= 2)
            {
                var first = result[0];
                var last = result[result.Count - 1];

                Vector3d d0 = first.Direction; d0.Unitize();
                Vector3d d1 = last.Direction; d1.Unitize();

                double dot = Math.Abs(d0.X * d1.X + d0.Y * d1.Y);
                double angle = Math.Acos(Math.Max(-1.0, Math.Min(1.0, dot)));

                if (angle < tolRad || last.Length < minLen)
                {
                    result[0] = new Line(last.From, first.To);
                    result.RemoveAt(result.Count - 1);
                }
            }

            return result;
        }

        // ── Input conversion ─────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to extract a polyline from any curve type.
        /// Falls back to DivideByCount(200) approximation for non-polyline curves.
        /// </summary>
        private static Polyline ToPolyline(Curve boundary, out bool wasConverted)
        {
            Polyline pl;
            if (boundary.TryGetPolyline(out pl))
            {
                wasConverted = false;
                return pl;
            }

            wasConverted = true;
            Point3d[] pts;
            boundary.DivideByCount(200, true, out pts);

            if (pts == null || pts.Length < 3)
                return null;

            var result = new Polyline(pts);
            if (!result.IsClosed)
                result.Add(pts[0]);
            return result;
        }
    }
}