# Bicep Extensibility

[![codecov](https://codecov.io/gh/Azure/bicep-extensibility/branch/main/graph/badge.svg)](https://codecov.io/gh/Azure/bicep-extensibility)

The Bicep Extensibility platform lets you build **Bicep extensions** — API services that enable users to deploy Azure data-plane or non-Azure resources through Bicep files and ARM templates.

## How it works

A Bicep extension implements a set of resource operations (preview, create/update, get, delete) behind an HTTP API that conforms to the [Extension API Contract](contract/contract.md). The Bicep Extensibility Host, a component of `Microsoft.Resources/deployments`, routes deployment requests to your extension and manages the lifecycle.

## SDKs

| Package | Audience | Description |
|---------|----------|-------------|
| **Azure.Deployments.Extensibility.Core** | All extensions | Transport-agnostic models, handler interfaces, discriminated unions, structured errors, and a fluent validation framework. |
| **Azure.Deployments.Extensibility.AspNetCore** | 1P (Microsoft-internal) | ASP.NET Core hosting layer with fluent handler registration, typed handler base classes, behavior pipeline, and version routing. |

> [!NOTE]
> The AspNetCore SDK is currently for first-party extension authors. A wrapper SDK for third-party and local extensions will be published separately.

## Quick links

- [Getting Started](tutorials/getting-started.md) — build your first extension in minutes
- [API Contract](contract/contract.md) — full specification of the extension protocol
- [Async Operations](contract/async-operations.md) — long-running operation patterns (RELO & LRO)
- [Preview Operation](contract/preview-operation.md) — unevaluated expressions, preview metadata, What-If
- [Core SDK](sdks/core.md) — models, `OneOf`, validation framework
- [AspNetCore SDK](sdks/aspnetcore.md) — `ExtensionApplication`, behaviors, typed handlers
- [Sample Extension](https://github.com/Azure/bicep-extensibility/tree/main/sample/MagicEightBallExtension) — Magic 8-Ball demo covering all 5 endpoints
