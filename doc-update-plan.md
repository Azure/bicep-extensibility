# Plan: V2 API Docs, TypeSpec/OpenAPI, and GitHub Pages Site

## TL;DR

Modernize the Bicep Extensibility documentation by: (A) choosing between TypeSpec or ASP.NET-generated OpenAPI for the API spec, (B) setting up docfx for SDK reference docs, (C) integrating Scalar into the AspNetCore SDK for dev-time API exploration, (D) updating the contract docs to match the V2 code, and (E) hosting everything on a combined GitHub Pages site. The plan details both TypeSpec and ASP.NET OpenAPI approaches so you can decide.

---

## Current Gaps (discovered from code vs docs)

The following V2 code changes are NOT reflected in the current `docs/v2/openapi.yaml` and `contract.md`:

1. **LRO endpoint changed**: OpenAPI has `GET /{resourceOperationPath}` with a generic path param. Code has `POST /{extensionVersion}/longRunningOperation/get` accepting a `JsonObject operationHandle` body.
2. **Accepted (202) response changed**: OpenAPI still has `Location` + `Retry-After` headers. Code returns an `Accepted<LongRunningOperation>` body with `operationHandle` (JsonObject) and `retryAfterSeconds` (int?).
3. **Resource model**: Missing optional `error` property (set when `Status="Failed"` in async operations).
4. **LongRunningOperation model**: Missing `operationHandle` (JsonObject?) and `retryAfterSeconds` (int?) properties.
5. **Preview request model**: OpenAPI uses `PreviewResource` as request body. Code uses `ResourcePreviewSpecification` (inherits `ResourceSpecification`) with a `Metadata` property containing `Unevaluated` paths.
6. **Preview response model**: Code uses `ResourcePreview` (separate from `Resource`) with `ResourcePreviewMetadata` (readOnly, immutable, calculated, unknown, unevaluated).
7. **contract.md stepwise LRO section**: Describes the old Location header manipulation pattern in steps 3-9. Must be rewritten for the operationHandle pattern.

---

## Phase 1: API Specification — TypeSpec

**Approach:** TypeSpec (chosen over ASP.NET OpenAPI generation)

**Why TypeSpec:**
- Transport-agnostic core spec (`main.tsp`) + separate HTTP binding (`http.tsp`)
- TypeSpec compiler generates standard OpenAPI 3.0 YAML
- First-class support for model composition, versioning, and decorators
- Clean separation of concern — API shape defined independently of implementation
- TypeSpec playground and docs are mature

**Trade-offs to keep in mind:**
- New tooling dependency (TypeSpec compiler, Node.js)
- Must manually keep TypeSpec models in sync with C# Core models (no codegen bridge)
- Learning curve for contributors unfamiliar with TypeSpec

**Steps:**

1. **Install TypeSpec tooling**
   - Add `tspconfig.yaml` at repo root or under a new `spec/` directory
   - Add `package.json` with `@typespec/compiler`, `@typespec/http`, `@typespec/rest`, `@typespec/openapi3` dependencies
   - Add `.gitignore` entries for `tsp-output/` and `node_modules/`

2. **Create transport-agnostic core TypeSpec** (`spec/main.tsp`)
   - Define all V2 models: `Resource`, `ResourcePreview`, `ResourcePreviewMetadata`, `ResourcePreviewSpecification`, `ResourcePreviewSpecificationMetadata`, `ResourceSpecification`, `ResourceReference`, `LongRunningOperation`, `Error`, `ErrorDetail`, `ErrorResponse`, `Config`, `ConfigId`, `Status`
   - Match property types, optionality, and descriptions from V2 C# models in `src/Azure.Deployments.Extensibility.Core/V2/Contracts/Models/`
   - Include doc comments for each model and property

3. **Create HTTP binding TypeSpec** (`spec/http.tsp`)
   - Define all 5 endpoints matching the route table in `WebApplicationExtensions.cs`:
     - `POST /{extensionVersion}/resource/preview`
     - `POST /{extensionVersion}/resource/createOrUpdate`
     - `POST /{extensionVersion}/resource/get`
     - `POST /{extensionVersion}/resource/delete`
     - `POST /{extensionVersion}/longRunningOperation/get`
   - Define request/response headers (from `Constants/RequestHeaderNames.cs` and `ResponseHeaderNames.cs`)
   - Map status codes to response types matching `HandlerDispatcher.cs` return types

4. **Generate OpenAPI spec**
   - Configure `tspconfig.yaml` to emit OpenAPI 3.0 YAML to `docs/v2/openapi.yaml`
   - Add npm script / CI step: `npx tsp compile spec/ --emit @typespec/openapi3`
   - Replace the hand-maintained `openapi.yaml` with the generated one

