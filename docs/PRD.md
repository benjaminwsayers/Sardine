# Sardine — Product Requirements Document

## Version 1.0.0 — MVP Build Contract

**Status:** Authoritative for the v1 proof-of-concept increment
**Release:** v1.0.0
**Product:** Sardine
**Owner:** Benj Sayers, WSP UK
**Target platform:** Rhino 8 / Grasshopper / Windows
**Target framework:** .NET Framework 4.8 (`net48`)
**Distribution:** Internal WSP UK
**Purpose:** Build and internally ship the compiled v1 proof of concept for the
proven Sardine car park generation workflow.

---

# 1. Purpose of This Document

This Product Requirements Document is the **build contract for Sardine v1.0.0**.

It defines:

* what v1.0.0 must do;
* what v1.0.0 must not do;
* the required public Grasshopper components;
* the architecture boundaries;
* the behaviour inherited from the existing proof of concept;
* the validation and approval strategy;
* the deferred engineering improvements;
* the rules for GitHub Copilot implementation;
* and the conditions required before v1.0.0 may be released.

Where this document conflicts with previous Sardine PRDs, development backlogs, experiments or proof-of-concept naming, **this document takes precedence**.

The existing Grasshopper C# scripts are the **reference implementation for proven geometric behaviour**, but they are not themselves the final software architecture.

Migration into a compiled plugin must not be interpreted as permission to redesign working algorithms unless an issue explicitly authorises a behavioural change.

---

# 2. Product Vision

Sardine generates a practical car park layout from a site boundary in Rhino and Grasshopper.

It is not intended to produce a final detailed design.

It produces a strong parametric starting point that an engineer can review, adjust and develop.

The purpose of v1.0.0 is:

> **Turn the working Sardine proof of concept into a reliable, manually
> validated and internally distributable Grasshopper plugin without
> unnecessarily redesigning it.**

The primary workflow is:

```text
Site boundary
    ↓
Sardine.Site
    ↓
Sardine.Layout
    ↓
Generated car park
    ↓
Sardine.Bake
    ↓
Rhino geometry
    ↓
Sardine.Export
    ↓
DWG + CSV
```

---

# 3. MVP Philosophy

v1.0.0 exists to **ship the working generator**.

The first release is not the point at which Sardine needs to solve every future car park design problem.

The guiding principles are:

1. Preserve proven behaviour.
2. Introduce only the architecture required to make that behaviour maintainable and testable.
3. Validate migrated behaviour in Rhino 8 and Grasshopper before relying on the compiled implementation.
4. Keep pull requests small and focused.
5. Require recorded product-owner approval before changes reach `main`.
6. Avoid speculative future architecture.
7. Ship once the MVP is reliable.

---

# 4. Product Principles

## 4.1 Ship proven functionality

The proof of concept has already demonstrated the core parking generation approach.

v1.0.0 should therefore favour:

* migration;
* simplification;
* encapsulation;
* regression protection;
* usability;
* reliability;
* reproducibility;

over algorithmic reinvention.

---

## 4.2 Preserve behaviour before improving behaviour

Existing algorithms must be migrated with their behaviour preserved unless a GitHub issue explicitly authorises a behavioural change.

Where possible:

```text
Migration PR
    ≠
Behaviour-change PR
```

Refactoring and changing geometry behaviour in the same pull request should be avoided.

---

## 4.3 Validation protects the product

A change is not complete because it compiles.

For this v1 proof-of-concept increment, every algorithm migration must pass a
Rhino 8 / Grasshopper manual feature gate. The pull request must provide:

1. the fixture geometry used to exercise the migrated behaviour;
2. the expected counts and geometric behaviour, derived from the known-working
   proof of concept or an explicitly approved requirement change;
3. exact Rhino and Grasshopper steps that another person can repeat;
4. a product-owner-recorded pass against those expectations before approval and
   merge.

Automated checks may supplement this evidence when available, but they are not
the acceptance gate for this increment.

---

## 4.4 `main` must remain releasable

`main` represents the current stable Sardine product.

Normal development must not take place directly on `main`.

All v1 changes must enter through focused pull requests. A pull request
may merge only after its required manual feature gate has an owner-recorded pass
and the product owner approves it.

---

## 4.5 Do not build future architecture early

Functionality must not be implemented simply because it may be useful in a future release.

In particular, v1.0.0 does not require:

* a graph model;
* persistent semantic parking objects;
* a layout registry;
* post-edit tracking;
* Rhino commands;
* row editing;
* pedestrian routing;
* vehicle routing.

These are potential future capabilities rather than MVP prerequisites.

---

# 5. v1.0.0 Scope

## 5.1 v1.0.0 MUST provide

Sardine v1.0.0 must:

* install as a compiled Grasshopper `.gha` plugin;
* run in Rhino 8 on Windows;
* use the existing `net48` project/runtime configuration;
* accept a closed planar site boundary;
* rationalise imperfect survey-style boundaries;
* generate perimeter parking;
* generate central parking;
* support configurable parking orientation;
* support angled central parking;
* generate central aisle geometry;
* support alternating one-way aisle flow where applicable;
* allow selected perimeter edges to be excluded;
* allow exclusion zones to remove invalid bays;
* allocate accessible bays;
* allow accessible bays to be located by proximity to a user-defined point;
* report perimeter, central, accessible and total bay counts;
* bake generated geometry into organised Rhino layers;
* export generated/baked geometry to DWG;
* append an option summary to CSV;
* provide clear Grasshopper warnings/errors for invalid inputs;
* preserve its migrated behaviour through the manual feature gate defined in
  section 4.3;
* provide a working example Grasshopper definition;
* provide installation instructions;
* produce an owner-approved, versioned release artifact.

---

# 6. Explicitly Out of Scope

The following must not be implemented as part of v1.0.0 unless this PRD is formally changed.

## 6.1 Graph and network modelling

* `CarParkGraph`
* `GH_CarParkGraph`
* `BayNode`
* `AisleEdge`
* `EntryNode`
* shortest-path algorithms
* Dijkstra or A*
* walking-distance analysis
* nearest-exit analysis
* traffic network modelling
* pedestrian network modelling
* entrance/exit network modelling
* sight-line analysis
* turning-radius network analysis

---

## 6.2 Persistent layout intelligence

* persistent `LayoutId`
* layout registry
* document-level Sardine database
* automatic tracking of manually edited geometry
* live Inspector
* automatic post-edit recounting
* automatic post-edit compliance checking
* persistent semantic bay objects

---

