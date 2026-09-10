using System;
using System.Collections.Generic;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Sardine.GH.Document
{
    /// <summary>
    /// Adds one generated option to the Rhino document on the Sardine layers.
    /// Each call is a discrete operation; nothing is tracked or overwritten.
    /// </summary>
    public sealed class LayoutBaker
    {
        /// <summary>Result of one bake operation.</summary>
        public sealed class Result
        {
            public List<Guid> ObjectIds { get; } = new List<Guid>();
            public int Failed { get; set; }
            public int BakedCount { get { return ObjectIds.Count; } }
        }

        private readonly RhinoDoc _doc;
        private readonly SardineLayers _layers;
        private readonly string _optionName;

        public LayoutBaker(RhinoDoc doc, string optionName)
        {
            if (doc == null) throw new ArgumentNullException("doc");
            _doc = doc;
            _layers = SardineLayers.Ensure(doc);
            _optionName = optionName ?? string.Empty;
        }

        public Result Bake(
            Curve boundary,
            IList<Curve> perimeter,
            IList<Curve> central,
            IList<Curve> accessible,
            IList<Curve> aisles,
            IList<Curve> flowArrows)
        {
            var result = new Result();

            if (boundary != null)
                AddCurve(boundary, _layers.Boundary, "boundary", result);

            AddCurves(perimeter, _layers.Perimeter, "perimeter", result);
            AddCurves(central, _layers.Central, "central", result);
            AddCurves(accessible, _layers.Accessible, "accessible", result);
            AddCurves(aisles, _layers.Aisles, "aisle", result);
            AddCurves(flowArrows, _layers.Flow, "flow", result);

            _doc.Views.Redraw();
            return result;
        }

        private void AddCurves(IList<Curve> curves, int layerIndex, string suffix, Result result)
        {
            if (curves == null) return;
            foreach (var c in curves)
                if (c != null) AddCurve(c, layerIndex, suffix, result);
        }

        private void AddCurve(Curve curve, int layerIndex, string suffix, Result result)
        {
            var attributes = new ObjectAttributes
            {
                LayerIndex = layerIndex,
                Name = _optionName.Length > 0 ? _optionName + "_" + suffix : suffix
            };

            Guid id = _doc.Objects.AddCurve(curve, attributes);
            if (id == Guid.Empty) result.Failed++;
            else result.ObjectIds.Add(id);
        }
    }
}
