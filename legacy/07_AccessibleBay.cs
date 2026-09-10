// Grasshopper Script Instance
// C07 — Disabled Bay Allocator (FIXED + count outputs)
//
// FIX: parentKey now groups by ALL path indices except the last (bay index).
// NEW: outputs perimCount, centralCount alongside remainingCount for visualiser.
//
// Tree path convention:
// - Accessible bays on {2; 0; n}
// - Margin strips on {2; 1; n}
// - Remaining standard bays keep original paths with gaps in indices

#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
#endregion

public class Script_Instance : GH_ScriptInstance
{
    // ══════════════════════════════════════════════════════════════════════════
    //  DATA CLASSES
    // ══════════════════════════════════════════════════════════════════════════

    private class BayInfo
    {
        public Curve Curve;
        public GH_Path Path;
        public Point3d Centroid;
        public Point3d[] Corners;
        public Vector3d RowDir;
        public Vector3d DepthDir;
        public double BayWidth;
        public double BayDepth;
        public double RowPosition;
        public string ParentKey;
    }

    private class BranchGroup
    {
        public string Key;
        public List<BayInfo> Bays;
        public Vector3d RowDir;
        public Vector3d DepthDir;
        public double MinDistToProximity;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  MAIN
    // ══════════════════════════════════════════════════════════════════════════

    private void RunScript(
        DataTree<Curve> allBays,
        Point3d proximityPt,
        int numAccessible,
        double accBayWidth,
        double accBayDepth,
        double stdBayWidth,
        double tolerance,
        ref object remainingBays,
        ref object accessibleBays,
        ref object accessibleMargins,
        ref object accessibleCount,
        ref object remainingCount,
        ref object perimCount,
        ref object centralCount,
        ref object baysConsumed)
    {
        // ── defaults ─────────────────────────────────────────────────────────
        if (accBayWidth <= 0) accBayWidth = 3.6;
        if (accBayDepth <= 0) accBayDepth = 6.0;
        if (stdBayWidth <= 0) stdBayWidth = 2.4;
        if (tolerance <= 0) tolerance = 0.001;

        if (allBays == null || allBays.DataCount == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No bays provided.");
            return;
        }

        if (numAccessible <= 0)
        {
            var counts = CountByType(allBays);
            remainingBays = allBays;
            accessibleBays = new DataTree<Curve>();
            accessibleMargins = new DataTree<Curve>();
            accessibleCount = 0;
            remainingCount = allBays.DataCount;
            perimCount = counts.perim;
            centralCount = counts.central;
            baysConsumed = 0;
            return;
        }

        // ── Step 1: Index every bay ──────────────────────────────────────────
        var bayInfos = IndexAllBays(allBays);

        // ── Step 2: Group by parent branch, rank by proximity ────────────────
        var groups = GroupByParentBranch(bayInfos, proximityPt);
        groups.Sort((a, b) => a.MinDistToProximity.CompareTo(b.MinDistToProximity));

        // ── Step 3: Bank dimensions ──────────────────────────────────────────
        double totalBankWidth = numAccessible * accBayWidth;
        int stdBaysNeeded = (int)Math.Ceiling(totalBankWidth / stdBayWidth);

        // ── Step 4: Find a consecutive run — try groups in proximity order ───
        var consumedPaths = new HashSet<string>();
        List<BayInfo> consumedRun = null;

        foreach (var group in groups)
        {
            var available = group.Bays
                .Where(b => !consumedPaths.Contains(PathKey(b.Path)))
                .OrderBy(b => b.RowPosition)
                .ToList();

            if (available.Count < stdBaysNeeded) continue;

            int closestIdx = ClosestIndex(available, proximityPt);
            var run = FindBestRun(available, closestIdx, stdBaysNeeded, proximityPt);
            if (run == null) continue;

            consumedRun = run;
            foreach (var bay in run)
                consumedPaths.Add(PathKey(bay.Path));

            break;
        }

        if (consumedRun == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                $"Could not find {stdBaysNeeded} consecutive standard bays for " +
                $"{numAccessible} accessible bays.");
            var counts = CountByType(allBays);
            remainingBays = allBays;
            accessibleBays = new DataTree<Curve>();
            accessibleMargins = new DataTree<Curve>();
            accessibleCount = 0;
            remainingCount = allBays.DataCount;
            perimCount = counts.perim;
            centralCount = counts.central;
            baysConsumed = 0;
            return;
        }