5. **Verification**
   - Compare generated OpenAPI against the C# models to ensure all properties match
   - Validate generated spec with an OpenAPI linter (e.g., Spectral)

**Relevant files to create:**
- `spec/package.json`
- `spec/tspconfig.yaml`
- `spec/main.tsp` — entry point (imports `http.tsp`)
- `spec/models.tsp` — transport-agnostic models (maps to `src/Azure.Deployments.Extensibility.Core/V2/Contracts/Models/*.cs`)
- `spec/http.tsp` — HTTP routes (maps to `src/Azure.Deployments.Extensibility.AspNetCore/WebApplicationExtensions.cs` and `HandlerDispatcher.cs`)

---

## Phase 2: Scalar Integration in AspNetCore SDK

*Depends on Phase 1 (need a valid OpenAPI spec to serve)*

**Goal:** Let extension authors launch Scalar API explorer during development to test their extension endpoints interactively.

**Steps:**

1. **Add `Scalar.AspNetCore` NuGet package** to `Azure.Deployments.Extensibility.AspNetCore.csproj`
   - Embed the TypeSpec-generated OpenAPI spec as an embedded resource

2. **Add opt-in extension method** in `WebApplicationExtensions.cs` or a new `ScalarExtensions.cs`:
   - `WebApplication UseScalarApiExplorer(this WebApplication app)` — conditionally maps `/scalar` and `/openapi/v2.json`
   - Serve the TypeSpec-generated `openapi.yaml` from an embedded resource or a known file path

3. **Document usage** in SDK docs: extension authors call `app.UseScalarApiExplorer()` in `Program.cs` during development

**Relevant files:**
- `src/Azure.Deployments.Extensibility.AspNetCore/Azure.Deployments.Extensibility.AspNetCore.csproj` — add package reference
- `src/Azure.Deployments.Extensibility.AspNetCore/WebApplicationExtensions.cs` — add Scalar mapping method (or new file)

---

## Phase 3: Update contract.md → Migrate to Site Content

*Parallel with Phase 1 & 2*

**Goal:** Rewrite `docs/v2/contract.md` to reflect the V2 API as implemented in code, then migrate it into the docfx site structure.

**Changes needed in contract content:**

1. **Stepwise LRO section** — Complete rewrite:
   - Remove all references to `Location` header manipulation by the Extensibility Host
   - Document the new flow: extension returns `202 Accepted` with a `LongRunningOperation` body containing `operationHandle` (opaque object) and `retryAfterSeconds`
   - Subsequent polling: Extensibility Host sends `POST /{extensionVersion}/longRunningOperation/get` with the `operationHandle` in the request body
   - Extension returns updated `LongRunningOperation` with current `status`, optionally updated `operationHandle`, and `error` if failed

2. **RELO section** — Minor updates:
   - Resource response now includes optional `error` property when `status` is `Failed`

3. **Preview API** — Add section or update existing:
   - Request body uses `ResourcePreviewSpecification` with optional `metadata.unevaluated` array
   - Response body uses `ResourcePreview` with `metadata` containing `readOnly`, `immutable`, `calculated`, `unevaluated` arrays

4. **Resource model** — Document the new optional `error` property

**Relevant files:**
- `docs/v2/contract.md` — rewrite, then move into docfx content structure

---

## Phase 4: SDK Documentation with docfx

*Parallel with Phase 1-3*

**Goal:** Generate API reference documentation for the Core and AspNetCore SDK projects using docfx.

**Steps:**

1. **Install docfx** — add as a .NET global tool or local tool (`dotnet tool install docfx`)

2. **Create docfx configuration** (`docs/docfx.json`):
   - Metadata source: `src/Azure.Deployments.Extensibility.Core/Azure.Deployments.Extensibility.Core.csproj` and `src/Azure.Deployments.Extensibility.AspNetCore/Azure.Deployments.Extensibility.AspNetCore.csproj`
   - Filter to public API only
   - Include XML doc comments (already enabled in `Directory.Build.props`: `<GenerateDocumentationFile>true</GenerateDocumentationFile>`)
   - Output to `docs/_site/`

3. **Add conceptual docs structure** under `docs/`:
   - `docs/articles/` — conceptual articles (getting started, architecture overview)
   - Migrate `contract.md` content here as `docs/articles/api-contract.md`
   - Add a `toc.yml` for navigation

