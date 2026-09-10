# Sardine v1.0.0 — clean source drop

This folder is a self-contained rewrite of the Sardine v1.0.0 logic as plain
C# files, ready to be moved into a fresh repository. It contains **no** project
files, solution files, icons or resources — only the `.cs` files and the
build plan (`BUILD_PLAN.md`).

Everything here was written against `docs/PRD.md` (the v1.0.0 build contract),
`docs/manual-validation/Sardine.Site.md` and the ten proof-of-concept scripts in
`legacy/`. The legacy scripts were used as the behavioural reference; the
existing `Sardine.Core` / `Site.cs` code in the repository root was consulted
but not reused.

Target: Rhino 8 / Grasshopper on Windows, .NET Framework 4.8 (`net48`),
C# 7.3 language level (the SDK default for `net48`). No C# 8+ features are used.
The files have been compile-checked against the RhinoCommon 8.30 and
Grasshopper 8.30 NuGet assemblies.

## Directory structure

```text
v1/
├── README.md                          ← this file
├── BUILD_PLAN.md                      ← v1 build plan, validation, deviations
└── src/
    ├── Sardine.Core/                  ← RhinoCommon only; NO Grasshopper, NO RhinoDoc
    │   ├── SardineDefaults.cs         ← authoritative v1 defaults (PRD §18)
    │   ├── Tolerances.cs              ← every tolerance / sampling constant (PRD §37)
    │   ├── Models/
    │   │   ├── SiteEdge.cs            ← one rationalised boundary edge
    │   │   ├── SiteBoundary.cs        ← Sardine.Site result
    │   │   ├── BayKind.cs             ← 0 perimeter / 1 central / 2 accessible
    │   │   ├── Bay.cs                 ← one bay rectangle with (kind, group, index)
    │   │   ├── SiteZones.cs           ← decomposition result
    │   │   ├── LayoutParameters.cs    ← Sardine.Layout inputs
    │   │   ├── LayoutResult.cs        ← Sardine.Layout outputs + counts + warnings
    │   │   └── OptionSummary.cs       ← one CSV record
    │   ├── Geometry/
    │   │   ├── SiteGeometry.cs        ← area, centroid, polyline conversion, dominant directions
    │   │   ├── CurveOffsets.cs        ← inward offset (02_ZoneDecomposer)
    │   │   └── BayGeometry.cs         ← rectangle corners, containment, overlap, exclusion tests
    │   ├── Site/
    │   │   ├── RationalisationParameters.cs
    │   │   └── SiteRationaliser.cs    ← 01_SiteRationaliser
    │   ├── Layout/
    │   │   ├── ZoneDecomposer.cs      ← 02_ZoneDecomposer
    │   │   ├── PerimeterBayGenerator.cs ← 03_PerimeterBays
    │   │   ├── CentralBayGenerator.cs ← 04_CentralBays
    │   │   ├── ExclusionCuller.cs     ← 06_BayCuller
    │   │   ├── AccessibleBayAllocator.cs ← 07_AccessibleBay
    │   │   └── LayoutGenerator.cs     ← 05_MergeBays + pipeline orchestration
    │   ├── Compliance/
    │   │   └── AccessibleProvision.cs ← max(1, floor(5% × standard)) rule (PRD §17)
    │   └── Reporting/
    │       ├── OptionNaming.cs        ← 08_Pedestrian naming, absorbed
    │       └── SummaryCsv.cs          ← CSV header/record formatting (pure)
    │
    └── Sardine.GH/                    ← Grasshopper / Rhino document boundary
        ├── SardineInfo.cs             ← GH_AssemblyInfo + category names
        ├── Components/
        │   ├── SiteComponent.cs       ← Sardine.Site
        │   ├── LayoutComponent.cs     ← Sardine.Layout
        │   ├── BakeComponent.cs       ← Sardine.Bake
        │   └── ExportComponent.cs     ← Sardine.Export
        ├── Conversion/
        │   ├── LayoutTreeBuilder.cs   ← Core result → {kind; group; bay} DataTree
        │   └── LayoutTreeReader.cs    ← DataTree → classified curve lists (Bake)
        └── Document/
            ├── SardineLayers.cs       ← Sardine::* layer hierarchy (PRD §21)
            ├── LayoutBaker.cs         ← adds curves to the document, returns ObjectIds
            ├── DwgExporter.cs         ← scripted -Export of explicit ObjectIds
            └── SummaryCsvFile.cs      ← CSV create/append (10_Export)
```

## Proof-of-concept migration map

| Legacy script | Destination |
| --- | --- |
| `01_SiteRationaliser.cs` | `Core/Site/SiteRationaliser.cs` + `GH/Components/SiteComponent.cs` |
| `02_ZoneDecomposer.cs` | `Core/Layout/ZoneDecomposer.cs`, `Core/Geometry/CurveOffsets.cs`, `SiteGeometry.DominantDirections` |
| `03_PerimeterBays.cs` | `Core/Layout/PerimeterBayGenerator.cs` |
| `04_CentralBays.cs` | `Core/Layout/CentralBayGenerator.cs` |
| `05_MergeBays.cs` | `Core/Layout/LayoutGenerator.cs` (composition) + `GH/Conversion/LayoutTreeBuilder.cs` (paths) |
| `06_BayCuller.cs` | `Core/Layout/ExclusionCuller.cs`, `BayGeometry.ConflictsWithExclusion` |
| `07_AccessibleBay.cs` | `Core/Layout/AccessibleBayAllocator.cs` + `Core/Compliance/AccessibleProvision.cs` |
| `08_Pedestrian.cs` | Absorbed as `Core/Reporting/OptionNaming.cs` (walkway modes dropped, out of scope) |
| `09_Visualiser.cs` | Replaced by component outputs (counts) and component `Message` captions |
| `10_Export.cs` | Split: `GH/Document/LayoutBaker.cs` + `SardineLayers.cs` (Bake) and `DwgExporter.cs` + `SummaryCsvFile.cs` + `Core/Reporting/SummaryCsv.cs` (Export) |

## Moving into the new repository

1. Create the solution and the two `net48` SDK-style projects described in
   `BUILD_PLAN.md` §1 (`Sardine.Core` referencing RhinoCommon, `Sardine.GH`
   referencing Grasshopper and `Sardine.Core`).
2. Copy `src/Sardine.Core/**` and `src/Sardine.GH/**` into the matching project
   folders. SDK-style projects pick up every `.cs` file automatically.
3. Build. The only external types used are RhinoCommon, Grasshopper and the
   .NET Framework BCL.
4. Follow the phase order in `BUILD_PLAN.md` §3 for the manual Rhino 8 /
   Grasshopper feature gates.
