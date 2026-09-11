using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Sardine.Core.Geometry;
using Sardine.Core.Models;

namespace Sardine.Core.Layout
{
    /// <summary>
    /// Accessible bay allocation (proof-of-concept 07_AccessibleBay).
    ///
    /// Accessible bays replace standard capacity: a consecutive run of standard bays
    /// in the group (edge or row) closest to the proximity point is consumed and a
    /// bank of accessible bays is built over its footprint, anchored at the end of
    /// the run nearest the proximity point. Any left-over footprint becomes a margin
    /// strip. Accessible bays are deeper than standard bays; the extra depth extends
    /// forward into the aisle, exactly as in the proof of concept.
    ///
    /// Addresses: accessible bays {2; 0; n}, margin strip {2; 1; 0}.
    /// </summary>
    public static class AccessibleBayAllocator
    {
        /// <summary>Output of accessible allocation.</summary>
        public sealed class Result
        {
            /// <summary>Standard bays that were not consumed, in original order.</summary>
            public List<Bay> Remaining { get; } = new List<Bay>();

            /// <summary>Accessible bays, bank 0.</summary>
            public List<Bay> Accessible { get; } = new List<Bay>();

            /// <summary>Margin strips, bank 1. Not parking spaces.</summary>
            public List<Bay> Margins { get; } = new List<Bay>();

            /// <summary>Number of standard bays consumed by the bank.</summary>
            public int BaysConsumed { get; set; }

            /// <summary>Warning when the requested bank could not be placed; otherwise null.</summary>
            public string Warning { get; set; }

            public bool Placed { get { return Accessible.Count > 0; } }
        }

        private sealed class Entry
        {
            public Bay Bay;
            public Point3d Centroid;
            public double RowPosition;
        }

        private sealed class Group
        {
            public int Order;
            public List<Entry> Entries = new List<Entry>();
            public double MinDistance;
        }

        /// <summary>
        /// Allocates <paramref name="count"/> accessible bays from <paramref name="standardBays"/>.
        /// When no suitable run exists every standard bay is returned unchanged and
        /// <see cref="Result.Warning"/> is set.
        /// </summary>
        public static Result Allocate(
            IReadOnlyList<Bay> standardBays,
            Point3d proximityPoint,
            int count,
            double accessibleWidth,
            double accessibleDepth,
            double standardBayWidth,
            double tolerance)
        {
            var result = new Result();
            if (standardBays == null) standardBays = new List<Bay>();
            result.Remaining.AddRange(standardBays);

            if (count <= 0 || standardBays.Count == 0) return result;

            // ── Bank dimensions ─────────────────────────────────────────────
            double bankWidth = count * accessibleWidth;
            int needed = (int)Math.Ceiling(bankWidth / standardBayWidth);
            if (needed < 1) needed = 1;

            // ── Group by (kind, group index) and rank by proximity ──────────
            var groups = BuildGroups(standardBays, proximityPoint);
            groups.Sort((a, b) =>
            {
                int cmp = a.MinDistance.CompareTo(b.MinDistance);
                return cmp != 0 ? cmp : a.Order.CompareTo(b.Order);
            });

            // ── Find the best consecutive run ───────────────────────────────
            List<Entry> run = null;
            foreach (var group in groups)
            {
                if (group.Entries.Count < needed) continue;
                int closest = ClosestIndex(group.Entries, proximityPoint);
                run = FindBestRun(group.Entries, closest, needed, proximityPoint);
                if (run != null) break;
            }

            if (run == null)
            {
                result.Warning = string.Format(
                    "Could not find {0} consecutive standard bays for {1} accessible bays. " +
                    "No accessible bays were created; standard bays are unchanged.",
                    needed, count);
                return result;
            }

            // ── Anchor and direction ────────────────────────────────────────
            Point3d runStart = run[0].Bay.Corners[0];
            Point3d runEnd = run[run.Count - 1].Bay.Corners[1];

            bool proximityAtStart = runStart.DistanceTo(proximityPoint) <= runEnd.DistanceTo(proximityPoint);
            Point3d anchor = proximityAtStart ? runStart : runEnd;
            Vector3d bankDir = proximityAtStart ? runEnd - runStart : runStart - runEnd;
            if (!bankDir.Unitize()) bankDir = run[0].Bay.WidthDirection;

            Vector3d depthDir = run[0].Bay.DepthDirection;
            double standardDepth = run[0].Bay.Depth;
            double depthExtend = accessibleDepth - standardDepth;
            var depthInterval = new Interval(-depthExtend, standardDepth);

            // ── Accessible bays ─────────────────────────────────────────────
            for (int i = 0; i < count; i++)
            {
                Point3d origin = anchor + bankDir * (i * accessibleWidth);
                var frame = new Plane(origin, bankDir, depthDir);
                var corners = BayGeometry.RectangleCorners(frame, new Interval(0.0, accessibleWidth), depthInterval);
                result.Accessible.Add(new Bay(BayKind.Accessible, 0, i, frame, corners, true));
            }

            // ── Margin strip ────────────────────────────────────────────────
            double footprint = runStart.DistanceTo(runEnd);
            double marginWidth = footprint - bankWidth;
            if (marginWidth > tolerance)
            {
                Point3d origin = anchor + bankDir * bankWidth;
                var frame = new Plane(origin, bankDir, depthDir);
                var corners = BayGeometry.RectangleCorners(frame, new Interval(0.0, marginWidth), depthInterval);
                result.Margins.Add(new Bay(BayKind.Accessible, 1, 0, frame, corners, false));
            }

            // ── Remaining standard bays ─────────────────────────────────────
            var consumed = new HashSet<Bay>();
            foreach (var e in run) consumed.Add(e.Bay);

            result.Remaining.Clear();
            foreach (var bay in standardBays)
                if (!consumed.Contains(bay)) result.Remaining.Add(bay);

            result.BaysConsumed = consumed.Count;
            return result;
        }

