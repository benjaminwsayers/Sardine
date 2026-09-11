using System.Globalization;
using System.IO;
using System.Text;

namespace Sardine.Core.Reporting
{
    /// <summary>
    /// Option naming absorbed from the proof-of-concept 08_Pedestrian script
    /// (walkway modes are out of scope for v1 and are omitted from the name).
    /// </summary>
    public static class OptionNaming
    {
        /// <summary>
        /// Builds a descriptive default option name, for example
        /// <c>Sardine_90deg_2.4x5.0m_A6.0m</c>.
        /// </summary>
        public static string BuildDefaultName(double bayWidth, double bayDepth, double bayAngleDeg, double aisleWidth)
        {
            var ic = CultureInfo.InvariantCulture;
            return string.Format(ic, "Sardine_{0}deg_{1}x{2}m_A{3}m",
                ((int)bayAngleDeg).ToString(ic),
                bayWidth.ToString("F1", ic),
                bayDepth.ToString("F1", ic),
                aisleWidth.ToString("F1", ic));
        }

        /// <summary>
        /// Returns <paramref name="name"/> trimmed, or the supplied fallback when blank.
        /// </summary>
        public static string Resolve(string name, string fallback)
        {
            return string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();
        }

        /// <summary>
        /// Converts an option name into a safe file-name stem: invalid characters and
        /// whitespace become underscores. Returns "Sardine_Option" for a blank input.
        /// </summary>
        public static string ToFileStem(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Sardine_Option";

            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (char c in name.Trim())
            {
                bool bad = char.IsWhiteSpace(c);
                for (int i = 0; i < invalid.Length && !bad; i++)
                    if (invalid[i] == c) bad = true;
                sb.Append(bad ? '_' : c);
            }
            var stem = sb.ToString().Trim('_', '.');
            return stem.Length == 0 ? "Sardine_Option" : stem;
        }
    }
}
