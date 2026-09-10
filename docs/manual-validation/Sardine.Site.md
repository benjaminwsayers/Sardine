# Sardine.Site manual regression

**Issue:** #13

**Reference:** `legacy/01_SiteRationaliser.cs`

**Fixture data:** `docs/manual-validation/Sardine.Site-fixtures.csv`

**Status:** Contributor run pending; product-owner gate pending

This is the Rhino 8 / Grasshopper acceptance record for `Sardine.Site`. It
captures the existing proof-of-concept behaviour without changing its 5 degree
merge angle, 1.0 m minimum edge length, 200-division conversion strategy or
RhinoCommon tolerances. All coordinates and lengths are metres.

## Public contract

| Input | Value for baseline |
| --- | --- |
| `Boundary` | Fixture curve |
| `MergeAngle` | `5.0` degrees |
| `MinEdgeLength` | `1.0` m |

Both optional thresholds accept `0`; zero disables the corresponding merge
rule. Negative and non-finite values are invalid.

The outputs, in order, are `RawBoundary`, `WorkingBoundary`, `Edges`,
`EdgeDirections`, `EdgeLengths`, `EdgeCount` and `Warnings`. `RawBoundary`
must remain a duplicate of the supplied curve. `WorkingBoundary` and all edge
outputs must describe the rationalised curve. Unless a fixture says otherwise,
compare coordinates and lengths to `0.000001` m and unit-vector components to
`0.000001`.

## Fixture construction

Set the Rhino document units to metres. Run each command on one line in the
Rhino command bar. The repeated first point closes each closed polyline while
preserving its declared starting vertex and edge order.

| ID | Rhino command |
| --- | --- |
| A | `-_Polyline 0,0,0 40,0,0 40,20,0 0,20,0 0,0,0 Enter` |
| B | `-_Polyline 0,0,0 12,0.15,0 24,0,0 31,7,0 30.8,18,0 14,24,0 0.2,17.8,0 0,0,0 Enter` |
| C | `-_Polyline 0,0,0 10,0,0 10,10,0 Enter` |
| D | `-_Polyline 0,0,0 10,0,0 10,10,1 0,10,0 0,0,0 Enter` |
| E | `-_Circle 0,0,0 50 Enter` |
| F | `-_Polyline 0,0,0 20,0,0 20,0.5,0 20,20,0 0,20,0 0,0,0 Enter` |
| G | `-_Polyline 0,0,0 10,0,0 20,0.5,0 20,20,0 0,20,0 0,0,0 Enter` |
| H | `-_Polyline 0,0,0 0.2,0,0 0.2,0.2,0 0,0.2,0 0,0,0 Enter` |
| I | `-_Polyline 0,0,0 10,10,0 0,10,0 10,0,0 0,0,0 Enter` |
| J | `-_Circle 0,0,0 10 Enter` |

Null and invalid-parameter checks reuse no geometry or fixture A respectively.

## Expected baseline

### Successful fixtures

| ID | Raw / working edges | Expected warning |
| --- | ---: | --- |
| A clean rectangle | 4 / 4 | None |
| B survey boundary | 7 / 6 | None |
| E non-polyline circle | analytic curve / 40 | Conversion warning |
| F short edge | 5 / 4 | None |
| G near-parallel edges | 5 / 4 | None |

Fixture E must also report:

```text
Input was not a polyline — approximated using DivideByCount(200). For best results supply an actual polyline from your DXF/DWG.
```

Expected ordered edge values:

| Fixture | Index | From | To | Unit direction | Length |
| --- | ---: | --- | --- | --- | ---: |
| A | 0 | `(0,0,0)` | `(40,0,0)` | `(1,0,0)` | 40 |
| A | 1 | `(40,0,0)` | `(40,20,0)` | `(0,1,0)` | 20 |
| A | 2 | `(40,20,0)` | `(0,20,0)` | `(-1,0,0)` | 40 |
| A | 3 | `(0,20,0)` | `(0,0,0)` | `(0,-1,0)` | 20 |
| B | 0 | `(0,0,0)` | `(24,0,0)` | `(1,0,0)` | 24 |
| B | 1 | `(24,0,0)` | `(31,7,0)` | `(0.707107,0.707107,0)` | 9.899495 |
| B | 2 | `(31,7,0)` | `(30.8,18,0)` | `(-0.018179,0.999835,0)` | 11.001818 |
| B | 3 | `(30.8,18,0)` | `(14,24,0)` | `(-0.941742,0.336336,0)` | 17.839282 |
| B | 4 | `(14,24,0)` | `(0.2,17.8,0)` | `(-0.912169,-0.409815,0)` | 15.128781 |
| B | 5 | `(0.2,17.8,0)` | `(0,0,0)` | `(-0.011235,-0.999937,0)` | 17.801124 |
| F/G | 0 | `(0,0,0)` | `(20,0.5,0)` | `(0.999688,0.024992,0)` | 20.006249 |
| F/G | 1 | `(20,0.5,0)` | `(20,20,0)` | `(0,1,0)` | 19.5 |
| F/G | 2 | `(20,20,0)` | `(0,20,0)` | `(-1,0,0)` | 20 |
| F/G | 3 | `(0,20,0)` | `(0,0,0)` | `(0,-1,0)` | 20 |

