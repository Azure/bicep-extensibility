# SDKs

The Bicep Extensibility platform ships a public Hosting SDK for third-party authors, plus first-party and managed wrappers for internal scenarios. Together they provide everything needed to build, host, and validate a Bicep extension.

> [!WARNING]
> The SDKs and docs are still evolving. The public authoring experience is a work in progress and is not yet ready for broad extension-author consumption.

| Package | Description |
|---------|-------------|
| **Azure.Deployments.Extensibility.Core** | Transport-agnostic models, handler interfaces, discriminated unions (`OneOf`), structured errors, and a fluent validation framework. |
| **Azure.Deployments.Extensibility.Hosting** | Public Hosting SDK for third-party and local extension authors that uses a standard host, an exact-version resolver, health checks, and `/ping`. |
| **Azure.Deployments.Extensibility.Hosting.FirstParty** | First-party hosting wrapper for Microsoft-internal teams that self-host their extension service and need the ARM-integrated hosting experience. |
| **Azure.Deployments.Extensibility.Hosting.Managed** | Managed-runtime hosting option for Microsoft-internal teams that want the shared managed environment rather than self-hosting their extension service. |

## Choose your entry point

### Third-party (3P) extension authors
Read the public authoring path first:

- [Hosting SDK](hosting.md) — the public package and host entry points.
- [Core SDK](core.md) — the shared contract models, handlers, and validation primitives.
- [Getting Started](../tutorials/getting-started.md) — a hands-on walkthrough for building your first extension.

### Microsoft-internal (1P) teams
Read the shared implementation path first:

- [Core SDK](core.md) — the shared foundation used by both packaging models.
- [AspNetCore runtime reference](aspnetcore.md) — the underlying hosting/runtime layer.
- [Hosting SDK](hosting.md) — useful context for the public authoring model and shared host behavior.
- Choose the hosting package that fits your runtime model:
  - `Azure.Deployments.Extensibility.Hosting.FirstParty` for teams like Microsoft Graph that are deeply integrated with ARM and self-host their extension service.
  - `Azure.Deployments.Extensibility.Hosting.Managed` for teams that want the managed runtime instead of self-hosting.

The **Core** package is used by both 1P and 3P extensions.

## Next steps

- [Core SDK](core.md) — models, `OneOf`, validation
- [Hosting SDK](hosting.md) — public wrapper for standard-host extensions

For implementation details of the shared AspNetCore runtime, see [AspNetCore runtime reference](aspnetcore.md).
