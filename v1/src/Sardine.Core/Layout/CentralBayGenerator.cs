using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;
using Sardine.Core.Geometry;
using Sardine.Core.Models;

namespace Sardine.Core.Layout
{
    /// <summary>
    /// Central bay generation (proof-of-concept 04_CentralBays).
    ///
    /// Rows are laid out in a grid frame rotated by the orientation angle. Each row
    /// pair is two back-to-back rows separated by an aisle:
    ///
    ///   row pitch      = 2 × bayDepth × sin(bayAngle) + aisleWidth
    ///   bay step       = bayWidth / sin(bayAngle)
    ///   angular offset = 90° − bayAngle   (0 at 90°: no rotation)
    ///
    /// Adjacent aisles alternate one-way flow direction; bay rotation flips with it.
    /// Row indices: rowPair × 2 for row A, rowPair × 2 + 1 for row B. Row pairs that
    /// place no bays leave a gap in the numbering (proof-of-concept behaviour).
    /// </summary>
    public static class CentralBayGenerator
    {
        /// <summary>Output of central generation.</summary>
        public sealed class Result
        {
            public List<Bay> Bays { get; } = new List<Bay>();
            public List<Curve> Aisles { get; } = new List<Curve>();
            public List<Curve> FlowArrows { get; } = new List<Curve>();

            /// <summary>Flow sign (+1 / −1) of each aisle, in aisle order.</summary>
            public List<int> FlowDirections { get; } = new List<int>();
        }

        public static Result Generate(
            Curve centralBoundary,
            double bayWidth,
            double bayDepth,
            double aisleWidth,
            double bayAngleDeg,
            double orientationDeg,
            bool startFlowPositive,
            bool showFlowArrows,
            double tolerance)
        {
            var result = new Result();
            if (centralBoundary == null || !centralBoundary.IsClosed) return result;
            if (bayWidth <= 0.0 || bayDepth <= 0.0 || aisleWidth <= 0.0 || bayAngleDeg <= 0.0) return result;

            // ── Grid axes ───────────────────────────────────────────────────
            double gridRad = RhinoMath.ToRadians(orientationDeg);
            var uAx = new Vector3d(Math.Cos(gridRad), Math.Sin(gridRad), 0.0);
            var vAx = new Vector3d(-Math.Sin(gridRad), Math.Cos(gridRad), 0.0);

            Point3d origin = SiteGeometry.ComputeCentroid(centralBoundary);

            // ── Oriented extent of the central boundary ─────────────────────
            double uMin = double.MaxValue, uMax = double.MinValue;
            double vMin = double.MaxValue, vMax = double.MinValue;

            Point3d[] samples;
            centralBoundary.DivideByCount(Tolerances.CentralBoundarySamples, true, out samples);
            if (samples == null || samples.Length == 0) return result;

            foreach (var p in samples)
            {
                var d = p - origin;
                double u = d.X * uAx.X + d.Y * uAx.Y;
                double v = d.X * vAx.X + d.Y * vAx.Y;
                if (u < uMin) uMin = u;
                if (u > uMax) uMax = u;
                if (v < vMin) vMin = v;
                if (v > vMax) vMax = v;
            }

            // ── Pitch and step ──────────────────────────────────────────────
            double sinA = Math.Sin(RhinoMath.ToRadians(bayAngleDeg));
            if (Math.Abs(sinA) < Tolerances.MinSinBayAngle) sinA = Tolerances.MinSinBayAngle;

            double bayDepthProj = bayDepth * sinA;
            double rowPitch = 2.0 * bayDepthProj + aisleWidth;
            double bayStep = bayWidth / sinA;
            double angularOffset = 90.0 - bayAngleDeg;

            double vStart = vMin - rowPitch;
            double vEnd = vMax + rowPitch;
            double uStart = uMin - bayStep * 2.0;
            double uEnd = uMax + bayStep * 2.0;

            int flowSign = startFlowPositive ? 1 : -1;
            int rowPair = 0;

            var xInterval = new Interval(-bayWidth * 0.5, bayWidth * 0.5);
            var yInterval = new Interval(0.0, bayDepth);

            for (double vBase = vStart; vBase < vEnd; vBase += rowPitch)
            {
                double vAisleStart = vBase + bayDepthProj;
                double vAisleEnd = vBase + bayDepthProj + aisleWidth;

                int rowA = rowPair * 2;
                int rowB = rowPair * 2 + 1;
                int countA = 0, countB = 0;

                // Bay rotation per flow direction. At 90° the offset is zero.
                double rotA = -flowSign * angularOffset;
                double rotB = flowSign * angularOffset;

                Vector3d bayDirA = SiteGeometry.RotateInXY(vAx, rotA);
                Vector3d bayWidA = SiteGeometry.RotateInXY(uAx, rotA);
                Vector3d bayDirB = SiteGeometry.RotateInXY(vAx, rotB);
                Vector3d bayWidB = SiteGeometry.RotateInXY(uAx, rotB);

                double pairUMin = double.MaxValue, pairUMax = double.MinValue;

                for (double u = uStart; u < uEnd; u += bayStep)
                {
                    double uCentre = u + bayStep * 0.5;

                    // Row A: bays extend away from the aisle in −vAx.
                    Point3d originA = origin + uAx * uCentre + vAx * vAisleStart;
                    var frameA = new Plane(originA, bayWidA, -bayDirA);
                    var cornersA = BayGeometry.RectangleCorners(frameA, xInterval, yInterval);
                    if (BayGeometry.AllCornersInsideOrCoincident(cornersA, centralBoundary, tolerance))
                    {
                        result.Bays.Add(new Bay(BayKind.Central, rowA, countA, frameA, cornersA, true));
                        countA++;
                        ExtendU(cornersA, origin, uAx, ref pairUMin, ref pairUMax);
                    }

                    // Row B: bays extend away from the aisle in +vAx.
                    Point3d originB = origin + uAx * uCentre + vAx * vAisleEnd;
                    var frameB = new Plane(originB, bayWidB, bayDirB);
                    var cornersB = BayGeometry.RectangleCorners(frameB, xInterval, yInterval);
                    if (BayGeometry.AllCornersInsideOrCoincident(cornersB, centralBoundary, tolerance))
                    {
                        result.Bays.Add(new Bay(BayKind.Central, rowB, countB, frameB, cornersB, true));
                        countB++;
                        ExtendU(cornersB, origin, uAx, ref pairUMin, ref pairUMax);
                    }
                }

                // ── Aisle between the two rows ──────────────────────────────
                if (countA > 0 || countB > 0)
                {
                    if (pairUMin > pairUMax) { pairUMin = 0.0; pairUMax = 0.0; }

                    double pad = bayStep * 0.5;
                    double aisleUMin = pairUMin - pad;
                    double aisleUMax = pairUMax + pad;

                    Point3d a0 = origin + uAx * aisleUMin + vAx * vAisleStart;
                    Point3d a1 = origin + uAx * aisleUMax + vAx * vAisleStart;
                    Point3d a2 = origin + uAx * aisleUMax + vAx * vAisleEnd;
                    Point3d a3 = origin + uAx * aisleUMin + vAx * vAisleEnd;
                    result.Aisles.Add(new PolylineCurve(new[] { a0, a1, a2, a3, a0 }));
                    result.FlowDirections.Add(flowSign);

                    if (showFlowArrows)
                    {
                        double vMid = (vAisleStart + vAisleEnd) * 0.5;
                        AddFlowArrows(result.FlowArrows, origin, uAx, vAx, flowSign,
                            aisleUMin, aisleUMax, vMid, aisleWidth);
                    }
                }

                flowSign *= -1;
                rowPair++;
            }

            return result;
        }

