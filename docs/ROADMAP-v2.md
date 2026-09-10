# Sardine — Roadmap beyond v1.0.0

**Status:** Draft for product-owner review. Not yet authoritative.
**Relationship to `docs/PRD.md`:** The v1 PRD remains the build contract for
everything shipped as v1.x. Sections 6.1 and 6.2 of that PRD forbid the graph
model and persistent layout identity. This roadmap proposes lifting those
constraints at v2.0 and nowhere earlier. Doing so requires a v2 PRD that
formally supersedes them; until that document exists and is approved, the
constraints stand.

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

This is also the strongest argument for the standards pack in v1.2: numbers
that live in a reviewable data file with clause references attached are
auditable. Numbers compiled into `SardineDefaults.cs` are not.

---

## 1. Where v1.0.0 actually leaves us

v1 generates **bay geometry**. It does not yet generate a **car park**. The
distance between those two things is the substance of v2, so it is worth being
precise about the gap. Each finding below was checked against the v1 source.

### 1.1 The layout has no connection to the outside world

There is no entry, no exit, no highway connection anywhere in the model. A
search of the v1 source for any entry or exit concept returns nothing but an
unrelated private class in the accessible allocator. The generated car park
cannot be entered.

### 1.2 The circulation is drawn as disconnected fragments

Central aisles are emitted as one rectangle per row pair, spanning the extent
of the bays that pair placed, plus half a bay step of padding. They terminate
where the bays run out, not where the perimeter ring begins. The intervening
ground is clear, so a vehicle could pass, but the drawing shows aisles floating
inside a ring rather than a connected network. There are no cross-aisles
joining one row pair to the next.

Consequence: the DWG reads as an incomplete design to anyone who receives it,
and there is no data structure that could answer "can you drive to bay 47".

### 1.3 Flow direction is computed and thrown away

`CentralBayGenerator.Result.FlowDirections` is populated with the one-way sign
of every aisle, and then no code anywhere reads it. `LayoutResult` has no field
for it and the Grasshopper component never asks. The same is true of
`SiteZones.FillDirections` and `FillAnglesDeg`: computed in the zone
decomposer, stored on the model, never consumed.

This matters more than it looks. Flow direction per aisle and bay access points
are most of what a directed circulation graph needs, and v1 already computes
them. Persisting them is cheap now and expensive to re-derive later.

### 1.4 Aisle width is decoupled from bay angle

`AisleWidth` is an independent input. Row pitch is `2 × bayDepth × sin θ +
aisleWidth`, so the angle affects the row depth but never the aisle. The whole
engineering purpose of angled parking is that a one-way aisle serving angled
bays can be substantially narrower than a two-way aisle serving perpendicular
bays.

As it stands, Sardine will silently produce a 45° layout with a 6.0 m aisle
(wasting yield) or a 90° layout with a 4.2 m aisle (unbuildable), and warn
about neither. Aisle width should be a function of bay angle and one-way or
two-way operation, taken from the standards pack, with an explicit override.

### 1.5 The accessible bay is a single envelope rectangle

The allocator draws one rectangle of `AccessibleWidth × AccessibleDepth`
(3.6 × 6.2 m). UK practice distinguishes the *marked bay* from its *access
zones* — the hatched areas to the side and rear that keep the bay usable by a
wheelchair user. Drawings show both, separately, because the hatching is what
gets enforced.

Two consequences. First, the DWG is not what a reviewer expects to see. Second,
whether adjacent bays in a bank may share an access zone changes the space
arithmetic for a bank of *n* bays, and therefore changes the yield. v1 assumes
unshared zones by construction without ever saying so.

### 1.6 Provision is one hardcoded ratio for one bay type

`AccessibleProvision.Ratio = 0.05` applied to standard capacity, minimum one.
That is the PRD's stated v1 rule and v1 implements it faithfully. But:

- the ratio is use-class dependent and routinely overridden by the LPA;
- the basis matters — a percentage of *total* capacity and a percentage of
  *standard* capacity give different answers, and nothing in the output says
  which was used;
- there is no provision engine at all for EV, family, motorcycle, cycle or
  trolley bays.

### 1.7 Electric vehicle provision is absent, and it is statutory

