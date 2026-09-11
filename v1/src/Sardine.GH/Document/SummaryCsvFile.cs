using System.IO;
using System.Text;
using Sardine.Core.Models;
using Sardine.Core.Reporting;

namespace Sardine.GH.Document
{
    /// <summary>
    /// Appends option summary records to a CSV file. The header is written only
    /// when the file is created (or is empty); existing records are preserved.
    /// </summary>
    public static class SummaryCsvFile
    {
        public static void Append(string csvPath, OptionSummary summary)
        {
            string directory = Path.GetDirectoryName(csvPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            bool needsHeader = !File.Exists(csvPath) || new FileInfo(csvPath).Length == 0;

            using (var writer = new StreamWriter(csvPath, true, Encoding.UTF8))
            {
                if (needsHeader) writer.WriteLine(SummaryCsv.Header);
                writer.WriteLine(SummaryCsv.FormatRecord(summary));
            }
        }
    }
}
