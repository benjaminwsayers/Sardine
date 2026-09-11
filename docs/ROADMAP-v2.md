# Sardine — Roadmap beyond v1.0.0

**Status:** Draft for product-owner review. Not yet authoritative.

**Relationship to `docs/PRD.md`:** The v1 PRD remains the build contract for
everything shipped as v1.x. Sections 6.1 and 6.2 of that PRD forbid the graph
model and persistent layout identity. This roadmap proposes lifting those
constraints at v2.0 and nowhere earlier. Doing so requires a v2 PRD that
formally supersedes them; until that document exists and is approved, the
constraints stand.

**Item types.** Every item in section 6 is tagged:

| Tag | Meaning |
| --- | --- |
| **F** — Feature | A committed deliverable for that release |
| **O** — Opportunity | Identified and worth doing, but not committed. Promote to Feature when a project needs it |

---

## 0. Read this before coding any number

Every dimension, percentage and threshold quoted in this roadmap is written
from general knowledge of UK practice. **None of it has been checked against a
current published standard.** Some of it is very likely to be slightly wrong,
and some of it changes with the local planning authority.

Sardine is a tool that engineers will use to produce evidence for planning
submissions. A wrong provision ratio compiled into the assembly is a
professional liability, not a bug.

So the rule for this whole roadmap is:

> Treat every figure below as *the rule Sardine must be able to express*,
> never as *the value to hard-code*. Before a number is committed, one person
> reads the current standard, records the clause reference next to the value,
> and the product owner signs it off.

This is also the strongest argument for the standards pack in v1.3: numbers
that live in a reviewable data file with clause references attached are
auditable. Numbers compiled into `SardineDefaults.cs` are not.

---

## 1. What we are actually building

The target is not a car park drawing tool. It is:

> **An engine that produces the best available layout for a site, balancing
> density against statutory compliance against the other competing objectives,
> and can show why that layout won.**

Three consequences follow immediately, and they shape everything below.

### 1.1 There is no single optimal car park

Yield, walking distance, circulation clarity, construction cost, landscape and
drainage trade against each other. A layout that maximises bay count will
usually have worse circulation and worse walking distances than one with two
fewer bays.

So the engine's output is not one answer. It is a **scored set of options with
a Pareto front**, and a defensible statement of what each option trades away.
`Option` must therefore be a first-class object in the model from the start,
not a name attached to a bake.

### 1.2 Orientation is the most fundamental variable, and it is reachable early

Orientation and row phase together determine more of the final bay count than
any other decision. Getting them right is worth more than every downstream
refinement combined.

The good news: **searching them does not require the graph, the standards pack
or multi-region support.** It requires exactly two things — a fast evaluation
function and a scoring model. Both are small. This is why the release plan
below inserts an orientation engine at v1.2, well before the graph.

### 1.3 Compliance is a constraint on the search, not a report at the end

If the standards live in a validator that runs after generation, the optimiser
will happily converge on layouts that fail. The rule set has to be shared
between the generator, the validator and the optimiser, so that an
infeasible candidate is rejected during search rather than discovered
afterwards.

---

## 2. Where v1.0.0 actually leaves us

v1 generates **bay geometry**. It does not yet generate a **car park**, and it
does not search. Each finding below was checked against the v1 source.

### 2.1 The layout has no connection to the outside world

There is no entry, no exit and no highway connection anywhere in the model. A
search of the v1 source for any entry concept returns nothing but an unrelated
private class in the accessible allocator. The generated car park cannot be
entered.

### 2.2 The circulation is drawn as disconnected fragments

Central aisles are emitted as one rectangle per row pair, spanning the extent
of the bays that pair placed plus half a bay step of padding. They terminate
where the bays run out, not where the perimeter ring begins, and there are no
cross-aisles joining one row pair to the next. The intervening ground is clear,
so a vehicle could pass, but the drawing shows aisles floating inside a ring.

### 2.3 Flow direction is computed and thrown away

`CentralBayGenerator.Result.FlowDirections` is populated with the one-way sign
of every aisle, and then no code anywhere reads it. `LayoutResult` has no field
for it and the Grasshopper component never asks. The same is true of
`SiteZones.FillDirections` and `FillAnglesDeg`.

Flow direction per aisle and bay access points are most of what a directed
circulation graph needs, and v1 already computes them. Persisting them is cheap
now and expensive to re-derive later.

### 2.4 Aisle width is decoupled from bay angle

`AisleWidth` is an independent input. Row pitch is `2 × bayDepth × sin θ +
aisleWidth`, so the angle affects the row depth but never the aisle. The whole
engineering purpose of angled parking is that a one-way aisle serving angled
bays can be substantially narrower than a two-way aisle serving perpendicular
bays. As it stands Sardine will silently produce a 45° layout with a 6.0 m
aisle or a 90° layout with a 4.2 m aisle, and warn about neither.

### 2.5 The accessible bay is a single envelope rectangle

The allocator draws one rectangle of `AccessibleWidth × AccessibleDepth`. UK
practice distinguishes the *marked bay* from its *access zones* — the hatched
areas that keep the bay usable by a wheelchair user — and drawings show both
separately because the hatching is what gets enforced. Whether adjacent bays in
a bank may share an access zone also changes the space arithmetic, and
therefore the yield. v1 assumes unshared zones by construction without saying so.

