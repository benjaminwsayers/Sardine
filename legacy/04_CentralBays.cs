// Grasshopper Script Instance
// C04 — Central Bay Generator (F02 + F03)
//
// F02: Herringbone aisle geometry
//   - Row pitch = 2 × bayDepth × sin(bayAngle) + aisleWidth
//   - Bay step along row = bayWidth / sin(bayAngle)
//   - Aisle rectangles sit between front edges of back-to-back pairs
//   - 90° degenerate case collapses cleanly (sin(90°) = 1)
//
// F03: One-way snake traffic flow
//   - Adjacent aisles alternate traffic direction
//   - Bay angles flip per aisle direction
//   - Flow arrow chevrons output as separate geometry
//   - startFlowPositive controls initial direction
//   - showFlowArrows controls arrow generation
//
// Tree output: {rowIndex, bayIndex} — one curve per branch
// Row pairing: rowIndex = rowPairIndex*2 (Row A), rowPairIndex*2+1 (Row B)

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
    private void RunScript(
        Curve centralBoundary,
        double bayWidth,
        double bayDepth,
        double aisleWidth,
        double bayAngle,
        double gridAngle,
        bool startFlowPositive,
        bool showFlowArrows,
        ref object bayRects,
        ref object aisles,
        ref object count,
        ref object flowArrows,
        ref object flowDirections)
    {
        // ── Defaults ──────────────────────────────────────────────────────────
        if (bayWidth <= 0) bayWidth = 2.4;
        if (bayDepth <= 0) bayDepth = 4.8;
        if (aisleWidth <= 0) aisleWidth = 6.0;
        if (bayAngle <= 0) bayAngle = 90.0;

        if (centralBoundary == null || !centralBoundary.IsClosed)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                "centralBoundary must be a closed curve.");
            return;
        }

        double tol = 0.001;
        double angleRad = bayAngle * Math.PI / 180.0;

        // ── Grid axes from gridAngle ──────────────────────────────────────────
        double gridRad = gridAngle * Math.PI / 180.0;
        Vector3d uAx = new Vector3d(Math.Cos(gridRad), Math.Sin(gridRad), 0);
        Vector3d vAx = new Vector3d(-Math.Sin(gridRad), Math.Cos(gridRad), 0);

        // ── Oriented bounding box of centralBoundary in grid frame ────────────
        var amp = AreaMassProperties.Compute(centralBoundary);
        if (amp == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                "Could not compute central boundary area.");
            return;
        }
        Point3d origin = amp.Centroid;

        double uMin = double.MaxValue, uMax = double.MinValue;
        double vMin = double.MaxValue, vMax = double.MinValue;

        Point3d[] boundaryPts;
        centralBoundary.DivideByCount(256, true, out boundaryPts);
        foreach (Point3d p in boundaryPts)
        {
            var d = p - origin;
            double u = d.X * uAx.X + d.Y * uAx.Y;
            double v = d.X * vAx.X + d.Y * vAx.Y;
            if (u < uMin) uMin = u; if (u > uMax) uMax = u;
            if (v < vMin) vMin = v; if (v > vMax) vMax = v;
        }

        // ── Row pitch and bay step (F02) ──────────────────────────────────────
        //
        // sinA = sin(bayAngle): at 90° = 1, at 60° ≈ 0.866, at 45° ≈ 0.707
        //
        // Perpendicular depth of one bay = bayDepth × sinA
        // Row pitch = 2 × bayDepth × sinA + aisleWidth
        // Bay footprint along row = bayWidth / sinA
        //
        // At 90° these collapse to:
        //   rowPitch = 2×bayDepth + aisleWidth (original)
        //   bayStep  = bayWidth (original)

        double sinA = Math.Sin(angleRad);
        double cosA = Math.Cos(angleRad);
        if (Math.Abs(sinA) < 1e-6) sinA = 1e-6; // safety

        double bayDepthProj = bayDepth * sinA;  // perpendicular depth of one bay
        double rowPitch = 2.0 * bayDepthProj + aisleWidth;
        double bayStep = bayWidth / sinA;      // footprint along row

        // ── Angular offset for bay rotation ───────────────────────────────────
        // At 90°: offset = 0° → no rotation
        // At 60°: offset = 30° → bays tilted 30° from perpendicular
        double angularOffset = 90.0 - bayAngle;

        // ── Generate rows ─────────────────────────────────────────────────────
        var outRects = new DataTree<Curve>();
        var outAisles = new List<Curve>();
        var outArrows = new List<Curve>();
        var outFlowDirs = new DataTree<int>();
        int totalCount = 0;
        int rowPairIndex = 0;

        double vStart = vMin - rowPitch;
        double vEnd = vMax + rowPitch;
        double uStart = uMin - bayStep * 2;
        double uEnd = uMax + bayStep * 2;

        // Flow direction alternates per row pair
        int flowSign = startFlowPositive ? 1 : -1;

        for (double vBase = vStart; vBase < vEnd; vBase += rowPitch)
        {
            // ── Row geometry in v-axis ────────────────────────────────────────
            // Row A back edge:   vBase
            // Row A front edge:  vBase + bayDepthProj
            // Aisle start:       vBase + bayDepthProj
            // Aisle end:         vBase + bayDepthProj + aisleWidth
            // Row B front edge:  vBase + bayDepthProj + aisleWidth
            // Row B back edge:   vBase + 2*bayDepthProj + aisleWidth

            double vAisleStart = vBase + bayDepthProj;
            double vAisleEnd = vBase + bayDepthProj + aisleWidth;

            int rowIndexA = rowPairIndex * 2;
            int rowIndexB = rowPairIndex * 2 + 1;

            int bayCountA = 0;
            int bayCountB = 0;

            // ── Bay rotation per flow direction (F03) ─────────────────────────
            // Row A: bays extend in -vAx direction from aisle face
            //   rotation = -flowSign × angularOffset
            // Row B: bays extend in +vAx direction from aisle face
            //   rotation = +flowSign × angularOffset
            //
            // At 90°: angularOffset = 0 → no rotation regardless of flow

            double rotA = -flowSign * angularOffset;
            double rotB = flowSign * angularOffset;

            Vector3d bayDirA = RotateInXY(vAx, rotA);
            Vector3d bayWidA = RotateInXY(uAx, rotA);
            Vector3d bayDirB = RotateInXY(vAx, rotB);
            Vector3d bayWidB = RotateInXY(uAx, rotB);

            for (double u = uStart; u < uEnd; u += bayStep)
            {
                double uCentre = u + bayStep * 0.5;

                // ── Row A bay ─────────────────────────────────────────────────
                Point3d originA = origin + uAx * uCentre + vAx * vAisleStart;
                Plane frameA = new Plane(originA, bayWidA, -bayDirA);
                var rectA = new Rectangle3d(frameA,
                    new Interval(-bayWidth * 0.5, bayWidth * 0.5),
                    new Interval(0, bayDepth));

                Curve curveA = rectA.ToPolyline().ToNurbsCurve();
                if (IsInsideBoundary(curveA, centralBoundary, tol))
                {
                    outRects.Add(curveA, new GH_Path(rowIndexA, bayCountA));
                    bayCountA++;
                    totalCount++;
                }

                // ── Row B bay ─────────────────────────────────────────────────
                Point3d originB = origin + uAx * uCentre + vAx * vAisleEnd;
                Plane frameB = new Plane(originB, bayWidB, bayDirB);
                var rectB = new Rectangle3d(frameB,
                    new Interval(-bayWidth * 0.5, bayWidth * 0.5),
                    new Interval(0, bayDepth));

                Curve curveB = rectB.ToPolyline().ToNurbsCurve();
                if (IsInsideBoundary(curveB, centralBoundary, tol))
                {
                    outRects.Add(curveB, new GH_Path(rowIndexB, bayCountB));
                    bayCountB++;
                    totalCount++;
                }
            }

            // ── Aisle rectangle (F02) ─────────────────────────────────────────
            // Edges parallel to uAx, width = aisleWidth perpendicular to uAx
            // Spans the full u-extent of generated bays in this pair
            if (bayCountA > 0 || bayCountB > 0)
            {
                // Find actual u-extent of placed bays for clean aisle edges
                double aisleUMin, aisleUMax;
                GetRowUExtent(outRects, rowIndexA, rowIndexB, origin, uAx,
                    out aisleUMin, out aisleUMax);

                // Pad by half a bay step for visual clearance
                double pad = bayStep * 0.5;
                Point3d a0 = origin + uAx * (aisleUMin - pad) + vAx * vAisleStart;
                Point3d a1 = origin + uAx * (aisleUMax + pad) + vAx * vAisleStart;
                Point3d a2 = origin + uAx * (aisleUMax + pad) + vAx * vAisleEnd;
                Point3d a3 = origin + uAx * (aisleUMin - pad) + vAx * vAisleEnd;
                outAisles.Add(new PolylineCurve(
                    new Point3d[] { a0, a1, a2, a3, a0 }));

                // ── Flow direction metadata (F03) ─────────────────────────────
                outFlowDirs.Add(flowSign, new GH_Path(rowPairIndex));

                // ── Flow arrow chevrons (F03) ─────────────────────────────────
                if (showFlowArrows)
                {
                    double vAisleMid = (vAisleStart + vAisleEnd) * 0.5;
                    GenerateFlowArrows(origin, uAx, vAx, flowSign,
                        aisleUMin - pad, aisleUMax + pad, vAisleMid,
                        aisleWidth, bayAngle, outArrows);
                }
            }

            // Alternate flow direction for next aisle (F03)
            flowSign *= -1;
            rowPairIndex++;
        }

        bayRects = outRects;
        aisles = outAisles;
        count = totalCount;
        flowArrows = outArrows;
        flowDirections = outFlowDirs;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  BOUNDARY CHECK (unchanged)
    // ══════════════════════════════════════════════════════════════════════════

    private bool IsInsideBoundary(Curve bay, Curve boundary, double tol)
    {
        Polyline pl;
        if (!bay.TryGetPolyline(out pl)) return false;

        for (int i = 0; i < pl.Count - 1; i++)
        {
            var pc = boundary.Contains(pl[i], Plane.WorldXY, tol);
            if (pc != PointContainment.Inside && pc != PointContainment.Coincident)
                return false;
        }
        return true;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  AISLE U-EXTENT (F02)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Find the min/max u-coordinate of placed bays in Row A and Row B
    /// so the aisle rectangle matches the actual bay extent.
    /// </summary>
    private void GetRowUExtent(
        DataTree<Curve> rects, int rowA, int rowB,
        Point3d origin, Vector3d uAx,
        out double uMin, out double uMax)
    {
        uMin = double.MaxValue;
        uMax = double.MinValue;

        for (int b = 0; b < rects.BranchCount; b++)
        {
            int[] idx = rects.Path(b).Indices;
            if (idx.Length < 1) continue;
            if (idx[0] != rowA && idx[0] != rowB) continue;

            foreach (Curve c in rects.Branch(b))
            {
                if (c == null) continue;
                Polyline pl;
                if (!c.TryGetPolyline(out pl)) continue;

                for (int i = 0; i < pl.Count - 1; i++)
                {
                    var d = pl[i] - origin;
                    double u = d.X * uAx.X + d.Y * uAx.Y;
                    if (u < uMin) uMin = u;
                    if (u > uMax) uMax = u;
                }
            }
        }

        // Fallback if no bays found
        if (uMin > uMax) { uMin = 0; uMax = 0; }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  FLOW ARROWS (F03)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Generate chevron arrow polylines along the aisle centreline.
    /// Arrow spacing ≈ 3× aisle width for readability.
    /// At 90° with two-way aisles, arrows point in the flow direction
    /// (user can disable via showFlowArrows).
    /// </summary>
    private void GenerateFlowArrows(
        Point3d origin, Vector3d uAx, Vector3d vAx,
        int flowSign, double uMin, double uMax, double vMid,
        double aisleWidth, double bayAngle,
        List<Curve> arrows)
    {
        double arrowSpacing = aisleWidth * 3.0;
        double arrowLength = aisleWidth * 0.4;
        double arrowHalfW = aisleWidth * 0.2;

        Vector3d flowDir = uAx * flowSign;

        double uLen = uMax - uMin;
        if (uLen < arrowSpacing) arrowSpacing = uLen; // at least one arrow

        // Start inset from edges
        double uPos = uMin + arrowSpacing * 0.5;

        while (uPos < uMax)
        {
            Point3d tip = origin + uAx * uPos + vAx * vMid
                + flowDir * (arrowLength * 0.5);
            Point3d tailL = origin + uAx * uPos + vAx * vMid
                - flowDir * (arrowLength * 0.5)
                + vAx * arrowHalfW;
            Point3d tailR = origin + uAx * uPos + vAx * vMid
                - flowDir * (arrowLength * 0.5)
                - vAx * arrowHalfW;

            // Chevron: tailL → tip → tailR
            arrows.Add(new PolylineCurve(
                new Point3d[] { tailL, tip, tailR }));

            uPos += arrowSpacing;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  ROTATE VECTOR IN XY (unchanged)
    // ══════════════════════════════════════════════════════════════════════════

    private Vector3d RotateInXY(Vector3d v, double degrees)
    {
        double rad = degrees * Math.PI / 180.0;
        double cosR = Math.Cos(rad);
        double sinR = Math.Sin(rad);
        return new Vector3d(
            v.X * cosR - v.Y * sinR,
            v.X * sinR + v.Y * cosR,
            0);
    }
}