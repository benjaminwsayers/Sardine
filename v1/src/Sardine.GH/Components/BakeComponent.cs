using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Sardine.Core;
using Sardine.Core.Reporting;
using Sardine.GH.Conversion;
using Sardine.GH.Document;

namespace Sardine.GH.Components
{
    /// <summary>
    /// Sardine.Bake — writes the current option into the Rhino document on the
    /// Sardine layer hierarchy (PRD sections 20 and 21).
    ///
    /// Baking happens only on a rising edge of the Trigger input, so repeated
    /// Grasshopper recomputation never duplicates geometry. The ObjectIds of the
    /// last bake remain on the outputs after the trigger returns to false so that
    /// Sardine.Export can consume them.
    /// </summary>
    public class BakeComponent : GH_Component
    {
        private bool _lastTrigger;
        private List<Guid> _lastIds = new List<Guid>();
        private string _lastStatus = "Ready. Set Trigger to True to bake.";

        public BakeComponent()
            : base("Sardine.Bake", "Bake",
                "Bake the generated option into Rhino layers. Fires once per Trigger rising edge.",
                SardineCategory.Tab, SardineCategory.Output)
        { }

        public override Guid ComponentGuid
        {
            get { return new Guid("A4D1F7C3-58E2-4B6A-9F0D-3C7E2B1A8D65"); }
        }

        public override GH_Exposure Exposure { get { return GH_Exposure.primary; } }

        private const int InBoundary = 0, InLayout = 1, InAisles = 2, InFlow = 3, InName = 4, InTrigger = 5;
        private const int OutIds = 0, OutCount = 1, OutStatus = 2;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Boundary", "B", "Site boundary to bake (Working or Raw)", GH_ParamAccess.item);
            pManager.AddCurveParameter("Layout", "L", "Layout DataTree from Sardine.Layout", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Aisles", "A", "Aisles from Sardine.Layout", GH_ParamAccess.list);
            pManager.AddCurveParameter("FlowArrows", "FA", "Flow arrows from Sardine.Layout", GH_ParamAccess.list);
            pManager.AddTextParameter("Name", "N", "Option name applied to baked object names", GH_ParamAccess.item, SardineDefaults.DefaultOptionName);
            pManager.AddBooleanParameter("Trigger", "T", "Bake on the False→True transition (connect a Button or Toggle)", GH_ParamAccess.item, false);

            for (int i = InBoundary; i <= InName; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new Param_Guid(), "ObjectIds", "ID", "Ids of the objects created by the last bake", GH_ParamAccess.list);
            pManager.AddIntegerParameter("BakedCount", "N", "Number of objects created by the last bake", GH_ParamAccess.item);
            pManager.AddTextParameter("Status", "S", "Bake status", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool trigger = false;
            DA.GetData(InTrigger, ref trigger);

            if (!trigger)
            {
                _lastTrigger = false;
                EmitLast(DA);
                return;
            }

            if (_lastTrigger)
            {
                // Already baked on this True; wait for the trigger to reset.
                EmitLast(DA);
                return;
            }
            _lastTrigger = true;

            try
            {
                _lastStatus = DoBake(DA);
            }
            catch (Exception ex)
            {
                _lastStatus = "ERROR: " + ex.Message;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, _lastStatus);
            }

            EmitLast(DA);
        }

        private string DoBake(IGH_DataAccess DA)
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No active Rhino document.");
                return "ERROR: No active Rhino document.";
            }

            Curve boundary = null;
            DA.GetData(InBoundary, ref boundary);

            GH_Structure<GH_Curve> layoutTree;
            DA.GetDataTree(InLayout, out layoutTree);
            var layout = LayoutTreeReader.Read(layoutTree);

            var aisles = new List<Curve>();
            DA.GetDataList(InAisles, aisles);

            var arrows = new List<Curve>();
            DA.GetDataList(InFlow, arrows);

            string name = SardineDefaults.DefaultOptionName;
            DA.GetData(InName, ref name);
            name = OptionNaming.Resolve(name, SardineDefaults.DefaultOptionName);

            if (layout.Unclassified > 0)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    layout.Unclassified + " layout curve(s) had a path prefix other than 0, 1 or 2 and were not baked.");

            if (boundary == null && layout.Total == 0 && aisles.Count == 0 && arrows.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Nothing to bake. Connect the Sardine.Layout outputs.");
                return "Nothing to bake.";
            }

            var baker = new LayoutBaker(doc, name);
            var result = baker.Bake(boundary, layout.Perimeter, layout.Central, layout.Accessible, aisles, arrows);

            _lastIds = new List<Guid>(result.ObjectIds);

            string status = string.Format("Baked {0} objects for '{1}' to the Sardine layers.", result.BakedCount, name);
            if (result.Failed > 0)
            {
                status += string.Format(" {0} object(s) could not be added.", result.Failed);
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, status);
            }
            return status;
        }

        private void EmitLast(IGH_DataAccess DA)
        {
            DA.SetDataList(OutIds, _lastIds);
            DA.SetData(OutCount, _lastIds.Count);
            DA.SetData(OutStatus, _lastStatus);
            Message = _lastIds.Count > 0 ? _lastIds.Count + " baked" : null;
        }
    }
}
