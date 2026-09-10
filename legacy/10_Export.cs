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
            Curve cleanPoly,
            List<Curve> panelGeo,
            string optionName,
            double bayWidth,
            double bayDepth,
            double bayAngle,
            double aisleWidth,
            int totalCount,
            int perimCount,
            int centralCount,
            bool bake,
            string dwgPath,
            string excelPath,
            ref object status)
    {
        // ── Rising edge detection — only fire when button transitions to true ──────
        if (!bake)
        {
            _lastBake = false;
            status = "Ready — press Bake to export.";
            return;
        }
        if (_lastBake)
        {
            status = "Baked. Toggle button off then on to bake again.";
            return;
        }
        _lastBake = true;

        if (string.IsNullOrWhiteSpace(optionName)) optionName = "Option_01";

        var log = new System.Text.StringBuilder();

        try
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null)
            {
                status = "ERROR: No active Rhino document.";
                return;
            }

            double tol = doc.ModelAbsoluteTolerance;

            // ── Layer setup ───────────────────────────────────────────────────────
            int layerBoundary = GetOrCreateLayer(doc, "CP-BOUNDARY", System.Drawing.Color.Red);
            int layerPerim = GetOrCreateLayer(doc, "CP-BAYS-PERIMETER", System.Drawing.Color.Green);
            int layerCentral = GetOrCreateLayer(doc, "CP-BAYS-CENTRAL", System.Drawing.Color.Lime);
            int layerAnnot = GetOrCreateLayer(doc, "CP-ANNOTATION", System.Drawing.Color.White);

            int bakedCount = 0;

            // ── Bake site boundary ────────────────────────────────────────────────
            if (cleanPoly != null)
            {
                var attr = new Rhino.DocObjects.ObjectAttributes();
                attr.LayerIndex = layerBoundary;
                attr.Name = optionName + "_boundary";
                doc.Objects.AddCurve(cleanPoly, attr);
                bakedCount++;
            }

            // ── Bake perimeter bays ───────────────────────────────────────────────
            if (perimBays != null)
            {
                var attr = new Rhino.DocObjects.ObjectAttributes();
                attr.LayerIndex = layerPerim;
                foreach (var branch in perimBays.Branches)
                    foreach (Curve c in branch)
                        if (c != null) { doc.Objects.AddCurve(c, attr); bakedCount++; }
            }

            // ── Bake central bays ─────────────────────────────────────────────────
            if (centralBays != null)
            {
                var attr = new Rhino.DocObjects.ObjectAttributes();
                attr.LayerIndex = layerCentral;
                foreach (var branch in centralBays.Branches)
                    foreach (Curve c in branch)
                        if (c != null) { doc.Objects.AddCurve(c, attr); bakedCount++; }
            }

            // ── Bake panel table geometry ─────────────────────────────────────────
            if (panelGeo != null)
            {
                var attr = new Rhino.DocObjects.ObjectAttributes();
                attr.LayerIndex = layerAnnot;
                foreach (Curve c in panelGeo)
                    if (c != null) { doc.Objects.AddCurve(c, attr); bakedCount++; }
            }

            log.AppendLine(string.Format("Baked {0} objects to Rhino document.", bakedCount));
            doc.Views.Redraw();

            // ── DWG export ────────────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(dwgPath))
            {
                // Ensure directory exists
                string dir = System.IO.Path.GetDirectoryName(dwgPath);
                if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                // Select all objects on our layers, export, deselect
                doc.Objects.UnselectAll();
                SelectObjectsOnLayers(doc, new int[]
                    { layerBoundary, layerPerim, layerCentral, layerAnnot });

                string script = string.Format(
                    "_-Export \"{0}\" _Enter", dwgPath);
                RhinoApp.RunScript(script, false);
                doc.Objects.UnselectAll();

                log.AppendLine(string.Format("DWG exported to: {0}", dwgPath));
            }
            else
            {
                log.AppendLine("DWG path empty — skipped DWG export.");
            }

            // ── CSV / Excel report ────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(excelPath))
            {
                string dir = System.IO.Path.GetDirectoryName(excelPath);
                if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                bool fileExists = System.IO.File.Exists(excelPath);

                using (var sw = new System.IO.StreamWriter(
                    excelPath, append: true,
                    encoding: System.Text.Encoding.UTF8))
                {
                    // Write header if new file
                    if (!fileExists)
                    {
                        sw.WriteLine(
                            "Option Name,Bay Width (m),Bay Depth (m)," +
                            "Parking Angle (deg),Aisle Width (m)," +
                            "Total Bays,Perimeter Bays,Central Bays," +
                            "Generated At");
                    }

                    // Write data row
                    sw.WriteLine(string.Format(
                        "{0},{1:F1},{2:F1},{3:F0},{4:F1},{5},{6},{7},{8}",
                        optionName,
                        bayWidth,
                        bayDepth,
                        bayAngle,
                        aisleWidth,
                        totalCount,
                        perimCount,
                        centralCount,
                        System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")));
                }

                log.AppendLine(string.Format("Report row appended to: {0}", excelPath));
            }
            else
            {
                log.AppendLine("Excel path empty — skipped report.");
            }

            log.AppendLine("Done.");
        }
        catch (Exception ex)
        {
            log.AppendLine("ERROR: " + ex.Message);
        }

        status = log.ToString();
    }

    // ── Persistent bake state ─────────────────────────────────────────────────────
    private bool _lastBake = false;

    // ── Get or create a layer by name ────────────────────────────────────────────
    private int GetOrCreateLayer(RhinoDoc doc, string name, System.Drawing.Color colour)
    {
        int idx = doc.Layers.FindByFullPath(name, -1);
        if (idx >= 0) return idx;

        var layer = new Rhino.DocObjects.Layer();
        layer.Name = name;
        layer.Color = colour;
        return doc.Layers.Add(layer);
    }

    // ── Select all objects on given layer indices ─────────────────────────────────
    private void SelectObjectsOnLayers(RhinoDoc doc, int[] layerIndices)
    {
        var layerSet = new System.Collections.Generic.HashSet<int>(layerIndices);
        foreach (var obj in doc.Objects)
        {
            if (obj == null) continue;
            if (layerSet.Contains(obj.Attributes.LayerIndex))
                obj.Select(true);
        }
    }
}
