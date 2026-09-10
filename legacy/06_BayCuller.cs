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
            DataTree<Curve> bayRects,
            List<Curve> exclusions,
            double tolerance,
            ref object clippedBays,
            ref object count)
    {
        if (tolerance <= 0) tolerance = 0.001;

        // Sanitise exclusions — must be closed curves
        var validExclusions = new List<Curve>();
        if (exclusions != null)
            foreach (var c in exclusions)
                if (c != null && c.IsClosed) validExclusions.Add(c);

        if (validExclusions.Count == 0)
        {
            // Nothing to cull — pass through unchanged
            clippedBays = bayRects;
            count = bayRects.DataCount;
            return;
        }

        var output = new DataTree<Curve>();
        int totalCount = 0;

        for (int b = 0; b < bayRects.BranchCount; b++)
        {
            GH_Path path = bayRects.Path(b);
            foreach (Curve bay in bayRects.Branch(b))
            {
                if (bay == null) continue;
                if (IsInAnyExclusion(bay, validExclusions, tolerance)) continue;
                output.Add(bay, path);
                totalCount++;
            }
        }

        clippedBays = output;
        count = totalCount;
    }

    private bool IsInAnyExclusion(Curve bay, List<Curve> exclusions, double tol)
    {
        Polyline bayPl;
        bay.TryGetPolyline(out bayPl);

        foreach (Curve ex in exclusions)
        {
            // 1. Bay corner inside exclusion
            if (bayPl != null)
            {
                foreach (Point3d corner in bayPl)
                {
                    var pc = ex.Contains(corner, Plane.WorldXY, tol);
                    if (pc == PointContainment.Inside ||
                        pc == PointContainment.Coincident) return true;
                }
            }

            // 2. Bay edge crosses exclusion boundary
            var ccx = Rhino.Geometry.Intersect.Intersection.CurveCurve(
                bay, ex, tol, tol);
            if (ccx != null && ccx.Count > 0) return true;

            // 3. Exclusion corner inside bay (small exclusion inside one bay)
            Polyline exPl;
            if (ex.TryGetPolyline(out exPl))
            {
                foreach (Point3d corner in exPl)
                {
                    var pc = bay.Contains(corner, Plane.WorldXY, tol);
                    if (pc == PointContainment.Inside ||
                        pc == PointContainment.Coincident) return true;
                }
            }
        }

        return false;
    }
}