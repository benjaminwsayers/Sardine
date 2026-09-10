using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino;
using Sardine.Core;
using Sardine.Core.Models;
using Sardine.Core.Reporting;
using Sardine.GH.Document;

namespace Sardine.GH.Components
{
    /// <summary>
    /// Sardine.Export — exports the objects produced by Sardine.Bake to DWG and
    /// appends an option summary record to a CSV (PRD section 22).
    ///
    /// Runs only on a rising edge of the Trigger input. The export scope is exactly
    /// the supplied ObjectIds; nothing else in the document is inferred.
    /// </summary>
    public class ExportComponent : GH_Component
    {
        private bool _lastTrigger;
        private string _lastDwg = string.Empty;
        private string _lastCsv = string.Empty;
        private string _lastStatus = "Ready. Set Trigger to True to export.";

        public ExportComponent()
            : base("Sardine.Export", "Export",
                "Export the baked option to DWG and append a summary record to CSV. Fires once per Trigger rising edge.",
                SardineCategory.Tab, SardineCategory.Output)
        { }

        public override Guid ComponentGuid
        {
            get { return new Guid("D8C5B2E9-1A4F-47C3-8E6B-5F0A9D3C7B21"); }
        }

        public override GH_Exposure Exposure { get { return GH_Exposure.primary; } }

        private const int InIds = 0, InOptionName = 1, InDirectory = 2, InBayWidth = 3, InBayDepth = 4, InBayAngle = 5,
            InAisleWidth = 6, InTotal = 7, InPerimeter = 8, InCentral = 9, InAccessible = 10, InTrigger = 11;
        private const int OutDwg = 0, OutCsv = 1, OutStatus = 2;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new Param_Guid(), "ObjectIds", "ID", "ObjectIds output of Sardine.Bake", GH_ParamAccess.list);
            pManager.AddTextParameter("OptionName", "N", "Option name (DWG file stem and CSV record). Derived from the parameters if blank.", GH_ParamAccess.item);
            pManager.AddTextParameter("OutputDirectory", "D", "Folder for the DWG and CSV. Created if missing.", GH_ParamAccess.item);
            pManager.AddNumberParameter("BayWidth", "BW", "Bay width used for the option (m)", GH_ParamAccess.item, SardineDefaults.BayWidth);
            pManager.AddNumberParameter("BayDepth", "BD", "Bay depth used for the option (m)", GH_ParamAccess.item, SardineDefaults.BayDepth);
            pManager.AddNumberParameter("BayAngle", "BA", "Bay angle used for the option (deg)", GH_ParamAccess.item, SardineDefaults.BayAngleDeg);
            pManager.AddNumberParameter("AisleWidth", "AW", "Aisle width used for the option (m)", GH_ParamAccess.item, SardineDefaults.AisleWidth);
            pManager.AddIntegerParameter("TotalCount", "N", "BayCount from Sardine.Layout", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("PerimeterCount", "NP", "PerimeterCount from Sardine.Layout", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("CentralCount", "NC", "CentralCount from Sardine.Layout", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("AccessibleCount", "NA", "AccessibleCount from Sardine.Layout", GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("Trigger", "T", "Export on the False→True transition (connect a Button or Toggle)", GH_ParamAccess.item, false);

            pManager[InOptionName].Optional = true;
            for (int i = InBayWidth; i <= InAccessible; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("DWGPath", "DWG", "Full path of the exported DWG", GH_ParamAccess.item);
            pManager.AddTextParameter("CSVPath", "CSV", "Full path of the summary CSV", GH_ParamAccess.item);
            pManager.AddTextParameter("Status", "S", "Export status", GH_ParamAccess.item);
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
                EmitLast(DA);
                return;
            }
            _lastTrigger = true;

            try
            {
                _lastStatus = DoExport(DA);
            }
            catch (Exception ex)
            {
                _lastStatus = "ERROR: " + ex.Message;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, _lastStatus);
            }

            EmitLast(DA);
        }

        private string DoExport(IGH_DataAccess DA)
        {
            var ids = new List<Guid>();
            DA.GetDataList(InIds, ids);

            string directory = null;
            DA.GetData(InDirectory, ref directory);

            double bayWidth = SardineDefaults.BayWidth, bayDepth = SardineDefaults.BayDepth;
            double bayAngle = SardineDefaults.BayAngleDeg, aisleWidth = SardineDefaults.AisleWidth;
            DA.GetData(InBayWidth, ref bayWidth);
            DA.GetData(InBayDepth, ref bayDepth);
            DA.GetData(InBayAngle, ref bayAngle);
            DA.GetData(InAisleWidth, ref aisleWidth);

            int total = 0, perimeter = 0, central = 0, accessible = 0;
            DA.GetData(InTotal, ref total);
            DA.GetData(InPerimeter, ref perimeter);
            DA.GetData(InCentral, ref central);
            DA.GetData(InAccessible, ref accessible);

            string optionName = null;
            DA.GetData(InOptionName, ref optionName);
            optionName = OptionNaming.Resolve(optionName, OptionNaming.BuildDefaultName(bayWidth, bayDepth, bayAngle, aisleWidth));

            if (string.IsNullOrWhiteSpace(directory))
                return Fail("OutputDirectory is required.");

            if (ids.Count == 0)
                return Fail("No ObjectIds supplied. Connect the ObjectIds output of Sardine.Bake and bake an option first.");

            var doc = RhinoDoc.ActiveDoc;
            if (doc == null)
                return Fail("No active Rhino document.");

            directory = directory.Trim();
            Directory.CreateDirectory(directory);

            string dwgPath = DwgExporter.UniquePath(Path.Combine(directory, OptionNaming.ToFileStem(optionName) + ".dwg"));
            string csvPath = Path.Combine(directory, SardineDefaults.SummaryCsvFileName);

            // ── DWG ─────────────────────────────────────────────────────────
            int selected;
            string dwgMessage;
            bool dwgOk = DwgExporter.Export(doc, ids, dwgPath, out selected, out dwgMessage);
            if (!dwgOk)
                return Fail("DWG export failed: " + dwgMessage);

            _lastDwg = dwgPath;

            // ── CSV ─────────────────────────────────────────────────────────
            var summary = new OptionSummary
            {
                OptionName = optionName,
                BayWidth = bayWidth,
                BayDepth = bayDepth,
                BayAngleDeg = bayAngle,
                AisleWidth = aisleWidth,
                TotalCount = total,
                PerimeterCount = perimeter,
                CentralCount = central,
                AccessibleCount = accessible,
                DwgFileName = Path.GetFileName(dwgPath),
                GeneratedAt = DateTime.Now
            };

            try
            {
                SummaryCsvFile.Append(csvPath, summary);
                _lastCsv = csvPath;
            }
            catch (Exception ex)
            {
                string msg = string.Format("DWG exported ({0} objects) to {1} but the CSV could not be written: {2}",
                    selected, dwgPath, ex.Message);
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, msg);
                return msg;
            }

            return string.Format("Exported {0} objects to {1} and appended '{2}' to {3}.",
                selected, dwgPath, optionName, csvPath);
        }

        private string Fail(string message)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, message);
            return "ERROR: " + message;
        }

        private void EmitLast(IGH_DataAccess DA)
        {
            DA.SetData(OutDwg, _lastDwg);
            DA.SetData(OutCsv, _lastCsv);
            DA.SetData(OutStatus, _lastStatus);
        }
    }
}