Fixture A has working area `800 m2` and perimeter `120 m`. Fixture B has
working area `621 m2` and perimeter `95.670500 m`. Fixtures F and G have
working area `395 m2` and perimeter `79.506249 m`.

Fixture E starts at `(50,0,0)` and produces 40 ordered 9-degree chord edges.
Every edge length is `7.845910 m`; edge 0 ends at
`(49.384417,7.821723,0)` with unit direction
`(-0.078459,0.996917,0)`. `RawBoundary` remains the analytic circle rather
than the 40-edge working polyline.

### Controlled failures

| Scenario | Expected result |
| --- | --- |
| Boundary disconnected/null | Required `Boundary` error; no outputs. Core diagnostic is `No boundary provided.` |
| C open boundary | Error `Boundary must be a closed curve.`; no outputs |
| D non-planar boundary | Error `Boundary must be planar.`; no outputs |
| `MergeAngle` set to `-1`, `NaN` or infinity | Error `MergeAngle and MinEdgeLength must be zero or greater.`; no outputs |
| `MinEdgeLength` set to `-1`, `NaN` or infinity | Same invalid-parameter error; no outputs |
| H collapsed boundary | Error `Boundary rationalisation produced fewer than three valid edges.`; no outputs |
| I zero-area bow-tie | Error `Boundary rationalisation produced degenerate geometry.`; no outputs |
| J small converted circle | Error `Boundary rationalisation produced fewer than three valid edges.`; no outputs |

Fixture J records a reference limitation rather than redefining it: with the
unchanged 200-division conversion and 1.0 m minimum edge length, every sampled
segment of a 10 m-radius circle is short and the working boundary collapses.
Fixture E proves the supported non-polyline conversion path without changing
that behaviour.

## Exact Rhino 8 / Grasshopper steps

1. In Visual Studio 2022, open `src\Sardine\Sardine.sln`, select `Release` and
   `Any CPU`, then build the solution.
2. Close Rhino. Copy `Sardine.GH.gha` and `Sardine.Core.dll` from
   `src\Sardine\Sardine.GH\bin\Release\net48` into an approved Grasshopper
   libraries folder. In Windows file properties, unblock both files if the
   **Unblock** checkbox is present.
3. Start Rhino 8 for Windows, open a new **Large Objects - Metres** document,
   run `Grasshopper`, create a blank definition and place `Sardine.Site`.
4. Connect Panels to `EdgeDirections`, `EdgeLengths`, `EdgeCount` and
   `Warnings`. Connect Curve parameters to `RawBoundary` and
   `WorkingBoundary`, and give them different preview colours.
5. Connect an integer slider and `List Item` to `Edges` so one indexed edge can
   be previewed at a time. Set the component inputs to `MergeAngle = 5.0` and
   `MinEdgeLength = 1.0`.
6. Construct fixtures A through J with the commands above. For each fixture,
   right-click the `Boundary` input, choose **Set one Curve**, select only that
   fixture and compare all outputs and runtime messages with the expected
   baseline.
7. For successful fixtures, change the `List Item` index from zero through
   `EdgeCount - 1`. Confirm the previewed edges, direction rows and length rows
   use the stated order. Press `F5` three times and confirm the order and values
   remain unchanged.
8. For A, B, E, F and G, confirm the differently coloured raw curve remains the
   supplied geometry while the working curve matches the rationalised edges.
9. Disconnect `Boundary` and confirm the required-input failure. Reconnect A,
   then set both optional inputs to `0`; confirm A succeeds with four edges and
   no warning. Connect G and confirm it retains five edges because both merge
   rules are disabled. Test `-1` for each input and confirm the controlled
   error. Grasshopper Panels may not create non-finite numbers, so `NaN` and
   infinity remain Core regression cases.
10. Save screenshots or a screen recording showing the component, messages,
    counts and representative indexed edges. Record the result below. Do not
    approve or merge if any required observation differs.

## Manual owner gate

| Field | Result |
| --- | --- |
| Commit tested | Pending |
| Contributor | Pending |
| Rhino 8 version | Pending |
| Test date | Pending |
| Observed result / diagnostics | Pending |
| Product owner | Pending |
| Owner result | **Pending - not yet passed** |