### 2.6 Provision is one hardcoded ratio for one bay type

`AccessibleProvision.Ratio = 0.05` applied to standard capacity, minimum one.
That is the PRD's stated v1 rule, faithfully implemented. But the ratio is use
class dependent and routinely overridden by the LPA; the basis matters, since a
percentage of total capacity and of standard capacity differ and nothing in the
output says which was used; and there is no provision engine at all for EV,
family, motorcycle, cycle or trolley bays.

### 2.7 Electric vehicle provision is absent, and it is statutory

Approved Document S applies to new buildings with associated parking in
England, setting minimum charge point and cable route requirements above a
threshold number of spaces. Sardine cannot currently produce a
Building-Regulations-compliant scheme for a new commercial development. This is
the most commercially significant gap on the list.

### 2.8 One orientation, one phase, one region

The central generator takes a single `Orientation` and lays one lattice across
the entire central region. `SiteZones.FillDirections` already computes the top
three dominant directions and, per 2.3, discards them.

Worse for the stated goal: the lattice phase is pinned. `vStart = vMin −
rowPitch` puts the first row wherever the bounding extent happens to fall. On a
site whose usable depth is not a whole multiple of the row pitch, the leftover
strip lands by accident. Sliding the lattice through one row pitch and
re-scoring can gain or lose an entire row, and v1 never tries.

### 2.9 Evaluation is far too slow to search

Nothing in v1 was built to be called in a loop, and two paths in particular
would dominate any sweep.

- **Containment.** `BayGeometry.AllCornersInsideOrCoincident` calls
  `Curve.Contains` up to four times per candidate bay, and the central
  generator tests every lattice position across the bounding box, most of
  which fail.
- **Perimeter overlap culling.** The cull loop restarts its scan from index
  zero after every removal. With *n* candidates and *k* removals that is
  O(*n·k*) pair checks, each doing up to eight `Contains` calls. Only the pair
  spanning the gap actually needs rechecking, which would make it linear.

Actual cost needs measuring rather than asserting, but the shape is clear: an
orientation sweep needs candidate evaluation in the low milliseconds, and this
is nowhere near it.

### 2.10 Other known limitations

Everything sits at Z = 0, so no falls and no gradient check on accessible bays.
No kerbs or footways, so pedestrians implicitly walk in the aisles. No walls,
columns or obstructions, so no end-bay width uplift. Metres only, with no
reference to document units. No automated tests of any kind.

---

## 3. Architecture

### 3.1 What v1 already does, and what it does not

**v1 already offsets, and already builds bay-aisle-bay.** The zone decomposer
insets the working boundary by one bay depth to get the perimeter alignment and
by bay depth plus aisle width to get the central region. The central generator
then lays rows at a pitch of two projected bay depths plus an aisle width, with
the aisle sitting between the two rows of the pair. Geometrically, that is a
double-loaded parking module. The perimeter ring is one aisle width wide by
construction, so the perimeter arrangement is effectively bay-aisle too.

The difference is that **v1 has module-shaped geometry without module objects.**

Concretely, v1 generates one infinite lattice in one orientation and clips it
to the polygon:

- the lattice phase is fixed at the bounding extent and never varied (2.8);
- there is one orientation for the whole site (2.8);
- the aisle rectangle is drawn *after* the bays, from the extent of the
  survivors, so it is a decoration rather than a structural element (2.2);
- nothing can be individually moved, shortened at an end condition, swapped
  between single- and double-loaded, or given its own orientation.

**So this is a generalisation, not a rewrite.** v1 is the one-region,
one-orientation, fixed-phase special case of the architecture below.
Generalising means making the module an object, searching orientation and phase
instead of fixing them, and allowing more than one region.

### 3.2 The parking module is the generative primitive

A parking module is one aisle plus the bay rows flanking it. This is how the
IStructE guidance and every human designer already thinks about car park
layout, and it is the right computational primitive for one reason:

> A bay that is not adjacent to an aisle is worthless. A module bundles the
> accessibility constraint into the geometry, so a module is drivable by
> construction.

That collapses the search space. Placing several hundred bays independently is
an intractable combinatorial problem and also the wrong problem. Placing
modules reduces the design variables, per region, to roughly:

| Variable | Typical range |
| --- | --- |
| Orientation | 0–180° |
| Bay angle | 45–90°, a few discrete values |
| Module type | single-loaded, double-loaded |
| Phase offset | 0 to one row pitch |
| End condition | square, angled, closed |

Four or five variables per region, across a handful of regions, is small enough
to search systematically.

### 3.3 The graph is a derived view, not the core model

Three candidate core representations, and why the third wins:

| Representation | Verdict |
| --- | --- |
| **Geometry-first** (v1: curves in a tree) | Simple, cannot reason about itself |
| **Graph-first** (nodes and edges primary, geometry derived) | Good for reasoning, bad for generation, and you cannot offset a graph to draw a kerb |
| **Module-first, with geometry and graph as dual views** | Both projections fall out of one arrangement |

Generating *from* a graph fails because nothing in the representation knows
about rectangle packing. You get plausible-looking networks with unfillable
gaps. Meanwhile the drawing is the deliverable, and geometry is what gets
drawn.

