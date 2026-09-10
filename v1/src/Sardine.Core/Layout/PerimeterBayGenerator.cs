using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Sardine.Core.Geometry;
using Sardine.Core.Models;

namespace Sardine.Core.Layout
{
    /// <summary>
    /// Perimeter bay generation (proof-of-concept 03_PerimeterBays).
    ///
    /// Phase 1 walks the perimeter alignment at the bay width, placing a candidate
    /// bay of the configured depth extending outward toward the site boundary.
    /// Phase 2 culls overlapping neighbours (including the closed-perimeter seam),
    /// removing the bay further from its edge midpoint, i.e. the bay nearest a corner.
    /// Phase 3 removes any bay with a corner outside the containment boundary.
    /// Surviving bays keep walk order and receive sequential indices per edge.
    /// </summary>
    public static class PerimeterBayGenerator
    {
        private sealed class Candidate
        {
            public Bay Bay;
            public int EdgeIndex;
            public Point3d Origin;
        }

        /// <summary>
        /// Generates perimeter bays.
        /// </summary>
        /// <param name="perimeterInner">Alignment walked by the generator (working boundary inset by bay depth).</param>
        /// <param name="site">Rationalised site; its edges define the enable/disable indices.</param>
        /// <param name="containment">Boundary every surviving bay must lie within (normally the raw boundary).</param>
        /// <param name="bayWidth">Bay width in metres.</param>
        /// <param name="bayDepth">Bay depth in metres.</param>
        /// <param name="edgesEnabled">One flag per site edge; missing entries default to true.</param>
        /// <param name="tolerance">Geometric tolerance.</param>
        public static List<Bay> Generate(
            Curve perimeterInner,
            SiteBoundary site,
            Curve containment,
            double bayWidth,
            double bayDepth,
            IList<bool> edgesEnabled,
            double tolerance)
        {
            var result = new List<Bay>();
            if (perimeterInner == null || site == null || bayWidth <= 0.0 || bayDepth <= 0.0)
                return result;

            double totalLength = perimeterInner.GetLength();
            if (totalLength <= bayWidth) return result;

            // Bays must extend outward from the inset alignment toward the site
            // boundary, regardless of the direction the curve happens to run in.
            double outwardSign = OutwardSign(perimeterInner);

            // ── Phase 1: walk and collect candidates ────────────────────────
            var candidates = new List<Candidate>();
            double walk = bayWidth * 0.5;
            double walkEnd = totalLength - bayWidth * 0.5;

            while (walk <= walkEnd)
            {
                double t;
                if (!perimeterInner.LengthParameter(walk, out t))
                {
                    walk += bayWidth;
                    continue;
                }

                Point3d origin = perimeterInner.PointAt(t);
                Vector3d tangent = perimeterInner.TangentAt(t);
                tangent.Z = 0.0;
                if (!tangent.Unitize())
                {
                    walk += bayWidth;
                    continue;
                }

                Vector3d normal = Vector3d.CrossProduct(Vector3d.ZAxis, tangent) * outwardSign;
                normal.Unitize();

                var frame = new Plane(origin, tangent, normal);
                var corners = BayGeometry.RectangleCorners(
                    frame,
                    new Interval(-bayWidth * 0.5, bayWidth * 0.5),
                    new Interval(0.0, bayDepth));

                // Outer face midpoint lies on the parent site edge; use it to classify.
                Point3d outerMid = origin + normal * bayDepth;
                int edgeIndex = NearestEdgeIndex(site.Edges, outerMid);

                if (IsEdgeEnabled(edgesEnabled, edgeIndex))
                {
                    candidates.Add(new Candidate
                    {
                        Bay = new Bay(BayKind.Perimeter, edgeIndex, 0, frame, corners, true),
                        EdgeIndex = edgeIndex,
                        Origin = origin
                    });
                }

                walk += bayWidth;
            }

            // ── Phase 2: overlap cull between consecutive candidates ────────
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < candidates.Count - 1; i++)
                {
                    if (BayGeometry.Overlap(candidates[i].Bay, candidates[i + 1].Bay, tolerance))
                    {
                        int remove = PickRemoval(candidates[i], candidates[i + 1], site);
                        candidates.RemoveAt(remove == 0 ? i : i + 1);
                        changed = true;
                        break; // restart so neighbours are rechecked
                    }
                }
            }

