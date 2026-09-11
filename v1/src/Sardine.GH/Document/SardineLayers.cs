using System;
using System.Drawing;
using Rhino;
using Rhino.DocObjects;

namespace Sardine.GH.Document
{
    /// <summary>
    /// Creates or reuses the Sardine layer hierarchy (PRD section 21):
    ///
    ///   Sardine
    ///   ├── Sardine::Boundary
    ///   ├── Sardine::Perimeter
    ///   ├── Sardine::Central
    ///   ├── Sardine::Accessible
    ///   ├── Sardine::Aisles
    ///   └── Sardine::Flow
    /// </summary>
    public sealed class SardineLayers
    {
        public const string Parent = "Sardine";
        public const string BoundaryName = "Boundary";
        public const string PerimeterName = "Perimeter";
        public const string CentralName = "Central";
        public const string AccessibleName = "Accessible";
        public const string AislesName = "Aisles";
        public const string FlowName = "Flow";

        public int ParentIndex { get; private set; }
        public int Boundary { get; private set; }
        public int Perimeter { get; private set; }
        public int Central { get; private set; }
        public int Accessible { get; private set; }
        public int Aisles { get; private set; }
        public int Flow { get; private set; }

        private SardineLayers() { }

        /// <summary>Ensures every Sardine layer exists in <paramref name="doc"/> and returns their indices.</summary>
        public static SardineLayers Ensure(RhinoDoc doc)
        {
            if (doc == null) throw new ArgumentNullException("doc");

            var layers = new SardineLayers();
            layers.ParentIndex = EnsureLayer(doc, Parent, null, Color.DimGray);
            Guid parentId = doc.Layers[layers.ParentIndex].Id;

            layers.Boundary = EnsureLayer(doc, BoundaryName, parentId, Color.Red);
            layers.Perimeter = EnsureLayer(doc, PerimeterName, parentId, Color.Green);
            layers.Central = EnsureLayer(doc, CentralName, parentId, Color.Lime);
            layers.Accessible = EnsureLayer(doc, AccessibleName, parentId, Color.DodgerBlue);
            layers.Aisles = EnsureLayer(doc, AislesName, parentId, Color.Gray);
            layers.Flow = EnsureLayer(doc, FlowName, parentId, Color.Orange);
            return layers;
        }

        private static int EnsureLayer(RhinoDoc doc, string name, Guid? parentId, Color colour)
        {
            string fullPath = parentId.HasValue ? Parent + "::" + name : name;
            int existing = doc.Layers.FindByFullPath(fullPath, -1);
            if (existing >= 0) return existing;

            var layer = new Layer { Name = name, Color = colour };
            if (parentId.HasValue) layer.ParentLayerId = parentId.Value;

            int index = doc.Layers.Add(layer);
            if (index < 0)
                throw new InvalidOperationException("Could not create Rhino layer '" + fullPath + "'.");
            return index;
        }
    }
}
