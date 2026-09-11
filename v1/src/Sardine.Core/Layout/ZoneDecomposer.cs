using System.Collections.Generic;
using Rhino.Geometry;
using Sardine.Core.Geometry;
using Sardine.Core.Models;

namespace Sardine.Core.Layout
{
    /// <summary>
    /// Site decomposition (proof-of-concept 02_ZoneDecomposer).
    ///
    /// Derives the perimeter alignment (boundary inset by bay depth) and the central
    /// parking region (boundary inset by bay depth + aisle width), plus the dominant
    /// fill directions of the central region.
    /// </summary>
    public static class ZoneDecomposer
    {
        public const string ErrorPerimeterOffset =
            "Perimeter offset failed. Bay depth may be too large for this site.";

        public const string WarningCentralOffset =
            "Central boundary offset failed. Bay depth + aisle width may be too large for this site; " +
            "central parking was skipped.";

        /// <summary>Maximum number of dominant directions reported.</summary>
        public const int MaxFillDirections = 3;

        /// <summary>
        /// Decomposes the working boundary. Returns null with <paramref name="error"/>
        /// set when the perimeter inset cannot be produced. A missing central inset is
        /// not fatal: the result has no central zone and <paramref name="warning"/> is set.
        /// </summary>
        public static SiteZones Decompose(
            Curve workingBoundary,
            double bayDepth,
            double aisleWidth,
            double tolerance,
            out string error,
            out string warning)
        {
            error = null;
            warning = null;

            if (workingBoundary == null || !workingBoundary.IsClosed)
            {
                error = "Working boundary must be a closed curve.";
                return null;
            }

            Curve perimeterInner = CurveOffsets.OffsetInward(workingBoundary, bayDepth, tolerance);
            if (perimeterInner == null)
            {
                error = ErrorPerimeterOffset;
                return null;
            }

            Curve centralBoundary = CurveOffsets.OffsetInward(workingBoundary, bayDepth + aisleWidth, tolerance);
            if (centralBoundary == null)
                warning = WarningCentralOffset;

            // Dominant fill directions: from the central region when it exists,
            // otherwise from the working boundary itself.
            var directions = SiteGeometry.DominantDirections(centralBoundary ?? workingBoundary, MaxFillDirections);
            var angles = new List<double>(directions.Count);
            foreach (var d in directions) angles.Add(SiteGeometry.AngleFromWorldXDeg(d));

            return new SiteZones(perimeterInner, centralBoundary, directions, angles);
        }
    }
}
