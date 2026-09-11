using System.Collections.Generic;
using Rhino.Geometry;
using Sardine.Core.Geometry;
using Sardine.Core.Models;

namespace Sardine.Core.Layout
{
    /// <summary>
    /// Exclusion-zone culling (proof-of-concept 06_BayCuller).
    /// Removes every bay that conflicts with any valid closed exclusion curve.
    /// Surviving bays are returned unchanged, in their original order and with
    /// their original addresses.
    /// </summary>
    public static class ExclusionCuller
    {
        /// <summary>
        /// Filters <paramref name="exclusions"/> to closed curves. Null entries and
        /// open curves are dropped and reported in <paramref name="warnings"/>.
        /// </summary>
        public static List<Curve> ValidExclusions(IList<Curve> exclusions, List<string> warnings)
        {
            var valid = new List<Curve>();
            if (exclusions == null) return valid;

            for (int i = 0; i < exclusions.Count; i++)
            {
                var c = exclusions[i];
                if (c == null)
                {
                    warnings.Add(string.Format("Exclusion zone {0} is null and was ignored.", i));
                    continue;
                }
                if (!c.IsClosed)
                {
                    warnings.Add(string.Format("Exclusion zone {0} is not a closed curve and was ignored.", i));
                    continue;
                }
                valid.Add(c);
            }
            return valid;
        }

        /// <summary>
        /// Returns the bays that do not conflict with any exclusion.
        /// </summary>
        public static List<Bay> Cull(IReadOnlyList<Bay> bays, IList<Curve> validExclusions, double tolerance, out int removed)
        {
            removed = 0;
            var kept = new List<Bay>();
            if (bays == null) return kept;

            if (validExclusions == null || validExclusions.Count == 0)
            {
                kept.AddRange(bays);
                return kept;
            }

            foreach (var bay in bays)
            {
                bool conflict = false;
                for (int i = 0; i < validExclusions.Count && !conflict; i++)
                    conflict = BayGeometry.ConflictsWithExclusion(bay, validExclusions[i], tolerance);

                if (conflict) removed++;
                else kept.Add(bay);
            }
            return kept;
        }
    }
}
