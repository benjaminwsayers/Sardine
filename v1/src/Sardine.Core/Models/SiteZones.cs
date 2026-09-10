using System.Collections.Generic;
using Rhino.Geometry;

namespace Sardine.Core.Models
{
    /// <summary>
    /// Result of site decomposition (internal stage corresponding to
    /// the proof-of-concept 02_ZoneDecomposer).
    /// </summary>
    public sealed class SiteZones
    {
        /// <summary>
        /// Working boundary inset by the bay depth. This is the alignment the
        /// perimeter generator walks; perimeter bays extend outward from it to
        /// the site boundary.
        /// </summary>
        public Curve PerimeterInner { get; }

        /// <summary>
        /// Working boundary inset by bay depth plus aisle width. Outer limit of
        /// the central parking grid. Null when the inset could not be produced;
        /// in that case central parking is skipped with a warning.
        /// </summary>
        public Curve CentralBoundary { get; }

        /// <summary>
        /// Dominant fill directions of the central boundary, strongest first,
        /// folded into the half-plane so opposite directions share one axis.
        /// </summary>
        public IReadOnlyList<Vector3d> FillDirections { get; }

        /// <summary>Angle of each fill direction from World X in degrees.</summary>
        public IReadOnlyList<double> FillAnglesDeg { get; }

        public SiteZones(
            Curve perimeterInner,
            Curve centralBoundary,
            IReadOnlyList<Vector3d> fillDirections,
            IReadOnlyList<double> fillAnglesDeg)
        {
            PerimeterInner = perimeterInner;
            CentralBoundary = centralBoundary;
            FillDirections = fillDirections ?? new List<Vector3d>();
            FillAnglesDeg = fillAnglesDeg ?? new List<double>();
        }

        /// <summary>True when a central parking region exists.</summary>
        public bool HasCentralZone { get { return CentralBoundary != null; } }
    }
}
