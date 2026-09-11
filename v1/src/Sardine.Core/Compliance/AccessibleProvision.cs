using System;

namespace Sardine.Core.Compliance
{
    /// <summary>
    /// Sardine v1.0.0 accessible-bay provision rule (PRD section 17).
    ///
    ///     RequiredAccessible = max(1, floor(TotalStandardCapacity × 0.05))
    ///
    /// This is the product rule for automatic allocation. It must not be
    /// reinterpreted by the implementation.
    /// </summary>
    public static class AccessibleProvision
    {
        /// <summary>Proportion of standard capacity converted to accessible bays.</summary>
        public const double Ratio = 0.05;

        /// <summary>Minimum number of accessible bays when any standard capacity exists.</summary>
        public const int Minimum = 1;

        /// <summary>
        /// Number of accessible bays required for a given standard capacity.
        /// Returns zero only when there is no standard capacity at all.
        /// </summary>
        public static int RequiredCount(int totalStandardCapacity)
        {
            if (totalStandardCapacity <= 0) return 0;
            return Math.Max(Minimum, (int)Math.Floor(totalStandardCapacity * Ratio));
        }

        /// <summary>
        /// Resolves the effective accessible count from the public input:
        /// a negative value means automatic, zero or greater is explicit.
        /// </summary>
        public static int Resolve(int requestedCount, int totalStandardCapacity, out bool wasAutomatic)
        {
            wasAutomatic = requestedCount < 0;
            return wasAutomatic ? RequiredCount(totalStandardCapacity) : requestedCount;
        }
    }
}