Derive the graph from module adjacency instead, where it costs nothing: each
module's aisle centreline is an edge, each bay's access point is already known,
and module abutment gives the junctions. Then the graph earns its keep on
everything geometry cannot answer — see section 7.

### 3.4 The five layers

Core stays a plain .NET library with no Grasshopper reference.

| Layer | Holds | Notes |
| --- | --- | --- |
| **Site** | Polygon with holes, obstructions, entries, levels | Clipper2 rather than RhinoCommon offsets |
| **Partition** | Regions derived from dominant directions and the straight skeleton | Deterministic and explainable to an engineer |
| **Modules** | Module instances tiled per region | The generative primitive |
| **Rules** | Hard constraints and soft objectives, as objects | One rule set, three consumers |
| **Views** | Geometry and graph | Both projections of the module arrangement |

### 3.5 Search strategy: deterministic sweep before metaheuristics

Once the module abstraction has shrunk the space, a systematic sweep is both
feasible and preferable.

Sweeping orientation in small steps against a few bay angles, module types and
phase offsets is on the order of a few thousand candidates for a single region.
At a few milliseconds per evaluation that is seconds of work.

**Prefer that to a genetic algorithm**, for a reason that matters more than
speed: a sweep returns the same answer every time. A Galapagos run does not.
An engineer defending a layout at a planning committee, or re-running a scheme
eighteen months later for a revised application, needs reproducibility. Record
determinism as a product requirement, not a preference.

Reach for metaheuristics or a constraint solver only when the region count
makes the product of the per-region sweeps genuinely explode.

### 3.6 The evaluation performance budget

Everything above depends on one number: how long it takes to score a candidate
layout. Treat it as a designed budget, not an emergent property.

| Evaluation cost | What becomes possible |
| --- | --- |
| ~100 ms | Roughly ten options, interactively |
| ~10 ms | Hundreds of options, a coarse sweep |
| ~1 ms | Thousands of options, a Pareto front, live sliders |

Getting there means keeping RhinoCommon's heavy geometry calls out of the inner
loop entirely: precomputed winding-number containment over a cached vertex
array, a spatial index for overlap queries, and no `AreaMassProperties` or
`Curve.Contains` per candidate bay. See 2.9 for where v1 currently violates
this.

### 3.7 Where the work runs relative to the Grasshopper solver

Grasshopper's dataflow model recomputes everything downstream of any change. A
three-second generator makes every slider nudge cost three seconds, and an
optimisation loop inside a component fights the solver.

So: expensive work happens on an explicit trigger or behind a cache keyed on a
hash of the inputs, the layout model passes between components as a custom Goo
type rather than being rebuilt from curves at each hop, and any sweep runs its
own loop internally rather than driving the GH solver round.

---

## 4. The organising idea

The graph is not a feature to add at the end. It is the destination of a
progressive enrichment of the model, where each step is independently useful:

```text
curves in a tree                 v1.0    a yield study
      ↓  + fast evaluation, scoring, orientation and phase search
a searched layout                v1.2    the best orientation, defensibly
      ↓  + typed bays, standards as constraints
a compliant layout               v1.3    planning evidence
      ↓  + module objects, regions, connected circulation
a buildable layout               v1.4    a drawing an engineer would sign
      ↓  + nodes and edges over the same objects
a graph                          v2.0    routing, walking distance, analysis
      ↓  + multi-objective search over the above
an engine                        v2.1    a Pareto front of defensible options
```

Four tracks run across those releases.

| Track | Question it answers | Owner-facing value |
| --- | --- | --- |
| **A — Compliance** | Does this scheme satisfy the standard? | Planning submissions, client sign-off |
| **B — Realism** | Is this drawing buildable? | Fewer redesign loops, credible DWGs |
| **C — Model** | Can we reason about this layout? | The graph, and everything downstream |
| **D — Search** | Is this the best layout available? | The stated product goal |

Track C is the one most tempting to defer and most expensive to defer, because
it changes data structures. Track D is the one the product is actually for.

---

## 5. UK requirements and client drivers

What the tool must eventually be able to express. Figures deliberately omitted
where they need verification; the point of the table is the rule shape.

