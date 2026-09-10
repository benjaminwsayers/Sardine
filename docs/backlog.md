# Sardine — Development Backlog
## Focused 1-Hour Learning Blocks

> **Historical, non-authoritative document.** This backlog records an earlier
> learning plan and is retained for context only. `docs/PRD.md` is the
> authoritative v1 delivery contract wherever the documents conflict.
>
> In particular, do not follow this backlog's .NET 7 or multi-targeting examples:
> v1 remains Rhino 8 on Windows and `net48`. `CarParkGraph`,
> `GH_CarParkGraph`, layout registries and the future architecture referenced
> below are explicitly out of scope. Its automated Rhino testing, GitHub CI,
> branch-protection and automated-release steps are deferred engineering intent,
> not prerequisites for the current proof-of-concept roadmap. Use the PRD's
> manual Rhino 8 / Grasshopper feature gate and owner-approval requirements.

**Philosophy:** Each block has one deliverable and one concept to understand properly.
Read the resource before writing code. If a block rolls into the next day, fine —
don't move on until you understand why the code works, not just that it works.

---

## Prerequisites Checklist
Complete before Block 01.

- [ ] Visual Studio 2022 (Community or Professional) with .NET desktop workload
- [ ] Rhino 8 installed and licensed
- [ ] Git installed: https://git-scm.com/download/win
- [ ] Git LFS installed: run `git lfs install` after Git
- [ ] GitHub account created
- [ ] GitHub CLI installed (optional but useful): https://cli.github.com
- [ ] .NET Framework 4.8 targeting pack installed (bundled with Visual Studio 2022; verify via *Visual Studio Installer → Individual components → .NET Framework 4.8 targeting pack*)
  > **Historical note:** an earlier version of this checklist referenced the .NET 7 SDK. That no longer applies — Sardine v1 targets `net48` (Rhino 8 / Grasshopper on Windows). The .NET 7 SDK is **not** required.

Verify these from PowerShell before starting:
```powershell
git --version          # should show 2.x
git lfs --version      # should show 3.x
```

---

## PHASE 1 — FOUNDATION
*Goal: A clean, versioned, building repo before a single line of Sardine code is written.*

---

### Block 01 — GitHub Repository and Folder Structure
**Time:** 1 hour
**Deliverable:** Private GitHub repo cloned locally with correct folder structure

**Step 1 — Create the GitHub repo**

Go to github.com → New repository:
- Name: `WSP-UK-sardine`
- Visibility: Private
- Tick: Add a README
- Do NOT add .gitignore or licence yet — you'll do that manually
- Click Create repository

**Step 2 — Clone to your machine**

```powershell
# Navigate to where you keep dev work
cd C:\Users\UKBWS001\dev

# Clone it
git clone https://github.com/YOUR_USERNAME/WSP-UK-sardine.git

# Move into the repo
cd WSP-UK-sardine
```

**Step 3 — Create the folder structure**

```powershell
# From repo root: C:\Users\UKBWS001\dev\WSP-UK-sardine\

mkdir src
mkdir docs
mkdir .github
mkdir .github\workflows
```

Your structure should now look like:
```
WSP-UK-sardine/
├── README.md            ← created by GitHub
├── src/                 ← all code goes here
├── docs/                ← PRD, backlog, architecture notes
└── .github/
    └── workflows/       ← CI YAML files go here
```

**Step 4 — Create .gitignore at repo root**

Create a file called `.gitignore` at `WSP-UK-sardine/` (not inside src).
Paste this content:

```gitignore
# Build outputs — never commit these
bin/
obj/

# Visual Studio
.vs/
*.user

# Compiled plugin outputs — built by CI, not committed
*.GHa
*.rhp
*.yak

# Yak manifest — auto-generated
manifest.yml

# Rhino temp files
*.rhl
~*.3dm
AutoSave/

# OS
.DS_Store
Thumbs.db
Thumbs.db:encryptable
```

**Step 5 — Create .gitattributes at repo root**

Create `.gitattributes` at `WSP-UK-sardine/`:

```gitattributes
# Grasshopper canvas files are binary — store with LFS
*.gh  filter=lfs diff=lfs merge=lfs -text
*.ghx filter=lfs diff=lfs merge=lfs -text
*.3dm filter=lfs diff=lfs merge=lfs -text

# Text files — consistent line endings
*.cs   text eol=crlf
*.csproj text eol=crlf
*.sln  text eol=crlf
*.json text eol=lf
*.yml  text eol=lf
*.md   text eol=lf
```

**Step 6 — First commit**

```powershell
git add .
git commit -m "chore: initial repo structure, gitignore, gitattributes"
git push
```

**Concept to learn: Why .gitignore matters**
Every `bin/` and `obj/` folder contains compiled binaries that get rebuilt on every
build. If you commit them, your repo fills with hundreds of binary files that change
constantly — useless noise. The `.gitignore` tells Git to pretend those folders
don't exist.

**You know you're done when:**
`git status` in the repo root shows nothing to commit after your push.

---

### Block 02 — Install McNeel Templates and Create the Solution
**Time:** 1 hour
**Deliverable:** Sardine.sln created in src/ with the Grasshopper plugin template project

**Step 1 — Install Rhino/Grasshopper Visual Studio templates**

Open PowerShell (not Developer PowerShell — just regular PowerShell) and run:

```powershell
dotnet new install Rhino.Templates
```

This downloads and registers the McNeel project templates so they appear in Visual
Studio's "Create a new project" dialog.

Verify it worked:
```powershell
dotnet new list | findstr -i grasshopper
```
You should see entries like `Grasshopper Component` or `Grasshopper Assembly`.

**Step 2 — Open Visual Studio, create solution with GH project**

1. Open Visual Studio 2022
2. Click "Create a new project"
3. In the search box type: `Grasshopper`
4. Select **Grasshopper Assembly** (from McNeel)
5. Click Next
6. Set these values:
   - Project name: `Sardine.GH`
   - Location: `C:\Users\UKBWS001\dev\WSP-UK-sardine\src`
   - Solution name: `Sardine`
   - Tick: **Place solution and project in the same directory** → **UNTICK THIS**
     You want: solution in `src/`, project in `src/Sardine.GH/`
7. Click Next → choose Rhino 8 as target → Create

After creation your structure should be:
```
src/
├── Sardine.sln           ← solution file
└── Sardine.GH/
    ├── Sardine.GH.csproj
    ├── SardineGHInfo.cs  ← plugin info class
    └── Components/
        └── SampleComponent.cs
```

**Step 3 — Understand what the template gave you**

Open `Sardine.GH.csproj`. It should look roughly like:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net48;net7.0</TargetFrameworks>
    ...
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Grasshopper" Version="8.*" ... />
    <PackageReference Include="RhinoCommon" Version="8.*" ... />
  </ItemGroup>
</Project>
```

This is SDK-style — modern format. NuGet packages for Grasshopper and RhinoCommon
instead of local DLL references. This is correct.

**Step 4 — Build and verify from terminal**

```powershell
cd C:\Users\UKBWS001\dev\WSP-UK-sardine\src
msbuild Sardine.sln
```

Should produce 0 errors. The sample component from the template builds.

**Concept to learn: SDK-style vs old-style .csproj**

Old-style (what Core.csproj was before):
- Long XML files with explicit file lists
- `AssemblyInfo.cs` required for version info
- `Directory.Build.props` doesn't auto-flow

SDK-style (what the GH template creates):
- Short, clean XML
- Automatically includes all `.cs` files in the folder
- Version flows from `Directory.Build.props` automatically
- Used for all new projects going forward

**You know you're done when:**
`msbuild Sardine.sln` exits with 0 errors and you can see `Sardine.GH.GHa` in
`src/Sardine.GH/bin/`.

---

### Block 03 — Add Sardine.Core Project
**Time:** 1 hour
**Deliverable:** Sardine.Core added to solution, referenced by GH, builds clean

**Step 1 — Add Core project in Visual Studio**

1. In Solution Explorer, right-click the solution `Sardine`
2. Add → New Project
3. Search: `Class Library`
4. Select **Class Library** (the one that says `.NET` — NOT `.NET Framework`)
5. Click Next
6. Project name: `Sardine.Core`
7. Location: `C:\Users\UKBWS001\dev\WSP-UK-sardine\src`
8. Click Next
9. Framework: **.NET Framework 4.8**
10. Click Create

**Step 2 — Convert Core to SDK-style format**

The template creates an old-style project. You need to convert it.
Close Visual Studio. Open `src/Sardine.Core/Sardine.Core.csproj` in a text editor.

Replace the entire file contents with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <Nullable>enable</Nullable>
    <GenerateAssemblyInfo>true</GenerateAssemblyInfo>
  </PropertyGroup>

  <ItemGroup>
    <!--
      RhinoCommon for geometry types (Point3d, Curve, Vector3d etc).
      ExcludeAssets=runtime means the DLL is not copied to output —
      Rhino provides it at runtime. PrivateAssets=all means it's
      not passed to projects that reference Sardine.Core.
    -->
    <PackageReference Include="RhinoCommon"
                      Version="8.*"
                      ExcludeAssets="runtime"
                      PrivateAssets="all" />
  </ItemGroup>

</Project>
```

