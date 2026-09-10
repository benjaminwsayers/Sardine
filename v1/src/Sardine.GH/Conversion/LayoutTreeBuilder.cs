using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using Sardine.Core.Models;

namespace Sardine.GH.Conversion
{
    /// <summary>
    /// Converts a Core <see cref="LayoutResult"/> to the public Grasshopper DataTree
    /// path convention (PRD section 19):
    ///
    ///   {0; edge; bay}   Perimeter standard bay
    ///   {1; row;  bay}   Central standard bay
    ///   {2; bank; bay}   Accessible geometry (bank 0 = bays, bank 1 = margin strip)
    ///
    /// One curve per branch. Index gaps left by exclusion or accessible
    /// allocation are preserved.
    /// </summary>
    public static class LayoutTreeBuilder
    {
        public static DataTree<Curve> Build(LayoutResult result)
        {
            var tree = new DataTree<Curve>();
            if (result == null) return tree;

            foreach (var bay in result.Bays)
                tree.Add(bay.Outline.DuplicateCurve(), PathFor(bay));

            return tree;
        }

        public static GH_Path PathFor(Bay bay)
        {
            return new GH_Path((int)bay.Kind, bay.GroupIndex, bay.BayIndex);
        }

        public static List<Curve> Duplicate(IReadOnlyList<Curve> curves)
        {
            var list = new List<Curve>();
            if (curves == null) return list;
            foreach (var c in curves)
                if (c != null) list.Add(c.DuplicateCurve());
            return list;
        }
    }
}