| Requirement | Typical source | Sardine today | What closes it | Release |
| --- | --- | --- | --- | --- |
| Accessible bay dimensions and access zones | BS 8300-1 | Single envelope rectangle | Marked bay + separate access zone geometry; decision on zone sharing | v1.3 |
| Accessible provision ratio | BS 8300-1, LPA policy | Hardcoded 5% of standard | Provision engine: ratio, minimum, basis, rounding, per use class | v1.3 |
| Accessible bays close to the entrance | BS 8300-1 | Straight-line proximity to a point | Route distance to a nominated entrance | v2.0 |
| Accessible route, dropped kerbs, level bays | BS 8300-1 | None | Footway geometry, then route and gradient validation | v1.4 / v2.0 |
| EV charge points and cable routes | Approved Doc S, LPA | None | EV bay type, active vs passive provision, cable route zones | v1.3 / v1.4 |
| Cycle parking | LTN 1/20, LPA | None | Stand geometry and provision by use class | v1.4 |
| Motorcycle parking | LPA | None | Bay type in the standards pack | v1.3 |
| Bay and aisle dimensions | IStructE guide, LPA | Fixed inputs | Standards pack; aisle width coupled to angle and flow | v1.3 |
| Fire appliance access | Approved Doc B | None | Route width, turning and reach checks on the graph | v2.1 |
| Refuse and servicing access | BS 5906, LPA | None | Same mechanism as fire access | v2.1 |
| Swept path / vehicle tracking | Client, planning | None | Export design-vehicle envelopes; basic turning check | v2.1 |
| Gradients and levels | BS 8300-1, IStructE | Planar only | Site plane or surface input; per-bay gradient check | v1.4 |
| Drainage and SuDS | Sched. 3 FWMA, LPA | None | Permeable area and falls reporting | v1.4 |
| Biodiversity net gain, tree canopy | Environment Act 2021, LPA | Islands as exclusions only | Count island area and canopy as a reported metric | v1.4 |
| Family / parent-and-child bays | Client (retail) | None | Bay type, provision and placement near entrance | v1.4 |
| Trolley bays | Client (retail) | None | Bay type and provision per N spaces | v1.4 |
| Lighting | BS 5489-1 | None | Out of scope; export bay centroids for a lighting model | — |
| Surveillance, sightlines, Park Mark | Safer Parking Scheme | None | Sightline analysis on the graph | v2.2+ |
| Yield and efficiency | Client | Four counts | m² per space, spaces per hectare, circulation ratio | v1.1 |

---

## 6. Release plan

Effort is relative and assumes part-time solo development:
**S** ≈ days · **M** ≈ 1–2 weeks · **L** ≈ 3–6 weeks · **XL** ≈ 2 months+

Type: **F** = committed Feature · **O** = Opportunity, not committed

### v1.1 — Connect and report

*Goal: the same generator, but the drawing is complete and the numbers are
presentable. No new geometry types, no architecture change.*

| # | Type | Item | Track | Effort | Why |
| --- | --- | --- | --- | --- | --- |
| 1.1.1 | F | Extend central aisles to the perimeter ring; add cross-aisles between row pairs | B | M | Fixes 2.2 — the drawing reads as disconnected |
| 1.1.2 | F | `Entry` input: points on the boundary, with in / out / both | B, C | S | Fixes 2.1; the first semantic non-bay object |
| 1.1.3 | F | Persist `FlowDirections` and aisle centrelines on `LayoutResult`; expose as outputs | C | S | Fixes 2.3; cheapest possible groundwork for the graph |
| 1.1.4 | F | Document and expose `Bay.Frame.Origin` as the bay access point | C | S | Already computed and stored; it is half the graph |
| 1.1.5 | F | `Option` as a first-class Core object carrying parameters, geometry, counts and scores | C, D | M | Prerequisite for comparison, Pareto fronts and the dashboard |
| 1.1.6 | F | Yield metrics: m² per space, spaces per hectare, aisle length per space | A | S | First thing a client asks after the bay count |
| 1.1.7 | F | Warnings that carry counts: bays removed by exclusion, containment, overlap | A | S | "3 bays removed" beats a silent difference |
| 1.1.8 | F | Bake to a per-option sublayer | B | S | Two alternatives without collision; still no registry |
| 1.1.9 | F | Component icons and the `examples/` template | B | M | Required for the v1 release anyway |
| 1.1.10 | O | Emit the perimeter ring as an explicit aisle rather than an implied annulus | B, C | S | Makes the ring a graph edge later, and draws correctly now |

**Exit:** a generated layout can be driven into and driven around, and the DWG
looks like a car park.

### v1.2 — The orientation engine

*Goal: stop asking the user for the orientation and start finding it. This is
the release that turns Sardine from a generator into an engine, and it needs
neither the graph nor the standards pack.*

| # | Type | Item | Track | Effort | Why |
| --- | --- | --- | --- | --- | --- |
| 1.2.1 | F | **Fast evaluation path** — precomputed winding-number containment, cached vertex arrays, spatial index for overlap; no RhinoCommon calls in the inner loop | D | L | Fixes 2.9; everything in this release depends on it |
| 1.2.2 | F | Fix the perimeter overlap cull to recheck only the pair spanning a removal | D | S | O(*n·k*) becomes linear; see 2.9 |
| 1.2.3 | F | `LayoutScore` model: yield, m² per space, circulation ratio, compactness, with explicit weights | D | M | The objective function, versioned and reviewable |
| 1.2.4 | F | **Orientation sweep** — evaluate orientation across a configurable step, return the best plus the full score curve | D | M | The single highest-yield variable; the stated fundamental element |
| 1.2.5 | F | **Phase sweep** — slide the lattice through one row pitch and re-score | D | S | Fixes half of 2.8; can gain a whole row for very little code |
| 1.2.6 | F | Deterministic, reproducible sweep with a recorded discretisation | D | S | Same answer every run; see 3.5 |
| 1.2.7 | F | `Sardine.Optimise` component returning ranked options and the score curve | D | M | Makes the search visible and defensible |
| 1.2.8 | O | Bay angle in the sweep alongside orientation | D | S | Cheap once 1.2.4 exists, if aisle width is coupled (needs 1.3.6) |
| 1.2.9 | O | Cache solved options on a hash of the inputs | D | M | Keeps the GH canvas responsive; see 3.7 |
| 1.2.10 | O | Benchmark harness recording evaluation time per candidate | D | S | Protects the 3.6 budget from regressions |

