using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;

namespace Sardine.Core.Models
{
    /// <summary>
    /// Complete output of <see cref="Sardine.Core.Layout.LayoutGenerator"/>.
    ///
    /// Uses ordinary .NET collections only; conversion to the Grasshopper DataTree
    /// path convention happens in Sardine.GH (PRD section 27).
    /// </summary>
    public sealed class LayoutResult
    {
        private readonly List<Bay> _bays = new List<Bay>();
        private readonly List<Curve> _aisles = new List<Curve>();
        private readonly List<Curve> _flowArrows = new List<Curve>();
        private readonly List<string> _warnings = new List<string>();

        /// <summary>False when generation could not proceed; see <see cref="Error"/>.</summary>
        public bool Success { get { return Error == null; } }

        /// <summary>Fatal error message, or null on success.</summary>
        public string Error { get; private set; }

        /// <summary>
        /// Every piece of bay geometry in deterministic order:
        /// perimeter bays, then central bays, then accessible geometry.
        /// </summary>
        public IReadOnlyList<Bay> Bays { get { return _bays; } }

        /// <summary>Central aisle rectangles, one per row pair that produced bays.</summary>
        public IReadOnlyList<Curve> Aisles { get { return _aisles; } }

        /// <summary>Flow-arrow chevrons along the aisle centrelines.</summary>
        public IReadOnlyList<Curve> FlowArrows { get { return _flowArrows; } }

        /// <summary>Non-fatal warnings in the order they were raised.</summary>
        public IReadOnlyList<string> Warnings { get { return _warnings; } }

        /// <summary>Decomposition zones, for diagnostics. Null when decomposition failed.</summary>
        public SiteZones Zones { get; set; }

        /// <summary>Final number of perimeter standard parking spaces.</summary>
        public int PerimeterCount { get { return CountSpaces(BayKind.Perimeter); } }

        /// <summary>Final number of central standard parking spaces.</summary>
        public int CentralCount { get { return CountSpaces(BayKind.Central); } }

        /// <summary>Final number of accessible parking spaces (margin strips excluded).</summary>
        public int AccessibleCount { get { return CountSpaces(BayKind.Accessible); } }

        /// <summary>Final total number of parking spaces.</summary>
        public int BayCount { get { return PerimeterCount + CentralCount + AccessibleCount; } }

        /// <summary>Number of standard spaces (perimeter + central).</summary>
        public int StandardCount { get { return PerimeterCount + CentralCount; } }

        public void AddBay(Bay bay)
        {
            if (bay != null) _bays.Add(bay);
        }

        public void AddBays(IEnumerable<Bay> bays)
        {
            if (bays == null) return;
            foreach (var bay in bays) AddBay(bay);
        }

        public void ReplaceBays(IEnumerable<Bay> bays)
        {
            _bays.Clear();
            AddBays(bays);
        }

        public void AddAisles(IEnumerable<Curve> aisles)
        {
            if (aisles != null) _aisles.AddRange(aisles.Where(c => c != null));
        }

        public void AddFlowArrows(IEnumerable<Curve> arrows)
        {
            if (arrows != null) _flowArrows.AddRange(arrows.Where(c => c != null));
        }

        public void AddWarning(string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning)) _warnings.Add(warning);
        }

        public void AddWarnings(IEnumerable<string> warnings)
        {
            if (warnings == null) return;
            foreach (var w in warnings) AddWarning(w);
        }

        /// <summary>Marks the result as failed. Geometry already added is retained for diagnostics.</summary>
        public void Fail(string error)
        {
            Error = string.IsNullOrWhiteSpace(error) ? "Layout generation failed." : error;
        }

        /// <summary>Convenience factory for a failed result.</summary>
        public static LayoutResult Failed(string error)
        {
            var r = new LayoutResult();
            r.Fail(error);
            return r;
        }

        private int CountSpaces(BayKind kind)
        {
            int n = 0;
            for (int i = 0; i < _bays.Count; i++)
                if (_bays[i].Kind == kind && _bays[i].IsParkingSpace) n++;
            return n;
        }
    }
}