        // ── Step 5: Anchor & direction ───────────────────────────────────────
        Point3d runStart = consumedRun.First().Corners[0];
        Point3d runEnd = consumedRun.Last().Corners[1];

        bool proxAtStart = runStart.DistanceTo(proximityPt)
                        <= runEnd.DistanceTo(proximityPt);

        Point3d anchor = proxAtStart ? runStart : runEnd;
        Vector3d bankDir = proxAtStart
            ? (runEnd - runStart)
            : (runStart - runEnd);
        bankDir.Unitize();

        Vector3d depthDir = consumedRun[0].DepthDir;
        double stdDepth = consumedRun[0].BayDepth;
        double depthExtend = accBayDepth - stdDepth;

        // ── Step 6: Build accessible bay rectangles ──────────────────────────
        var outAccessible = new DataTree<Curve>();

        for (int i = 0; i < numAccessible; i++)
        {
            Point3d origin = anchor + bankDir * (i * accBayWidth);
            Plane frame = new Plane(origin, bankDir, depthDir);

            var rect = new Rectangle3d(frame,
                new Interval(0, accBayWidth),
                new Interval(-depthExtend, stdDepth));

            outAccessible.Add(
                rect.ToPolyline().ToNurbsCurve(),
                new GH_Path(2, 0, i));
        }

        // ── Step 7: Build margin strip ───────────────────────────────────────
        var outMargins = new DataTree<Curve>();

        double consumedFootprint = runStart.DistanceTo(runEnd);
        double marginWidth = consumedFootprint - totalBankWidth;

        if (marginWidth > tolerance)
        {
            Point3d marginOrigin = anchor + bankDir * totalBankWidth;
            Plane marginFrame = new Plane(marginOrigin, bankDir, depthDir);

            var marginRect = new Rectangle3d(marginFrame,
                new Interval(0, marginWidth),
                new Interval(-depthExtend, stdDepth));

            outMargins.Add(
                marginRect.ToPolyline().ToNurbsCurve(),
                new GH_Path(2, 1, 0));
        }

        // ── Step 8: Remaining bays — original tree minus consumed ────────────
        var outRemaining = new DataTree<Curve>();
        int remCount = 0;
        int remPerim = 0;
        int remCentral = 0;

        for (int b = 0; b < allBays.BranchCount; b++)
        {
            GH_Path path = allBays.Path(b);
            if (consumedPaths.Contains(PathKey(path))) continue;

            int typePrefix = path.Indices[0];

            foreach (Curve c in allBays.Branch(b))
            {
                if (c == null) continue;
                outRemaining.Add(c, path);
                remCount++;

                if (typePrefix == 0) remPerim++;
                else if (typePrefix == 1) remCentral++;
            }
        }