**Exit:** Sardine reports the best orientation and phase for a site, with the
score curve that justifies it, and the same inputs always give the same answer.

### v1.3 — The standards pack

*Goal: stop hard-coding one organisation's numbers, and turn compliance into a
constraint the search respects rather than a report at the end.*

| # | Type | Item | Track | Effort | Why |
| --- | --- | --- | --- | --- | --- |
| 1.3.1 | F | `ParkingStandard` model: bay dimensions, aisle table, provision rules, gradients, clause references | A | L | The core of the release |
| 1.3.2 | F | Load standards from JSON, not compiled constants | A | M | An engineer must add an LPA without Visual Studio |
| 1.3.3 | F | Named presets: BS 8300 + AD S baseline, IStructE, LPA template, client override | A | M | Adoption depends on this |
| 1.3.4 | F | **Rule engine** — each constraint an object that evaluates *and* explains itself, shared by generator, validator and optimiser | A, D | L | See 1.3; prevents optimising infeasible layouts |
| 1.3.5 | F | Hard constraint vs soft objective separation | A, D | M | Feasibility and quality are different questions |
| 1.3.6 | F | Aisle width from bay angle and one-way/two-way, with override and a warning below the table | A, B | S | Fixes 2.4; unblocks 1.2.8 |
| 1.3.7 | F | Bay types as first-class kinds: standard, accessible, EV, family, motorcycle, parallel | A, C | L | Touches the DataTree contract — see decision 8.1 |
| 1.3.8 | F | Provision engine: required count per type, with basis and rounding stated | A | M | Replaces `AccessibleProvision`, generalised |
| 1.3.9 | F | Accessible bay drawn as marked bay + access zone; decide zone sharing | A, B | M | Fixes 2.5 |
| 1.3.10 | F | **Compliance schedule output** — required vs provided per type, pass/fail, clause reference | A | M | The thing that goes in the Transport Statement |
| 1.3.11 | F | Compliance as a term in the objective function | A, D | M | Search prefers compliant layouts rather than discovering failure later |
| 1.3.12 | O | End-bay width uplift where a bay abuts a wall, column or kerb | B | M | Needs an obstruction input |
| 1.3.13 | O | Standards schema versioning and a validator for user-authored packs | A | M | Protects against a malformed LPA file failing silently |

**Exit:** Sardine can be pointed at a different authority's standards without a
code change, prints a schedule an engineer can put in a report, and the
optimiser will not return a non-compliant option.

### v1.4 — Modules and regions

*Goal: generalise the single clipped lattice into placed modules across
partitioned regions. This is where 3.1's "not a rewrite" is cashed in.*

| # | Type | Item | Track | Effort | Why |
| --- | --- | --- | --- | --- | --- |
| 1.4.1 | F | **`Module` as a first-class object** — aisle plus flanking rows, single or double loaded, with end conditions | C, D | L | The generative primitive; see 3.2 |
| 1.4.2 | F | Replace RhinoCommon offsetting with Clipper2 | B | M | Removes the both-directions-and-guess workaround; robust on concave polygons |
| 1.4.3 | F | **Region partition** — BSP guided by dominant boundary directions, medial axis or straight skeleton for the primary spine | B, D | XL | Fixes the rest of 2.8; largest single yield gain |
| 1.4.4 | F | Per-region orientation, angle, module type and phase in the sweep | D | L | Extends v1.2's search to the partitioned model |
| 1.4.5 | F | Cross-aisles and region connection as structural objects | B, C | L | Circulation becomes real geometry, not decoration |
| 1.4.6 | F | Single-loaded modules against boundaries and obstructions | B | M | Yield on awkward edges |
| 1.4.7 | F | EV bay allocation, charger positions and cable route zones | A, B | L | Statutory; see 2.7 |
| 1.4.8 | F | Family and trolley bays placed by proximity to entrance | A, B | M | Retail client requirement |
| 1.4.9 | F | Parallel bays on short or awkward edges | B | M | Yield on irregular sites |
| 1.4.10 | F | Pedestrian footway strips and crossing points | B | L | Prerequisite for pedestrian routing in v2.0 |
| 1.4.11 | F | Site plane or surface input; per-bay gradient check | A, B | L | Fixes part of 2.10 |
| 1.4.12 | O | Landscape islands and tree positions; report canopy and permeable area | A | M | BNG and SuDS reporting |
| 1.4.13 | O | Cycle parking generator | A | M | Usually a planning condition |
| 1.4.14 | O | Tiling by adjacency rules (wave function collapse or a shape grammar) for module arrangement | D | L | Alternative to BSP where regions are highly irregular |

**Exit:** a scheme generated by Sardine is close to what an engineer would draw
by hand, on an irregular site, and the compliance schedule covers the statutory
items.

### v2.0 — The semantic model and the graph

*Goal: represent the car park as a network and answer what geometry cannot.*
*Requires a v2 PRD superseding v1 PRD §6.1 and §6.2.*