4. **Ensure XML doc comments exist** on all public V2 APIs:
   - Review `src/Azure.Deployments.Extensibility.Core/V2/Contracts/` for missing `<summary>` and `<remarks>` tags
   - Review `src/Azure.Deployments.Extensibility.AspNetCore/` public types
   - Add missing doc comments (existing code already has `GenerateDocumentationFile` enabled, so compiler warns on missing docs)

5. **Customize docfx template** (optional):
   - Add landing page (`docs/index.md`)
   - Brand with project name and link to GitHub repo

**Relevant files to create:**
- `docs/docfx.json` — docfx project configuration
- `docs/toc.yml` — top-level table of contents
- `docs/index.md` — landing page
- `docs/articles/toc.yml` — articles navigation
- `docs/articles/api-contract.md` — migrated contract content
- `docs/articles/getting-started.md` — quick start guide for extension authors

**Relevant existing files:**
- `src/Directory.Build.props` — already has `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- All `.cs` files in Core/V2 and AspNetCore — need XML doc comment review

---

## Phase 5: GitHub Pages Site

*Depends on Phase 1, 2, 3, 4*

**Goal:** Host combined SDK docs + API explorer on GitHub Pages.

**Steps:**

1. **Configure docfx to produce a combined site:**
   - API reference (from docfx metadata)
   - Conceptual articles (contract, getting started)
   - Embed Scalar or link to it for interactive API exploration

2. **Integrate OpenAPI/Scalar into the docfx site:**
   - Embed a Scalar standalone HTML page in docfx output (Scalar can be loaded via CDN as a single HTML page referencing the OpenAPI spec)

3. **Create GitHub Actions workflow** (`.github/workflows/docs.yml`):
   - Trigger: push to `main` (or manual dispatch)
   - Steps:
     1. Checkout
     2. Setup .NET SDK 8.0
     3. Setup Node.js, `npm install`, `npx tsp compile` to generate OpenAPI
     4. Install docfx: `dotnet tool install -g docfx`
     5. Build docfx: `docfx docs/docfx.json`
     6. Copy OpenAPI spec + Scalar HTML to `docs/_site/api-explorer/`
     7. Deploy to GitHub Pages using `actions/deploy-pages@v4`

4. **Enable GitHub Pages** in repo settings (source: GitHub Actions)

**Relevant files to create:**
- `.github/workflows/docs.yml` — CI/CD for docs
- `docs/api-explorer/index.html` — Scalar standalone page referencing `openapi.yaml`

---

## Recommended Execution Order

```
Phase 1 (API Spec)  ──┐
                       ├──► Phase 2 (Scalar)  ──┐
Phase 3 (Contract)  ──┘                         ├──► Phase 5 (GitHub Pages)
Phase 4 (docfx)     ────────────────────────────┤
Phase 6 (Example)   ────────────────────────────┘
```

- **Phase 1 and Phase 3** can start in parallel (spec + contract updates)
- **Phase 4** (docfx) can start in parallel with Phase 1 and 3
- **Phase 2** (Scalar) depends on Phase 1 (needs an OpenAPI spec to serve)
- **Phase 6** (Example project) can start in parallel with Phase 1 and 3, but benefits from Phase 2 (Scalar) being available to demo
- **Phase 5** (GitHub Pages) depends on all prior phases

---

## Verification

1. **TypeSpec/OpenAPI** — Run `tsp compile`, validate output with [Spectral](https://github.com/stoplightio/spectral) OpenAPI linter
2. **Model parity** — Manually compare every TypeSpec/OpenAPI model property against C# V2 model properties (a checklist is in the "Current Gaps" section above)
3. **Scalar** — Run the AspNetCore project locally with Scalar enabled, verify all 5 endpoints render correctly in the UI, test sending requests
4. **docfx** — Build locally with `docfx docs/docfx.json`, open `docs/_site/index.html`, verify API reference for all public types in Core.V2 and AspNetCore
5. **GitHub Pages** — Push to a branch, trigger the workflow, verify the deployed site at `https://<org>.github.io/bicep-extensibility/`
6. **contract.md accuracy** — Review the rewritten LRO section against the code in `HandlerDispatcher.DispatchLongRunningOperationGetHandlerAsync` and `LongRunningOperation.cs`
7. **Example project** — `dotnet build` and `dotnet run` the example project, verify all 5 endpoints respond correctly, confirm the README walkthrough is accurate

---

## Phase 6: Example Extension Project

*Parallel with Phase 1-3; benefits from Phase 2 (Scalar) for interactive API exploration demo*

**Goal:** Provide a minimal, runnable example project showing extension authors how to build an extension using the AspNetCore SDK. This serves as both a quickstart reference and a living integration test.

**Steps:**