Delete `Properties/AssemblyInfo.cs` — it's no longer needed. SDK-style generates
assembly info automatically from `Directory.Build.props`.

**Step 3 — Add project reference from GH to Core**

1. Reopen Visual Studio
2. In Solution Explorer, right-click `Sardine.GH` → Add → Project Reference
3. Tick `Sardine.Core` → OK

**Step 4 — Create the folder structure in Core**

In Solution Explorer, right-click `Sardine.Core` and add these folders:
`Algorithms`, `Compliance`, `Graph`, `IO`, `Models`, `Registry`, `Naming`,
`Utilities`, `Visualisation`

Delete the auto-created `Class1.cs`.

**Step 5 — Build**

```powershell
msbuild /t:Rebuild Sardine.sln
```

0 errors. Commit:
```powershell
git add .
git commit -m "feat: add Sardine.Core SDK-style project, reference from GH"
git push
```

**Concept to learn: Project references vs DLL references**

Old approach (what your previous CORE used):
```xml
<Reference Include="RhinoCommon">
  <HintPath>C:\Users\YOU\...\RhinoCommon.dll</HintPath>
</Reference>
```
This path is machine-specific. Breaks on any other machine.

New approach (what Core.csproj now uses):
```xml
<PackageReference Include="RhinoCommon" Version="8.*" />
```
NuGet downloads the correct version automatically. Works on any machine.
This is why the GH template uses NuGet and why you should too.

**You know you're done when:**
Solution Explorer shows both projects. Build is green. No local DLL references anywhere.

---

### Block 04 — Add Sardine.Tests.Core Project
**Time:** 1 hour
**Deliverable:** Test project added, one passing test, runs from terminal

**Step 1 — Add test project in Visual Studio**

1. Right-click solution → Add → New Project
2. Search: `xUnit`
3. Select **xUnit Test Project**
4. Project name: `Sardine.Tests.Core`
5. Location: `C:\Users\UKBWS001\dev\WSP-UK-sardine\src`
6. Framework: `.NET Framework 4.8`
7. Click Create

**Step 2 — Open the .csproj and verify it's SDK-style**

It should look like:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
    <PackageReference Include="xunit" Version="2.7.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.7" />
  </ItemGroup>
</Project>
```

If it looks different (old-style), replace it with the above.

**Step 3 — Add FluentAssertions and reference to Core**

Right-click `Sardine.Tests.Core` → Manage NuGet Packages → Browse:
- Install `FluentAssertions` (version 6.x)

Then add project reference: right-click → Add → Project Reference → tick `Sardine.Core`

**Step 4 — Write your first test**

Delete `UnitTest1.cs`. Create `Tests/SmokeTest.cs`:

```csharp
using Xunit;
using FluentAssertions;