## 6.3 Editing tools

* `Sardine.Refine`
* region-based regeneration
* row-level editing
* row grips
* Rhino command-based editing
* selective regeneration of baked layouts

---

## 6.4 Other future functionality

* Rhino `.rhp` plugin
* 3D parking geometry
* multi-storey parking
* split-level parking
* swept-path analysis
* EV charging allocation
* Civil 3D integration
* machine learning
* surrogate models
* public distribution
* Yak packaging
* public package registry

---

# 7. Framework Freeze

Sardine v1.0.0 targets:

```text
Rhino 8
Windows
.NET Framework 4.8
TargetFramework: net48
```

The existing Sardine project/template was created using `net48`.

**This framework choice is frozen for v1.0.0.**

The purpose of the MVP is to productise and protect the existing Sardine implementation, not migrate the runtime.

For v1.0.0:

* `Sardine.Core` must target `net48`;
* `Sardine.GH` must target `net48`;
* `Sardine.Tests` must target `net48`;
* existing project framework configuration must be preserved;
* multi-targeting must not be introduced;
* dependencies added to the solution must support `net48`;
* machine-specific DLL paths should not be introduced where supported NuGet references can be used.

The following are explicitly outside v1.0.0:

* migration to `.NET 8`;
* migration to any later .NET runtime;
* conversion to a modern .NET-only deployment;
* cross-platform runtime migration;
* framework-driven architecture redesign.

A future framework migration must be treated as its own architectural change with its own compatibility assessment, regression testing and release plan.

GitHub Copilot must not change framework settings as part of cleanup, modernisation or unrelated implementation work.

---

# 8. Primary User Workflow

v1.0.0 has one primary workflow:

```text
Sardine.Site
      ↓
Sardine.Layout
      ↓
Sardine.Bake
      ↓
Sardine.Export
```

The shipped Grasshopper template must demonstrate this complete workflow.

The template should open with a simple working example boundary.

A user should be able to:

1. replace the sample boundary;
2. adjust parking parameters;
3. generate a layout;
4. review counts and warnings;
5. bake the geometry;
6. export the result.

The user should not need to understand or manually wire the individual internal algorithm stages used by the proof of concept.

---

# 9. Public Grasshopper Component Surface

v1.0.0 requires four primary user-facing components:

```text
Sardine.Site
Sardine.Layout
Sardine.Bake
Sardine.Export
```

Prototype processing stages remain internal implementation details.

Do not expose one Grasshopper component for every historical C# script unless there is a clear product requirement.

---

# 10. Sardine.Site

## 10.1 Responsibility

Prepare the supplied site boundary for parking generation.

The component wraps the boundary rationalisation behaviour currently demonstrated by the proof-of-concept `01_SiteRationaliser.cs`.

---

## 10.2 Inputs

| Input | Type | Default | Required |
| --- | --- | ---: | --- |
| Boundary | Curve | — | Yes |
| MergeAngle | Number | 5° | No |
| MinEdgeLength | Number | 1.0 m | No |

Both optional thresholds accept zero. A zero value disables the corresponding
angle- or length-based merge rule. Negative and non-finite values are invalid.

---

## 10.3 Outputs

| Output | Type | Purpose |
| --- | --- | --- |
| RawBoundary | Curve | Original supplied boundary |
| WorkingBoundary | Curve | Rationalised boundary used for generation |
| Edges | Curve list | Indexed rationalised edges |
| EdgeDirections | Vector list | Direction of each edge |
| EdgeLengths | Number list | Length of each edge |
| EdgeCount | Integer | Number of rationalised edges |
| Warnings | Text list | Non-fatal input warnings |

---

## 10.4 Required behaviour

`Sardine.Site` must:

* reject a null boundary;
* reject an open boundary;
* accept a valid closed polyline;
* provide controlled handling for non-polyline closed curves;
* rationalise short and near-parallel survey segments;
* preserve the original supplied boundary separately;
* produce deterministic edge ordering;
* treat normal edge rationalisation as expected processing, not as a warning;
* never silently return invalid geometry.

Warnings and errors belong in the Grasshopper wrapper.

Core geometry methods must not call Grasshopper runtime messaging APIs.

---

# 11. Sardine.Layout

## 11.1 Responsibility

Generate the complete car park layout.

This component wraps the existing prototype generation pipeline corresponding broadly to:

```text
02_ZoneDecomposer
03_PerimeterBays
04_CentralBays
05_MergeBays
06_BayCuller
07_AccessibleBay
```

These remain internal implementation stages.

They are not separate user-facing product features.

---

# 12. Layout Inputs

| Input | Type | Default |
| --- | --- | ---: |
| WorkingBoundary | Curve | — |
| RawBoundary | Curve | — |
| Orientation | Number | 0° |
| BayWidth | Number | 2.4 m |
| BayDepth | Number | 5.0 m |
| AisleWidth | Number | 6.0 m |
| BayAngle | Number | 90° |
| PerimeterEdges | Boolean list | All True |
| ExclusionZones | Curve list | Empty |
| AccessiblePoint | Point | Site centroid if omitted |
| AccessibleCount | Integer | -1 = automatic |
| AccessibleWidth | Number | 3.6 m |
| AccessibleDepth | Number | 6.2 m |
| StartFlowPositive | Boolean | True |
| ShowFlow | Boolean | True |

All geometric dimensions are interpreted in metres for v1.0.0.

Automatic document-unit conversion is out of scope.

---

# 13. Site Decomposition

The internal layout process must determine the usable perimeter and central parking regions from the rationalised site boundary.

The migrated behaviour should preserve the existing principles:

* perimeter zone derived from bay depth;
* central zone derived from bay depth plus aisle width;
* dominant site/fill directions derived from the available geometry;
* deterministic behaviour for equivalent inputs.

Failures to create valid inset geometry must produce a controlled failure or warning rather than an exception escaping into Grasshopper.

---

# 14. Perimeter Parking

Perimeter parking must use the migrated behaviour of the existing perimeter generator.

It must:

* walk the generated perimeter alignment;
* place candidate bays at the configured bay width;
* generate bays at the configured depth;
* respect enabled/disabled perimeter edges;
* detect invalid overlap between neighbouring bays;
* remove bays that protrude outside the valid site;
* account for the closed perimeter seam;
* preserve deterministic bay ordering.

Perimeter generation must not retain geometry that is knowingly outside the valid boundary.

---

# 15. Central Parking

Central parking must preserve the working behaviour of the proof-of-concept central generator.

It must support:

* configurable parking angle;
* configurable grid orientation;
* back-to-back parking rows;
* central aisle geometry;
* angled bay pitch calculations;
* alternating aisle flow;
* flow-arrow geometry;
* boundary containment.

90° parking must remain a supported special case.

Angled parking must use deterministic geometric calculations rather than visual approximations.

---

# 16. Exclusion Zones

The user may provide zero or more closed exclusion curves.

A bay must be removed when it conflicts with an exclusion.

Conflict includes:

* a bay corner lying inside an exclusion;
* a bay corner coincident with the exclusion;
* a bay edge intersecting an exclusion boundary;
* an exclusion point/corner sitting entirely inside a bay.

Invalid or open exclusion curves must not crash generation.

They may be ignored with an appropriate warning.

Unaffected bays must remain unchanged.

---

# 17. Accessible Bays

Accessible bays must be created by replacing available standard parking capacity rather than being added on top of the generated capacity.

Placement should preferentially use a suitable consecutive run of bays closest to `AccessiblePoint`.

The v1.0.0 product rule for automatic allocation is:

```text
RequiredAccessible =
max(1, floor(TotalStandardCapacity × 0.05))
```

This is the Sardine v1.0.0 implementation rule.

The implementation must not independently reinterpret or change this rule.

If:

```text
AccessibleCount >= 0
```

the explicit user-provided value takes precedence.

If there is insufficient valid space to create the requested accessible bank:

* generation must not crash;
* standard bays must not silently disappear;
* a clear warning must be returned;
* the returned geometry must remain internally consistent.

---

# 18. Authoritative v1.0.0 Dimensions

The v1.0.0 defaults are:

| Parameter | Default |
| --- | ---: |
| Standard bay width | 2.4 m |
| Standard bay depth | 5.0 m |
| Aisle width | 6.0 m |
| Accessible bay width | 3.6 m |
| Accessible bay depth | 6.2 m |

These values supersede inconsistent hard-coded defaults within the proof-of-concept scripts.

Any correction of prototype defaults must be:

1. intentional;
2. recorded in the implementation issue;
3. covered by the manual feature gate with the original and corrected expected
   values stated explicitly.

---

# 19. Layout Output Contract

`Sardine.Layout` must return a deterministic Grasshopper DataTree.

The public path convention is:

```text
{0; edge; bay}    Perimeter standard bay
{1; row; bay}     Central standard bay
{2; bank; bay}    Accessible geometry
```

The meaning of these prefixes must remain stable throughout the v1.x release line unless a future version explicitly introduces a breaking change.

---

## 19.1 Outputs

| Output | Type |
| --- | --- |
| Layout | DataTree<Curve> |
| Aisles | Curve list |
| FlowArrows | Curve list |
| BayCount | Integer |
| PerimeterCount | Integer |
| CentralCount | Integer |
| AccessibleCount | Integer |
| Warnings | Text list |

`BayCount` represents the final number of parking spaces, not temporary candidate geometry.

Accessible replacement must therefore be reflected correctly in the final total.

---

# 20. Sardine.Bake

## 20.1 Responsibility

Convert the current generated option into Rhino document geometry.

Bake is deliberately separate from generation.

Recomputation of the Grasshopper solution must not automatically duplicate baked Rhino geometry.

---

## 20.2 Inputs

| Input | Type |
| --- | --- |
| Boundary | Curve |
| Layout | DataTree<Curve> |
| Aisles | Curve list |
| FlowArrows | Curve list |
| Name | Text |
| Trigger | Boolean |

---

## 20.3 Outputs

| Output | Type |
| --- | --- |
| ObjectIds | Guid list |
| BakedCount | Integer |
| Status | Text |

---

## 20.4 Trigger behaviour

Bake must operate on a deliberate button/rising-edge trigger.

The behaviour must prevent repeated Grasshopper recomputation from repeatedly baking duplicate geometry.

A user must deliberately trigger each bake operation.

---

# 21. Rhino Layer Convention

Bake must create or reuse the following layer hierarchy:

```text
Sardine
├── Sardine::Boundary
├── Sardine::Perimeter
├── Sardine::Central
├── Sardine::Accessible
├── Sardine::Aisles
└── Sardine::Flow
```

The Layout DataTree prefix determines the bay layer classification.

For v1.0.0, Bake does not require:

* persistent layout IDs;
* a layout registry;
* custom UserDictionary metadata;
* post-bake tracking;
* automatic overwrite of previous layouts.

Each bake is a discrete operation.

---

# 22. Sardine.Export

## 22.1 Responsibility

Export the objects produced by `Sardine.Bake` and record a simple option summary.

---

## 22.2 Inputs

| Input | Type |
| --- | --- |
| ObjectIds | Guid list |
| OptionName | Text |
| OutputDirectory | Text |
| BayWidth | Number |
| BayDepth | Number |
| BayAngle | Number |
| AisleWidth | Number |
| TotalCount | Integer |
| PerimeterCount | Integer |
| CentralCount | Integer |
| AccessibleCount | Integer |
| Trigger | Boolean |

---

## 22.3 Outputs

| Output | Type |
| --- | --- |
| DWGPath | Text |
| CSVPath | Text |
| Status | Text |

---

## 22.4 Required behaviour

Export must:

* run only on a deliberate trigger;
* use the ObjectIds supplied by Bake to define export scope;
* create the target output directory when required;
* produce a DWG;
* append one summary record to a CSV;
* create the CSV header if the file does not yet exist;
* preserve existing CSV records;
* report failures clearly.

Export must not scan arbitrary Rhino geometry to infer what belongs to Sardine.

---

# 23. Proof-of-Concept Migration Map

The existing scripts are behavioural references.

| Prototype | v1.0.0 destination |
| --- | --- |
| `01_SiteRationaliser.cs` | Core rationalisation + `Sardine.Site` |
| `02_ZoneDecomposer.cs` | Internal Layout algorithm |
| `03_PerimeterBays.cs` | Internal Layout algorithm |
| `04_CentralBays.cs` | Internal Layout algorithm |
| `05_MergeBays.cs` | Internal Layout composition |
| `06_BayCuller.cs` | Internal Layout exclusion logic |
| `07_AccessibleBay.cs` | Internal Layout accessible allocation |
| `08_Pedestrian.cs` | Remove or absorb option-naming behaviour |
| `09_Visualiser.cs` | Replace with component outputs / template presentation as appropriate |
| `10_Export.cs` | Split into Bake and Export responsibilities |

Migration must be incremental.

