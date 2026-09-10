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
using System.Security.Cryptography;
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
            Curve boundary,
            double mergeAngle,
            double minEdgeLen,
            List<bool> edgesOn,
            ref object cleanPoly,
            ref object edges,
            ref object edgeDirs,
            ref object edgeLens,
            ref object edgeCount,
            ref object activeEdges)
    {
        // ── defaults ────────────────────────────────────────────────────────────
        if (mergeAngle <= 0) mergeAngle = 5.0;
        if (minEdgeLen <= 0) minEdgeLen = 1.0;

        // ── validate input ───────────────────────────────────────────────────────
        if (boundary == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No boundary provided.");
            return;
        }
        if (!boundary.IsClosed)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Boundary must be a closed curve.");
            return;
        }

        Polyline rawPoly;
        if (!boundary.TryGetPolyline(out rawPoly))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                "Boundary is not a polyline — converting via DivideByCount. " +
                "For best results, supply an actual polyline from your DXF.");

            // Fallback: approximate as polyline using 200 divisions
            Point3d[] pts;
            boundary.DivideByCount(200, true, out pts);
            rawPoly = new Polyline(pts);
            rawPoly.Add(pts[0]);
        }

        // Ensure closed
        if (!rawPoly.IsClosed) rawPoly.Add(rawPoly[0]);

        // ── build raw segments ───────────────────────────────────────────────────
        var rawSegs = new List<Line>();
        for (int i = 0; i < rawPoly.Count - 1; i++)
            rawSegs.Add(new Line(rawPoly[i], rawPoly[i + 1]));

        // ── rationalise: merge short / near-parallel segments ───────────────────
        var mergedSegs = MergeSegments(rawSegs, mergeAngle, minEdgeLen);

        // ── rebuild clean polyline from merged segments ──────────────────────────
        var cleanPts = new List<Point3d>();
        foreach (var seg in mergedSegs)
            cleanPts.Add(seg.From);
        cleanPts.Add(cleanPts[0]); // close

        var poly = new Polyline(cleanPts);

        // ── per-edge data ────────────────────────────────────────────────────────
        var edgeLines = new List<Line>();
        var edgeDirList = new List<Vector3d>();
        var edgeLenList = new List<double>();

        foreach (var seg in mergedSegs)
        {
            edgeLines.Add(seg);
            var dir = seg.Direction;
            dir.Unitize();
            edgeDirList.Add(dir);
            edgeLenList.Add(seg.Length);
        }

        int count = mergedSegs.Count;

        // ── apply edgesOn filter ─────────────────────────────────────────────────
        // If edgesOn list is shorter than edge count, default missing values to true
        var activeList = new List<Line>();
        for (int i = 0; i < count; i++)
        {
            bool on = (edgesOn != null && i < edgesOn.Count) ? edgesOn[i] : true;
            if (on) activeList.Add(edgeLines[i]);
        }

        // ── output ───────────────────────────────────────────────────────────────
        cleanPoly = poly.ToNurbsCurve();
        edges = edgeLines;
        edgeDirs = edgeDirList;
        edgeLens = edgeLenList;
        edgeCount = count;
        activeEdges = activeList;
    }

    // ── Merge consecutive segments that are short or near-parallel ───────────────

    private List<Line> MergeSegments(List<Line> segs, double mergeAngleDeg, double minLen)
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

            bool tooShort = seg.Length < minLen;
            bool nearParallel = angle < tolRad;

            if (tooShort || nearParallel)
            {
                // Extend current merged segment to include this one
                currentEnd = seg.To;
                // Recompute direction of extended segment
                currentDir = currentEnd - currentStart;
                currentDir.Unitize();
            }
            else
            {
                // Commit current segment, start a new one
                result.Add(new Line(currentStart, currentEnd));
                currentStart = seg.From;
                currentEnd = seg.To;
                currentDir = segDir;
            }
        }

        // Commit final segment
        result.Add(new Line(currentStart, currentEnd));

        // ── One pass: also merge the last segment into the first if they're
        //    near-parallel (handles wrap-around at the polygon close point)
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
                // Merge last into first — extend first segment's start back
                var merged = new Line(last.From, first.To);
                result[0] = merged;
                result.RemoveAt(result.Count - 1);
            }
        }

        return result;
    }
}
