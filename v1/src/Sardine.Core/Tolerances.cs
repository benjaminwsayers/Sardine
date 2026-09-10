namespace Sardine.Core
{
    /// <summary>
    /// Central definition of every geometric tolerance and sampling constant used
    /// by the Sardine algorithms (PRD section 37).
    ///
    /// Changing any value here can alter parking behaviour and therefore requires
    /// regression review against the approved fixtures.
    /// </summary>
    public static class Tolerances
    {
        /// <summary>
        /// Absolute geometric tolerance in metres used for containment, overlap,
        /// intersection and offset operations. Matches the proof-of-concept value.
        /// </summary>
        public const double Geometry = 0.001;

        /// <summary>
        /// Number of divisions used when a non-polyline boundary is approximated
        /// as a polyline by Sardine.Site (proof-of-concept DivideByCount(200)).
        /// </summary>
        public const int PolylineConversionDivisions = 200;

        /// <summary>
        /// Number of divisions used to sample the central boundary when computing
        /// its oriented extent in the grid frame (proof-of-concept value 256).
        /// </summary>
        public const int CentralBoundarySamples = 256;

        /// <summary>
        /// Number of divisions used when a non-polyline curve must be analysed for
        /// dominant directions (proof-of-concept value 64).
        /// </summary>
        public const int DirectionSamples = 64;

        /// <summary>Edges shorter than this are ignored by dominant-direction detection.</summary>
        public const double MinEdgeForDirection = 0.5;

        /// <summary>Angular cluster width for dominant-direction detection, in degrees.</summary>
        public const double DirectionClusterAngleDeg = 15.0;

        /// <summary>
        /// Guard applied to sin(bayAngle) so the angled pitch calculations never divide by zero.
        /// </summary>
        public const double MinSinBayAngle = 1e-6;

        /// <summary>
        /// Two neighbouring bays whose centre spacing exceeds this multiple of the bay
        /// width are not treated as a consecutive run by the accessible allocator.
        /// </summary>
        public const double ConsecutiveRunGapFactor = 1.5;

        /// <summary>
        /// Minimum centroid span (metres) before a bay group's row direction is
        /// derived from the span rather than from the first bay.
        /// </summary>
        public const double MinRowSpan = 0.01;

        /// <summary>Vector component below which a value is treated as zero.</summary>
        public const double VectorZero = 1e-6;
    }
}