A single pull request should not rewrite all prototype stages.

---

# 24. Solution Architecture

Use the simplest architecture that provides clean separation, repeatable manual
validation and a future path to automated testing.

```text
WSP-UK-sardine/
│
├── .github/
│   ├── workflows/             # deferred engineering, when implemented
│   │   ├── ci.yml
│   │   └── release.yml
│   └── copilot-instructions.md
│
├── docs/
│   └── PRD.md
│
├── examples/
│   └── Sardine_Template_v1.gh
│
├── Directory.Build.props
│
└── src/
    ├── Sardine.sln
    │
    ├── Sardine.Core/
    │   ├── Geometry/
    │   ├── Layout/
    │   ├── Compliance/
    │   └── Models/
    │
    ├── Sardine.GH/
    │   └── Components/
    │
    └── Sardine.Tests/         # existing/future automated protection
        ├── Fixtures/
        └── Tests/
```

Additional folders may be introduced where they make the code clearer.

Folder structure should not become an architectural goal in itself.

---

# 25. Sardine.Core

`Sardine.Core` contains reusable generation logic.

Core may use RhinoCommon geometry types where required, including:

```text
Point3d
Vector3d
Line
Curve
Polyline
Plane
BoundingBox
```

Core must not:

* reference Grasshopper;
* depend on `GH_Component`;
* depend on `GH_Document`;
* depend on `DataTree`;
* call `AddRuntimeMessage`;
* access `RhinoDoc.ActiveDoc`;
* depend on Rhino UI state;
* execute Rhino commands;
* show dialogs;
* bake geometry;
* export files.

A Core algorithm should remain callable without Grasshopper application state.
This supports clear manual fixtures now and future automated tests without
changing the architecture boundary.

Where practical, Core methods should:

```text
explicit inputs
    ↓
algorithm
    ↓
explicit result object
```

rather than depend on global document state.

---

# 26. Sardine.GH

`Sardine.GH` represents the Grasshopper/Rhino application boundary.

It is responsible for:

* Grasshopper component definitions;
* component inputs and outputs;
* converting Grasshopper structures to Core inputs;
* converting Core results back to Grasshopper structures;
* runtime messages;
* Rhino document access;
* layer management;
* baking;
* command-driven DWG export.

Grasshopper components should be thin wrappers around Core behaviour where practical.

Geometry-generation logic must not be duplicated in component classes.

---

# 27. DataTree Boundary Rule

`Grasshopper.DataTree` is a presentation/application-layer structure.

Core algorithms must not require Grasshopper DataTree types.

Core should use ordinary .NET collections or simple domain result objects.

Conversion to/from paths such as:

```text
{0; edge; bay}
{1; row; bay}
{2; bank; bay}
```

must happen in `Sardine.GH`.

This keeps the algorithms directly testable.

---

# 28. Dependency Rules

Dependencies must be kept minimal.

For v1.0.0:

* every dependency must support `net48`;
* RhinoCommon may be used where necessary;
* Grasshopper dependencies belong in `Sardine.GH`, not Core;
* machine-specific DLL references should be avoided;
* additional third-party libraries require a clear justification.

Do not introduce a third-party framework when the existing .NET or Rhino APIs adequately solve the problem.

---

# 29. Architecture Simplicity

Do not introduce architecture purely for hypothetical future requirements.

Unless clearly justified by an existing v1 requirement, avoid:

* dependency injection containers;
* service locators;
* event buses;
* repository patterns;
* mediator frameworks;
* abstract factory hierarchies;
* unnecessary interfaces;
* custom persistence frameworks;
* custom serialization infrastructure;
* custom Grasshopper Goo types;
* plugin-within-plugin frameworks.

Prefer:

* small classes;
* static algorithms where appropriate;
* explicit inputs;
* explicit outputs;
* understandable result models;
* straightforward composition.

---

# 30. Version Management

Version information must have a single authoritative source.

Use:

```text
Directory.Build.props
```

to define version metadata shared by the projects.

For v1.0.0:

```xml
<Version>1.0.0</Version>
<AssemblyVersion>1.0.0.0</AssemblyVersion>
<FileVersion>1.0.0.0</FileVersion>
```

Production projects must not independently maintain conflicting version values.

---

# 31. Testing Philosophy

Testing is a product requirement.

The objective is not to maximise the number of tests or achieve an arbitrary coverage percentage.

The objective is to prevent:

> **a change that appears locally correct from silently breaking previously working Sardine behaviour.**

No minimum percentage code-coverage target is required for v1.0.0.

Behavioural coverage and meaningful regression protection are more important than raw coverage metrics.

---

# 32. v1 Proof-of-Concept Validation Model

The acceptance gate for this increment is Rhino-hosted manual regression:

```text
Fixture geometry and expected behaviour
                 ↓
Exact Rhino 8 / Grasshopper test steps
                 ↓
Contributor records observed counts/behaviour
                 ↓
Product owner records pass or fail
                 ↓
Approval and merge
```

This gate applies to every algorithm migration. Automated Rhino-engine tests are
deferred engineering work and are not required before this increment proceeds.
The engineering intent to automate them is retained in section 42.

---

# 33. Manual Feature Gate Contract

Each algorithm-migration pull request must contain or link one validation record
that identifies:

* the fixture file or exact fixture-construction geometry, in metres;
* all input values, including defaults and tolerances;
* the proof-of-concept result used as the behavioural reference;
* expected perimeter, central, accessible and total counts where applicable;
* expected geometric behaviour, warnings and output structure;
* numbered steps to open Rhino 8 on Windows, launch Grasshopper, load the
  definition or fixture, run the migrated implementation and inspect the result;
* the observed result, tester, supported Rhino version and date;
* a product-owner-recorded pass or fail.

The steps must be exact enough for a second person to repeat without relying on
unstated setup knowledge. A pass must not be inferred from a contributor saying
that the change works; the owner must record it before approval and merge.

---

# 34. Algorithm Regression

Algorithm regression is the most important behavioural protection in Sardine.

Before a proven algorithm is significantly changed or refactored, representative known-good scenarios must be captured as regression fixtures.

For this increment, those fixtures are executed manually in Rhino 8 and
Grasshopper under section 33. Future automation must preserve the same approved
fixtures and expectations rather than redefine them.

---

# 35. Required Regression Fixtures

The fixture catalogue below defines the minimum relevant manual coverage across
the v1 migration. Each migration uses the fixtures that exercise its behaviour
and records the exact expected results before owner approval.

## Fixture A — Clean rectangle

