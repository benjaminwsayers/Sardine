using System.Globalization;
using System.Text;
using Sardine.Core.Models;

namespace Sardine.Core.Reporting
{
    /// <summary>
    /// Formats the option summary CSV written by Sardine.Export. Pure string
    /// formatting only; file access lives in Sardine.GH.
    /// </summary>
    public static class SummaryCsv
    {
        /// <summary>Header line. Written once when the CSV file is first created.</summary>
        public const string Header =
            "Option Name,Bay Width (m),Bay Depth (m),Parking Angle (deg),Aisle Width (m)," +
            "Total Bays,Perimeter Bays,Central Bays,Accessible Bays,DWG File,Generated At";

        /// <summary>One CSV record for the supplied summary. Never null.</summary>
        public static string FormatRecord(OptionSummary summary)
        {
            var ic = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append(Escape(summary.OptionName)).Append(',');
            sb.Append(summary.BayWidth.ToString("F1", ic)).Append(',');
            sb.Append(summary.BayDepth.ToString("F1", ic)).Append(',');
            sb.Append(summary.BayAngleDeg.ToString("F0", ic)).Append(',');
            sb.Append(summary.AisleWidth.ToString("F1", ic)).Append(',');
            sb.Append(summary.TotalCount.ToString(ic)).Append(',');
            sb.Append(summary.PerimeterCount.ToString(ic)).Append(',');
            sb.Append(summary.CentralCount.ToString(ic)).Append(',');
            sb.Append(summary.AccessibleCount.ToString(ic)).Append(',');
            sb.Append(Escape(summary.DwgFileName)).Append(',');
            sb.Append(summary.GeneratedAt.ToString("yyyy-MM-dd HH:mm", ic));
            return sb.ToString();
        }

        /// <summary>Quotes a field when it contains a comma, quote or line break.</summary>
        public static string Escape(string field)
        {
            if (field == null) return string.Empty;
            bool needsQuotes = field.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (!needsQuotes) return field;
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }
    }
}
