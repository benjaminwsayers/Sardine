using System;

namespace Sardine.Core.Models
{
    /// <summary>
    /// One option record as written to the CSV summary by Sardine.Export.
    /// </summary>
    public sealed class OptionSummary
    {
        public string OptionName { get; set; }
        public double BayWidth { get; set; }
        public double BayDepth { get; set; }
        public double BayAngleDeg { get; set; }
        public double AisleWidth { get; set; }
        public int TotalCount { get; set; }
        public int PerimeterCount { get; set; }
        public int CentralCount { get; set; }
        public int AccessibleCount { get; set; }

        /// <summary>File name (not full path) of the DWG produced alongside this record. May be empty.</summary>
        public string DwgFileName { get; set; }

        /// <summary>Local time the record was generated.</summary>
        public DateTime GeneratedAt { get; set; }
    }
}