        private static void ExtendU(Point3d[] corners, Point3d origin, Vector3d uAx, ref double uMin, ref double uMax)
        {
            for (int i = 0; i < corners.Length; i++)
            {
                var d = corners[i] - origin;
                double u = d.X * uAx.X + d.Y * uAx.Y;
                if (u < uMin) uMin = u;
                if (u > uMax) uMax = u;
            }
        }

        /// <summary>
        /// Chevron arrows along the aisle centreline, spaced at three aisle widths
        /// (at least one arrow per aisle), pointing in the flow direction.
        /// </summary>
        private static void AddFlowArrows(
            List<Curve> arrows,
            Point3d origin, Vector3d uAx, Vector3d vAx,
            int flowSign, double uMin, double uMax, double vMid,
            double aisleWidth)
        {
            double spacing = aisleWidth * 3.0;
            double length = aisleWidth * 0.4;
            double halfWidth = aisleWidth * 0.2;

            Vector3d flowDir = uAx * flowSign;

            double uLen = uMax - uMin;
            if (uLen <= 0.0) return;
            if (uLen < spacing) spacing = uLen;

            double uPos = uMin + spacing * 0.5;
            while (uPos < uMax)
            {
                Point3d centre = origin + uAx * uPos + vAx * vMid;
                Point3d tip = centre + flowDir * (length * 0.5);
                Point3d tailL = centre - flowDir * (length * 0.5) + vAx * halfWidth;
                Point3d tailR = centre - flowDir * (length * 0.5) - vAx * halfWidth;
                arrows.Add(new PolylineCurve(new[] { tailL, tip, tailR }));
                uPos += spacing;
            }
        }
    }
}