| # | Type | Item | Track | Effort | Why |
| --- | --- | --- | --- | --- | --- |
| 2.0.1 | F | `LayoutModel`: bays, modules, aisles, entries, destinations with stable IDs | C | L | The representation change |
| 2.0.2 | F | **`CarParkGraph` derived from module adjacency**, not built separately | C | L | See 3.3 and section 7 |
| 2.0.3 | F | `GH_CarParkGraph` Goo type | C | M | Deliberate reversal of PRD §29 — see decision 8.3 |
| 2.0.4 | F | Vehicle reachability and egress validation from every entry | B | M | Catches unreachable bays, undetectable in v1 |
| 2.0.5 | F | Dead-end and turning-head detection | B | M | Real design rule |
| 2.0.6 | F | One-way consistency validation | B | S | Falls out of the directed graph |
| 2.0.7 | F | Pedestrian walking distance from every bay to nominated destinations | A | M | BS 8300 cares about route, not crow-flies |
| 2.0.8 | F | Accessible placement by route distance | A | M | The compliance answer, replacing proximity |
| 2.0.9 | F | **Walking distance as an objective term**, not just a report | A, D | M | Closes the loop: the engine optimises what the standard cares about |
| 2.0.10 | F | `Sardine.Analyse`: per-bay distances, worst case, mean, isochrone colouring | A | M | Makes the graph visible and sellable |
| 2.0.11 | F | **Automated test project** — graph algorithms, no Rhino host required | — | M | See section 10 |
| 2.0.12 | O | QuikGraph rather than hand-rolled traversals | C | S | Fewer bugs, standard algorithms |
| 2.0.13 | O | Reconstruct the graph from baked Rhino geometry | C | XL | Only if the Inspector is genuinely wanted — see 8.6 |

**Exit:** Sardine can prove every bay is reachable, state the walking distance
to the entrance for each one, and use both in the search.

### v2.1 — Multi-objective optimisation

*Goal: the full engine. Competing objectives, a Pareto front, and a defensible
account of what each option trades away.*

| # | Type | Item | Track | Effort | Why |
| --- | --- | --- | --- | --- | --- |
| 2.1.1 | F | **Multi-objective scoring and Pareto front extraction** | D | L | See 1.1; the product goal |
| 2.1.2 | F | Option comparison dashboard across the front | A, D | L | How a human picks |
| 2.1.3 | F | Trade-off reporting: what each option gives up, in the client's terms | A, D | M | Makes the recommendation defensible |
| 2.1.4 | F | Fire appliance access route check against Approved Doc B | A | L | Statutory |
| 2.1.5 | F | Refuse and servicing vehicle routes | A | M | Planning requirement |
| 2.1.6 | O | Swept path export for AutoTrack, and a basic turning feasibility check | A, B | L | Removes a manual step |
| 2.1.7 | O | Nearest-exit and evacuation analysis | A | M | Falls out of the graph |
| 2.1.8 | O | **OR-Tools CP-SAT** for module placement where the sweep explodes | D | XL | Only if 3.5's sweep proves insufficient |
| 2.1.9 | O | Surrogate model to guide search if evaluation cannot be made fast enough | D | XL | Fallback, not a first choice |
| 2.1.10 | O | Rhino Compute / Hops for headless sweeps at scale | D | L | Also the route to a web front end |

### v2.2 and beyond

| Type | Item | Note |
| --- | --- | --- |
| O | Post-bake Inspector | Depends on decision 8.2 and 2.0.13 |
| O | Region-level editing and regeneration (`Sardine.Refine`) | The PRD's deferred editing tools |
| O | Multi-storey and split-level | Ramps, headroom, structural grids: a different product |
| O | Sightline and surveillance analysis | Park Mark and Secured by Design |
| O | IFC and Civil 3D export | Downstream coordination |
| O | Learned ranking of equally compliant options | See 9.4 |
| O | ML classification of survey line work into boundary, kerb and building | See 9.4 |

---

## 7. The graph, in enough detail to build

Two graphs sharing one node set: a **directed vehicle graph** and an
**undirected pedestrian graph**. Sharing nodes is what lets you ask how far
someone walks from a bay a car can actually reach.

Both are derived from the module arrangement (3.3), not authored separately.

### Nodes

| Type | Represents | Key attributes |
| --- | --- | --- |
| `EntryNode` | Highway connection | direction (in / out / both), lanes, control |
| `AisleNode` | Junction or terminus in circulation | position, is-junction |
| `BayNode` | One parking space | bay kind, dimensions, module, index, access point |
| `DestinationNode` | Building entrance, lift, stair, trolley bay | kind, weight (share of trips) |
| `CrossingNode` | Pedestrian crossing on an aisle | controlled or informal |

### Edges

| Type | Between | Key attributes |
| --- | --- | --- |
| `AisleEdge` | AisleNode ↔ AisleNode | width, flow (+1 / −1 / two-way), length, gradient, centreline |
| `BayAccessEdge` | BayNode ↔ AisleNode | manoeuvre direction; angled bays are one-way-in |
| `WalkEdge` | any ↔ any | length, surface (footway / carriageway / crossing), step-free |

### Algorithms and what each catches

