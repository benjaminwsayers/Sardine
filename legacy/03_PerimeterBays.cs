// Grasshopper Script Instance
// C03 — Perimeter Bay Generator (F01-A + F01-B)
//
// F01-A: Overlap detection & cull
//   - PlacedBay collection during walk (no output built during walk)
//   - Post-placement overlap cull: consecutive bay pairs checked for corner
//     penetration, overlapping bay further from its edge midpoint is removed
//   - Wrap-around check: last bay vs first bay (closed perimeter seam)
//
// F01-B: Boundary containment cull
//   - All four corners of every surviving bay checked against cleanPoly
//   - Any bay with a corner outside the boundary is removed
//   - Catches protrusions at concave corners and tight curves
//
// Added tolerance input (default 0.001)
// Output trees built from survivors only, sequential bay_index per edge
// Placement algorithm is UNCHANGED from original C03.

#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
#endregion

public class Script_Instance : GH_ScriptInstance
{
    // ── Helper: holds placement data before cull pass ─────────────────────────

    private class PlacedBay
    {
        public Curve Rect;
        public Plane Frame;
        public double WalkLen;
        public int EdgeIndex;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  MAIN
    // ══════════════════════════════════════════════════════════════════════════

    private void RunScript(
        Curve perimInner,
        double bayWidth,
        double bayDepth,
        List<bool> edgesOn,
        Curve cleanPoly,
        Curve cullBoundary,
        double tolerance,
        ref object bayRects,
        ref object bayPlanes,
        ref object count)
    {
        if (perimInner == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Missing perimInner curve.");
            return;
        }

        if (bayWidth <= 0) bayWidth = 2.4;
        if (bayDepth <= 0) bayDepth = 4.8;
        if (tolerance <= 0) tolerance = 0.001;

        double totalLen = perimInner.GetLength();

        // ── Build edge parameter ranges from cleanPoly for edgesOn filtering ─
        var edgeRanges = BuildEdgeRanges(cleanPoly, totalLen);

        // ══════════════════════════════════════════════════════════════════════
        //  PHASE 1: Walk curve and collect all candidate bays
        // ══════════════════════════════════════════════════════════════════════

        var allPlaced = new List<PlacedBay>();

        double walkLen = bayWidth * 0.5;

        while (walkLen <= totalLen - bayWidth * 0.5)
        {
            double t;
            if (!perimInner.LengthParameter(walkLen, out t))
            {
                walkLen += bayWidth;
                continue;
            }

            // ── Horizontal frame ─────────────────────────────────────────────
            Point3d pt = perimInner.PointAt(t);
            Vector3d tangent = perimInner.TangentAt(t);
            tangent.Z = 0;
            if (!tangent.Unitize())
            {
                walkLen += bayWidth;
                continue;
            }

            Vector3d zAxis = Vector3d.ZAxis;
            Vector3d yAxis = Vector3d.CrossProduct(zAxis, tangent);
            yAxis.Unitize();

            Plane frame = new Plane(pt, tangent, yAxis);

            // ── Rectangle ────────────────────────────────────────────────────
            var rect = new Rectangle3d(
                frame,
                new Interval(-bayWidth * 0.5, bayWidth * 0.5),
                new Interval(0, bayDepth));

            // ── Edge on/off filter ───────────────────────────────────────────
            int edgeIndex = GetEdgeIndex(walkLen, edgeRanges);
            bool edgeActive = (edgesOn == null || edgeIndex >= edgesOn.Count)
                ? true
                : edgesOn[edgeIndex];

            if (edgeActive)
            {
                allPlaced.Add(new PlacedBay
                {
                    Rect = rect.ToPolyline().ToNurbsCurve(),
                    Frame = frame,
                    WalkLen = walkLen,
                    EdgeIndex = edgeIndex
                });
            }

            walkLen += bayWidth;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PHASE 2: Overlap cull — remove overlapping consecutive bays
        // ══════════════════════════════════════════════════════════════════════
        //
        //  Walk the placed list. For each consecutive pair that overlaps,
        //  remove the bay whose walkLen is further from its own edge's
        //  midpoint (i.e. the bay closer to a corner). Restart after each
        //  removal to recheck neighbours.

        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int i = 0; i < allPlaced.Count - 1; i++)
            {
                if (BaysOverlap(allPlaced[i].Rect, allPlaced[i + 1].Rect, tolerance))
                {
                    int removeIdx = PickRemoval(allPlaced[i], allPlaced[i + 1], edgeRanges);
                    allPlaced.RemoveAt(removeIdx == 0 ? i : i + 1);
                    changed = true;
                    break;  // restart from beginning
                }
            }
        }

