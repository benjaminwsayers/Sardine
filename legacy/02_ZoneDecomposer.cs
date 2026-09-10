// Grasshopper Script Instance
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
    #region Notes
    /* 
      Members:
        RhinoDoc RhinoDocument
        GH_Document GrasshopperDocument
        IGH_Component Component
        int Iteration

      Methods (Virtual & overridable):
        Print(string text)
        Print(string format, params object[] args)
        Reflect(object obj)
        Reflect(object obj, string method_name)
    */
    #endregion

    private void RunScript(
            Curve cleanPoly,
            double bayDepth,
            double aisleWidth,
            int numDirections,
            ref object perimInner,
            ref object centralBoundary,
            ref object fillDirections,
            ref object fillAngles,
            ref object zoneCount)
    {
        // ── defaults ─────────────────────────────────────────────────────────────
        if (bayDepth <= 0) bayDepth = 4.8;
        if (aisleWidth <= 0) aisleWidth = 6.0;
        if (numDirections < 1) numDirections = 1;
        if (numDirections > 3) numDirections = 3;

        if (cleanPoly == null || !cleanPoly.IsClosed)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                "cleanPoly must be a closed curve.");
            return;
        }

        double tol = 0.001;

        // ── offset 1: perimeter inner edge ───────────────────────────────────────
        // Inset by bayDepth — this is the back edge of perimeter bays
        Curve perimInnerCurve = OffsetInward(cleanPoly, bayDepth, tol);
        if (perimInnerCurve == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                "Perimeter offset failed. Bay depth may be too large for this site.");
            return;
        }

        // ── offset 2: central fill boundary ──────────────────────────────────────
        // Inset by bayDepth + aisleWidth — outer limit of central parking grid
        Curve centralBoundaryCurve = OffsetInward(cleanPoly, bayDepth + aisleWidth, tol);
        if (centralBoundaryCurve == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                "Central boundary offset failed. Bay depth + aisle width may be too large for this site.");
            return;
        }

        // ── detect dominant fill directions from central boundary ─────────────────
        Polyline centralPoly;
        List<Vector3d> dirs;
        List<double> angles;

        if (centralBoundaryCurve.TryGetPolyline(out centralPoly))
        {
            dirs = GetDominantDirections(centralPoly, numDirections);
            angles = dirs.Select(d => Math.Atan2(d.Y, d.X) * 180.0 / Math.PI).ToList();
        }
        else
        {
            // Fallback: use longest edge of original clean poly
            dirs = GetDominantDirectionsFromCurve(cleanPoly, numDirections);
            angles = dirs.Select(d => Math.Atan2(d.Y, d.X) * 180.0 / Math.PI).ToList();
        }

        // ── output ────────────────────────────────────────────────────────────────
        perimInner = perimInnerCurve;
        centralBoundary = centralBoundaryCurve;
        fillDirections = dirs;
        fillAngles = angles;
        zoneCount = dirs.Count;
    }

    // ── Offset a closed curve inward using RhinoCommon ───────────────────────────
    // Returns the largest resulting closed curve, or null if offset fails.

    private Curve OffsetInward(Curve curve, double distance, double tol)
    {
        // Determine inward normal direction: offset both ways, pick the one
        // whose area is smaller (i.e. the inward one)
        var offsetPos = curve.Offset(Plane.WorldXY, -distance, tol,
            CurveOffsetCornerStyle.Sharp);
        var offsetNeg = curve.Offset(Plane.WorldXY, distance, tol,
            CurveOffsetCornerStyle.Sharp);

        Curve best = LargestClosedCurve(offsetPos);
        Curve neg = LargestClosedCurve(offsetNeg);

        if (best == null && neg == null) return null;
        if (best == null) return neg;
        if (neg == null) return best;

        // Pick the one that is actually inside the original
        // Test by checking if its centroid is inside the original curve
        bool bestInside = IsInsideOriginal(best, curve, tol);
        bool negInside = IsInsideOriginal(neg, curve, tol);

        if (bestInside && !negInside) return best;
        if (negInside && !bestInside) return neg;

        // Both inside (unusual) — return the smaller area one (more inset)
        var ampBest = AreaMassProperties.Compute(best);
        var ampNeg = AreaMassProperties.Compute(neg);
        if (ampBest == null) return neg;
        if (ampNeg == null) return best;
        return ampBest.Area < ampNeg.Area ? best : neg;
    }

    private Curve LargestClosedCurve(Curve[] curves)
    {
        if (curves == null || curves.Length == 0) return null;
        Curve largest = null;
        double maxArea = 0;
        foreach (var c in curves)
        {
            if (c == null || !c.IsClosed) continue;
            var amp = AreaMassProperties.Compute(c);
            if (amp == null) continue;
            if (amp.Area > maxArea)
            {
                maxArea = amp.Area;
                largest = c;
            }
        }
        return largest;
    }

    private bool IsInsideOriginal(Curve candidate, Curve original, double tol)
    {
        var amp = AreaMassProperties.Compute(candidate);
        if (amp == null) return false;
        var containment = original.Contains(amp.Centroid, Plane.WorldXY, tol);
        return containment == PointContainment.Inside;
    }

    // ── Dominant direction detection from a polyline ─────────────────────────────
    // Clusters edge directions by angle, weights by length, returns top N.

    private List<Vector3d> GetDominantDirections(Polyline poly, int maxDirs)
    {
        var edges = new List<(Vector3d dir, double len)>();

        for (int i = 0; i < poly.Count - 1; i++)
        {
            var vec = poly[i + 1] - poly[i];
            double len = vec.Length;
            if (len < 0.5) continue; // ignore tiny edges
            vec.Unitize();

            // Fold into [0°, 180°) — opposite directions are the same axis
            if (vec.X < -1e-6 || (Math.Abs(vec.X) < 1e-6 && vec.Y < 0.0))
                vec = -vec;

            edges.Add((vec, len));
        }

        if (edges.Count == 0)
            return new List<Vector3d> { Vector3d.XAxis };

        // Cluster within 15°
        var clusters = new List<(Vector3d dir, double len)>();
        foreach (var (dir, len) in edges)
        {
            bool merged = false;
            for (int i = 0; i < clusters.Count; i++)
            {
                double dot = Math.Max(-1.0, Math.Min(1.0,
                    dir.X * clusters[i].dir.X + dir.Y * clusters[i].dir.Y));
                double angle = Math.Acos(dot) * 180.0 / Math.PI;

                if (angle < 15.0)
                {
                    // Weighted average direction
                    var nd = clusters[i].dir * clusters[i].len + dir * len;
                    nd.Unitize();
                    clusters[i] = (nd, clusters[i].len + len);
                    merged = true;
                    break;
                }
            }
            if (!merged) clusters.Add((dir, len));
        }

        // Sort by total edge length descending
        clusters.Sort((a, b) => b.len.CompareTo(a.len));

        // Return top N
        return clusters
            .Take(maxDirs)
            .Select(c => c.dir)
            .ToList();
    }

    private List<Vector3d> GetDominantDirectionsFromCurve(Curve curve, int maxDirs)
    {
        // Approximate as polyline and run same algorithm
        Point3d[] pts;
        curve.DivideByCount(64, true, out pts);
        var poly = new Polyline(pts);
        poly.Add(pts[0]);
        return GetDominantDirections(poly, maxDirs);
    }
}
