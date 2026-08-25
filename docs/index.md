# Bicep Extensibility

[![codecov](https://codecov.io/gh/Azure/bicep-extensibility/branch/main/graph/badge.svg)](https://codecov.io/gh/Azure/bicep-extensibility)

> [!IMPORTANT]
> **Work in Progress**: The Bicep Extensibility platform and SDKs are actively under development and subject to breaking changes. They are not yet ready for production use or general consumption by extension authors.

The Bicep Extensibility platform lets you build **Bicep extensions** — API services that enable users to deploy Azure data-plane or non-Azure resources directly through Bicep files and ARM templates.

## How it works

A Bicep extension implements a set of resource operations (preview, create/update, get, delete) behind an HTTP API conforming to the [Extension API Contract](contract/contract.md). The Bicep Extensibility Host, a component of `Microsoft.Resources/deployments`, routes deployment requests to your extension and manages resource lifecycle.

---

## Choose Your Path

Depending on your role and deployment model, follow the recommended learning path below:

### 🚀 3P Managed Extension Developer (Public)
> **Goal**: Build and publish a containerized Bicep extension hosted in Azure Container Apps or Azure Container Instances.

1. **Quickstart**: Follow [Getting Started](tutorials/getting-started.md) to create your first extension using `Azure.Deployments.Extensibility.Hosting.Managed`.
2. **Hosting & Lifecycle**: Read the [Managed Hosting SDK Guide](sdks/managed.md) for metadata discovery, exact version routing, and `/ping` health checks.
3. **Data Modeling**: Read [Typed Handlers](tutorials/typed-handlers.md) to work with strongly-typed C# models and [Validators](tutorials/validators.md) to enforce declarative schemas.
4. **Cross-Cutting Pipeline**: Read [Behaviors](tutorials/behaviors.md) for middleware decorators (logging, auth, error handling).
5. **Local Testing**: Explore [Scalar API Explorer](sdks/managed.md#development-api-explorer-scalar) to test your extension locally with interactive UI and OpenAPI specifications.

---

### 🏢 1P Microsoft Team (First-Party Hosting)
> **Goal**: Build an extension that integrates directly into ARM deployment services without requiring the managed extension runtime (e.g., Microsoft Graph extension).

1. **Hosting Model Decision**:
   - **Self-Hosted / ARM-Integrated (1P Model)**: If your extension is closely integrated into the ARM deployment service and your team hosts its own extension service, use `Azure.Deployments.Extensibility.Hosting.FirstParty` (closed-source in the Blueprint ADO repository).
   - **Managed Runtime Option**: Microsoft internal teams can also choose the **3P Managed model** (`Azure.Deployments.Extensibility.Hosting.Managed`) if they prefer the containerized runtime managed by the extensibility platform.
2. **Contracts & Handlers**: Read the [Core SDK Guide](sdks/core.md) for standard handler interfaces (`IResource*Handler`), discriminated unions (`OneOf`), and error models.
3. **Protocol & Semantics**: Review the [Extension API Contract](contract/contract.md), [Async Operations](contract/async-operations.md) (RELO/LRO), and [Preview & What-If Operations](contract/preview-operation.md).
4. **Validation & Behaviors**: Use the [Validators tutorial](tutorials/validators.md) and [Behaviors tutorial](tutorials/behaviors.md) to build robust handlers.

---

### 🌐 Protocol & Non-.NET Implementer
> **Goal**: Implement a Bicep extension in Go, Python, Rust, Node.js, or any HTTP stack.

1. **API Protocol**: Read the [Extension API Contract](contract/contract.md).
2. **OpenAPI Spec**: Inspect the [OpenAPI Specification](contract/v2/openapi.yaml) and [Contract Reference](contract/index.md).
3. **Async & Preview Patterns**: Understand [Async Operations](contract/async-operations.md) and [Preview / What-If Operation](contract/preview-operation.md).

---

## SDK Ecosystem

| Package | Audience / Visibility | Role |
|---------|-----------------------|------|
| **Azure.Deployments.Extensibility.Core** | All extensions (Public) | Shared contracts, handler interfaces, discriminated unions (`OneOf`), models, and fluent validation. |
| **Azure.Deployments.Extensibility.Hosting.Managed** | 3P Extensions (Public) | High-level hosting SDK for containerized extensions — assembly metadata discovery, exact-version routing, and health checks. |
| **Azure.Deployments.Extensibility.Hosting.FirstParty** | 1P Extensions (Internal) | High-level hosting SDK for Microsoft-internal control plane services (maintained in Blueprint ADO repo). |
| **Azure.Deployments.Extensibility.AspNetCore** | Base hosting layer (Public) | Low-level ASP.NET Core protocol dispatcher, behavior pipeline, and JSON serializers. Foundation for higher-level hosting SDKs. |

## Quick links

- [Getting Started](tutorials/getting-started.md) — build a 3P extension in minutes
- [API Contract](contract/contract.md) — protocol specification
- [Async Operations](contract/async-operations.md) — long-running operation patterns
- [Preview Operation](contract/preview-operation.md) — What-If and unevaluated expressions
- [Core SDK Reference](sdks/core.md) — contracts and validation
- [Managed Hosting SDK Reference](sdks/managed.md) — containerized hosting guide
- [Sample Extension](https://github.com/Azure/bicep-extensibility/tree/main/sample/MagicEightBallExtension) — complete reference implementation of all 5 endpoints