1. **Create example project** (`examples/SampleExtension/`)
   - New ASP.NET Core minimal API project targeting `net8.0`
   - Project reference to `Azure.Deployments.Extensibility.AspNetCore`
   - Keep dependencies minimal — no external databases or services

2. **Implement a simple in-memory resource provider**
   - `InMemoryResourceCreateOrUpdateHandler` — stores resources in a `ConcurrentDictionary`, returns `Resource` with status `Succeeded`
   - `InMemoryResourceGetHandler` — retrieves from the dictionary
   - `InMemoryResourceDeleteHandler` — removes from the dictionary
   - `InMemoryResourcePreviewHandler` — validates and returns a `ResourcePreview` without persisting
   - `InMemoryLongRunningOperationGetHandler` — demonstrates async operation polling with a simulated delay

3. **Wire up `Program.cs`** demonstrating the SDK registration pattern:
   ```csharp
   var builder = WebApplication.CreateBuilder(args);

   builder.AddExtensionApplication(extensionApp =>
   {
       extensionApp.AddResourcePreviewHandler<InMemoryResourcePreviewHandler>();
       extensionApp.AddResourceCreateOrUpdateHandler<InMemoryResourceCreateOrUpdateHandler>();
       extensionApp.AddResourceGetHandler<InMemoryResourceGetHandler>();
       extensionApp.AddResourceDeleteHandler<InMemoryResourceDeleteHandler>();
       extensionApp.AddLongRunningOperationGetHandler<InMemoryLongRunningOperationGetHandler>();
   });

   var app = builder.Build();

   app.UseExtensionApplicationMiddlewares();
   app.MapResourceActions();
   app.MapLongRunningOperationActions();

   app.Run();
   ```

4. **Add request validators** (optional but recommended to demonstrate):
   - `SampleResourceSpecificationValidator` — validates required properties in the resource body
   - Register via `app.AddResourceSpecificationValidator<SampleResourceSpecificationValidator>()`

5. **Add a README** (`examples/SampleExtension/README.md`):
   - Prerequisites (`.NET 8 SDK`)
   - How to build and run
   - Example `curl` / HTTP requests for each of the 5 endpoints
   - Expected responses with explanations
   - How to enable Scalar API explorer (once Phase 2 is complete)

6. **Add to solution** — include the example project in `Azure.Deployments.Extensibility.sln` under a `Examples` solution folder (but exclude from CI build to keep it as a reference only)

**Relevant files to create:**
- `examples/SampleExtension/SampleExtension.csproj`
- `examples/SampleExtension/Program.cs`
- `examples/SampleExtension/Handlers/InMemoryResourceCreateOrUpdateHandler.cs`
- `examples/SampleExtension/Handlers/InMemoryResourceGetHandler.cs`
- `examples/SampleExtension/Handlers/InMemoryResourceDeleteHandler.cs`
- `examples/SampleExtension/Handlers/InMemoryResourcePreviewHandler.cs`
- `examples/SampleExtension/Handlers/InMemoryLongRunningOperationGetHandler.cs`
- `examples/SampleExtension/Validation/SampleResourceSpecificationValidator.cs` (optional)
- `examples/SampleExtension/README.md`

---

## Decisions

- **TypeSpec vs ASP.NET OpenAPI**: **Decision: TypeSpec (Option A)**. Chosen for transport-agnostic spec layer and clean separation of API shape from implementation.
- **Scalar integration**: Will be in the AspNetCore SDK as an opt-in dev toggle, not a separate DevHost project.
- **contract.md**: Will be migrated fully into the docfx site structure (not kept as a standalone doc).
- **`unknown` in PreviewMetadata**: The OpenAPI spec has `unknown` in preview metadata, but the C# `ResourcePreviewMetadata` only has `ReadOnly`, `Immutable`, `Calculated`, `Unevaluated`. Need to confirm whether `unknown` was dropped or should be added to the C# model.

## Further Considerations

1. **XML doc comments**: The C# V2 models and AspNetCore public APIs need a doc comment audit. Some may be missing `<summary>` tags which docfx relies on. This is a prerequisite for good docfx output — recommend doing it as part of Phase 4 step 4.
2. **DevContainer update**: `.devcontainer/devcontainer.json` references `dotnet 6.0` but the project targets `net8.0`. Node.js should also be added to the devcontainer features for TypeSpec tooling. This is optional but helpful for contributors.
3. **CI integration**: Consider adding TypeSpec compile and docfx build as CI checks in `.github/workflows/ci.yml` to prevent spec/doc drift.
4. **Example project scope**: The example intentionally uses an in-memory store to avoid external dependencies. A more advanced example (e.g., backed by Azure Storage or CosmosDB) could be added later as a second sample.

