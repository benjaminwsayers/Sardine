namespace Sardine.Core.Pipeline
{
    /// <summary>
    /// Parameter bag for the Sardine.Site rationalisation pipeline.
    /// </summary>
    public class RationalisationParameters
    {
        /// <summary>
        /// Consecutive edges whose bearing differs by less than this angle are merged.
        /// Set to zero to disable angle-based merging.
        /// </summary>
        public double MergeAngleDeg { get; set; } = 5.0;

        /// <summary>
        /// Edges shorter than this length are absorbed into their neighbour.
        /// Set to zero to disable length-based merging.
        /// </summary>
        public double MinEdgeLength { get; set; } = 1.0;
    }
}