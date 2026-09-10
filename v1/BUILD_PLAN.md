# Sardine v1.0.0 — Build Plan

This plan takes the `.cs` files in `v1/src` to an owner-approved, internally
distributable `Sardine.gha`. It follows `docs/PRD.md` phase by phase and records
every place where the implementation deliberately differs from the
proof-of-concept scripts so the manual feature gate can check them explicitly.

---

## 1. Project setup (Phase 0)

Nothing in `v1/src` depends on a particular project layout, but the code
assumes two assemblies with the architecture boundary from PRD §24–§28.

Recommended minimal SDK-style projects (all `net48`, frozen per PRD §7):

```xml
<!-- src/Sardine.Core/Sardine.Core.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <LangVersion>7.3</LangVersion>
    <RootNamespace>Sardine.Core</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="RhinoCommon" Version="8.30.26103.11001" ExcludeAssets="runtime" />
  </ItemGroup>
</Project>
```

```xml
<!-- src/Sardine.GH/Sardine.GH.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <LangVersion>7.3</LangVersion>
    <RootNamespace>Sardine.GH</RootNamespace>
    <TargetExt>.gha</TargetExt>
    <AssemblyName>Sardine</AssemblyName>
    <UseWindowsForms>true</UseWindowsForms>
    <NoWarn>NU1701</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Grasshopper" Version="8.30.26103.11001" ExcludeAssets="runtime" />
    <PackageReference Include="RhinoCommon" Version="8.30.26103.11001" ExcludeAssets="runtime" />
    <ProjectReference Include="..\Sardine.Core\Sardine.Core.csproj" />
  </ItemGroup>
</Project>
```

```xml
<!-- Directory.Build.props at the repository root (PRD §30) -->
<Project>
  <PropertyGroup>
    <Version>1.0.0</Version>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <FileVersion>1.0.0.0</FileVersion>
    <Company>WSP UK</Company>
    <Product>Sardine</Product>
    <Authors>Benj Sayers</Authors>
  </PropertyGroup>
</Project>
```

Checklist:

- [ ] Repository, `.gitignore`, `.gitattributes` with LFS for `*.gh`, `*.ghx`, `*.3dm` (PRD §67)
- [ ] `src/Sardine.sln` with the two projects above; no machine-specific DLL paths
- [ ] `Directory.Build.props` as the single version source
- [ ] Release build produces `Sardine.gha` + `Sardine.Core.dll` in `src/Sardine.GH/bin/Release/net48`
- [ ] Both files copied to the Grasshopper Libraries folder load in Rhino 8 (unblock if prompted)
- [ ] `.github/copilot-instructions.md` pointing at `docs/PRD.md`
- [ ] Owner approval of the baseline validation approach

Architecture check that must hold on every PR (PRD §40): `Sardine.Core` has no
reference to `Grasshopper`, `GH_*`, `DataTree`, `AddRuntimeMessage` or
`RhinoDoc`. In this drop the only file that touches `RhinoDoc.ActiveDoc` is the
Bake and Export components; the only files that touch `DataTree`/`GH_Structure`
are `GH/Conversion/*` and the components.

---

## 2. What each component does

### Sardine.Site (`SiteComponent` → `SiteRationaliser`)

Inputs `Boundary`, `MergeAngle` (5°), `MinEdgeLength` (1.0 m). Outputs
`RawBoundary`, `WorkingBoundary`, `Edges`, `EdgeDirections`, `EdgeLengths`,
`EdgeCount`, `Warnings`. All error texts and the conversion warning match
`docs/manual-validation/Sardine.Site.md` verbatim; zero thresholds disable the
corresponding rule. `WorkingBoundary` is emitted as a `PolylineCurve` so the
segment order (= edge index order) survives into Sardine.Layout.

### Sardine.Layout (`LayoutComponent` → `LayoutGenerator`)

Inputs exactly as PRD §12 (15 inputs, defaults from `SardineDefaults`).
Pipeline, in order:

1. `LayoutGenerator.BuildSite` rebuilds the edge list from the `WorkingBoundary`
   polyline segments (this is why edge indices from Site and Layout agree).
2. `ZoneDecomposer` insets the working boundary by `BayDepth` (perimeter
   alignment) and by `BayDepth + AisleWidth` (central region).
3. `PerimeterBayGenerator` walks the alignment at `BayWidth`, culls overlapping
   neighbours and the closed seam, then removes bays with any corner outside
   `RawBoundary` (falls back to `WorkingBoundary`).
4. `CentralBayGenerator` lays out back-to-back rows with aisles, angled bay
   pitch (`2·d·sin θ + aisle`, step `w / sin θ`), alternating flow and chevrons.
5. Composition: perimeter then central, as one `List<Bay>`.
6. `ExclusionCuller` removes bays conflicting with closed exclusion curves;
   invalid exclusions are ignored with a warning; surviving indices keep gaps.
7. `AccessibleProvision.Resolve` gives the count (`-1` → `max(1, floor(5%))`,
   `≥ 0` explicit); `AccessibleBayAllocator` consumes a consecutive run of
   standard bays nearest `AccessiblePoint` (site centroid by default).