A simple rectangular site.

Verify:

* rationalisation succeeds;
* generation succeeds;
* perimeter bays exist;
* central bays exist;
* bay counts are deterministic;
* no known invalid geometry is returned.

---

## Fixture B — Survey-style boundary

A real or representative irregular survey boundary.

Verify:

* no exception occurs;
* rationalisation succeeds;
* vertex/edge processing is deterministic;
* layout generation returns stable results.

---

## Fixture C — Concave boundary

Verify:

* bays outside the valid boundary are removed;
* concave corners do not leave known protruding parking bays;
* generation completes without exception.

---

## Fixture D — Exclusion zone

Verify:

* intersecting/conflicting bays are removed;
* unaffected bays remain;
* an exclusion does not corrupt DataTree/index structure.

---

## Fixture E — Disabled perimeter edge

Verify:

* no perimeter parking remains on the disabled edge;
* parking on enabled edges is not unnecessarily changed.

---

## Fixture F — 90° central parking

Verify:

* rows generate;
* bay spacing is correct;
* aisle geometry generates;
* counts are deterministic.

---

## Fixture G — Angled parking

Use at least one supported non-90° example.

Verify:

* angled bays generate;
* row pitch is correct;
* aisle geometry is valid;
* bay geometry remains within the permitted boundary;
* output is deterministic.

---

## Fixture H — Accessible allocation

Verify:

* automatic accessible count is calculated correctly;
* explicit accessible count overrides automatic mode;
* accessible bays consume standard capacity;
* accessible placement forms a suitable consecutive bank where possible;
* final counts remain internally consistent.

---

## Fixture I — Insufficient accessible capacity

Verify:

* no unhandled exception occurs;
* warning is produced;
* unaffected standard geometry is retained;
* counts remain internally consistent.

---

# 36. Regression Baseline

Expected regression behaviour must come from:

1. the known working proof of concept; or
2. an explicitly approved requirement change.

Copilot must not invent new expected values simply because a refactored implementation produces them.

Where possible, fixtures should assert stable characteristics such as:

* final bay count;
* perimeter count;
* central count;
* accessible count;
* row count;
* branch count;
* deterministic indexing;
* representative centroids;
* boundary containment;
* absence of known overlap;
* expected warnings.

---

# 37. Geometric Comparison Tolerance

Geometric comparisons must use explicit tolerances.

Validation must not require bit-for-bit equality of floating-point geometry.

Tolerance values must be:

* defined centrally where practical;
* understandable;
* appropriate to the existing Sardine geometric scale;
* changed only intentionally.

Changing a tolerance may alter parking behaviour and therefore requires regression review.

---

# 38. Bug-Fix Rule

Every confirmed bug fix should include a repeatable regression scenario whenever practical.

Preferred sequence:

```text
Reproduce bug
    ↓
Record the failing fixture and steps
    ↓
Fix implementation
    ↓
Repeat the steps successfully
    ↓
Relevant approved fixtures still pass
```

A bug fix is not considered robust if the same failure can silently return in a later change.

---

# 39. Test Integrity

Contributors and Copilot must not:

* delete a failing fixture or validation step solely to obtain approval;
* weaken expected behaviour solely to obtain approval;
* change expected counts solely because new code produces different results;
* skip a failing scenario without a documented reason;
* silently alter fixture geometry;
* silently increase geometric tolerances.

If intended behaviour changes, the pull request must state:

1. what changed;
2. why it changed;
3. which fixtures, steps or expectations changed;
4. why the new expected behaviour is correct.

---

# 40. Architecture Boundary Review

Every pull request must preserve the Core/GH architecture boundary. Reviewers
must verify that `Sardine.Core` does not begin depending on application-layer
behaviour.

At minimum, Core must contain no dependency on:

```text
Grasshopper
GH_Component
GH_Document
GH_Path
DataTree
AddRuntimeMessage
RhinoDoc.ActiveDoc
```

This review is part of the pull request approval record. Lightweight automated
checks may be added later without changing the boundary itself.

---

# 41. Build Verification

Every implementation pull request must build the existing `net48` projects
through the current Rhino 8 / Visual Studio workflow, produce the expected
`.gha`, and load that artifact in the supported Rhino 8 / Grasshopper
environment before its manual feature gate is run.

Hardening a repeatable command-line MSBuild invocation is deferred for this
increment. Its absence does not block algorithm migration or a documentation-only
pull request. This deferral does not authorise a framework change, multi-targeting
or a machine-specific project rewrite.

---

# 42. Deferred Engineering Status

The following engineering improvements remain desirable, but they are explicitly
deferred and do not block this proof-of-concept increment:

| Deferred work | Current status / follow-up |
| --- | --- |
| Automated Rhino-engine regression tests | Follow-up issue #4 |
| GitHub Actions CI for build, regression and architecture checks | Deferred roadmap work; no required CI gate for this increment |
| Branch protection status checks | Deferred until stable CI checks exist; focused PRs and owner approval remain required |
| Command-line MSBuild hardening | Follow-up issues #3 and #5 cover the related project/build modernisation without changing the `net48` contract |
| Automated release publication | Deferred roadmap work; v1 proof-of-concept publication is owner-controlled and manual |

These deferrals change the delivery gate, not the intended long-term engineering
direction. They must not be used to weaken the `net48`, architecture, behavioural
reference, fixture or owner-approval requirements.

---

# 43. Manual Validation Record

The pull request is the durable validation record for this increment. It must
contain or link:

* the fixture geometry;
* the expected counts and behaviour;
* the exact Rhino 8 / Grasshopper steps;
* the contributor's observed result and useful failure diagnostics;
* the product owner's recorded pass or fail.

A failed required step blocks approval and merge. The result must be updated when
the fixture, implementation, public contract, default or tolerance changes.

---

# 44. Repository Merge Controls

For this increment:

* no routine direct push to `main`;
* every change uses one focused feature branch and pull request;
* every algorithm migration has the manual feature-gate evidence required by
  section 33;
* failure of a required manual step blocks approval and merge;
* the product owner records the pass and approves before merge;
* the next dependent branch starts only from the newly merged and updated
  `main`, never from an unmerged feature branch.

`main` should always represent a state that could reasonably be released.

---

# 45. Development Workflow

Normal development flow:

```text
main
  ↓
feature/<issue-description>
  ↓
implementation
  ↓
Rhino 8 / Grasshopper manual feature gate
  ↓
pull request
  ↓
owner-recorded pass
  ↓
review
  ↓
merge
  ↓
updated main
  ↓
next dependent feature branch
```