| Algorithm | Question | Failure it catches |
| --- | --- | --- |
| BFS from each `EntryNode` over the vehicle graph | Can you drive to every bay? | Unreachable bays — undetectable in v1 |
| Reverse BFS to exit-capable entries | Can you drive out from every bay? | One-way traps |
| Degree-1 `AisleNode` that is not an entry | Where are the dead ends? | Missing turning heads |
| Dijkstra on the pedestrian graph from each destination | How far does each bay walk? | Accessible bays too far from the entrance |
| Constrained selection over route distance | Which bays are the accessible bank? | Replaces crow-flies placement |
| Total `AisleEdge` length ÷ bay count | How efficient is the circulation? | Over-provisioned aisles |

### Why v1.1 items 3 and 4 matter so much

The bay access point is **already stored** — it is `Bay.Frame.Origin`, the
point on the aisle face where the bay is entered. The aisle flow sign is
**already computed**, then discarded. Between them that is `BayNode`,
`BayAccessEdge` and the direction on `AisleEdge`.

Persisting those two things while the generator code is fresh turns v2.0's
graph construction from an archaeology exercise into a short mapping function.
Days of work now against weeks later. If nothing else on the v1.1 list gets
done, do these.

---

## 8. Decisions to take before the work starts

Cheap to decide now, expensive to reverse later.

### 8.1 Does the graph replace the DataTree output, or sit beside it?

The PRD promises path stability for the whole v1.x line, and a breaking change
would strand every saved definition. But v1.3.7 adds bay types beyond the three
existing prefixes, which needs a rule.

**Recommendation:** keep `{0;edge;bay}`, `{1;row;bay}` and `{2;bank;bay}` as
the public contract and add the graph as a new output. Keep the three prefixes
as classification *by placement*, and carry bay *type* (EV, family, motorcycle)
as a model attribute surfaced on a parallel `BayTypes` output rather than as
new path prefixes.

### 8.2 Are bay identities stable across regeneration?

Needed for the Inspector, option comparison and post-bake tracking. The current
`{kind; group; index}` address churns whenever an upstream bay is removed.

**Recommendation:** derive the ID from a spatial hash of the bay centroid
rounded to tolerance, not from the ordinal index. Decide before 2.0.1, because
it determines whether the Inspector is possible at all.

### 8.3 Is a custom Grasshopper Goo type permitted?

PRD §29 says avoid them. That was right for v1. Passing a graph or a layout
model between components effectively requires one, and the original backlog
anticipated `GH_CarParkGraph`.

**Recommendation:** permit exactly one Goo type wrapping the layout model, and
record it in the v2 PRD as a deliberate reversal with its reason. Rebuilding
the model from curves at every component hop would also break the 3.6 budget.

### 8.4 Where do the standards live?

Compiled constants make the numbers unauditable and mean a new LPA needs a
release.

**Recommendation:** JSON resource files loaded at runtime, with a schema
version and a clause reference beside every value. This is the difference
between a WSP tool and a personal tool.

### 8.5 Determinism as a product requirement

**Recommendation:** record in the v2 PRD that identical inputs must produce
identical outputs, and that any stochastic search must expose and record its
seed. A layout defended at planning committee has to be reproducible eighteen
months later. This rules out an unseeded genetic algorithm as the default
search, though not as an option.

### 8.6 Does the graph need to survive baking?

If the Inspector is genuinely wanted, the graph must be reconstructible from
baked Rhino geometry, which is materially harder than building it from a live
solution.

**Recommendation:** decide before 2.0.1. If the answer is no, item 2.0.13 and
most of v2.2's editing tools come off the list, which is a significant saving.

### 8.7 Framework: stay on net48 or move to .NET 8?

PRD §77 already anticipates this migration and defers it past v1.0.0. Moving
unlocks `System.Text.Json` for the standards pack, spans and modern LINQ for
the fast evaluation path, and the current builds of Clipper2, OR-Tools and
QuikGraph.

**Recommendation:** migrate at v1.2, because that release is where evaluation
performance starts to matter. Treat it as its own change with its own
regression run, per PRD §77, and not bundled with feature work.

### 8.8 Units

v1 is metres-only by decision, with no reference to document units.

**Recommendation:** warn in v1.1, convert in v1.3. A standards file quoting
millimetres will otherwise be a recurring source of error.

---

## 9. Technology choices

### 9.1 Clipper2 for polygon operations

v1's `CurveOffsets.OffsetInward` offsets in both directions and picks the
result whose centroid falls inside the original. That is a workaround for
RhinoCommon offset behaviour on concave polygons, where self-intersection
produces multiple fragments. Clipper2 does polygon offsetting and boolean
operations with proper self-intersection handling and returns clean polygon
sets. For a tool whose first stage is two insets of an arbitrary survey
boundary, this is a meaningful robustness upgrade. **Item 1.4.2.**

### 9.2 OR-Tools CP-SAT, if the sweep is not enough

Module placement across partitioned regions can be formulated as a constraint
satisfaction problem, and CP-SAT is a strong free solver with .NET bindings.

It is listed as an Opportunity (2.1.8), not a Feature, deliberately. The sweep
in 3.5 should be tried first and may well be sufficient. Reaching for a solver
before the abstraction is right is how layout projects fail.

### 9.3 QuikGraph for graph algorithms

Standard implementations of BFS, Dijkstra and connectivity, rather than
hand-rolled versions with their own bugs. **Item 2.0.12.**

