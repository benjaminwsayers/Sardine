namespace Sardine.Core.Site
{
    /// <summary>
    /// Inputs for <see cref="SiteRationaliser"/>. Mirrors the optional
    /// Sardine.Site component inputs (PRD section 10.2).
    /// </summary>
    public sealed class RationalisationParameters
    {
        /// <summary>
        /// Consecutive edges whose bearing differs by less than this angle (degrees)
        /// are merged. Zero disables angle-based merging.
        /// </summary>
        public double MergeAngleDeg { get; set; } = SardineDefaults.MergeAngleDeg;

        /// <summary>
        /// Edges shorter than this length (metres) are absorbed into their neighbour.
        /// Zero disables length-based merging.
        /// </summary>
        public double MinEdgeLength { get; set; } = SardineDefaults.MinEdgeLength;
    }
}
