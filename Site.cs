using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Sardine.Core.Models;
using Sardine.Core.Pipeline;

namespace Sardine.GH.Components
{
    /// <summary>
    /// Sardine.Site component.
    /// </summary>
    public class SiteComponent : GH_Component
    {
        public SiteComponent()
            : base(
                name: "Sardine.Site",
                nickname: "Site",
                description: "Prepare a closed site boundary for Sardine layout generation.",
                category: "Sardine",
                subCategory: "")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter(
                "Boundary", "B", "Closed planar site boundary",
                GH_ParamAccess.item);

            pManager.AddNumberParameter(
                "MergeAngle", "MA",
                "Near-parallel merge threshold in degrees. Set 0 to disable. Default 5.0",
                GH_ParamAccess.item, 5.0);

            pManager.AddNumberParameter(
                "MinEdgeLength", "MEL",
                "Minimum edge length in metres. Set 0 to disable. Default 1.0",
                GH_ParamAccess.item, 1.0);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("RawBoundary", "R", "Original supplied boundary", GH_ParamAccess.item);
            pManager.AddCurveParameter("WorkingBoundary", "W", "Rationalised boundary used for generation", GH_ParamAccess.item);
            pManager.AddCurveParameter("Edges", "E", "Indexed rationalised boundary edges", GH_ParamAccess.list);
            pManager.AddVectorParameter("EdgeDirections", "ED", "Unitised edge direction vectors", GH_ParamAccess.list);
            pManager.AddNumberParameter("EdgeLengths", "EL", "Edge lengths in metres", GH_ParamAccess.list);
            pManager.AddIntegerParameter("EdgeCount", "EC", "Number of rationalised boundary edges", GH_ParamAccess.item);
            pManager.AddTextParameter("Warnings", "W!", "Non-fatal Site warnings", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve boundary = null;
            double mergeAngle = 5.0;
            double minEdgeLength = 1.0;

            if (!DA.GetData(0, ref boundary)) return;
            DA.GetData(1, ref mergeAngle);
            DA.GetData(2, ref minEdgeLength);

            var parameters = new RationalisationParameters
            {
                MergeAngleDeg = mergeAngle,
                MinEdgeLength = minEdgeLength
            };

            var site = new SiteRationaliser(parameters).Rationalise(boundary, out var error);
            if (site == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, error);
                return;
            }

            foreach (var warning in site.Warnings)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, warning);

            var edgeCurves = new List<Curve>();
            var edgeDirections = new List<Vector3d>();
            var edgeLengths = new List<double>();
            foreach (var edge in site.Edges)
            {
                edgeCurves.Add(new LineCurve(edge.Geometry));
                edgeDirections.Add(edge.Direction);
                edgeLengths.Add(edge.Length);
            }

            DA.SetData(0, site.RawBoundary);
            DA.SetData(1, site.WorkingBoundary.ToNurbsCurve());
            DA.SetDataList(2, edgeCurves);
            DA.SetDataList(3, edgeDirections);
            DA.SetDataList(4, edgeLengths);
            DA.SetData(5, site.EdgeCount);
            DA.SetDataList(6, site.Warnings);
        }

        public override Guid ComponentGuid =>
            new Guid("3F8A1C2D-74B5-4E9F-A021-DC3B56789012");

        protected override Bitmap Icon
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();
                var stream = assembly.GetManifestResourceStream("Sardine.GH.Resources.site-boundary.png");
                return stream != null ? new Bitmap(stream) : base.Icon;
            }
        }
    }
}