8. Counts are computed from the final bay list, so `BayCount` reflects accessible
   replacement (PRD §19.1).

`LayoutTreeBuilder` converts to `{0;edge;bay}`, `{1;row;bay}`, `{2;bank;bay}`
(bank 0 = accessible bays, bank 1 = margin strip, one curve per branch).

### Sardine.Bake (`BakeComponent` → `LayoutBaker`, `SardineLayers`)

Rising-edge trigger. Creates or reuses `Sardine` and its six child layers, bakes
Boundary / Perimeter / Central / Accessible / Aisles / Flow, names every object
`<Name>_<kind>`, outputs `ObjectIds`, `BakedCount`, `Status`. Classification is
by DataTree prefix only.

### Sardine.Export (`ExportComponent` → `DwgExporter`, `SummaryCsvFile`)

Rising-edge trigger. Selects exactly the supplied `ObjectIds`, runs
`_-Export "<dir>\<OptionName>.dwg" _Enter`, verifies the file exists, then
appends one record to `<dir>\Sardine_Summary.csv` (header written on creation).
`OptionName` falls back to `Sardine_<angle>deg_<w>x<d>m_A<aisle>m`.

---

## 3. Migration phases and feature gates (PRD §52–§62)

Each phase is one focused PR with the manual Rhino 8 / Grasshopper record from
PRD §33. Suggested branch names and the fixtures each gate must run:

| Phase | PR / branch | Files under test | Fixtures (PRD §35) |
| --- | --- | --- | --- |
| 1 Site | `feature/site-rationalisation` | `Core/Site/*`, `Core/Geometry/SiteGeometry.cs`, `SiteComponent` | Site fixtures A–J in `docs/manual-validation/Sardine.Site.md` |
| 2 Zones + perimeter | `feature/perimeter-bays` | `ZoneDecomposer`, `CurveOffsets`, `PerimeterBayGenerator`, `BayGeometry` | A, B, C, E |
| 3 Central | `feature/central-bays` | `CentralBayGenerator` | F, G |
| 4 Composition + exclusions | `feature/layout-composition` | `LayoutGenerator`, `ExclusionCuller`, `LayoutTreeBuilder`, `LayoutComponent` | D, A |
| 5 Accessible | `feature/accessible-bays` | `AccessibleBayAllocator`, `AccessibleProvision` | H, I |
| 6 Layout component sign-off | `feature/layout-component` | `LayoutComponent` inputs/outputs vs PRD §12 / §19 | A–I re-run |
| 7 Bake | `feature/bake` | `BakeComponent`, `LayoutBaker`, `SardineLayers`, `LayoutTreeReader` | Bake A twice: second press must not duplicate |
| 8 Export | `feature/export` | `ExportComponent`, `DwgExporter`, `SummaryCsvFile`, `SummaryCsv`, `OptionNaming` | Export A; re-export → `_2.dwg`, CSV gains one row |
| 9 Template | `feature/template` | `examples/Sardine_Template_v1.gh` | Smoke test §63 |
| 10 Release hardening | `release/v1.0.0` | — | All fixtures + smoke test |

Because the whole code drop already exists, phases 2–5 can be validated from
one build while still being reviewed as separate PRs (copy the relevant files in
per PR, or open the PRs in sequence from one branch).

### Predicted results to confirm (not approved baselines)

These come from tracing the code by hand, not from running it. They exist so the
first Rhino run has something concrete to compare against. If Rhino disagrees,
investigate the code before touching the expectation (PRD §36, §39).

- **Fixture A (40 × 20 m rectangle, defaults):** the central inset
  (`5 + 6 = 11 m` each side, needs 22 m) fails, so Layout warns
  `Central boundary offset failed … central parking was skipped.` and produces
  perimeter parking only. The 30 × 10 m alignment yields 33 candidate bays
  (roughly 13 + 4 + 12 + 4 by edge; the 13th walk position lands exactly on
  the first corner, so whether that bay counts to edge 0 or edge 1 depends on
  which segment Rhino's `TangentAt` picks there), none overlapping, all inside.
  Automatic accessible count = 1, consuming 2 standard bays on the long edge
  nearest the centroid → expected
  `PerimeterCount 31, CentralCount 0, AccessibleCount 1, BayCount 32`, with a
  two-index gap in that edge's `{0;edge;bay}` paths plus `{2;0;0}` (accessible
  bay) and `{2;1;0}` (1.2 m margin strip).
- **60 × 40 m rectangle, defaults:** central region 38 × 18 m gives one row
  pair (rows 2 and 3; rows 0 and 1 are empty and absent from the tree) with
  15 bays each, one aisle rectangle and flow arrows.

---

## 4. Deliberate differences from the proof-of-concept

Each is small, was judged necessary for the PRD requirement it names, and must
be stated in the corresponding PR's "Behaviour impact" section.

