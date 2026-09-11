using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;
using Sardine.Core.Compliance;
using Sardine.Core.Geometry;
using Sardine.Core.Models;

namespace Sardine.Core.Layout
{
    /// <summary>
    /// Complete car park generation behind Sardine.Layout (PRD section 11).
    ///
    /// Composes the internal stages in order:
    ///   decomposition → perimeter bays → central bays → composition →
    ///   exclusion culling → accessible allocation → counts.
    ///
    /// Never throws for invalid input: validation failures produce a failed
    /// <see cref="LayoutResult"/> with an error message, and recoverable problems
    /// produce warnings.
    /// </summary>
    public static class LayoutGenerator
    {
        public static LayoutResult Generate(LayoutParameters p)
        {
            var result = new LayoutResult();
            if (p == null) { result.Fail("No layout parameters supplied."); return result; }

            double tol = p.Tolerance > 0.0 && !double.IsNaN(p.Tolerance) ? p.Tolerance : Tolerances.Geometry;

            // ── Validate dimensions ─────────────────────────────────────────
            string dimError = ValidateDimensions(p);
            if (dimError != null) { result.Fail(dimError); return result; }

            // ── Build the site model from the supplied boundaries ───────────
            string siteError;
            SiteBoundary site = BuildSite(p.WorkingBoundary, p.RawBoundary, result, out siteError);
            if (site == null) { result.Fail(siteError); return result; }

            if (p.PerimeterEdges != null && p.PerimeterEdges.Count > site.EdgeCount)
                result.AddWarning(string.Format(
                    "PerimeterEdges has {0} entries but the boundary has {1} edges; extra entries were ignored.",
                    p.PerimeterEdges.Count, site.EdgeCount));

            // ── Decomposition ───────────────────────────────────────────────
            string zoneError, zoneWarning;
            var workingCurve = site.WorkingBoundaryCurve();
            SiteZones zones = ZoneDecomposer.Decompose(workingCurve, p.BayDepth, p.AisleWidth, tol, out zoneError, out zoneWarning);
            if (zones == null) { result.Fail(zoneError); return result; }
            if (zoneWarning != null) result.AddWarning(zoneWarning);
            result.Zones = zones;

            // ── Perimeter bays ──────────────────────────────────────────────
            var perimeter = PerimeterBayGenerator.Generate(
                zones.PerimeterInner, site, site.RawBoundary,
                p.BayWidth, p.BayDepth, p.PerimeterEdges, tol);

            // ── Central bays ────────────────────────────────────────────────
            var standard = new List<Bay>(perimeter);
            if (zones.HasCentralZone)
            {
                var central = CentralBayGenerator.Generate(
                    zones.CentralBoundary,
                    p.BayWidth, p.BayDepth, p.AisleWidth, p.BayAngleDeg, p.OrientationDeg,
                    p.StartFlowPositive, p.ShowFlow, tol);

                standard.AddRange(central.Bays);
                result.AddAisles(central.Aisles);
                result.AddFlowArrows(central.FlowArrows);
            }

            // ── Exclusion zones ─────────────────────────────────────────────
            var exclusionWarnings = new List<string>();
            var exclusions = ExclusionCuller.ValidExclusions(p.ExclusionZones, exclusionWarnings);
            result.AddWarnings(exclusionWarnings);

            int removed;
            standard = ExclusionCuller.Cull(standard, exclusions, tol, out removed);

            if (standard.Count == 0)
                result.AddWarning("No standard bays could be generated for this boundary and parameter set.");

            // ── Accessible bays ─────────────────────────────────────────────
            bool automatic;
            int accessibleCount = AccessibleProvision.Resolve(p.AccessibleCount, standard.Count, out automatic);

            if (accessibleCount > 0)
            {
                Point3d proximity = p.AccessiblePoint ?? site.Centroid;
                var accessible = AccessibleBayAllocator.Allocate(
                    standard, proximity, accessibleCount,
                    p.AccessibleWidth, p.AccessibleDepth, p.BayWidth, tol);

                if (accessible.Warning != null) result.AddWarning(accessible.Warning);

                result.AddBays(accessible.Remaining);
                result.AddBays(accessible.Accessible);
                result.AddBays(accessible.Margins);
            }
            else
            {
                result.AddBays(standard);
            }

            return result;
        }

