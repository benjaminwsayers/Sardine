using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Sardine.Core;
using Sardine.Core.Layout;
using Sardine.Core.Models;
using Sardine.GH.Conversion;

namespace Sardine.GH.Components
{
    /// <summary>
    /// Sardine.Layout — generates the complete car park layout from the Site
    /// outputs (PRD sections 11, 12 and 19).
    /// </summary>
    public class LayoutComponent : GH_Component
    {
        public LayoutComponent()
            : base("Sardine.Layout", "Layout",
                "Generate perimeter, central and accessible parking from a rationalised site boundary.",
                SardineCategory.Tab, SardineCategory.Generate)
        { }

        public override Guid ComponentGuid
        {
            get { return new Guid("6B2E4F10-9C3A-4D7E-B5A1-2F8C0D9E1A47"); }
        }

        public override GH_Exposure Exposure { get { return GH_Exposure.primary; } }

        // Input indices
        private const int InWorking = 0, InRaw = 1, InOrientation = 2, InBayWidth = 3, InBayDepth = 4,
            InAisleWidth = 5, InBayAngle = 6, InPerimeterEdges = 7, InExclusions = 8, InAccessiblePoint = 9,
            InAccessibleCount = 10, InAccessibleWidth = 11, InAccessibleDepth = 12, InStartFlow = 13, InShowFlow = 14;