Each feature branch should normally address one GitHub issue or one tightly related change.

Prefer small PRs over large multi-purpose PRs.

---

# 46. Pull Request Contract

Every pull request must include the following information.

## What changed

A concise description of the implementation.

## Why

The issue, bug or requirement being addressed.

## Behaviour impact

State either:

```text
No intended behavioural change.
```

or clearly describe the intended change.

## Testing

State:

* fixtures and regression scenarios added, modified or affected;
* exact Rhino 8 / Grasshopper steps and observed result for implementation
  changes;
* `Not applicable - documentation only` when no runtime behaviour can be
  exercised.

## Validation

Confirm:

* the manual feature gate has an owner-recorded pass for an algorithm migration,
  or explain why the gate is not applicable;
* the Core/GH boundary, public contracts, defaults and tolerances remain
  unchanged unless the issue explicitly authorises a change;
* no unrelated functionality was intentionally changed.

---

# 47. Pull Request Size

PRs should be kept focused enough that a reviewer can understand the behavioural risk.

Avoid pull requests that simultaneously:

* migrate several major algorithms;
* redesign architecture;
* change defaults;
* alter public component APIs;
* change CI;
* fix unrelated bugs.

Where practical, separate these changes.

---

# 48. GitHub Copilot Role

GitHub Copilot is an implementation assistant.

It is not the product owner and must not independently redefine Sardine requirements.

The authoritative hierarchy is:

```text
PRD
 ↓
GitHub issue / acceptance criteria
 ↓
approved fixture expectations and validation records
 ↓
existing implementation
 ↓
Copilot implementation proposal
```

Where uncertainty remains, preserve current behaviour and report the ambiguity.

---

# 49. Copilot Implementation Workflow

For every implementation issue, Copilot must follow this sequence.

## 1. Read the issue

Understand the requested outcome and acceptance criteria.

## 2. Read this PRD

Identify relevant scope and architectural constraints.

## 3. Inspect the existing implementation

Do not assume how Sardine works from filenames alone.

## 4. Inspect related fixtures and validation records

Understand existing regression protection before editing production code.

## 5. Identify intended behaviour

Determine whether the issue is:

* migration;
* refactoring;
* bug fixing;
* intentional product behaviour change.

## 6. Implement only the issue

Avoid unrelated cleanup.

## 7. Prepare the feature-gate evidence

Provide the fixture geometry, expected counts/behaviour and exact Rhino 8 /
Grasshopper steps required by section 33.

## 8. Build and validate

For implementation changes, build through the current supported workflow and
run the relevant manual fixtures in Rhino 8 / Grasshopper.

## 9. Report the result

Summarise:

* files changed;
* behaviour changed;
* fixtures or validation steps added;
* steps executed;
* observed outcome;
* unresolved concerns.

Copilot may record contributor observations, but it must not claim product-owner
approval. The owner records the pass before approval and merge.

---

# 50. Copilot Guardrails

Copilot must not:

* change the target framework from `net48`;
* introduce multi-targeting;
* migrate Sardine to modern .NET;
* regenerate projects from another template without instruction;
* add dependencies incompatible with `net48`;
* rewrite a working algorithm solely because another implementation appears cleaner;
* implement functionality listed as out of scope;
* create `CarParkGraph`;
* create speculative graph/domain infrastructure;
* introduce speculative abstractions;
* change public component inputs or outputs without explicit requirement;
* silently change default dimensions;
* silently change geometric tolerances;
* silently change DataTree path conventions;
* delete regression fixtures or validation records;
* weaken expected behaviour purely to obtain approval;
* update expected regression results purely to match new output;
* change several independent algorithms in one issue without justification;
* access `RhinoDoc.ActiveDoc` from Core;
* add Grasshopper references to Core;
* add unnecessary third-party frameworks;
* perform large unrelated cleanup while implementing a feature.

If Copilot identifies a limitation caused by an existing architectural decision, it should report it rather than independently redesign the product.

---

# 51. Definition of Done — Implementation Issue

An issue is complete only when:

* acceptance criteria are satisfied;
* implementation exists;
* the required fixture geometry, expectations and exact test steps exist;
* the related Rhino 8 / Grasshopper manual regression steps pass;
* the product owner records that pass before approval and merge;
* the current supported build produces a loadable `.gha`;
* architecture rules remain satisfied;
* no unrelated behavioural change has been introduced;
* the PR clearly explains behavioural impact.

```text
Code written
    ≠
Done
```

---

# 52. Phase 0 — Establish the Safety Net

Before migrating the generation algorithms:

1. establish/confirm the Git repository;
2. establish/confirm the `Sardine.sln` solution;
3. confirm all v1 projects target `net48`;
4. confirm the current supported workflow produces a `.gha` that loads in Rhino
   8 / Grasshopper;
5. identify initial fixture geometry;
6. record known-good proof-of-concept counts and behaviour;
7. define exact repeatable Rhino 8 / Grasshopper steps;
8. add `.github/copilot-instructions.md`;
9. obtain owner approval of the baseline validation approach.

**Algorithm migration may begin when this manual safety net is approved. The
deferred automation in section 42 is not a prerequisite.**

---

# 53. Phase 1 — Site Rationalisation

Migrate:

```text
01_SiteRationaliser.cs
```

Deliver:

* Core boundary rationalisation implementation;
* `Sardine.Site` wrapper;
* clean rectangle validation;
* non-polyline input validation;
* invalid/open-boundary validation;
* survey-style fixture regression evidence.

Merge only after the manual feature gate has an owner-recorded pass.

---

# 54. Phase 2 — Site Decomposition and Perimeter Parking

Migrate the required behaviour from:

```text
02_ZoneDecomposer.cs
03_PerimeterBays.cs
```

Deliver:

* internal zone/decomposition logic;
* perimeter generation;
* enabled/disabled edge behaviour;
* overlap removal;
* boundary containment;
* deterministic indexing;
* manual regression evidence.

---

# 55. Phase 3 — Central Parking

Migrate:

```text
04_CentralBays.cs
```

Deliver:

* central row generation;
* 90° parking;
* angled parking;
* aisle geometry;
* alternating flow;
* flow arrows;
* containment logic;
* manual regression evidence.

---

# 56. Phase 4 — Layout Composition and Exclusions

Migrate:

```text
05_MergeBays.cs
06_BayCuller.cs
```

Deliver:

* composition of perimeter and central results;
* exclusion handling;
* deterministic count handling;
* Core collection model;
* Grasshopper DataTree conversion;
* manual regression evidence.

