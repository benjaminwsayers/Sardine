using System;
using Rhino.Geometry;

namespace Sardine.Core.Models
{
    /// <summary>
    /// One rectangular piece of generated parking geometry.
    ///
    /// A bay is addressed by (Kind, GroupIndex, BayIndex), which maps directly to
    /// the public DataTree path {kind; group; bay}. Indices may contain gaps once
    /// exclusion zones or accessible allocation have removed bays: the original
    /// index of every surviving bay is preserved (proof-of-concept behaviour).
    /// </summary>
    public sealed class Bay
    {
        /// <summary>Perimeter, central or accessible.</summary>
        public BayKind Kind { get; }

        /// <summary>Edge index (perimeter), row index (central) or bank index (accessible).</summary>
        public int GroupIndex { get; }

        /// <summary>Sequential index within the group at generation time.</summary>
        public int BayIndex { get; }

        /// <summary>
        /// Four corners in local frame order:
        /// [0] = (xMin, yMin), [1] = (xMax, yMin), [2] = (xMax, yMax), [3] = (xMin, yMax).
        /// [0]→[1] is the bay width direction; [1]→[2] is the bay depth direction.
        /// </summary>
        public Point3d[] Corners { get; }

        /// <summary>Closed rectangular outline (five points, last equals first).</summary>
        public PolylineCurve Outline { get; }

        /// <summary>Placement frame used to construct the rectangle.</summary>
        public Plane Frame { get; }

        /// <summary>
        /// True when the bay counts as a parking space. False for supporting
        /// geometry such as the accessible bank margin strip.
        /// </summary>
        public bool IsParkingSpace { get; }

        public Bay(BayKind kind, int groupIndex, int bayIndex, Plane frame, Point3d[] corners, bool isParkingSpace)
        {
            if (corners == null || corners.Length != 4)
                throw new ArgumentException("A bay requires exactly four corners.", "corners");

            Kind = kind;
            GroupIndex = groupIndex;
            BayIndex = bayIndex;
            Frame = frame;
            Corners = corners;
            IsParkingSpace = isParkingSpace;
            Outline = new PolylineCurve(new[] { corners[0], corners[1], corners[2], corners[3], corners[0] });
        }

        /// <summary>Average of the four corners.</summary>
        public Point3d Centroid
        {
            get
            {
                return new Point3d(
                    (Corners[0].X + Corners[1].X + Corners[2].X + Corners[3].X) * 0.25,
                    (Corners[0].Y + Corners[1].Y + Corners[2].Y + Corners[3].Y) * 0.25,
                    (Corners[0].Z + Corners[1].Z + Corners[2].Z + Corners[3].Z) * 0.25);
            }
        }

        /// <summary>Unitised direction from corner 0 to corner 1 (bay width direction).</summary>
        public Vector3d WidthDirection
        {
            get
            {
                var v = Corners[1] - Corners[0];
                v.Unitize();
                return v;
            }
        }

        /// <summary>Unitised direction from corner 1 to corner 2 (bay depth direction).</summary>
        public Vector3d DepthDirection
        {
            get
            {
                var v = Corners[2] - Corners[1];
                v.Unitize();
                return v;
            }
        }

        /// <summary>Distance from corner 0 to corner 1.</summary>
        public double Width { get { return Corners[0].DistanceTo(Corners[1]); } }

        /// <summary>Distance from corner 1 to corner 2.</summary>
        public double Depth { get { return Corners[1].DistanceTo(Corners[2]); } }

        /// <summary>Returns a copy of this bay with a different address but identical geometry.</summary>
        public Bay WithAddress(BayKind kind, int groupIndex, int bayIndex)
        {
            return new Bay(kind, groupIndex, bayIndex, Frame, (Point3d[])Corners.Clone(), IsParkingSpace);
        }
    }
}
