# SDK Reference

The Bicep Extensibility SDK is layered so hosting policy stays separate from the shared protocol runtime.

| Package | Description |
|---------|-------------|
| **Azure.Deployments.Extensibility.Core** | Transport-agnostic models, handler interfaces, discriminated unions (`OneOf`), structured errors, and a fluent validation framework. |
| **Azure.Deployments.Extensibility.Hosting.Managed** | Public managed host with assembly identity, exact-version routing, startup validation, and `GET /ping`. Default route for 3P, local, and internal extensions. |
| **Azure.Deployments.Extensibility.Hosting.FirstParty** | Self-hosted first-party host for extensions deeply integrated with the ARM deployment service, with multi-version routing and tenant policy integration. |

Extension authors should reference **Hosting.Managed**. It brings in Core and the internal ASP.NET Core runtime transitively while preserving direct access to standard `WebApplicationBuilder` and `WebApplication` APIs. Do not reference the base runtime package directly.

First-party hosting is a self-hosted route for a small set of extensions closely integrated with the ARM deployment service, such as the MS Graph extension. Its authoring documentation is maintained internally. Internal teams that don't need it can use Hosting.Managed like anyone else.

## Next steps

- [Core SDK](core.md): models, `OneOf`, validation
- [Managed SDK](managed.md): build a 3P or local extension
