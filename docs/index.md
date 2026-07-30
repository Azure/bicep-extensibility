# Bicep Extensibility

[![codecov](https://codecov.io/gh/Azure/bicep-extensibility/branch/main/graph/badge.svg)](https://codecov.io/gh/Azure/bicep-extensibility)

> [!WARNING]
> The Bicep Extensibility platform is a work in progress. The SDKs are not yet ready for extension authors to consume. APIs and packages may change without notice, and there is no supported path to publish an extension yet. This documentation is published early for design review and feedback.

The Bicep Extensibility platform lets you build **Bicep extensions**: API services that let users deploy Azure data-plane or non-Azure resources through Bicep files and ARM templates.

## How it works

A Bicep extension implements a set of resource operations (preview, create/update, get, delete) behind an HTTP API that conforms to the [Extension API Contract](contract/contract.md). The Bicep Extensibility Host, a component of `Microsoft.Resources/deployments`, routes deployment requests to your extension and manages the lifecycle.

## Choose your path

**New to Bicep extensions?**
Start with [Getting Started](tutorials/getting-started.md), then work through the guides below.

**Building a third-party or local extension?**

1. [Getting Started](tutorials/getting-started.md)
2. [Typed Handlers](tutorials/typed-handlers.md)
3. [Behaviors](tutorials/behaviors.md)
4. [Validators](tutorials/validators.md)
5. [Managed SDK](sdks/managed.md)

Keep the [Core SDK](sdks/core.md) and [API Contract](contract/contract.md) handy as reference.

**On a first-party team?**
First-party hosting (`Hosting.FirstParty`) is a self-hosted route for extensions that are closely integrated into the ARM deployment service, such as the MS Graph extension, and don't need the managed extension runtime. Teams on this route run their own extension service, so it's only for teams that want that model. Internal teams that don't need it can build on the [Managed SDK](sdks/managed.md) like anyone else. First-party hosting and its authoring guide are maintained internally; on this site, read the [API Contract](contract/contract.md) and [Core SDK](sdks/core.md), which apply to all extensions.

**Integrating the wire protocol?**
Read the [API Contract](contract/contract.md), then [Preview Operation](contract/preview-operation.md) and [Async Operations](contract/async-operations.md).

## SDKs

| Package | Audience | Description |
|---------|----------|-------------|
| **Azure.Deployments.Extensibility.Core** | All extensions | Transport-agnostic models, handler interfaces, discriminated unions, structured errors, and a fluent validation framework. |
| **Azure.Deployments.Extensibility.Hosting.Managed** | 3P, local, and internal | Public managed host that adds project identity, exact-version dispatch, lifecycle validation, and health checks. |
| **Azure.Deployments.Extensibility.Hosting.FirstParty** | Self-hosted 1P | First-party host for extensions deeply integrated with the ARM deployment service that run their own service without the managed runtime. Maintained and documented internally. |

> [!NOTE]
> The Managed package includes its ASP.NET Core runtime dependency transitively. Extension authors should not reference the base runtime package directly.

## Reference

- [API Contract](contract/contract.md): full specification of the extension protocol
- [Preview Operation](contract/preview-operation.md): unevaluated expressions, preview metadata, What-If
- [Async Operations](contract/async-operations.md): long-running operation patterns (RELO and LRO)
- [Core SDK](sdks/core.md): models, `OneOf`, validation framework
- [Managed SDK](sdks/managed.md): public hosting SDK for 3P, local, and internal extensions
- <xref:Azure.Deployments.Extensibility.Core.V2.Contracts.Models>: Core API Reference
- <xref:Azure.Deployments.Extensibility.Hosting.Managed>: Managed API Reference
- [Sample Extension](https://github.com/Azure/bicep-extensibility/tree/main/sample/MagicEightBallExtension): Magic 8-Ball demo covering all 5 endpoints