        // Output indices
        private const int OutLayout = 0, OutAisles = 1, OutFlow = 2, OutBayCount = 3, OutPerimeterCount = 4,
            OutCentralCount = 5, OutAccessibleCount = 6, OutWarnings = 7;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("WorkingBoundary", "W", "Rationalised boundary from Sardine.Site", GH_ParamAccess.item);
            pManager.AddCurveParameter("RawBoundary", "R", "Original boundary from Sardine.Site (containment limit for perimeter bays)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Orientation", "O", "Central grid orientation in degrees from World X. Default 0.", GH_ParamAccess.item, SardineDefaults.OrientationDeg);
            pManager.AddNumberParameter("BayWidth", "BW", "Standard bay width in metres. Default 2.4.", GH_ParamAccess.item, SardineDefaults.BayWidth);
            pManager.AddNumberParameter("BayDepth", "BD", "Standard bay depth in metres. Default 5.0.", GH_ParamAccess.item, SardineDefaults.BayDepth);
            pManager.AddNumberParameter("AisleWidth", "AW", "Aisle width in metres. Default 6.0.", GH_ParamAccess.item, SardineDefaults.AisleWidth);
            pManager.AddNumberParameter("BayAngle", "BA", "Central bay angle to the aisle in degrees (90 = perpendicular). Default 90.", GH_ParamAccess.item, SardineDefaults.BayAngleDeg);
            pManager.AddBooleanParameter("PerimeterEdges", "PE", "One flag per Site edge; false disables perimeter parking on that edge. Missing entries are True.", GH_ParamAccess.list);
            pManager.AddCurveParameter("ExclusionZones", "X", "Closed curves; bays conflicting with them are removed.", GH_ParamAccess.list);
            pManager.AddPointParameter("AccessiblePoint", "AP", "Accessible bays are placed closest to this point. Site centroid if omitted.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("AccessibleCount", "AC", "Number of accessible bays. -1 = automatic (max(1, floor(5% of standard capacity))).", GH_ParamAccess.item, SardineDefaults.AutomaticAccessibleCount);
            pManager.AddNumberParameter("AccessibleWidth", "AcW", "Accessible bay width in metres. Default 3.6.", GH_ParamAccess.item, SardineDefaults.AccessibleWidth);
            pManager.AddNumberParameter("AccessibleDepth", "AcD", "Accessible bay depth in metres. Default 6.2.", GH_ParamAccess.item, SardineDefaults.AccessibleDepth);
            pManager.AddBooleanParameter("StartFlowPositive", "SF", "Flow direction of the first aisle. Default True.", GH_ParamAccess.item, SardineDefaults.StartFlowPositive);
            pManager.AddBooleanParameter("ShowFlow", "F", "Generate flow arrows. Default True.", GH_ParamAccess.item, SardineDefaults.ShowFlow);

            for (int i = InRaw; i <= InShowFlow; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Layout", "L", "Bay outlines as {0;edge;bay} perimeter, {1;row;bay} central, {2;bank;bay} accessible", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Aisles", "A", "Central aisle rectangles", GH_ParamAccess.list);
            pManager.AddCurveParameter("FlowArrows", "FA", "Flow-arrow chevrons", GH_ParamAccess.list);
            pManager.AddIntegerParameter("BayCount", "N", "Total parking spaces (standard + accessible)", GH_ParamAccess.item);
            pManager.AddIntegerParameter("PerimeterCount", "NP", "Perimeter standard spaces", GH_ParamAccess.item);
            pManager.AddIntegerParameter("CentralCount", "NC", "Central standard spaces", GH_ParamAccess.item);
            pManager.AddIntegerParameter("AccessibleCount", "NA", "Accessible spaces", GH_ParamAccess.item);
            pManager.AddTextParameter("Warnings", "W!", "Non-fatal generation warnings", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var p = new LayoutParameters();

            Curve working = null;
            if (!DA.GetData(InWorking, ref working)) return;
            p.WorkingBoundary = working;

            Curve raw = null;
            if (DA.GetData(InRaw, ref raw)) p.RawBoundary = raw;

            double d = 0.0;
            if (DA.GetData(InOrientation, ref d)) p.OrientationDeg = d;
            if (DA.GetData(InBayWidth, ref d)) p.BayWidth = d;
            if (DA.GetData(InBayDepth, ref d)) p.BayDepth = d;
            if (DA.GetData(InAisleWidth, ref d)) p.AisleWidth = d;
            if (DA.GetData(InBayAngle, ref d)) p.BayAngleDeg = d;
            if (DA.GetData(InAccessibleWidth, ref d)) p.AccessibleWidth = d;
            if (DA.GetData(InAccessibleDepth, ref d)) p.AccessibleDepth = d;

            var edges = new List<bool>();
            if (DA.GetDataList(InPerimeterEdges, edges) && edges.Count > 0) p.PerimeterEdges = edges;

            var exclusions = new List<Curve>();
            if (DA.GetDataList(InExclusions, exclusions) && exclusions.Count > 0) p.ExclusionZones = exclusions;

            Point3d accessiblePoint = Point3d.Unset;
            if (DA.GetData(InAccessiblePoint, ref accessiblePoint) && accessiblePoint.IsValid)
                p.AccessiblePoint = accessiblePoint;

            int count = SardineDefaults.AutomaticAccessibleCount;
            if (DA.GetData(InAccessibleCount, ref count)) p.AccessibleCount = count;

            bool b = false;
            if (DA.GetData(InStartFlow, ref b)) p.StartFlowPositive = b;
            if (DA.GetData(InShowFlow, ref b)) p.ShowFlow = b;

            LayoutResult result;
            try
            {
                result = LayoutGenerator.Generate(p);
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Layout generation failed unexpectedly: " + ex.Message);
                return;
            }

            foreach (var warning in result.Warnings)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, warning);

            if (!result.Success)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, result.Error);
                DA.SetDataList(OutWarnings, new List<string>(result.Warnings));
                return;
            }

            Message = result.BayCount + " bays";

            DA.SetDataTree(OutLayout, LayoutTreeBuilder.Build(result));
            DA.SetDataList(OutAisles, LayoutTreeBuilder.Duplicate(result.Aisles));
            DA.SetDataList(OutFlow, LayoutTreeBuilder.Duplicate(result.FlowArrows));
            DA.SetData(OutBayCount, result.BayCount);
            DA.SetData(OutPerimeterCount, result.PerimeterCount);
            DA.SetData(OutCentralCount, result.CentralCount);
            DA.SetData(OutAccessibleCount, result.AccessibleCount);
            DA.SetDataList(OutWarnings, new List<string>(result.Warnings));
        }
    }
}
