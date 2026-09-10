using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace Sardine.Core.Models
{
    /// <summary>
    /// Immutable output model of the site rationalisation pipeline.
    /// Produced by SiteRationaliser, consumed by all downstream components.
    /// 
    /// Construction is via the nested Builder class only.
    /// </summary>
    public class SiteBoundary
    {
        // ── Geometry ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Original supplied boundary, preserved separately from the working polyline.
        /// </summary>
        public Curve RawBoundary { get; private set; }

        /// <summary>
        /// Rationalised working polygon produced by the Site pipeline.
        /// All computation (OBB, tray modules, bay placement) runs on this.
        /// </summary>
        public Polyline WorkingBoundary { get; private set; }

        // ── Topology ─────────────────────────────────────────────────────────────

        /// <summary>Decomposed edges of WorkingBoundary with per-edge metadata.</summary>
        public IReadOnlyList<SiteEdge> Edges { get; private set; }

        /// <summary>Total edge count of WorkingBoundary.</summary>
        public int EdgeCount { get; private set; }

        // ── Spatial properties ────────────────────────────────────────────────────

        /// <summary>
        /// Planar area of WorkingBoundary in model units squared.
        /// Used by downstream layout calculations.
        /// </summary>
        public double Area { get; private set; }

        /// <summary>Perimeter length of WorkingBoundary in model units.</summary>
        public double PerimeterLength { get; private set; }

        /// <summary>
        /// Area centroid of WorkingBoundary.
        /// Used as the default proximity point when downstream layout inputs omit one.
        /// </summary>
        public Point3d Centroid { get; private set; }

        /// <summary>Axis-aligned bounding box of WorkingBoundary.</summary>
        public BoundingBox BoundingBox { get; private set; }

        /// <summary>
        /// Best-fit plane through WorkingBoundary vertices.
        /// Used for orienting downstream layout geometry.
        /// </summary>
        public Plane BoundaryPlane { get; private set; }

        // ── Orientation seed ──────────────────────────────────────────────────────

        /// <summary>
        /// Dominant edge direction of WorkingBoundary (longest edge wins).
        /// Used as the downstream layout orientation seed.
        /// </summary>
        public Vector3d PrimaryAxis { get; private set; }

        /// <summary>
        /// Angle of PrimaryAxis relative to World X, in degrees.
        /// Exposed as the downstream layout orientation seed in degrees.
        /// </summary>
        public double PrimaryAxisAngleDeg { get; private set; }

        // ── Diagnostics ───────────────────────────────────────────────────────────

        /// <summary>
        /// True if the input curve was not a polyline and was approximated.
        /// The GH component surfaces this as a warning message.
        /// </summary>
        public bool InputWasConverted { get; private set; }

        /// <summary>
        /// Diagnostic and warning strings from the rationalisation pipeline.
        /// Always a valid list — never null. Empty on clean input.
        /// </summary>
        public IReadOnlyList<string> Warnings { get; private set; }

        // ── Construction ──────────────────────────────────────────────────────────

        private SiteBoundary() { }

        public class Builder
        {
            private readonly SiteBoundary _site = new SiteBoundary();

            public Builder SetBoundaries(Curve raw, Polyline working)
            {
                _site.RawBoundary = raw;
                _site.WorkingBoundary = working;
                return this;
            }

            public Builder SetEdges(IReadOnlyList<SiteEdge> edges)
            {
                _site.Edges = edges;
                _site.EdgeCount = edges.Count;

                return this;
            }

            public Builder SetSpatialProperties(
                double area,
                double perimeterLength,
                Point3d centroid,
                BoundingBox boundingBox,
                Plane boundaryPlane)
            {
                _site.Area = area;
                _site.PerimeterLength = perimeterLength;
                _site.Centroid = centroid;
                _site.BoundingBox = boundingBox;
                _site.BoundaryPlane = boundaryPlane;
                return this;
            }

            public Builder SetOrientation(Vector3d primaryAxis, double primaryAxisAngleDeg)
            {
                _site.PrimaryAxis = primaryAxis;
                _site.PrimaryAxisAngleDeg = primaryAxisAngleDeg;
                return this;
            }

            public Builder SetDiagnostics(bool inputWasConverted, IReadOnlyList<string> warnings)
            {
                _site.InputWasConverted = inputWasConverted;
                _site.Warnings = warnings ?? new List<string>();
                return this;
            }

            public SiteBoundary Build()
            {
                if (_site.RawBoundary == null)
                    throw new InvalidOperationException(
                        "SiteBoundary.Builder: SetBoundaries() must be called before Build().");
                if (_site.WorkingBoundary == null)
                    throw new InvalidOperationException(
                        "SiteBoundary.Builder: WorkingBoundary is null — pipeline did not complete.");
                if (_site.Warnings == null)
                    _site.Warnings = new List<string>();

                return _site;
            }
        }
    }
}