Approved Document S (infrastructure for charging electric vehicles) applies to
new buildings with associated parking in England. For new non-residential
development above a threshold number of spaces there are minimum charge point
and cable route requirements.

Sardine therefore cannot currently produce a Building-Regulations-compliant
scheme for a new commercial development. This is the single most commercially
significant gap and it is not a nice-to-have.

*(Thresholds, ratios, exemptions and the residential and major-renovation cases
all need checking against the current document — see section 0.)*

### 1.8 One orientation for the whole site

The central generator takes a single `Orientation` and lays one grid across the
entire central region. On an L-shaped, wedge-shaped or otherwise irregular
site, a single grid direction leaves large unusable slivers.

`SiteZones.FillDirections` already computes the top three dominant directions —
and, per 1.3, discards them. Partitioning the central zone and running the
generator per region with its own orientation is a real yield improvement, and
it forces the circulation question, because regions have to be joined.

### 1.9 Other known limitations, lower priority

- Everything is planar at Z = 0. No falls, no gradients, so no check that
  accessible bays are level or that ramps are within gradient limits.
- No kerbs or footways. Pedestrians are implicitly walking in the aisles.
- No concept of walls, columns or other obstructions, so no end-bay width
  uplift where a bay abuts one.
- Metres only, with no reference to document units.
- No automated tests of any kind.

---

## 2. The organising idea

The temptation is to read "graph network" as a feature to be added at the end.
It is better read as the **destination of a progressive enrichment of the
model**, where each step is independently useful.

v1's layout is a pile of curves in a DataTree. The graph needs a layout whose
parts know what they are, where they connect, and which way traffic runs. You
get there by making the model progressively more semantic — and each increment
pays for itself before the graph exists:

```text
curves in a tree                 v1.0    yield study
      ↓  + typed bays, standards
typed, compliant bays            v1.2    planning evidence
      ↓  + entries, connected circulation, access points
a connected layout               v1.3    a buildable drawing
      ↓  + nodes and edges over the same objects
a graph                          v2.0    analysis, routing, optimisation
```

Three tracks run across those releases:

| Track | Question it answers | Owner-facing value |
| --- | --- | --- |
| **A — Compliance** | Does this scheme satisfy the standard? | Planning submissions, client sign-off |
| **B — Realism** | Is this drawing buildable? | Fewer redesign loops, credible DWGs |
| **C — Model** | Can we reason about this layout? | The graph, and everything downstream |

Every release below draws from all three. Track C is the one that is tempting
to defer and most expensive to defer, because it changes data structures.

---

## 3. UK requirements and client drivers

What the tool must eventually be able to express. Figures deliberately omitted
where they need verification; the point of the table is the *rule shape*.

| Requirement | Typical source | Sardine today | What closes it | Release |
| --- | --- | --- | --- | --- |
| Accessible bay dimensions and access zones | BS 8300-1 | Single 3.6 × 6.2 envelope | Bay type with marked bay + separate access zone geometry; decision on zone sharing | v1.2 |
| Accessible provision ratio | BS 8300-1, LPA policy | Hardcoded 5% of standard | Provision engine: ratio, minimum, basis, rounding, per use class | v1.2 |
| Accessible bays close to the entrance | BS 8300-1 | Straight-line proximity to a user point | Route distance to a nominated entrance | v2.0 |
| Accessible route, dropped kerbs, level bays | BS 8300-1 | None | Footway geometry (v1.3); route validation and gradients (v2.0) | v1.3 / v2.0 |
| EV charge points and cable routes | Approved Doc S, LPA | None | EV bay type, active vs passive provision, cable route zones, charger positions | v1.2 / v1.3 |
| Cycle parking | LTN 1/20, LPA | None | Stand geometry and provision by use class; small separate generator | v1.3 |
| Motorcycle parking | LPA | None | Bay type in the standards pack | v1.2 |
| Bay and aisle dimensions | IStructE car park guide, LPA | Fixed inputs | Standards pack, aisle width coupled to angle and flow | v1.2 |
| Fire appliance access | Approved Doc B | None | Route width, turning and reach checks on the graph | v2.1 |
| Refuse vehicle access | BS 5906, LPA | None | Same mechanism as fire access | v2.1 |
| Swept path / vehicle tracking | Client, planning | None | Export design-vehicle envelopes; basic turning check | v2.1 |
| Gradients and levels | BS 8300-1, IStructE | Planar only | Site plane or surface input, per-bay gradient check | v1.3 |
| Drainage and SuDS | Sched. 3 FWMA, LPA | None | Permeable area and falls reporting | v1.3 (report only) |
| Biodiversity net gain, tree canopy | Environment Act 2021, LPA | Islands only as exclusions | Count island area and canopy as a reported metric | v1.3 |
| Family / parent-and-child bays | Client (retail) | None | Bay type + provision + placement near entrance | v1.3 |
| Trolley bays | Client (retail) | None | Bay type + provision per N spaces | v1.3 |
| Lighting | BS 5489-1 | None | Out of scope; export bay centroids for a lighting model | — |
| Surveillance, sightlines, Park Mark | Safer Parking Scheme | None | Sightline analysis on the graph | v2.2+ |
| Yield and efficiency | Client | Four counts | m² per space, spaces per hectare, circulation ratio | v1.1 |