namespace Sardine.Tests.Core
{
    public class SmokeTests
    {
        [Fact]
        public void TrueIsTrue()
        {
            // This tests that the test project is wired up correctly.
            // Not a useful test — replace in Block 06.
            true.Should().BeTrue();
        }
    }
}
```

**Step 5 — Run from terminal**

```powershell
cd C:\Users\UKBWS001\dev\WSP-UK-sardine\src
dotnet test Sardine.Tests.Core\Sardine.Tests.Core.csproj
```

You should see:
```
Passed!  - Failed: 0, Passed: 1, Skipped: 0
```

Commit:
```powershell
git add .
git commit -m "feat: add Sardine.Tests.Core, first smoke test passing"
git push
```

**Concept to learn: `dotnet test` vs running tests in VS**

`dotnet test` runs from the terminal — no Visual Studio required. This is what CI
will run. Always verify your tests pass from the terminal before pushing.
VS Test Explorer is more convenient while developing, but the terminal is the
source of truth.

**You know you're done when:**
`dotnet test` from the terminal shows Passed: 1, Failed: 0.

---

### Block 05 — Directory.Build.props and Version Flow
**Time:** 1 hour
**Deliverable:** Version, company, product flow from one file into all three projects

**Step 1 — Create Directory.Build.props at repo root**

Create `Directory.Build.props` at `C:\Users\UKBWS001\dev\WSP-UK-sardine\`
(not inside src — at the root alongside .gitignore):

```xml
<Project>
  <PropertyGroup>
    <Version>1.0.0</Version>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <FileVersion>1.0.0.0</FileVersion>
    <Authors>Benj Sayers</Authors>
    <Company>WSP UK</Company>
    <Copyright>© 2026 WSP UK</Copyright>
    <Product>Sardine</Product>
    <RepositoryUrl>https://github.com/YOUR_USERNAME/WSP-UK-sardine</RepositoryUrl>
  </PropertyGroup>
</Project>
```

**Step 2 — Understand why this works**

MSBuild walks up the directory tree from each `.csproj` looking for
`Directory.Build.props`. Starting from `src/Sardine.Core/`:

```
src/Sardine.Core/       → not found
src/                    → not found
WSP-UK-sardine/         → FOUND — imported automatically
```

All three SDK-style projects (`Sardine.GH`, `Sardine.Core`, `Sardine.Tests.Core`)
inherit these properties with zero additional configuration.

**Step 3 — Rebuild and verify**

```powershell
cd src
msbuild /t:Rebuild Sardine.sln
```

Then check the version flowed through:
```powershell
(Get-Item "Sardine.GH\bin\Debug\net7.0\Sardine.GH.GHa").VersionInfo `
  | Select FileVersion, CompanyName, ProductName
```

Expected output:
```
FileVersion  CompanyName  ProductName
-----------  -----------  -----------
1.0.0.0      WSP UK       Sardine
```

**Step 4 — Commit**

```powershell
cd ..   # back to repo root
git add Directory.Build.props
git commit -m "chore: add Directory.Build.props, version 1.0.0"
git push
```

**Concept to learn: Single source of truth**

Before this, version numbers lived in three places: `AssemblyInfo.cs` in Core,
auto-generated info in GH, and nowhere in Tests. They'd drift out of sync.
Now there is one place — `Directory.Build.props`. When you ship v1.1.0 you change
one number and rebuild. Everything updates.

**You know you're done when:**
The PowerShell version check shows `1.0.0.0` and `WSP UK` without any manual
AssemblyInfo.cs in any project.

---

### Block 06 — Move Existing Code Into the New Solution
**Time:** 1–2 hours (allow extra time — this is the most fiddly block)
**Deliverable:** All existing algorithm files live in Sardine.Core with correct namespaces,
solution builds clean

**Step 1 — Copy files from old solution**

From your old repo, copy each folder into the new Core project:
- `Compliance/` → `src/Sardine.Core/Compliance/`
- `Geometry/` → `src/Sardine.Core/Algorithms/`
- `IO/` → `src/Sardine.Core/IO/`
- `Models/` → `src/Sardine.Core/Models/`
- `Naming/` → `src/Sardine.Core/Naming/`
- `Utilities/` → `src/Sardine.Core/Utilities/`
- `Visualisation/` → `src/Sardine.Core/Visualisation/`