Core must not depend on `DataTree`.

---

# 57. Phase 5 — Accessible Bays

Migrate:

```text
07_AccessibleBay.cs
```

Deliver:

* automatic accessible count;
* explicit accessible count;
* proximity-based allocation;
* consecutive-bank selection;
* standard-capacity consumption;
* insufficient-space warning;
* correct final counts;
* manual regression evidence.

Prototype default dimension inconsistencies must be corrected to the authoritative v1 values.

---

# 58. Phase 6 — Sardine.Layout

Combine the migrated generation algorithms behind the public:

```text
Sardine.Layout
```

component.

Verify:

* public inputs match this PRD;
* public outputs match this PRD;
* internal stages are hidden from the normal user;
* the component generates a full layout from the Site outputs;
* warnings are surfaced clearly;
* regression behaviour remains stable.

---

# 59. Phase 7 — Sardine.Bake

Extract the Rhino-document behaviour required from the proof-of-concept export stage.

Deliver:

* deliberate trigger behaviour;
* layer creation/reuse;
* boundary baking;
* perimeter classification;
* central classification;
* accessible classification;
* aisle baking;
* flow-arrow baking;
* ObjectId output;
* clear status messages.

Do not introduce persistent layout management.

---

# 60. Phase 8 — Sardine.Export

Extract the export responsibilities from the proof-of-concept `10_Export.cs`.

Deliver:

* explicit ObjectId export scope;
* DWG export;
* CSV creation;
* CSV append behaviour;
* output directory creation;
* option naming;
* controlled errors;
* trigger protection.

---

# 61. Phase 9 — Product Template

Create:

```text
examples/Sardine_Template_v1.gh
```

The template must:

* contain the four primary Sardine components;
* be pre-wired;
* contain a simple sample boundary;
* generate a result on first use once Sardine is installed;
* expose understandable user parameters;
* contain clear grouping/labels;
* avoid exposing implementation complexity.

---

# 62. Phase 10 — Release Hardening

Before v1.0.0:

* run all required manual regression fixtures;
* review all known warnings;
* confirm no release-blocking defects;
* build the release candidate through the supported Rhino 8 / Visual Studio
  workflow;
* complete the Rhino smoke test;
* test the example definition;
* verify DWG output;
* verify CSV output;
* verify version metadata;
* prepare installation instructions;
* prepare release notes.

---

# 63. Manual Rhino Release Smoke Test

A manual smoke test is required before the internal v1 release.

The release candidate must be tested in the supported WSP Rhino 8 environment.

Test:

1. install the compiled `.gha`;
2. launch Rhino 8;
3. launch Grasshopper;
4. verify the Sardine tab/components load;
5. open `Sardine_Template_v1.gh`;
6. generate the example layout;
7. change layout orientation;
8. change parking angle;
9. disable at least one perimeter edge;
10. add an exclusion zone;
11. generate accessible parking;
12. verify reported counts;
13. bake the result;
14. verify layer organisation;
15. export a DWG;
16. open/inspect the exported DWG;
17. verify the CSV summary record.

Failure of a required smoke-test step blocks release.

Automated licensed Rhino UI testing is not required for v1.0.0.

The product owner must record the release smoke-test pass before approving the
artifact for internal distribution.

---

# 64. Release Process

A release begins from an owner-approved `main`.

The process for this increment is:

```text
Feature branch
     ↓
Pull Request
     ↓
Manual feature gate
     ↓
Owner-recorded pass and approval
     ↓
Merge to main
     ↓
Release candidate smoke test
     ↓
Owner-recorded release pass
     ↓
Collect artifact
     ↓
Versioned internal publication
     ↓
Internal distribution
```

A release tag or manually published artifact must never bypass a failed required
manual validation step.

---

# 65. Manual Release Publication

Automated release publication is deferred for this increment. The product owner
may authorise manual internal publication of the approved versioned artifact.

For v1.0.0 the version identifier remains:

```text
v1.0.0
```

The recorded manual release process must:

1. identify the exact approved commit on `main`;
2. confirm the required manual feature and release gates have owner-recorded
   passes;
3. build through the supported workflow;
4. verify the version and `.gha`;
5. collect and publish the approved artifact to the internal distribution
   location.

The release binary must come from the recorded approved source revision.

---

# 66. Artifact Integrity

The internally distributed Sardine binary must be the same artifact approved
through the recorded manual release process.

Do not:

* rebuild the plugin after owner approval without repeating and recording the
  required validation;
* distribute a locally built binary instead;
* modify the binary before SharePoint distribution.

The pull request validation records, approved source revision and internal
publication record together identify the v1 proof-of-concept release. A future
automated release workflow may replace this manual process.

---

# 67. Git LFS Workflow for Rhino and Grasshopper Files

The repository's root `.gitattributes` tracks `*.gh`, `*.ghx` and `*.3dm` with
Git LFS. This is the authoritative workflow for the upcoming product template
and other Rhino/Grasshopper fixtures:

1. Install Git LFS once on each contributor machine, then initialise it:

```powershell
   git lfs install
```

2. Confirm the repository rules before adding a file:

```powershell
   git check-attr filter -- examples\Sardine_Template_v1.gh
```

   The reported filter must be `lfs`.

3. Add, commit and push the file through normal Git commands:

```powershell
git add examples\Sardine_Template_v1.gh
git commit -m "docs: add Sardine v1 Grasshopper template"
git push
```

   Git stores a small pointer in the Git commit and uploads the corresponding
   LFS object to GitHub. The file still appears and versions normally in commits
   and pull requests.

4. After cloning or switching to a revision, fetch the required file content
   with the normal checkout or explicitly run:

```powershell
   git lfs pull
```

5. Verify tracked files when diagnosing a missing or pointer-only file:

```powershell
   git lfs ls-files
```

Checking out an older commit retrieves the LFS object associated with that
version. Contributors must not remove these extensions from `.gitattributes`,
commit Rhino/Grasshopper binaries with LFS filters bypassed, or assume that a
Git pointer alone contains the binary content.

---

# 68. Release Artifacts

v1.0.0 should include:

```text
Sardine.gha
Sardine_Template_v1.gh
Installation instructions
Release notes
```

Optional supporting files may be included where useful.

---

# 69. Installation

The v1 installation process should remain simple.

Expected workflow:

1. obtain the approved Sardine v1.0.0 release;
2. copy `Sardine.gha` to the approved Grasshopper libraries location;
3. restart Rhino/Grasshopper;
4. open `Sardine_Template_v1.gh`;
5. verify Sardine components load.