---

## 4. Release plan

Effort is relative and assumes part-time solo development:
**S** ≈ days · **M** ≈ 1–2 weeks · **L** ≈ 3–6 weeks · **XL** ≈ 2 months+

### v1.1 — Make the output usable and reportable

*Goal: the same generator, but the drawing is complete and the numbers are
presentable. No new geometry types, no architecture change.*

| # | Item | Track | Effort | Why |
| --- | --- | --- | --- | --- |
| 1.1.1 | Extend central aisles to meet the perimeter ring; add cross-aisles between row pairs | B | M | Fixes 1.2 — the drawing currently reads as disconnected |
| 1.1.2 | `Entry` input: one or more points on the boundary, with in/out/both | B, C | S | Fixes 1.1; the first semantic non-bay object |
| 1.1.3 | Persist `FlowDirections` and aisle centrelines on `LayoutResult` and expose as outputs | C | S | Fixes 1.3; the cheapest possible groundwork for the graph |
| 1.1.4 | Document `Bay.Frame.Origin` as the bay access point and expose it | C | S | It is already computed and stored; it is half the graph |
| 1.1.5 | Yield metrics output: m² per space, spaces per hectare, aisle length per space | A | S | First thing a client asks after the bay count |
| 1.1.6 | Warnings that carry counts: bays removed by exclusion, by containment, by overlap | A | S | "3 bays removed" beats a silent difference |
| 1.1.7 | Bake to a per-option sublayer (`Sardine::Option_01::Perimeter`) | B | S | Bake two alternatives without collision; still no persistent registry |
| 1.1.8 | Component icons, and the `examples/` template promised by PRD §61 | B | M | Required for the v1 release anyway |

**Exit:** a generated layout can be driven into, driven around, and the DWG
looks like a car park.

### v1.2 — The standards pack

*Goal: stop hard-coding one organisation's numbers. This is the highest-leverage
release in the roadmap.*

| # | Item | Track | Effort | Why |
| --- | --- | --- | --- | --- |
| 1.2.1 | `ParkingStandard` model: bay type dimensions, aisle table, provision rules, gradients, clause references | A | L | The core of the release |
| 1.2.2 | Load standards from JSON, not compiled constants | A | M | An engineer must be able to add an LPA without Visual Studio |
| 1.2.3 | Named presets: BS 8300 + AD S baseline, IStructE, generic LPA template, client override | A | M | Adoption depends on this |
| 1.2.4 | Bay types as first-class kinds: standard, accessible, EV, family, motorcycle, parallel | A, C | L | Extends `BayKind`; touches the DataTree contract — see §6.1 |
| 1.2.5 | Provision engine: required count per type from capacity + policy, with basis and rounding stated | A | M | Replaces `AccessibleProvision`, generalised |
| 1.2.6 | Aisle width from bay angle and one-way/two-way, with override and a warning when overridden below the table | A, B | S | Fixes 1.4 |
| 1.2.7 | Accessible bay drawn as marked bay + access zone; decide zone sharing | A, B | M | Fixes 1.5 |
| 1.2.8 | End-bay width uplift where a bay abuts a wall, column or kerb | B | M | Needs an obstruction input |
| 1.2.9 | **Compliance schedule output** — required vs provided per bay type, pass/fail, clause reference | A | M | The thing that goes in the Transport Statement |