SDK-style projects automatically include all `.cs` files in their folder — no need
to add files to the `.csproj` manually.

**Step 2 — Fix namespaces**

Each file will have `namespace CORE` or similar at the top. Do a find-and-replace
across all files in Core:

In Visual Studio: Edit → Find and Replace → Replace in Files
- Find: `namespace CORE`
- Replace: `namespace Sardine.Core`
- Look in: `src\Sardine.Core`

Also check for `using CORE;` references and update to `using Sardine.Core;`.

**Step 3 — Copy GH component files**

From old `GH/Components/`, copy all `C0N_*.cs` component files to
`src/Sardine.GH/Components/`.

Update namespaces from `namespace GH` to `namespace Sardine.GH`.

Also copy `GHInfo.cs` but rename it `SardineInfo.cs` and update class name and
GH category string:

```csharp
public override string AssemblyName => "Sardine";
public override string AssemblyDescription => "Car park layout generator for Rhino";
// GH category name — this is what appears in the Sardine tab
public override string AssemblyVersion => 
    Assembly.GetExecutingAssembly().GetName().Version.ToString();
```

**Step 4 — Fix the RhinoDoc.ActiveDoc issue in Core**

Search all files in `Sardine.Core` for `RhinoDoc.ActiveDoc`. Any occurrence is
an architecture violation — Core should not access the document.

For each occurrence, change the method signature to accept a `RhinoDoc` parameter:

```csharp
// BEFORE (wrong — Core accessing document directly)
public static Curve GenerateAisle(...)
{
    var doc = RhinoDoc.ActiveDoc;  // ← violation
    ...
}

// AFTER (correct — doc passed in from GH wrapper)
public static Curve GenerateAisle(RhinoDoc doc, ...)
{
    ...
}
```

The GH wrapper then passes `RhinoDoc.ActiveDoc` when calling Core methods.
Mark each fixed location with a comment: `// Fixed: BUG-03`

**Step 5 — Build and fix errors**

```powershell
msbuild Sardine.sln 2>&1 | findstr "error"
```

Work through errors one at a time. Most will be namespace or missing using
statements. Don't fix more than one error category at a time.

**Step 6 — Commit when green**

```powershell
git add .
git commit -m "feat: migrate algorithms to Sardine.Core, fix BUG-03 RhinoDoc leak"
git push
```

**Concept to learn: Namespace conventions**

`namespace Sardine.Core.Algorithms` (nested) vs `namespace Sardine.Core` (flat).
For this project, flat is fine — you don't have enough files to need nesting.
Pick one and be consistent. The project name (`Sardine.Core`) should match the
root namespace.

**You know you're done when:**
`msbuild Sardine.sln` exits 0 errors. No `RhinoDoc.ActiveDoc` in any Core file.

---

### Block 07 — GitHub Branch Protection and CI Workflow
**Time:** 1 hour
**Deliverable:** Branch protection on main, CI runs Tier 1 tests on every PR

**Step 1 — Create your first feature branch**

From now on you never commit directly to main. Everything goes through a branch:

```powershell
git checkout -b feature/ci-setup
```

**Step 2 — Create ci.yml**

Create `.github/workflows/ci.yml`:

```yaml
name: CI

on:
  pull_request:
    branches: [ main ]

jobs:
  core-tests:
    name: Core Tests
    runs-on: windows-latest   # net48 requires Windows

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '7.0.x'

      - name: Restore
        run: dotnet restore src/Sardine.Tests.Core/Sardine.Tests.Core.csproj

      - name: Test
        run: |
          dotnet test src/Sardine.Tests.Core/Sardine.Tests.Core.csproj `
            --no-restore `
            --logger "trx;LogFileName=test-results.trx" `
            --results-directory TestResults

      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: TestResults/

  build-check:
    name: Build Release
    runs-on: windows-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '7.0.x'

      - name: Build Release
        run: msbuild src/Sardine.sln /p:Configuration=Release

      - name: Verify GHa exists
        run: |
          $gha = Get-ChildItem -Recurse -Filter "*.GHa" `
                   -Path "src" | Select-Object -First 1
          if (-not $gha) {
            Write-Error "No .GHa file found in build output"
            exit 1
          }
          Write-Host "Found: $($gha.FullName)"
        shell: pwsh
```

