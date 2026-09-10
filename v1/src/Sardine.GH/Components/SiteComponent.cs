using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Sardine.Core;
using Sardine.Core.Site;

namespace Sardine.GH.Components
{
    /// <summary>
    /// Sardine.Site — prepares a closed planar site boundary for layout generation
    /// (PRD section 10).
    /// </summary>
    public class SiteComponent : GH_Component
    {
        public SiteComponent()
            : base("Sardine.Site", "Site",
                "Rationalise a closed site boundary for Sardine layout generation.",
                SardineCategory.Tab, SardineCategory.Generate)
        { }

        public override Guid ComponentGuid
        {
            get { return new Guid("3F8A1C2D-74B5-4E9F-A021-DC3B56789012"); }
        }

        public override GH_Exposure Exposure { get { return GH_Exposure.primary; } }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Boundary", "B",
                "Closed planar site boundary. Polylines from a DXF/DWG survey give the best results.",
                GH_ParamAccess.item);

            pManager.AddNumberParameter("MergeAngle", "MA",
                "Consecutive edges within this angle (degrees) are merged. 0 disables. Default 5.",
                GH_ParamAccess.item, SardineDefaults.MergeAngleDeg);

            pManager.AddNumberParameter("MinEdgeLength", "MEL",
                "Edges shorter than this (metres) are absorbed into their neighbour. 0 disables. Default 1.0.",
                GH_ParamAccess.item, SardineDefaults.MinEdgeLength);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("RawBoundary", "R", "Original supplied boundary", GH_ParamAccess.item);
            pManager.AddCurveParameter("WorkingBoundary", "W", "Rationalised boundary used for generation", GH_ParamAccess.item);
            pManager.AddCurveParameter("Edges", "E", "Indexed rationalised edges", GH_ParamAccess.list);
            pManager.AddVectorParameter("EdgeDirections", "ED", "Unit direction of each edge", GH_ParamAccess.list);
            pManager.AddNumberParameter("EdgeLengths", "EL", "Length of each edge in metres", GH_ParamAccess.list);
            pManager.AddIntegerParameter("EdgeCount", "EC", "Number of rationalised edges", GH_ParamAccess.item);
            pManager.AddTextParameter("Warnings", "W!", "Non-fatal input warnings", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve boundary = null;
            double mergeAngle = SardineDefaults.MergeAngleDeg;
            double minEdgeLength = SardineDefaults.MinEdgeLength;

            if (!DA.GetData(0, ref boundary)) return;
            DA.GetData(1, ref mergeAngle);
            DA.GetData(2, ref minEdgeLength);

            var parameters = new RationalisationParameters
            {
                MergeAngleDeg = mergeAngle,
                MinEdgeLength = minEdgeLength
            };

            string error;
            var site = SiteRationaliser.Rationalise(boundary, parameters, out error);
            if (site == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, error);
                return;
            }

            foreach (var warning in site.Warnings)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, warning);

            var edgeCurves = new List<Curve>(site.EdgeCount);
            var directions = new List<Vector3d>(site.EdgeCount);
            var lengths = new List<double>(site.EdgeCount);
            foreach (var edge in site.Edges)
            {
                edgeCurves.Add(new LineCurve(edge.Geometry));
                directions.Add(edge.Direction);
                lengths.Add(edge.Length);
            }

            Message = site.EdgeCount + " edges";

            DA.SetData(0, site.RawBoundary);
            DA.SetData(1, site.WorkingBoundaryCurve());
            DA.SetDataList(2, edgeCurves);
            DA.SetDataList(3, directions);
            DA.SetDataList(4, lengths);
            DA.SetData(5, site.EdgeCount);
            DA.SetDataList(6, new List<string>(site.Warnings));
        }
    }
}