        // ── Validation ──────────────────────────────────────────────────────

        private static string ValidateDimensions(LayoutParameters p)
        {
            if (!IsFinitePositive(p.BayWidth)) return "BayWidth must be a positive number of metres.";
            if (!IsFinitePositive(p.BayDepth)) return "BayDepth must be a positive number of metres.";
            if (!IsFinitePositive(p.AisleWidth)) return "AisleWidth must be a positive number of metres.";
            if (!IsFinitePositive(p.AccessibleWidth)) return "AccessibleWidth must be a positive number of metres.";
            if (!IsFinitePositive(p.AccessibleDepth)) return "AccessibleDepth must be a positive number of metres.";
            if (double.IsNaN(p.BayAngleDeg) || p.BayAngleDeg <= 0.0 || p.BayAngleDeg > 90.0)
                return "BayAngle must be greater than 0 and at most 90 degrees.";
            if (double.IsNaN(p.OrientationDeg) || double.IsInfinity(p.OrientationDeg))
                return "Orientation must be a finite angle in degrees.";
            return null;
        }

        private static bool IsFinitePositive(double v)
        {
            return !double.IsNaN(v) && !double.IsInfinity(v) && v > 0.0;
        }

        // ── Site model reconstruction ───────────────────────────────────────

        /// <summary>
        /// Rebuilds the site model from the WorkingBoundary / RawBoundary curves
        /// supplied to the component. The working polyline's segment order defines
        /// the edge indices used by PerimeterEdges.
        /// </summary>
        private static SiteBoundary BuildSite(Curve working, Curve raw, LayoutResult result, out string error)
        {
            error = null;

            if (working == null) { error = "WorkingBoundary is required. Connect the WorkingBoundary output of Sardine.Site."; return null; }
            if (!working.IsClosed) { error = "WorkingBoundary must be a closed curve."; return null; }
            if (!working.IsPlanar()) { error = "WorkingBoundary must be planar."; return null; }

            bool converted;
            Polyline poly = SiteGeometry.ToPolyline(working, out converted);
            if (poly == null) { error = "WorkingBoundary could not be interpreted as a polyline."; return null; }
            if (converted)
                result.AddWarning("WorkingBoundary was not a polyline and was approximated. Connect the WorkingBoundary output of Sardine.Site for reliable edge indexing.");
            if (!poly.IsClosed) poly.Add(poly[0]);

            var edges = new List<SiteEdge>();
            for (int i = 0; i < poly.Count - 1; i++)
            {
                var line = new Line(poly[i], poly[i + 1]);
                if (line.Length < RhinoMath.ZeroTolerance) continue;
                edges.Add(new SiteEdge(edges.Count, line));
            }
            if (edges.Count < 3) { error = "WorkingBoundary must have at least three edges."; return null; }

            double area = SiteGeometry.ComputeArea(poly);
            if (area <= RhinoMath.ZeroTolerance) { error = "WorkingBoundary has zero area."; return null; }

            Curve containment = raw;
            if (containment != null && !containment.IsClosed)
            {
                result.AddWarning("RawBoundary is not a closed curve; WorkingBoundary is used for containment instead.");
                containment = null;
            }
            if (containment == null) containment = new PolylineCurve(poly);

            return new SiteBoundary(
                containment,
                poly,
                edges,
                area,
                SiteGeometry.ComputePerimeter(poly),
                SiteGeometry.ComputeCentroid(poly),
                SiteGeometry.ComputeBoundingBox(poly),
                converted,
                new List<string>());
        }
    }
}