        // ── Wrap-around check: last bay vs first bay (closed perimeter) ──────
        if (allPlaced.Count >= 2)
        {
            var first = allPlaced[0];
            var last = allPlaced[allPlaced.Count - 1];

            if (BaysOverlap(first.Rect, last.Rect, tolerance))
            {
                // Remove whichever is further from its edge midpoint
                int removeIdx = PickRemoval(last, first, edgeRanges);
                if (removeIdx == 0)
                    allPlaced.RemoveAt(allPlaced.Count - 1);
                else
                    allPlaced.RemoveAt(0);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PHASE 2b: Boundary containment cull (F01-B / F01.3)
        // ══════════════════════════════════════════════════════════════════════
        //
        //  Any bay with a corner outside cullBoundary (raw site boundary) is
        //  removed. Uses the original survey polyline, NOT cleanPoly, because
        //  rationalisation can cut real site area at corners.

        if (cullBoundary != null)
        {
            allPlaced = allPlaced
                .Where(b => AllCornersInside(b.Rect, cullBoundary, tolerance))
                .ToList();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PHASE 3: Build output trees from survivors
        // ══════════════════════════════════════════════════════════════════════

        var outRects = new DataTree<Curve>();
        var outPlanes = new DataTree<Plane>();

        // Sequential bay_index per edge
        var edgeBayCounts = new Dictionary<int, int>();

        foreach (var bay in allPlaced)
        {
            int edge = bay.EdgeIndex;
            if (!edgeBayCounts.ContainsKey(edge))
                edgeBayCounts[edge] = 0;

            int bayIndex = edgeBayCounts[edge];
            var path = new GH_Path(0, edge, bayIndex);

            outRects.Add(bay.Rect, path);
            outPlanes.Add(bay.Frame, path);

            edgeBayCounts[edge]++;
        }

        bayRects = outRects;
        bayPlanes = outPlanes;
        count = allPlaced.Count;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  OVERLAP DETECTION
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Two bay rectangles overlap if any corner of one lies strictly inside
    /// the other. Uses Curve.Contains with PointContainment.Inside — corner
    /// touching (Coincident) does NOT count as overlap.
    /// </summary>
    private bool BaysOverlap(Curve a, Curve b, double tol)
    {
        Polyline plA, plB;
        if (!a.TryGetPolyline(out plA) || !b.TryGetPolyline(out plB))
            return false;

        // Check corners of A inside B (first 4 points; index 4 is closure)
        for (int i = 0; i < 4; i++)
        {
            if (b.Contains(plA[i], Plane.WorldXY, tol) == PointContainment.Inside)
                return true;
        }

        // Check corners of B inside A
        for (int i = 0; i < 4; i++)
        {
            if (a.Contains(plB[i], Plane.WorldXY, tol) == PointContainment.Inside)
                return true;
        }

        return false;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  BOUNDARY CONTAINMENT (F01-B / F01.3)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns true if all four corners of the bay rectangle are inside
    /// (or on the edge of) the boundary curve. A single corner outside = false.
    /// </summary>
    private bool AllCornersInside(Curve rect, Curve boundary, double tol)
    {
        Polyline pl;
        if (!rect.TryGetPolyline(out pl)) return false;

        for (int i = 0; i < 4; i++)
        {
            var containment = boundary.Contains(pl[i], Plane.WorldXY, tol);
            if (containment == PointContainment.Outside)
                return false;
        }

        return true;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  REMOVAL DECISION
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Given two overlapping bays, returns 0 to remove bayA or 1 to remove bayB.
    /// Removes the bay further from its own edge's arc-length midpoint — i.e.
    /// the bay closer to a corner gets culled.
    /// </summary>
    private int PickRemoval(
        PlacedBay bayA, PlacedBay bayB,
        List<(double start, double end)> edgeRanges)
    {
        double midA = EdgeMidLen(bayA.EdgeIndex, edgeRanges);
        double midB = EdgeMidLen(bayB.EdgeIndex, edgeRanges);

        double distA = Math.Abs(bayA.WalkLen - midA);
        double distB = Math.Abs(bayB.WalkLen - midB);

        return distA >= distB ? 0 : 1;
    }

    /// <summary>
    /// Returns the arc-length midpoint of an edge range.
    /// </summary>
    private double EdgeMidLen(int edgeIndex, List<(double start, double end)> ranges)
    {
        if (edgeIndex < 0 || edgeIndex >= ranges.Count)
            return 0;
        return (ranges[edgeIndex].start + ranges[edgeIndex].end) * 0.5;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  EDGE RANGE UTILITIES (unchanged from original)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Build proportional arc-length ranges per clean poly edge.
    /// </summary>
    private List<(double start, double end)> BuildEdgeRanges(Curve cleanPoly, double totalPerimLen)
    {
        var ranges = new List<(double, double)>();
        if (cleanPoly == null) return ranges;

        Polyline pl;
        if (!cleanPoly.TryGetPolyline(out pl)) return ranges;

        double cleanTotal = 0;
        var lens = new List<double>();
        for (int i = 0; i < pl.Count - 1; i++)
        {
            double l = pl[i].DistanceTo(pl[i + 1]);
            lens.Add(l);
            cleanTotal += l;
        }

        double cum = 0;
        foreach (double l in lens)
        {
            double start = cum;
            double end = cum + (l / cleanTotal) * totalPerimLen;
            ranges.Add((start, end));
            cum = end;
        }

        return ranges;
    }

    /// <summary>
    /// Which edge does this arc-length position fall on?
    /// </summary>
    private int GetEdgeIndex(double walkLen, List<(double start, double end)> ranges)
    {
        for (int i = 0; i < ranges.Count; i++)
            if (walkLen >= ranges[i].start && walkLen < ranges[i].end)
                return i;
        return Math.Max(0, ranges.Count - 1);
    }
}