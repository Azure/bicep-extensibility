# Plan: V2 API Docs, TypeSpec/OpenAPI, and GitHub Pages Site

## TL;DR

Set up docfx for SDK reference docs and host everything on a GitHub Pages site.

All phases are ✅ **complete**.

---

## ~~Phase 1: API Specification — TypeSpec~~ ✅ Done

TypeSpec spec lives in `spec/` with `core.tsp` (transport-agnostic models), `http.tsp` (HTTP binding), and `main.tsp` (entry point). The `tspconfig.yaml` emits OpenAPI 3.0 YAML to `docs/v2/openapi.yaml`. All V2 models and 5 endpoints are defined.

---

## ~~Phase 2: Scalar Integration in AspNetCore SDK~~ ✅ Done

- `Scalar.AspNetCore` and `Microsoft.OpenApi.Readers` added to AspNetCore csproj
- `openapi.yaml` embedded as an assembly resource
- `ScalarExtensions.cs` serves the spec at `/openapi/v2.json` and Scalar UI at `/scalar/v2`
- `OpenApiExamplesBuilder` allows extension authors to inject per-operation request/response examples
- `ExtensionApplication.EnableDevelopmentScalarApiExplorer()` provides opt-in fluent API (dev-only)

---

## ~~Phase 3: Update contract.md~~ ✅ Done

- `contract.md` documents all five operations, preview metadata, LRO with operationHandle, RELO with status/error, configuration handling rules
- `async-operations.md` provides detailed RELO and LRO guidance with sequence diagrams and step-by-step examples

---

## ~~Phase 4: Example Extension Project~~ ✅ Done

The **MagicEightBall** sample lives in `sample/MagicEightBallExtension/`. It demonstrates:
- All 5 V2 endpoints (preview, createOrUpdate, get, delete, LRO get)
- Version-based handler routing (v1 `1.*.*` vs v2 `2.*.*` with extra fields)
- Resource-type-scoped handler registration via `ForResourceType`
- Behavior pipeline: response logging, name validation, API version validation, preview metadata processing
- Long-running operations with simulated delay
- Typed handler base classes with custom model serialization
- Scalar API explorer with per-version examples
- Comprehensive README with curl examples for every endpoint

---

## ~~Phase 5: SDK Documentation with docfx~~ ✅ Done

**Goal:** Generate API reference documentation for the Core and AspNetCore SDK projects using docfx.

**Status:** Complete.

**Steps:**

1. **Install docfx** — add as a .NET local tool (`dotnet tool install docfx`)

2. **Create docfx configuration** (`docs/docfx.json`):
   - Metadata source: `src/Azure.Deployments.Extensibility.Core/Azure.Deployments.Extensibility.Core.csproj` and `src/Azure.Deployments.Extensibility.AspNetCore/Azure.Deployments.Extensibility.AspNetCore.csproj`
   - Filter to public API only
   - XML doc comments already enabled in `Directory.Build.props`
   - Output to `docs/_site/`

3. **Add conceptual docs structure** under `docs/`:
   - `docs/articles/` — conceptual articles (getting started, architecture overview)
   - Link to existing `contract.md` and `async-operations.md`
   - Add a `toc.yml` for navigation

4. **Customize docfx template** (optional):
   - Add landing page (`docs/index.md`)
   - Brand with project name and link to GitHub repo

**Files to create:**
- `docs/docfx.json`
- `docs/toc.yml`
- `docs/index.md`
- `docs/articles/toc.yml`
- `docs/articles/getting-started.md` — quick start guide for extension authors

---

## ~~Phase 6: GitHub Pages Site~~ ✅ Done

*Depends on Phase 5*

**Goal:** Host combined SDK docs + API explorer on GitHub Pages.

**Status:** Complete.

**Steps:**

1. **Configure docfx to produce a combined site:**
   - API reference (from docfx metadata)
   - Conceptual articles (contract, getting started)
   - Embed a Scalar standalone HTML page referencing the OpenAPI spec

2. **Create GitHub Actions workflow** (`.github/workflows/docs.yml`):
   - Trigger: push to `main` (or manual dispatch)
   - Steps:
     1. Checkout
     2. Setup .NET SDK 8.0
     3. Setup Node.js, `npm install`, `npx tsp compile` to generate OpenAPI
     4. Install docfx: `dotnet tool install -g docfx`
     5. Build docfx: `docfx docs/docfx.json`
     6. Copy OpenAPI spec + Scalar HTML to `docs/_site/api-explorer/`
     7. Deploy to GitHub Pages using `actions/deploy-pages@v4`

3. **Enable GitHub Pages** in repo settings (source: GitHub Actions)

**Files to create:**
- `.github/workflows/docs.yml` — CI/CD for docs
- `docs/api-explorer/index.html` — Scalar standalone page referencing `openapi.yaml`

---

## Remaining Work

None — all phases are complete.