Detailed WSP-specific installation instructions may be documented separately.

---

# 70. Semantic Versioning

Use semantic versioning.

```text
1.0.0    First internally approved compiled proof of concept
1.0.1    Backwards-compatible bug fix
1.1.0    Backwards-compatible feature release
2.0.0    Major product/API/architecture change
```

Git tags use:

```text
v1.0.0
v1.0.1
v1.1.0
v2.0.0
```

---

# 71. Known MVP Limitations

The following are acceptable v1.0.0 limitations when documented and handled safely:

* generation is not guaranteed to produce the globally optimal parking arrangement;
* complex irregular boundaries may result in unused areas;
* noisy/short survey edges may influence placement despite rationalisation;
* accessible-bank placement may be imperfect on unusual boundaries;
* the generated layout requires engineering review;
* manual edits made after baking are not tracked by Sardine;
* baked alternatives are not automatically managed;
* no entrance/exit network is modelled;
* no pedestrian routing is modelled;
* no traffic routing network is modelled;
* no graph model exists;
* no post-bake Inspector exists.

These limitations do not justify opportunistic redesign during unrelated v1 work.

---

# 72. Release-Blocking Defects

Examples of release-blocking defects include:

* plugin fails to load;
* valid basic boundary crashes generation;
* a required manual regression fixture fails;
* generated geometry is knowingly outside valid boundaries in normal supported cases;
* accessible allocation corrupts standard parking counts;
* Bake repeatedly duplicates geometry without deliberate triggering;
* DWG export fails in the supported release workflow;
* Release build fails;
* a required owner-recorded validation pass is missing;
* `.gha` is not produced;
* example Grasshopper definition is unusable.

---

# 73. Non-Blocking Defects

A known defect may be deferred when:

* it does not prevent the primary MVP workflow;
* it does not corrupt generated output;
* it does not create unsafe or misleading results;
* it is documented;
* a backlog issue exists where appropriate.

Deferral requires an explicit decision rather than accidental omission.

---

# 74. Release Acceptance Criteria

Sardine v1.0.0 may be released only when all of the following are true.

## Product

* `Sardine.Site` is functional.
* `Sardine.Layout` is functional.
* `Sardine.Bake` is functional.
* `Sardine.Export` is functional.
* The example Grasshopper definition works.
* Supported proof-of-concept generation behaviour has been migrated.

## Geometry

* perimeter generation passes its required manual regression fixtures;
* central generation passes its required manual regression fixtures;
* 90° parking passes;
* angled parking passes;
* exclusion handling passes;
* disabled-edge handling passes;
* boundary containment validation passes;
* accessible allocation validation passes.

## Engineering

* all production projects remain `net48`;
* no unapproved framework migration exists;
* Release build succeeds;
* all required manual fixture records have product-owner-recorded passes;
* the Core/GH architecture review passes;
* `.gha` artifact is produced;
* every included change entered through a focused, approved pull request;
* the release source is the exact owner-approved revision on `main`.

## Release

* the product owner records a pass for the manual Rhino smoke test;
* example definition passes;
* installation instructions exist;
* release notes exist;
* version is `1.0.0`;
* tag `v1.0.0` identifies the owner-approved revision on `main`;
* the manual publication record is complete;
* approved artifact is distributed internally.

---

# 75. v1.0.0 Exit Condition

The MVP is complete when a WSP user who did not develop Sardine can:

1. install Sardine;
2. open the supplied template;
3. supply a valid boundary;
4. generate a usable car park;
5. modify primary parking parameters;
6. exclude unsuitable edges;
7. define exclusion zones;
8. generate accessible bays;
9. understand the reported bay counts;
10. bake the option;
11. export the option;

without needing access to the original C# script components or understanding the internal generation pipeline.

At that point:

> **Ship v1.0.0.**

---

# 76. Deferred Post-v1 Development

Future requirements should be informed by actual usage of v1.0.0.

Likely areas include:

## v1.x

* usability improvements;
* clearer warnings;
* richer reporting;
* improved accessible placement;
* additional validation;
* packaging improvements;
* bug fixes;
* additional tested parking configurations.

## Future major release

Potential future architecture includes:

* persistent layout identity;
* semantic parking-space models;
* post-bake Inspector;
* editable/regeneratable parking regions;
* entrance/exit modelling;
* pedestrian modelling;
* `CarParkGraph`;
* walking-distance analysis;
* network routing;
* nearest-exit analysis;
* richer design optimisation.

These must not constrain the v1.0.0 implementation unnecessarily.

---

# 77. Future Framework Migration

Migration away from `net48` may be considered after v1.0.0.

It must be treated as an independent technical initiative.

## Deferred TODO — Align with Rhino 8's current .NET runtime

- [ ] After v1.0.0, assess and migrate Sardine to the latest modern .NET runtime
  supported by the WSP-deployed Rhino 8 version at the time of implementation.
- [ ] Align `Sardine.Core`, `Sardine.GH` and `Sardine.Tests` on the selected
  target framework and compatible RhinoCommon/Grasshopper packages.
- [ ] Remove or update incompatible Visual Studio launch profiles so each
  profile's Rhino runtime (`netcore` or `netfx`) matches its managed debugger
  and project target.
- [ ] Validate loading, debugging, deployment and the approved Sardine fixture
  catalogue under the selected runtime before changing the v1 default.

The migration must follow Rhino's supported runtime rather than selecting a .NET
version independently. Until it is completed, use the `Rhino 8 - netfx` launch
profile when debugging the frozen `net48` v1 projects; the retained
`Rhino 8 - netcore` profile is incompatible with their Desktop CLR debugger.

Before changing the framework, the project must assess:

* Rhino compatibility;
* Grasshopper compatibility;
* WSP deployment environment;
* NuGet/package compatibility;
* build pipeline changes;
* test infrastructure;
* binary compatibility;
* deployment behaviour;
* existing plugin behaviour.

The approved v1 fixture catalogue and validation records should become the
baseline for any future runtime migration and its automated regression suite.

Framework modernisation must not be combined casually with unrelated feature development.

---

# 78. Final Engineering Rule

When choosing between:

> a more sophisticated architecture that might be useful later

and:

> a simple, understandable, manually validated implementation that satisfies v1.0.0 today,

choose the simple, validated implementation.

Sardine v1.0.0 exists to:

> **ship a reliable car park generator.**

Everything else can follow from a stable first release.
