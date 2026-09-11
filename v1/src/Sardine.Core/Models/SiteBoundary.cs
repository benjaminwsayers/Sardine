using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace Sardine.Core.Models
{
    /// <summary>
    /// Result of site rationalisation (Sardine.Site).
    /// Holds the original boundary, the rationalised working polyline, the
    /// indexed edges and the non-fatal warnings raised during processing.
    /// </summary>
    public sealed class SiteBoundary
    {
        /// <summary>Duplicate of the originally supplied boundary curve.</summary>
        public Curve RawBoundary { get; }

        /// <summary>Closed, rationalised working polyline used for generation.</summary>
        public Polyline WorkingBoundary { get; }

        /// <summary>Indexed rationalised edges of <see cref="WorkingBoundary"/>.</summary>
        public IReadOnlyList<SiteEdge> Edges { get; }

        /// <summary>Number of rationalised edges.</summary>
        public int EdgeCount { get { return Edges.Count; } }

        /// <summary>Planar area of the working boundary in square metres.</summary>
        public double Area { get; }

        /// <summary>Perimeter length of the working boundary in metres.</summary>
        public double PerimeterLength { get; }

        /// <summary>Area centroid of the working boundary.</summary>
        public Point3d Centroid { get; }

        /// <summary>Axis-aligned bounding box of the working boundary.</summary>
        public BoundingBox BoundingBox { get; }

        /// <summary>True when the input was not a polyline and had to be approximated.</summary>
        public bool InputWasConverted { get; }

        /// <summary>Non-fatal warnings. Never null; empty on clean input.</summary>
        public IReadOnlyList<string> Warnings { get; }

        public SiteBoundary(
            Curve rawBoundary,
            Polyline workingBoundary,
            IReadOnlyList<SiteEdge> edges,
            double area,
            double perimeterLength,
            Point3d centroid,
            BoundingBox boundingBox,
            bool inputWasConverted,
            IReadOnlyList<string> warnings)
        {
            if (rawBoundary == null) throw new ArgumentNullException("rawBoundary");
            if (workingBoundary == null) throw new ArgumentNullException("workingBoundary");
            if (edges == null) throw new ArgumentNullException("edges");

            RawBoundary = rawBoundary;
            WorkingBoundary = workingBoundary;
            Edges = edges;
            Area = area;
            PerimeterLength = perimeterLength;
            Centroid = centroid;
            BoundingBox = boundingBox;
            InputWasConverted = inputWasConverted;
            Warnings = warnings ?? new List<string>();
        }

        /// <summary>The working boundary as a closed <see cref="PolylineCurve"/>.</summary>
        public PolylineCurve WorkingBoundaryCurve()
        {
            return new PolylineCurve(WorkingBoundary);
        }
    }
}
