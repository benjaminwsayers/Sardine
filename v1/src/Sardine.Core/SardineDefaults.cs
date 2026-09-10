namespace Sardine.Core
{
    /// <summary>
    /// Authoritative Sardine v1.0.0 default values (PRD sections 10.2, 12 and 18).
    ///
    /// These supersede the inconsistent hard-coded defaults found in the
    /// proof-of-concept scripts (for example 4.8 m bay depth and 6.0 m
    /// accessible depth). All dimensions are metres, all angles are degrees.
    /// </summary>
    public static class SardineDefaults
    {
        // ── Sardine.Site ────────────────────────────────────────────────────

        /// <summary>Consecutive edges whose bearing differs by less than this are merged.</summary>
        public const double MergeAngleDeg = 5.0;

        /// <summary>Edges shorter than this are absorbed into their neighbour.</summary>
        public const double MinEdgeLength = 1.0;

        // ── Sardine.Layout ──────────────────────────────────────────────────

        /// <summary>Absolute grid orientation of the central parking rows.</summary>
        public const double OrientationDeg = 0.0;

        /// <summary>Standard bay width.</summary>
        public const double BayWidth = 2.4;

        /// <summary>Standard bay depth.</summary>
        public const double BayDepth = 5.0;

        /// <summary>Aisle width between back-to-back central rows.</summary>
        public const double AisleWidth = 6.0;

        /// <summary>Bay angle relative to the aisle. 90 = perpendicular parking.</summary>
        public const double BayAngleDeg = 90.0;

        /// <summary>Accessible bay width.</summary>
        public const double AccessibleWidth = 3.6;

        /// <summary>Accessible bay depth.</summary>
        public const double AccessibleDepth = 6.2;

        /// <summary>
        /// Sentinel meaning "calculate the accessible count automatically".
        /// Any value of zero or greater is an explicit user request.
        /// </summary>
        public const int AutomaticAccessibleCount = -1;

        /// <summary>Flow direction of the first central aisle.</summary>
        public const bool StartFlowPositive = true;

        /// <summary>Whether flow-arrow chevrons are generated.</summary>
        public const bool ShowFlow = true;

        // ── Sardine.Bake / Sardine.Export ───────────────────────────────────

        /// <summary>Option name used when the user does not supply one to Bake.</summary>
        public const string DefaultOptionName = "Option_01";

        /// <summary>File name of the CSV summary that Export appends to.</summary>
        public const string SummaryCsvFileName = "Sardine_Summary.csv";
    }
}