        private static List<Group> BuildGroups(IReadOnlyList<Bay> bays, Point3d proximityPoint)
        {
            var byKey = new Dictionary<string, Group>();
            var groups = new List<Group>();

            foreach (var bay in bays)
            {
                string key = ((int)bay.Kind).ToString() + ";" + bay.GroupIndex.ToString();
                Group g;
                if (!byKey.TryGetValue(key, out g))
                {
                    g = new Group { Order = groups.Count };
                    byKey[key] = g;
                    groups.Add(g);
                }
                g.Entries.Add(new Entry { Bay = bay, Centroid = bay.Centroid });
            }

            foreach (var g in groups)
            {
                var entries = g.Entries;
                Vector3d rowDir;

                if (entries.Count >= 2)
                {
                    Vector3d refDir = entries[0].Bay.WidthDirection;
                    Point3d refOrigin = entries[0].Centroid;
                    StableSort(entries, e => (e.Centroid - refOrigin) * refDir);

                    Vector3d span = entries[entries.Count - 1].Centroid - entries[0].Centroid;
                    rowDir = span.Length > Tolerances.MinRowSpan ? span : entries[0].Bay.WidthDirection;
                    rowDir.Unitize();
                }
                else
                {
                    rowDir = entries[0].Bay.WidthDirection;
                }

                Point3d refPt = entries[0].Centroid;
                foreach (var e in entries)
                    e.RowPosition = (e.Centroid - refPt) * rowDir;
                StableSort(entries, e => e.RowPosition);

                double min = double.MaxValue;
                foreach (var e in entries)
                {
                    double d = e.Centroid.DistanceTo(proximityPoint);
                    if (d < min) min = d;
                }
                g.MinDistance = min;
            }

            return groups;
        }

        private static void StableSort(List<Entry> entries, Func<Entry, double> key)
        {
            var indexed = new List<KeyValuePair<int, Entry>>(entries.Count);
            for (int i = 0; i < entries.Count; i++) indexed.Add(new KeyValuePair<int, Entry>(i, entries[i]));
            indexed.Sort((a, b) =>
            {
                int cmp = key(a.Value).CompareTo(key(b.Value));
                return cmp != 0 ? cmp : a.Key.CompareTo(b.Key);
            });
            entries.Clear();
            foreach (var kv in indexed) entries.Add(kv.Value);
        }

        private static int ClosestIndex(List<Entry> entries, Point3d point)
        {
            int best = 0;
            double bestDist = double.MaxValue;
            for (int i = 0; i < entries.Count; i++)
            {
                double d = entries[i].Centroid.DistanceTo(point);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            return best;
        }

        /// <summary>
        /// Among the windows of <paramref name="needed"/> consecutive bays that contain
        /// the closest bay, returns the one whose nearest bay is closest to the
        /// proximity point. A window is rejected when neighbouring bays are further
        /// apart than 1.5 × bay width (a gap left by exclusion culling).
        /// </summary>
        private static List<Entry> FindBestRun(List<Entry> entries, int closestIdx, int needed, Point3d proximityPoint)
        {
            if (entries.Count < needed) return null;

            int bestStart = -1;
            double bestDist = double.MaxValue;

            int lo = Math.Max(0, closestIdx - needed + 1);
            int hi = Math.Min(entries.Count - needed, closestIdx);

            for (int start = lo; start <= hi; start++)
            {
                bool valid = true;
                for (int j = start; j < start + needed - 1; j++)
                {
                    double gap = Math.Abs(entries[j + 1].RowPosition - entries[j].RowPosition);
                    double expected = entries[j].Bay.Width;
                    if (gap > expected * Tolerances.ConsecutiveRunGapFactor)
                    {
                        valid = false;
                        break;
                    }
                }
                if (!valid) continue;

                double runMin = double.MaxValue;
                for (int j = start; j < start + needed; j++)
                {
                    double d = entries[j].Centroid.DistanceTo(proximityPoint);
                    if (d < runMin) runMin = d;
                }

                if (runMin < bestDist)
                {
                    bestDist = runMin;
                    bestStart = start;
                }
            }

            if (bestStart < 0) return null;
            return entries.GetRange(bestStart, needed);
        }
    }
}
