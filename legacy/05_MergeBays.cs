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
            DataTree<Curve> perimBays,
            DataTree<Curve> centralBays,
            ref object allBays,
            ref object perimCount,
            ref object centralCount,
            ref object totalCount)
    {
        var output = new DataTree<Curve>();

        // ── Perimeter bays → branch {0 ; edge ; bay} ─────────────────────────────
        int pCount = 0;
        if (perimBays != null)
        {
            for (int b = 0; b < perimBays.BranchCount; b++)
            {
                // Prepend 0 to existing path: {edge;bay} → {0;edge;bay}
                int[] indices = perimBays.Path(b).Indices;
                var newPath = new GH_Path(PrependIndex(indices, 0));

                foreach (Curve c in perimBays.Branch(b))
                {
                    if (c == null) continue;
                    output.Add(c, newPath);
                    pCount++;
                }
            }
        }

        // ── Central bays → branch {1 ; row ; bay} ────────────────────────────────
        int cCount = 0;
        if (centralBays != null)
        {
            for (int b = 0; b < centralBays.BranchCount; b++)
            {
                // Prepend 1 to existing path: {row;bay} → {1;row;bay}
                int[] indices = centralBays.Path(b).Indices;
                var newPath = new GH_Path(PrependIndex(indices, 1));

                foreach (Curve c in centralBays.Branch(b))
                {
                    if (c == null) continue;
                    output.Add(c, newPath);
                    cCount++;
                }
            }
        }

        allBays = output;
        perimCount = pCount;
        centralCount = cCount;
        totalCount = pCount + cCount;
    }

    // ── Prepend an index to an existing path indices array ───────────────────────

    private int[] PrependIndex(int[] existing, int prefix)
    {
        var result = new int[existing.Length + 1];
        result[0] = prefix;
        for (int i = 0; i < existing.Length; i++)
            result[i + 1] = existing[i];
        return result;
    }
}