**Exit:** Sardine can be pointed at a different authority's standards without a
code change, and prints a schedule an engineer can put in a report.

### v1.3 — Layout realism

*Goal: the geometry a real scheme needs, still without a graph.*

| # | Item | Track | Effort | Why |
| --- | --- | --- | --- | --- |
| 1.3.1 | EV bay allocation, charger positions, cable route zones | A, B | L | Statutory; see 1.7 |
| 1.3.2 | Family and trolley bays placed by proximity to entrance | A, B | M | Retail client requirement |
| 1.3.3 | Parallel bays on short or awkward edges | B | M | Yield on irregular sites |
| 1.3.4 | Pedestrian footway strips and crossing points (geometry only) | B | L | Prerequisite for pedestrian routing in v2.0 |
| 1.3.5 | **Multi-region layout** — partition the central zone, orientation per region | B | XL | Fixes 1.8; largest single yield gain; forces circulation to connect |
| 1.3.6 | Landscape islands and tree positions; report canopy and permeable area | A | M | BNG and SuDS reporting |
| 1.3.7 | Site plane or surface input; per-bay gradient check against the standard | A, B | L | Fixes part of 1.9 |
| 1.3.8 | Cycle parking generator | A | M | Usually a planning condition |

**Exit:** a scheme generated by Sardine is close to what an engineer would draw
by hand, and the compliance schedule covers the statutory items.

### v2.0 — The semantic layout model and the graph

*Goal: represent the car park as a network, and answer questions about it.*
*Requires a v2 PRD superseding v1 PRD §6.1 and §6.2.*

| # | Item | Track | Effort | Why |
| --- | --- | --- | --- | --- |
| 2.0.1 | `LayoutModel`: bays, aisles, entries, destinations as objects with stable IDs | C | L | The representation change |
| 2.0.2 | `CarParkGraph` — nodes and edges per §5 below | C | L | The destination |
| 2.0.3 | Graph construction from a generated layout | C | M | Cheap if v1.1 items 3 and 4 are done |
| 2.0.4 | `GH_CarParkGraph` Goo type so the graph passes between components | C | M | Deliberate reversal of PRD §29 — see §6.3 |
| 2.0.5 | Vehicle reachability and egress validation from every entry | B | M | Catches unreachable bays, which v1 cannot detect at all |
| 2.0.6 | Dead-end and turning-head detection | B | M | Real design rule |
| 2.0.7 | One-way consistency validation | B | S | Falls out of the directed graph |
| 2.0.8 | Pedestrian walking distance from every bay to nominated destinations | A | M | BS 8300 cares about route, not crow-flies |
| 2.0.9 | Accessible placement by route distance, replacing straight-line proximity | A | M | Fixes 1.5's placement half; the compliance answer |
| 2.0.10 | `Sardine.Analyse` component: per-bay distances, worst case, mean, isochrone colouring | A | M | Makes the graph visible and sellable |
| 2.0.11 | **Automated test project** — graph algorithms, no Rhino host required | — | M | See §9 |

**Exit:** Sardine can prove every bay is reachable and state the walking
distance to the entrance for each one.

### v2.1 — Analysis and optimisation

| # | Item | Track | Effort |
| --- | --- | --- | --- |
| 2.1.1 | Fire appliance access route check against Approved Doc B | A | L |
| 2.1.2 | Refuse and servicing vehicle routes | A | M |
| 2.1.3 | Swept path export for AutoTrack, and a basic turning feasibility check | A, B | L |
| 2.1.4 | Nearest-exit and evacuation analysis | A | M |
| 2.1.5 | Option optimisation: search orientation, angle and region splits against yield, mean walk and compliance | C | XL |
| 2.1.6 | Option comparison dashboard across baked alternatives | A | L |

### v2.2 and beyond

Post-bake Inspector · region-level editing and regeneration (`Sardine.Refine`)
· multi-storey and split-level · sightline and surveillance analysis ·
IFC and Civil 3D export · machine learning on the accumulated option set.

---

## 5. The graph, in enough detail to build

Two graphs sharing one node set: a **directed vehicle graph** and an
**undirected pedestrian graph**. Sharing nodes is what lets you ask "how far
does someone walk from the bay a car can actually reach".