| # | Change | Why | Where |
| --- | --- | --- | --- |
| 1 | Perimeter bays always extend **outward** from the inset alignment, decided from the alignment's closed-curve orientation. | The scripts used the left-hand normal, so a counter-clockwise boundary (e.g. Site fixture A as drawn) put every perimeter bay in the aisle. PRD §14 "remove bays that protrude"/"deterministic" needs a direction that does not depend on how the curve was drawn. | `PerimeterBayGenerator.OutwardSign` |
| 2 | A bay's edge index is the **nearest working-boundary edge** to its outer face, not the proportional arc-length range. | The proportional mapping misassigns bays near every corner (even on a plain rectangle the 30 m inset edge maps to 26.7 m of the 40 m edge), so a disabled edge kept bays and an enabled neighbour lost them. Fixture E requires no parking on the disabled edge. | `PerimeterBayGenerator.NearestEdgeIndex` |
| 3 | Central inset failure is a **warning**, perimeter parking is still produced. Perimeter inset failure remains a fatal error. | PRD §13 "controlled failure or warning rather than an exception". Small sites are otherwise unusable. | `ZoneDecomposer.Decompose`, `LayoutGenerator` |
| 4 | Defaults corrected to PRD §18: bay depth 5.0 (was 4.8), accessible depth 6.2 (was 6.0). Zero thresholds in Site disable a rule instead of restoring the default. | PRD §10.2 and §18. | `SardineDefaults`, `SiteRationaliser` |
| 5 | Bake keeps the **last ObjectIds on its outputs** after the trigger returns to false. | A Button releases immediately; without caching, Export would never see the ids. | `BakeComponent.EmitLast` |
| 6 | Export never overwrites: an existing `<Option>.dwg` becomes `<Option>_2.dwg` etc. | Scripted `-Export` would otherwise prompt or clobber. | `DwgExporter.UniquePath` |
| 7 | CSV gains `Accessible Bays` and `DWG File` columns. | PRD §22.2 supplies `AccessibleCount`; linking the record to its DWG makes the summary usable. | `SummaryCsv.Header` |
| 8 | Option name defaults to `Sardine_<angle>deg_<w>x<d>m_A<aisle>m` when blank. | Absorbs 08_Pedestrian per PRD §23; walkway modes dropped as out of scope. | `OptionNaming` |
| 9 | Non-polyline exclusion curves are tested by centroid for the "exclusion entirely inside a bay" rule. | The script only handled polyline exclusions in that branch. | `BayGeometry.ConflictsWithExclusion` |
| 10 | Layout validates dimensions (positive, finite; `0 < BayAngle ≤ 90`) and reports a Grasshopper error rather than silently substituting defaults. | PRD §5.1 "clear warnings/errors for invalid inputs". | `LayoutGenerator.ValidateDimensions` |

Everything else — merge rules, walk spacing, overlap and containment rules,
pitch and step formulas, flow alternation, arrow spacing, exclusion tests,
consecutive-run search, bank anchoring, margin strip, `{kind;group;bay}`
paths with preserved gaps — follows the scripts line for line.

---

## 5. Open questions for the product owner

1. **Orientation semantics.** PRD §12 gives `Orientation` a default of 0°.
   The implementation treats it as an absolute angle from World X, exactly like
   the script's `gridAngle`. The dominant fill directions are still computed
   (`SiteZones.FillDirections`) but are not used automatically. If 0° should
   mean "align with the dominant site edge", change one line in
   `LayoutGenerator` (`p.OrientationDeg` → `p.OrientationDeg + zones.FillAnglesDeg[0]`)
   and record it as a behaviour change.
2. **DWG export inside a Grasshopper solve.** `RhinoApp.RunScript("_-Export …")`
   is what the proof of concept used. If Rhino 8 refuses to run a command while
   Grasshopper is solving, the fallback is `RhinoDoc.ExportSelected(path)` or
   `Rhino.FileIO.FileDwg` write options; both stay inside `DwgExporter`.
3. **Accessible bays extend 1.2 m into the aisle** (accessible depth 6.2 minus
   standard 5.0), as in the script. Confirm this is the intended safety-zone
   convention rather than extending rearwards.
4. **Angled accessible banks** are built along the chord between the first and
   last consumed bay corners (script behaviour) and are visibly approximate for
   non-90° central rows. Listed as a known limitation (PRD §71).

---

## 6. Release checklist (PRD §62–§68)

- [ ] All fixtures A–I have owner-recorded passes on the exact release commit
- [ ] Release smoke test §63 steps 1–17 recorded
- [ ] `Sardine.gha` version metadata reads 1.0.0.0 / WSP UK / Sardine
- [ ] `examples/Sardine_Template_v1.gh` opens and generates with a sample boundary
- [ ] Installation instructions and release notes written
- [ ] Tag `v1.0.0` on the approved `main` commit; publish the approved binary unchanged

---

## 7. Deferred (not in this drop, per PRD §42)

- Automated Rhino-engine tests (`Sardine.Tests`, Rhino.Testing) — the Core API
  is already document-free so tests can call `SiteRationaliser.Rationalise` and
  `LayoutGenerator.Generate` directly once a Rhino test host exists.
- GitHub Actions CI, branch protection, automated release.
- Component icons and a category icon (`SardineInfo.Icon` returns null).
