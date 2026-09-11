using System.Collections.Generic;
using Rhino.Geometry;

namespace Sardine.Core.Models
{
    /// <summary>
    /// Explicit inputs for <see cref="Sardine.Core.Layout.LayoutGenerator"/>.
    /// Mirrors the public Sardine.Layout component inputs (PRD section 12).
    /// All dimensions are metres, all angles are degrees.
    /// </summary>
    public sealed class LayoutParameters
    {
        /// <summary>Rationalised closed polyline boundary produced by Sardine.Site. Required.</summary>
        public Curve WorkingBoundary { get; set; }

        /// <summary>
        /// Original supplied boundary. Used as the containment limit for perimeter bays
        /// because rationalisation can cut real site area at corners. Falls back to
        /// <see cref="WorkingBoundary"/> when omitted.
        /// </summary>
        public Curve RawBoundary { get; set; }

        /// <summary>Absolute grid orientation for central rows, degrees from World X.</summary>
        public double OrientationDeg { get; set; } = SardineDefaults.OrientationDeg;

        public double BayWidth { get; set; } = SardineDefaults.BayWidth;

        public double BayDepth { get; set; } = SardineDefaults.BayDepth;

        public double AisleWidth { get; set; } = SardineDefaults.AisleWidth;

        /// <summary>Bay angle relative to the aisle. 90 = perpendicular. Must be in (0, 90].</summary>
        public double BayAngleDeg { get; set; } = SardineDefaults.BayAngleDeg;

        /// <summary>
        /// One flag per rationalised edge. Missing entries default to true.
        /// Null or empty enables every edge.
        /// </summary>
        public IList<bool> PerimeterEdges { get; set; }

        /// <summary>Closed exclusion curves. Null entries and open curves are ignored with a warning.</summary>
        public IList<Curve> ExclusionZones { get; set; }

        /// <summary>Accessible bays are placed closest to this point. Site centroid when null.</summary>
        public Point3d? AccessiblePoint { get; set; }

        /// <summary>-1 = automatic (PRD section 17). Zero or greater = explicit count.</summary>
        public int AccessibleCount { get; set; } = SardineDefaults.AutomaticAccessibleCount;

        public double AccessibleWidth { get; set; } = SardineDefaults.AccessibleWidth;

        public double AccessibleDepth { get; set; } = SardineDefaults.AccessibleDepth;

        public bool StartFlowPositive { get; set; } = SardineDefaults.StartFlowPositive;

        public bool ShowFlow { get; set; } = SardineDefaults.ShowFlow;

        /// <summary>Geometric tolerance in metres.</summary>
        public double Tolerance { get; set; } = Tolerances.Geometry;
    }
}