### Nodes

| Type | Represents | Key attributes |
| --- | --- | --- |
| `EntryNode` | Highway connection | direction (in / out / both), lanes, control (barrier, free) |
| `AisleNode` | Junction or terminus in circulation | position, is-junction |
| `BayNode` | One parking space | bay kind, dimensions, group and index, access point |
| `DestinationNode` | Building entrance, lift, stair, trolley bay, pedestrian exit | kind, weight (share of trips) |
| `CrossingNode` | Pedestrian crossing on an aisle | controlled or informal |

### Edges

| Type | Between | Key attributes |
| --- | --- | --- |
| `AisleEdge` | AisleNode ↔ AisleNode | width, flow (+1 / −1 / two-way), length, gradient, centreline |
| `BayAccessEdge` | BayNode ↔ AisleNode | manoeuvre direction; angled bays are one-way-in |
| `WalkEdge` | any ↔ any | length, surface (footway / carriageway / crossing), step-free |

### Algorithms and what each one answers

| Algorithm | Question | Failure it catches |
| --- | --- | --- |
| BFS from each `EntryNode` over the vehicle graph | Can you drive to every bay? | Unreachable bays — undetectable in v1 |
| Reverse BFS to exit-capable `EntryNode`s | Can you drive out from every bay? | One-way traps |
| Degree-1 `AisleNode` that is not an entry | Where are the dead ends? | Missing turning heads |
| Dijkstra on the pedestrian graph from each `DestinationNode` | How far does each bay walk? | Accessible bays too far from the entrance |
| Constrained selection over route distance | Which bays should be the accessible bank? | Replaces crow-flies placement |
| Total `AisleEdge` length ÷ bay count | How efficient is the circulation? | Over-provisioned aisles |

### Why v1.1 items 3 and 4 matter so much

The bay access point is **already stored** — it is `Bay.Frame.Origin`, the
point on the aisle face where the bay is entered. The aisle flow sign is
**already computed** and discarded. Between them, that is `BayNode`,
`BayAccessEdge` and the direction on `AisleEdge`.

Persisting those two things while the generator code is fresh turns v2.0's
graph construction from an archaeology exercise into a short mapping function.
It is a few days of work now against weeks later. If nothing else on the v1.1
list gets done, do these.

---

## 6. Decisions to take before v2.0 starts

These are cheap to decide now and expensive to reverse later.

### 6.1 Does the graph replace the DataTree output, or sit beside it?

**Recommendation: sit beside it.** Keep `{0;edge;bay}` / `{1;row;bay}` /
`{2;bank;bay}` as the public contract through v1.x and v2.x, and add the graph
as a new output. The PRD promises path stability for the whole v1.x line, and a
breaking change would strand every saved definition.

Note the tension with v1.2.4: adding bay types beyond the three existing
prefixes needs a rule. **Recommendation:** keep the three top-level prefixes as
*classification by placement* (perimeter / central / accessible) and carry the
bay *type* (EV, family, motorcycle) as an attribute in the model, surfaced as a
parallel `BayTypes` output rather than as new path prefixes.

### 6.2 Are bay identities stable across regeneration?

Needed for the Inspector, option comparison and post-bake tracking. The current
`{kind; group; index}` address churns whenever an upstream bay is removed.

**Recommendation:** derive the ID from a spatial hash of the bay centroid
rounded to tolerance, not from the ordinal index. Decide before v2.0.1, because
it determines whether the Inspector is possible at all.

### 6.3 Is a custom Grasshopper Goo type permitted?

PRD §29 says avoid custom Goo types. That was right for v1. Passing a graph
between components effectively requires `GH_CarParkGraph`, which the original
backlog also anticipated.

**Recommendation:** permit exactly one Goo type, wrapping the graph, and record
it in the v2 PRD as a deliberate reversal with its reason.

### 6.4 Where do the standards live?

**Recommendation:** JSON resource files, loaded at runtime, with a schema
version and a clause reference beside every value. Compiled constants make the
numbers unauditable and mean a new LPA needs a release. This is the difference
between a WSP tool and a Benj tool.

### 6.5 Units