        // ── Outputs ──────────────────────────────────────────────────────────
        remainingBays = outRemaining;
        accessibleBays = outAccessible;
        accessibleMargins = outMargins;
        accessibleCount = numAccessible;
        remainingCount = remCount;
        perimCount = remPerim;
        centralCount = remCentral;
        baysConsumed = consumedPaths.Count;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  COUNT HELPER
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Count bays by type prefix from the merged tree.
    /// Used for early-exit paths where no bays are consumed.
    /// </summary>
    private (int perim, int central) CountByType(DataTree<Curve> tree)
    {
        int p = 0, c = 0;
        for (int b = 0; b < tree.BranchCount; b++)
        {
            int typePrefix = tree.Path(b).Indices[0];
            int n = tree.Branch(b).Count;
            if (typePrefix == 0) p += n;
            else if (typePrefix == 1) c += n;
        }
        return (p, c);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  INDEXING
    // ══════════════════════════════════════════════════════════════════════════

    private List<BayInfo> IndexAllBays(DataTree<Curve> tree)
    {
        var result = new List<BayInfo>();

        for (int b = 0; b < tree.BranchCount; b++)
        {
            GH_Path path = tree.Path(b);
            int[] idx = path.Indices;

            string parentKey;
            if (idx.Length >= 2)
            {
                var parentIndices = new int[idx.Length - 1];
                Array.Copy(idx, parentIndices, idx.Length - 1);
                parentKey = string.Join(";", parentIndices);
            }
            else
            {
                parentKey = $"{idx[0]}";
            }

            foreach (Curve c in tree.Branch(b))
            {
                if (c == null) continue;

                Polyline pl;
                if (!c.TryGetPolyline(out pl) || pl.Count < 5) continue;

                Point3d[] corners = new Point3d[] { pl[0], pl[1], pl[2], pl[3] };

                Point3d centroid = new Point3d(
                    (corners[0].X + corners[1].X + corners[2].X + corners[3].X) * 0.25,
                    (corners[0].Y + corners[1].Y + corners[2].Y + corners[3].Y) * 0.25,
                    0);

                Vector3d rowDir = corners[1] - corners[0];
                Vector3d depthDir = corners[2] - corners[1];
                double bayW = rowDir.Length;
                double bayD = depthDir.Length;
                rowDir.Unitize();
                depthDir.Unitize();

                result.Add(new BayInfo
                {
                    Curve = c,
                    Path = path,
                    Centroid = centroid,
                    Corners = corners,
                    RowDir = rowDir,
                    DepthDir = depthDir,
                    BayWidth = bayW,
                    BayDepth = bayD,
                    ParentKey = parentKey,
                    RowPosition = 0
                });
            }
        }

        return result;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  GROUPING
    // ══════════════════════════════════════════════════════════════════════════

    private List<BranchGroup> GroupByParentBranch(List<BayInfo> bays, Point3d proximityPt)
    {
        var dict = new Dictionary<string, List<BayInfo>>();
        foreach (var bay in bays)
        {
            if (!dict.ContainsKey(bay.ParentKey))
                dict[bay.ParentKey] = new List<BayInfo>();
            dict[bay.ParentKey].Add(bay);
        }

        var groups = new List<BranchGroup>();

        foreach (var kvp in dict)
        {
            var groupBays = kvp.Value;
            if (groupBays.Count == 0) continue;

            Vector3d rowDir;
            Vector3d depthDir = groupBays[0].DepthDir;

            if (groupBays.Count >= 2)
            {
                var refDir = groupBays[0].RowDir;
                var origin = groupBays[0].Centroid;

                groupBays.Sort((a, b2) =>
                {
                    double da = (a.Centroid - origin) * refDir;
                    double db = (b2.Centroid - origin) * refDir;
                    return da.CompareTo(db);
                });

                var span = groupBays.Last().Centroid - groupBays.First().Centroid;
                rowDir = span.Length > 0.01 ? span : groupBays[0].RowDir;
                rowDir.Unitize();
            }
            else
            {
                rowDir = groupBays[0].RowDir;
            }

            var refPt = groupBays[0].Centroid;
            foreach (var bay in groupBays)
                bay.RowPosition = (bay.Centroid - refPt) * rowDir;

            groupBays.Sort((a, b2) => a.RowPosition.CompareTo(b2.RowPosition));

            double minDist = groupBays.Min(b2 => b2.Centroid.DistanceTo(proximityPt));

            groups.Add(new BranchGroup
            {
                Key = kvp.Key,
                Bays = groupBays,
                RowDir = rowDir,
                DepthDir = depthDir,
                MinDistToProximity = minDist
            });
        }

        return groups;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  ALLOCATION HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private int ClosestIndex(List<BayInfo> available, Point3d pt)
    {
        int best = 0;
        double bestDist = double.MaxValue;
        for (int i = 0; i < available.Count; i++)
        {
            double d = available[i].Centroid.DistanceTo(pt);
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    private List<BayInfo> FindBestRun(
        List<BayInfo> available, int closestIdx, int needed, Point3d proximityPt)
    {
        if (available.Count < needed) return null;

        int bestStart = -1;
        double bestDist = double.MaxValue;

        int lo = Math.Max(0, closestIdx - needed + 1);
        int hi = Math.Min(available.Count - needed, closestIdx);

        for (int start = lo; start <= hi; start++)
        {
            bool valid = true;
            for (int j = start; j < start + needed - 1; j++)
            {
                double gap = Math.Abs(
                    available[j + 1].RowPosition - available[j].RowPosition);
                double expected = available[j].BayWidth;

                if (gap > expected * 1.5)
                {
                    valid = false;
                    break;
                }
            }
            if (!valid) continue;

            double runMinDist = double.MaxValue;
            for (int j = start; j < start + needed; j++)
            {
                double d = available[j].Centroid.DistanceTo(proximityPt);
                if (d < runMinDist) runMinDist = d;
            }

            if (runMinDist < bestDist)
            {
                bestDist = runMinDist;
                bestStart = start;
            }
        }

        if (bestStart < 0) return null;
        return available.GetRange(bestStart, needed);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  UTILITIES
    // ══════════════════════════════════════════════════════════════════════════

    private string PathKey(GH_Path path)
    {
        return string.Join(";", path.Indices);
    }
}