            // Closed-perimeter seam: last candidate against first.
            if (candidates.Count >= 2)
            {
                var first = candidates[0];
                var last = candidates[candidates.Count - 1];
                if (BayGeometry.Overlap(first.Bay, last.Bay, tolerance))
                {
                    int remove = PickRemoval(last, first, site);
                    if (remove == 0) candidates.RemoveAt(candidates.Count - 1);
                    else candidates.RemoveAt(0);
                }
            }

            // ── Phase 3: containment cull ───────────────────────────────────
            if (containment != null)
            {
                var kept = new List<Candidate>(candidates.Count);
                foreach (var c in candidates)
                    if (BayGeometry.AllCornersInsideOrOn(c.Bay.Corners, containment, tolerance))
                        kept.Add(c);
                candidates = kept;
            }

            // ── Output: sequential bay index per edge in walk order ─────────
            var perEdge = new Dictionary<int, int>();
            foreach (var c in candidates)
            {
                int n;
                perEdge.TryGetValue(c.EdgeIndex, out n);
                result.Add(c.Bay.WithAddress(BayKind.Perimeter, c.EdgeIndex, n));
                perEdge[c.EdgeIndex] = n + 1;
            }

            return result;
        }

        /// <summary>
        /// +1 when the left-hand normal (Z × tangent) of the alignment points away
        /// from the enclosed region, −1 when it points into it.
        /// </summary>
        private static double OutwardSign(Curve alignment)
        {
            var orientation = alignment.ClosedCurveOrientation(Plane.WorldXY);
            if (orientation == CurveOrientation.CounterClockwise) return -1.0; // left is inside
            if (orientation == CurveOrientation.Clockwise) return 1.0;         // left is outside

            // Undetermined orientation: probe a point on the left of the start.
            double t = alignment.Domain.Mid;
            Point3d p = alignment.PointAt(t);
            Vector3d tan = alignment.TangentAt(t);
            tan.Z = 0.0;
            if (!tan.Unitize()) return 1.0;
            Vector3d left = Vector3d.CrossProduct(Vector3d.ZAxis, tan);
            var probe = p + left * Tolerances.Geometry * 10.0;
            return alignment.Contains(probe, Plane.WorldXY, Tolerances.Geometry) == PointContainment.Inside ? -1.0 : 1.0;
        }

        private static int NearestEdgeIndex(IReadOnlyList<SiteEdge> edges, Point3d point)
        {
            int best = 0;
            double bestDist = double.MaxValue;
            for (int i = 0; i < edges.Count; i++)
            {
                double d = edges[i].DistanceTo(point);
                if (d < bestDist) { bestDist = d; best = i; } // ties keep the lowest index
            }
            return best;
        }

        private static bool IsEdgeEnabled(IList<bool> edgesEnabled, int edgeIndex)
        {
            if (edgesEnabled == null || edgeIndex < 0 || edgeIndex >= edgesEnabled.Count) return true;
            return edgesEnabled[edgeIndex];
        }

        /// <summary>
        /// Returns 0 to remove <paramref name="a"/> or 1 to remove <paramref name="b"/>:
        /// the bay further from its own edge's midpoint (closer to a corner) is removed.
        /// </summary>
        private static int PickRemoval(Candidate a, Candidate b, SiteBoundary site)
        {
            double da = a.Origin.DistanceTo(site.Edges[a.EdgeIndex].Midpoint);
            double db = b.Origin.DistanceTo(site.Edges[b.EdgeIndex].Midpoint);
            return da >= db ? 0 : 1;
        }
    }
}