### 9.4 Where machine learning does and does not pay

Not for layout generation, for four reasons: there is no training corpus of
approved schemes in machine-readable form; the constraints are hard geometric
ones that learned models handle badly; determinism is a requirement (8.5); and
classical optimisation over a well-chosen abstraction will outperform a learned
policy at this scale.

Two places it would genuinely pay later, both listed as Opportunities:

- **Ranking equally compliant options.** Which of two feasible layouts does an
  engineer actually prefer? That is a learning-to-rank problem with a plausible
  data collection story, since the tool can log which option was chosen.
- **Classifying survey line work.** Sorting imported DWG geometry into
  boundary, kerb, building and services is a real recurring pain point and a
  reasonable classification task.

### 9.5 Rhino Compute and Hops

Running the generator headless on a server enables large sweeps without tying
up a workstation, and is also the natural route to a web front end for
non-Rhino users. Listed as an Opportunity (2.1.10) because it only makes sense
once the sweep is genuinely expensive.

---

## 10. Critical path

If capacity allows only three things, do these, in this order:

1. **v1.1 items 1.1.2, 1.1.3 and 1.1.4** — entries, flow directions, access
   points. Days of work. Turns the graph from a rewrite into a mapping.
2. **v1.2 — the orientation engine.** Fast evaluation plus a scoring model plus
   an orientation and phase sweep. This is the stated product goal, and it is
   reachable without the graph or the standards pack.
3. **v1.3 — the standards pack with a shared rule engine.** Without it the
   engine optimises against one organisation's assumptions and cannot prove
   compliance.

The graph then follows naturally, because by that point the model has entries,
modules, connected circulation and a scoring function to feed.

---

## 11. Explicitly not doing

- **Not** rewriting the v1 generators because a graph exists. The generators
  produce the geometry; the graph describes it. They stay, and get generalised.
- **Not** making the graph the core model. See 3.3.
- **Not** building an optimiser before evaluation is fast. A slow fitness
  function makes every search strategy look bad.
- **Not** using an unseeded stochastic search as the default. See 8.5.
- **Not** modelling lighting, drainage design, structures or ventilation.
  Sardine exports what those models need and stops there.
- **Not** multi-storey before surface parking is solved.
- **Not** machine learning for layout generation. See 9.4.
- **Not** public distribution or Yak packaging. Still internal.

---

## 12. Process and validation changes

v1's acceptance gate is manual regression in Rhino 8, because Rhino-hosted
automated testing is awkward and was deferred (PRD §42, §32).

**Both the search work and the graph change this argument.** Scoring functions,
provision arithmetic, reachability, Dijkstra and dead-end detection are
operations on plain numbers and adjacency lists. They need no Rhino document,
no licence and no UI. `Point3d` and `Vector3d` are value types whose arithmetic
runs headless.

- Design the scoring and graph layers to be **constructible from plain
  coordinates and adjacency**, not only from RhinoCommon curves, precisely so
  they are testable without a host.
- Ship `Sardine.Tests` **with v2.0 at the latest**, and ideally alongside the
  v1.2 scoring model, covering scores, provision and graph algorithms.
- Add a **benchmark harness** (item 1.2.10) so the 3.6 evaluation budget is
  protected against regressions the same way behaviour is.
- Leave geometry generation on the manual Rhino gate. That split is honest:
  test what can be tested cheaply, inspect the rest.
- Extend the fixture catalogue (PRD §35, fixtures A–I), do not replace it. Add
  fixtures for connectivity, provision, walking distance, and a known-yield
  site for the orientation sweep.

---

## 13. Open questions for the product owner

1. **Which standards pack ships first?** A generic BS 8300 + Approved Doc S
   baseline, or the specific authority's standards a live project needs? The
   live project probably wins, but it shapes v1.3.
2. **Is there a live scheme to validate against?** One real WSP car park with a
   known approved bay count would be worth more than any number of synthetic
   fixtures. It would also give the orientation sweep something to be measured
   against, which is the only way to know the engine is actually better than a
   human.
3. **What are the objective weights?** The engine has to trade yield against
   walking distance against circulation efficiency. Someone has to say what the
   default weighting is, and whether it varies by scheme type. This is a
   product decision, not an engineering one.
4. **Who is the user in eighteen months?** Benj alone, the WSP UK parking team,
   or anyone in WSP with Rhino? The answer decides how much decision 8.4
   matters and whether a web front end (9.5) is ever needed.
5. **Orientation semantics**, still open from v1: does `Orientation = 0` mean
   World X, or align to the dominant site edge? v1.2's sweep partly dissolves
   this question, since the user stops choosing, but the manual override still
   needs a defined meaning.
6. **Does the graph need to survive baking?** See 8.6. Materially changes the
   v2.2 scope.

---

## Appendix — how this maps to the original backlog

`docs/backlog.md` anticipated `CarParkGraph`, `GH_CarParkGraph` and a layout
registry in its Phase 3 blocks, and Rhino-hosted testing in blocks 20–21. That
naming is preserved here deliberately, so the v2 work reads as a continuation
of what was planned before the v1 PRD deferred it, rather than as a new idea.
The registry concept is deferred to v2.2, because nothing before the Inspector
needs it.
