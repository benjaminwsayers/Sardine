using Rhino.Geometry;
using Sardine.Core.Models;

namespace Sardine.Core.Geometry
{
    /// <summary>
    /// Rectangle construction and containment tests shared by the bay generators.
    /// All tests are planar, evaluated in World XY with an explicit tolerance.
    /// </summary>
    public static class BayGeometry
    {
        /// <summary>
        /// Builds the four corners of a rectangle on <paramref name="frame"/> spanning
        /// <paramref name="x"/> along the frame X axis and <paramref name="y"/> along
        /// the frame Y axis, in the corner order documented on <see cref="Bay.Corners"/>.
        /// </summary>
        public static Point3d[] RectangleCorners(Plane frame, Interval x, Interval y)
        {
            return new[]
            {
                frame.PointAt(x.T0, y.T0),
                frame.PointAt(x.T1, y.T0),
                frame.PointAt(x.T1, y.T1),
                frame.PointAt(x.T0, y.T1)
            };
        }

        /// <summary>
        /// True when every corner is inside or on the boundary
        /// (perimeter containment rule: a single corner strictly outside fails).
        /// </summary>
        public static bool AllCornersInsideOrOn(Point3d[] corners, Curve boundary, double tolerance)
        {
            if (corners == null || boundary == null) return false;
            for (int i = 0; i < corners.Length; i++)
            {
                if (boundary.Contains(corners[i], Plane.WorldXY, tolerance) == PointContainment.Outside)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// True when every corner is strictly inside or coincident with the boundary
        /// (central containment rule; an Unset result fails).
        /// </summary>
        public static bool AllCornersInsideOrCoincident(Point3d[] corners, Curve boundary, double tolerance)
        {
            if (corners == null || boundary == null) return false;
            for (int i = 0; i < corners.Length; i++)
            {
                var pc = boundary.Contains(corners[i], Plane.WorldXY, tolerance);
                if (pc != PointContainment.Inside && pc != PointContainment.Coincident)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Two bays overlap when any corner of one lies strictly inside the other.
        /// Touching corners (Coincident) do not count as overlap.
        /// </summary>
        public static bool Overlap(Bay a, Bay b, double tolerance)
        {
            if (a == null || b == null) return false;

            for (int i = 0; i < 4; i++)
            {
                if (b.Outline.Contains(a.Corners[i], Plane.WorldXY, tolerance) == PointContainment.Inside)
                    return true;
            }
            for (int i = 0; i < 4; i++)
            {
                if (a.Outline.Contains(b.Corners[i], Plane.WorldXY, tolerance) == PointContainment.Inside)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// True when the bay conflicts with a closed exclusion curve (PRD section 16):
        /// a bay corner inside or coincident with the exclusion, a bay edge crossing the
        /// exclusion boundary, or an exclusion vertex inside or coincident with the bay.
        /// </summary>
        public static bool ConflictsWithExclusion(Bay bay, Curve exclusion, double tolerance)
        {
            if (bay == null || exclusion == null) return false;

            // 1. Bay corner inside (or on) the exclusion.
            for (int i = 0; i < 4; i++)
            {
                var pc = exclusion.Contains(bay.Corners[i], Plane.WorldXY, tolerance);
                if (pc == PointContainment.Inside || pc == PointContainment.Coincident)
                    return true;
            }

            // 2. Bay edge crosses the exclusion boundary.
            var ccx = Rhino.Geometry.Intersect.Intersection.CurveCurve(bay.Outline, exclusion, tolerance, tolerance);
            if (ccx != null && ccx.Count > 0)
                return true;

            // 3. Exclusion vertex inside (or on) the bay: a small exclusion inside one bay.
            Polyline exPl;
            if (exclusion.TryGetPolyline(out exPl))
            {
                for (int i = 0; i < exPl.Count; i++)
                {
                    var pc = bay.Outline.Contains(exPl[i], Plane.WorldXY, tolerance);
                    if (pc == PointContainment.Inside || pc == PointContainment.Coincident)
                        return true;
                }
            }
            else
            {
                // Non-polyline exclusion entirely inside a bay: test its centroid.
                var amp = AreaMassProperties.Compute(exclusion);
                if (amp != null)
                {
                    var pc = bay.Outline.Contains(amp.Centroid, Plane.WorldXY, tolerance);
                    if (pc == PointContainment.Inside || pc == PointContainment.Coincident)
                        return true;
                }
            }

            return false;
        }
    }
}
