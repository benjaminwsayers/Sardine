// Grasshopper Script Instance
// C10 — Visualiser / Panel (updated for accessible bay count)

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
        Curve cleanPoly,
        int totalCount,
        int perimCount,
        int centralCount,
        int accessibleCount,
        double bayWidth,
        double bayDepth,
        double angle,
        double aisleWidth,
        double offset,
        ref object panelGeo,
        ref object panelText,
        ref object panelLabels)
    {
        if (cleanPoly == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No cleanPoly provided.");
            return;
        }

        if (offset <= 0) offset = 10.0;

        BoundingBox bbox = cleanPoly.GetBoundingBox(true);

        // ── Panel anchor: top-right of bounding box + offset ─────────────────
        double panelX = bbox.Max.X + offset;
        double panelY = bbox.Max.Y;

        // ── Panel dimensions scaled to site ──────────────────────────────────
        double siteW = bbox.Max.X - bbox.Min.X;
        double siteH = bbox.Max.Y - bbox.Min.Y;
        double panelW = siteW * 0.32;
        double rowH = siteH * 0.045;
        double col0W = panelW * 0.55;  // label column
        double col1W = panelW * 0.45;  // value column
        double margin = rowH * 0.25;

        Point3d anchor = new Point3d(panelX, panelY, 0);

        var geo = new List<Curve>();
        var pts = new List<Point3d>();
        var labels = new List<string>();

        // ── Table rows: label | value ─────────────────────────────────────────
        var rows = new List<(string label, string value, bool isHeader)>
        {
            ("Total Bays",          totalCount.ToString(),                     false),
            ("Perimeter Bays",      perimCount.ToString(),                     false),
            ("Central Bays",        centralCount.ToString(),                   false),
            ("Accessible Bays",     accessibleCount.ToString(),                false),
            ("---",                 "---",                                     false), // divider
            ("Bay Size",            string.Format("{0:F1} x {1:F1} m", bayWidth, bayDepth), false),
            ("Parking Angle",       string.Format("{0:F0} deg", angle),        false),
            ("Aisle Width",         string.Format("{0:F1} m", aisleWidth),     false),
        };

        double totalH = rowH * rows.Count;

        // ── Draw outer border ─────────────────────────────────────────────────
        geo.Add(MakeRect(anchor, panelW, -totalH));

        // ── Draw each row ─────────────────────────────────────────────────────
        for (int i = 0; i < rows.Count; i++)
        {
            double rowY = anchor.Y - rowH * i;

            // Horizontal rule under every row
            geo.Add(MakeLine(
                new Point3d(anchor.X, rowY - rowH, 0),
                new Point3d(anchor.X + panelW, rowY - rowH, 0)));

            // Divider row — just a thicker visual break, no text
            if (rows[i].label == "---")
                continue;

            // Vertical column divider
            geo.Add(MakeLine(
                new Point3d(anchor.X + col0W, rowY, 0),
                new Point3d(anchor.X + col0W, rowY - rowH, 0)));

            // Label — left column, vertically centred in row
            pts.Add(new Point3d(anchor.X + margin, rowY - rowH * 0.5, 0));
            labels.Add(rows[i].label);

            // Value — right column
            pts.Add(new Point3d(anchor.X + col0W + margin, rowY - rowH * 0.5, 0));
            labels.Add(rows[i].value);

            // Header row — extra line underneath to visually separate
            if (rows[i].isHeader)
            {
                geo.Add(MakeLine(
                    new Point3d(anchor.X, rowY - rowH, 0),
                    new Point3d(anchor.X + panelW, rowY - rowH, 0)));
            }
        }

        panelGeo = geo;
        panelText = pts;
        panelLabels = labels;
    }

    // ── Geometry helpers ──────────────────────────────────────────────────────

    private Curve MakeRect(Point3d origin, double w, double h)
    {
        var pts = new Point3d[]
        {
            origin,
            origin + new Vector3d(w, 0, 0),
            origin + new Vector3d(w, h, 0),
            origin + new Vector3d(0, h, 0),
            origin
        };
        return new PolylineCurve(pts);
    }

    private Curve MakeLine(Point3d a, Point3d b)
    {
        return new LineCurve(a, b);
    }
}