**Step 3 — Create release.yml**

Create `.github/workflows/release.yml`:

```yaml
name: Release

on:
  push:
    tags:
      - 'v*.*.*'

jobs:
  release:
    name: Build and publish
    runs-on: windows-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '7.0.x'

      - name: Extract version
        id: version
        run: |
          $tag = "${{ github.ref_name }}"
          $ver = $tag.TrimStart('v')
          echo "VERSION=$ver" >> $env:GITHUB_OUTPUT
        shell: pwsh

      - name: Run Core Tests
        run: dotnet test src/Sardine.Tests.Core/Sardine.Tests.Core.csproj

      - name: Build Release
        run: |
          msbuild src/Sardine.sln /p:Configuration=Release `
            /p:Version=${{ steps.version.outputs.VERSION }}

      - name: Collect artifacts
        run: |
          New-Item -ItemType Directory dist -Force
          Get-ChildItem -Recurse -Filter "*.GHa" -Path "src" |
            Copy-Item -Destination dist/
        shell: pwsh

      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          name: Sardine ${{ github.ref_name }}
          files: dist/*.GHa
          prerelease: ${{ contains(github.ref_name, '-') }}
```

**Step 4 — Push and open a PR**

```powershell
git add .github/
git commit -m "ci: add CI and release workflows"
git push -u origin feature/ci-setup
```

Go to GitHub → you'll see a banner "Compare & pull request" → click it →
Create pull request → watch the Actions tab as CI runs.

**Step 5 — Enable branch protection**

GitHub → Settings → Branches → Add rule:
- Branch name pattern: `main`
- ✅ Require a pull request before merging
- ✅ Require status checks to pass: `core-tests`, `build-check`
- ✅ Do not allow bypassing the above settings

Merge the PR once CI is green.

**Concept to learn: Why branch protection matters solo**

You're one developer. You could just push to main. Branch protection forces
the habit of small PRs. When something breaks, `git log --oneline` on main
shows clean, atomic changes. You always know which change caused which problem.

**You know you're done when:**
A PR with a broken test shows a red X. A PR with passing tests shows green ✅.
Merging requires CI to pass.

---

## PHASE 2 — TESTING INFRASTRUCTURE
*Now that the solution is set up correctly, write real tests.*

---

### Block 08 — First Real Core Tests
**Time:** 1 hour
**Deliverable:** C01 tests written and passing — rectangle and real survey boundary

**Create a feature branch:**
```powershell
git checkout main && git pull
git checkout -b feature/core-tests
```

**Create `src/Sardine.Tests.Core/Fixtures/` folder**

Serialise a real survey boundary to JSON. In a temporary C# script or
the Grasshopper script component, get your boundary vertices and:

```csharp
var vertices = boundary.ToNurbsCurve().Points
    .Select(p => new { X = p.Location.X, Y = p.Location.Y, Z = p.Location.Z })
    .ToList();
var json = System.Text.Json.JsonSerializer.Serialize(vertices);
File.WriteAllText("survey_01.json", json);
```

Save as `src/Sardine.Tests.Core/Fixtures/survey_boundary_01.json`.

**Create `Tests/C01_Tests.cs`:**

```csharp
using Xunit;
using FluentAssertions;
using Sardine.Core.Algorithms;

namespace Sardine.Tests.Core
{
    public class BoundaryRationaliserTests
    {
        [Fact]
        public void Rationalise_CleanRectangle_ReturnsThreePolygons()
        {
            // Arrange — build a simple rectangle as vertices
            var vertices = new List<Rhino.Geometry.Point3d>
            {
                new(0, 0, 0),
                new(30, 0, 0),
                new(30, 50, 0),
                new(0, 50, 0)
            };

            // Act
            var result = BoundaryRationaliser.Run(
                vertices, tolerance: 0.1, minEdge: 0.5);

            // Assert
            result.Raw.Should().NotBeNull();
            result.Working.Should().NotBeNull();
            result.Display.Should().NotBeNull();
            result.Warnings.Should().BeEmpty();
        }

        [Fact]
        public void Rationalise_SurveyBoundary_DoesNotThrow()
        {
            // Arrange — load real survey data
            var json = File.ReadAllText(
                Path.Combine("Fixtures", "survey_boundary_01.json"));
            var pts = JsonSerializer.Deserialize<List<PointData>>(json)!
                .Select(p => new Rhino.Geometry.Point3d(p.X, p.Y, p.Z))
                .ToList();

            // Act
            var act = () => BoundaryRationaliser.Run(pts, 0.1, 0.5);

            // Assert — should not throw regardless of boundary shape
            act.Should().NotThrow();
        }
    }

    // Local DTO for JSON deserialisation
    record PointData(double X, double Y, double Z);
}
```

Adjust the class and method names to match your actual Core API.

**Run and fix until green:**
```powershell
dotnet test src/Sardine.Tests.Core/Sardine.Tests.Core.csproj
```

Commit and open a PR when green.

**Concept to learn: Arrange-Act-Assert**
Every test has three parts. Arrange: set up the inputs. Act: call the thing
being tested. Assert: verify the output. Keep each part clearly separated —
it makes failing tests much easier to diagnose.

---

## PHASE 3 — CORE MODELS
*(Blocks 09–11 from original backlog — CarParkGraph, Layout Registry)*
*These blocks are unchanged from the original backlog. Proceed from Block 05 onwards
in the original document, substituting `Sardine` for `BayArray` throughout.*

---

## PHASE 4 onwards
*GH Components, Rhino Testing, CI/CD, Polish, Ship*
*Follow the original backlog Blocks 07–22, substituting `Sardine` for `BayArray`.*

---

## Summary

| Phase | Blocks | Outcome |
|---|---|---|
| 1. Foundation | 01–07 | Repo, solution, 3 projects, CI, green build |
| 2. Testing | 08 | Real Core tests passing |
| 3. Models | 09–11 | CarParkGraph, LayoutRegistry in Core |
| 4. GH Fundamentals | 12–13 | Component anatomy, GH_CarParkGraph |
| 5. Components | 14–19 | All 5 GH components built |
| 6. Rhino Testing | 20–21 | Tier 2 tests running |
| 7. CI/CD | Already done in Block 07 | — |
| 8. Ship | 22–25 | Icons, template, distribute |

---

## Key Commands Reference

```powershell
# Always work from src/ for msbuild
cd C:\Users\UKBWS001\dev\WSP-UK-sardine\src

# Build
msbuild Sardine.sln

# Clean + build
msbuild /t:Rebuild Sardine.sln

# Release build
msbuild Sardine.sln /p:Configuration=Release

# Run tests
dotnet test Sardine.Tests.Core\Sardine.Tests.Core.csproj

# New feature branch (always from updated main)
git checkout main
git pull
git checkout -b feature/your-feature-name

# Commit and push
git add .
git commit -m "type: description"
git push -u origin feature/your-feature-name

# Tag a release (triggers release.yml)
git tag v1.0.0
git push origin v1.0.0
```

## Commit Message Convention

```
feat:     new feature
fix:      bug fix
chore:    config, deps, tooling (no production code change)
docs:     documentation only
test:     adding or fixing tests
refactor: code change that neither fixes a bug nor adds a feature
ci:       CI/CD changes
```

---

## Resources

**Books:**
- *C# in Depth* — Jon Skeet (4th ed.)

**Bookmark:**
- https://developer.rhino3d.com — McNeel official guides
- https://discourse.mcneel.com — McNeel forum
- https://learn.microsoft.com/dotnet — C# reference
- https://github.com/mcneel/Rhino.Testing — testing framework
- https://docs.github.com/actions — GitHub Actions

**McNeel NuGet packages used:**
- `RhinoCommon` — geometry types, used in Core and GH
- `Grasshopper` — GH plugin types, used in GH only
- `Rhino.Templates` — project templates, install once
- `Rhino.Testing` — Tier 2 tests (Block 20)