using Rhino.Geometry;

namespace Sardine.Core.Geometry
{
    /// <summary>
    /// Inward offset of a closed planar curve (proof-of-concept 02_ZoneDecomposer).
    /// </summary>
    public static class CurveOffsets
    {
        /// <summary>
        /// Offsets a closed curve inward by <paramref name="distance"/> using sharp
        /// corners. Both offset directions are attempted and the closed result whose
        /// centroid lies inside the original curve is returned. Returns null when no
        /// valid inset exists (for example when the distance exceeds the site's half-width).
        /// </summary>
        public static Curve OffsetInward(Curve curve, double distance, double tolerance)
        {
            if (curve == null || !curve.IsClosed || distance <= 0.0) return null;

            var offsetA = curve.Offset(Plane.WorldXY, -distance, tolerance, CurveOffsetCornerStyle.Sharp);
            var offsetB = curve.Offset(Plane.WorldXY, distance, tolerance, CurveOffsetCornerStyle.Sharp);

            Curve a = LargestClosedCurve(offsetA);
            Curve b = LargestClosedCurve(offsetB);

            if (a == null && b == null) return null;
            if (a == null) return IsInside(b, curve, tolerance) ? b : null;
            if (b == null) return IsInside(a, curve, tolerance) ? a : null;

            bool aInside = IsInside(a, curve, tolerance);
            bool bInside = IsInside(b, curve, tolerance);

            if (aInside && !bInside) return a;
            if (bInside && !aInside) return b;
            if (!aInside && !bInside) return null;

            // Both inside (unusual): return the smaller, i.e. the more inset one.
            var ampA = AreaMassProperties.Compute(a);
            var ampB = AreaMassProperties.Compute(b);
            if (ampA == null) return b;
            if (ampB == null) return a;
            return ampA.Area < ampB.Area ? a : b;
        }

        /// <summary>Largest closed curve by area in an offset result, or null.</summary>
        public static Curve LargestClosedCurve(Curve[] curves)
        {
            if (curves == null || curves.Length == 0) return null;

            Curve largest = null;
            double maxArea = 0.0;
            foreach (var c in curves)
            {
                if (c == null || !c.IsClosed) continue;
                var amp = AreaMassProperties.Compute(c);
                if (amp == null) continue;
                if (amp.Area > maxArea)
                {
                    maxArea = amp.Area;
                    largest = c;
                }
            }
            return largest;
        }

        private static bool IsInside(Curve candidate, Curve original, double tolerance)
        {
            var amp = AreaMassProperties.Compute(candidate);
            if (amp == null) return false;
            return original.Contains(amp.Centroid, Plane.WorldXY, tolerance) == PointContainment.Inside;
        }
    }
}
