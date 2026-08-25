# SDKs

> [!IMPORTANT]
> **Work in Progress**: The Extensibility SDKs are currently under active development and are not yet ready for production or general consumption. APIs may change without notice.

The Bicep Extensibility platform provides a modular suite of NuGet packages designed to build, host, and validate Bicep extensions.

## Choosing the Right SDK

| If you are... | Use this SDK | Notes |
|---------------|--------------|-------|
| Building a **Third-Party (3P) Extension** or **Managed Internal Extension** (Containerized) | [**Azure.Deployments.Extensibility.Hosting.Managed**](managed.md) | High-level hosting SDK for standard ASP.NET Core apps with assembly metadata discovery, exact version routing, and health checks. Suitable for 3P authors and MSFT teams choosing the managed container model. |
| Building a **First-Party (1P) Self-Hosted Extension** (ARM-Integrated) | **Azure.Deployments.Extensibility.Hosting.FirstParty** (Internal) + [**Core**](core.md) | For extensions closely integrated into the ARM deployment service (such as MS Graph) where the team hosts its own extension service. Maintained in the internal Blueprint ADO repo. |
| Implementing custom serialization, contracts, or testing utilities | [**Azure.Deployments.Extensibility.Core**](core.md) | Transport-agnostic models, handler interfaces, `OneOf` discriminated unions, and fluent validation framework. |
| Looking for the base hosting foundation | [**Azure.Deployments.Extensibility.AspNetCore**](aspnetcore.md) (Base Layer) | Shared protocol dispatching and behavior pipeline. Extension authors typically consume this indirectly via `Hosting.Managed` or `Hosting.FirstParty`. |

## Architecture

The SDKs follow a clean, layered architecture:

```
+-------------------------------------------------------------+
|    Managed SDK (3P)        |      FirstParty SDK (1P)       |
| (Hosting.Managed - Public) | (Internal ADO Repo - Private)  |
+-------------------------------------------------------------+
|                     AspNetCore SDK                          |
|             (Base ASP.NET Core Hosting Layer)               |
+-------------------------------------------------------------+
|                        Core SDK                             |
|          (Contracts, Handlers, Models, Validation)          |
+-------------------------------------------------------------+
```

- **Core SDK (`Azure.Deployments.Extensibility.Core`)**: Contains the transport-agnostic interfaces (`IResource*Handler`), models (`ResourceSpecification`, `Resource`, `ErrorResponse`), discriminated union types (`OneOf`), and the `ModelValidator<T>` validation engine.
- **AspNetCore SDK (`Azure.Deployments.Extensibility.AspNetCore`)**: Implements the base HTTP protocol contract, JSON serialization, correlation header middleware, and the `IHandlerBehavior` pipeline.
- **Managed SDK (`Azure.Deployments.Extensibility.Hosting.Managed`)**: Public-facing SDK tailoring `AspNetCore` for containerized hosting in Azure Container Apps/Instances (automatic assembly metadata reader, exact-version matching, `/ping` probe, and extension methods for `WebApplicationBuilder`/`WebApplication`).
- **FirstParty SDK (`Azure.Deployments.Extensibility.Hosting.FirstParty`)**: Closed-source SDK tailoring `AspNetCore` for internal Microsoft control plane integration.

## Documentation Index

- [Managed Hosting SDK Guide](managed.md) — 3P containerized hosting, metadata, health checks, and Scalar API explorer
- [Core SDK Guide](core.md) — handler interfaces, `OneOf`, and validation framework
- [AspNetCore SDK (Base Layer)](aspnetcore.md) — internal base hosting architecture and behaviors

