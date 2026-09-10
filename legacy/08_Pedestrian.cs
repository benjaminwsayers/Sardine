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
            double bayWidth,
            double bayDepth,
            double angle,
            double aisleWidth,
            int walkwayMode,
            bool perimBays,
            ref object optionName)
    {
        string walkway = walkwayMode == 0 ? "NoWalk"
                       : walkwayMode == 1 ? "RouteWalk"
                       : "AllWalk";

        string perim = perimBays ? "Perim" : "NoPerim";

        optionName = string.Format(
            "CPG_{0}deg_{1}x{2}m_A{3}m_{4}_{5}",
            ((int)angle).ToString(),
            bayWidth.ToString("F1"),
            bayDepth.ToString("F1"),
            aisleWidth.ToString("F1"),
            walkway,
            perim);
    }
}
