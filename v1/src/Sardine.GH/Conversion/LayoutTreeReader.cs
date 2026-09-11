using System.Collections.Generic;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Sardine.Core.Models;

namespace Sardine.GH.Conversion
{
    /// <summary>
    /// Reads a Layout DataTree back into classified curve lists using the first
    /// path index as the <see cref="BayKind"/> (PRD section 21: the DataTree prefix
    /// determines the bay layer classification).
    /// </summary>
    public sealed class LayoutTreeReader
    {
        public List<Curve> Perimeter { get; } = new List<Curve>();
        public List<Curve> Central { get; } = new List<Curve>();
        public List<Curve> Accessible { get; } = new List<Curve>();

        /// <summary>Number of curves whose path prefix was not 0, 1 or 2.</summary>
        public int Unclassified { get; private set; }

        /// <summary>Number of null or invalid items skipped.</summary>
        public int Skipped { get; private set; }

        public int Total { get { return Perimeter.Count + Central.Count + Accessible.Count; } }

        public static LayoutTreeReader Read(GH_Structure<GH_Curve> tree)
        {
            var reader = new LayoutTreeReader();
            if (tree == null) return reader;

            for (int b = 0; b < tree.PathCount; b++)
            {
                GH_Path path = tree.get_Path(b);
                var branch = tree.get_Branch(path);
                if (branch == null) continue;

                int prefix = path.Indices.Length > 0 ? path.Indices[0] : -1;

                foreach (object item in branch)
                {
                    var goo = item as GH_Curve;
                    if (goo == null || goo.Value == null || !goo.Value.IsValid)
                    {
                        reader.Skipped++;
                        continue;
                    }

                    switch (prefix)
                    {
                        case (int)BayKind.Perimeter: reader.Perimeter.Add(goo.Value); break;
                        case (int)BayKind.Central: reader.Central.Add(goo.Value); break;
                        case (int)BayKind.Accessible: reader.Accessible.Add(goo.Value); break;
                        default: reader.Unclassified++; break;
                    }
                }
            }

            return reader;
        }
    }
}