v1 is metres-only by decision. Before the standards pack lands, decide whether
v2 reads document units or continues to require metres and warns loudly when
the document is not in metres. **Recommendation:** warn in v1.1, convert in
v1.2, because a standards file quoting millimetres will otherwise be a
recurring source of error.

---

## 7. Critical path

If capacity allows only three things, do these, in this order:

1. **v1.1 items 1.1.2, 1.1.3 and 1.1.4** — entries, flow directions, access
   points. Days of work. Turns the graph from a rewrite into a mapping.
2. **v1.2 — the standards pack.** Without it Sardine is a tool that generates
   car parks to one set of assumptions. With it, it is a tool any UK project can
   use, which is the difference between an internal experiment and something
   worth maintaining.
3. **v2.0 items 2.0.2, 2.0.5 and 2.0.8** — the graph, reachability and walking
   distance. This is the capability nobody else in the team has, and the answer
   to the question every reviewer asks: is this layout any good?

Everything else is valuable, and everything else can wait behind those.

---

## 8. Explicitly not doing

Recording these prevents the same conversation recurring.

- **Not** rewriting the v1 generators because a graph exists. The generators
  produce the geometry; the graph describes it. They stay.
- **Not** building an optimiser before the graph. Optimisation needs a fitness
  function, and the fitness function is walking distance plus yield plus
  compliance — none of which exist until v2.0.
- **Not** modelling lighting, drainage design, structures or ventilation.
  Sardine exports what those models need and stops there.
- **Not** multi-storey before surface parking is solved. Ramps, headroom and
  structural grids are a different product.
- **Not** machine learning until there is a corpus of real approved schemes to
  learn from. There is currently none.
- **Not** public distribution or Yak packaging. Still internal.

---

## 9. Process and validation changes

v1's acceptance gate is manual regression in Rhino 8, because Rhino-hosted
automated testing is awkward and was deferred (PRD §42, §32).

**The graph changes this argument.** Reachability, Dijkstra, dead-end detection
and provision arithmetic are operations on adjacency lists and plain numbers.
They need no Rhino document, no licence and no UI. `Point3d` and `Vector3d` are
value types whose arithmetic runs headless.

So:

- The graph layer should be **designed to be constructible from plain
  coordinates and adjacency**, not only from RhinoCommon curves, precisely so
  it is testable without a host.
- The `Sardine.Tests` project the PRD deferred should arrive **with** v2.0, not
  after it, covering the graph algorithms and the provision engine.
- Geometry generation stays on the manual Rhino gate. That split is honest:
  test what can be tested cheaply, inspect the rest.

The manual fixture catalogue (PRD §35, fixtures A–I) remains the baseline and
should be extended, not replaced, with fixtures for connectivity, provision and
walking distance.

---

## 10. Open questions for the product owner

1. **Which standards pack ships first?** BS 8300 + Approved Doc S as a generic
   baseline, or a specific client's or authority's standards that a live
   project needs? The live project probably wins, but it shapes v1.2.
2. **Is there a live scheme to validate against?** A real WSP car park with a
   known approved bay count would be worth more than any number of synthetic
   fixtures, and would settle the predicted-versus-actual questions left open in
   `v1/BUILD_PLAN.md`.
3. **Who is the user in 18 months?** Benj alone, the WSP UK parking team, or
   anyone in WSP with Rhino? The answer decides how much of §6.4 matters and
   whether a UI beyond Grasshopper is ever needed.
4. **Orientation semantics**, still open from v1: does `Orientation = 0` mean
   World X, or align to the dominant site edge? v1.3.5 (multi-region) forces
   this to be answered properly.
5. **Does the graph need to survive baking?** If the Inspector is genuinely
   wanted, the graph has to be reconstructible from baked Rhino geometry, which
   is a materially harder problem than building it from a live solution. Worth
   deciding before v2.0.1 rather than during.

---

## Appendix — how this maps to the original backlog

`docs/backlog.md` anticipated `CarParkGraph`, `GH_CarParkGraph` and a layout
registry in its Phase 3 blocks, and Rhino-hosted testing in blocks 20–21. That
naming is preserved here deliberately, so the v2 work reads as a continuation of
what was planned before the v1 PRD deferred it, rather than as a new idea. The
registry concept is deferred further, to v2.2, because nothing before the
Inspector needs it.
