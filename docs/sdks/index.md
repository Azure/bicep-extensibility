# SDKs

The Bicep Extensibility platform ships two NuGet packages. Together they provide everything needed to build, host, and validate a Bicep extension.

| Package | Description |
|---------|-------------|
| **Azure.Deployments.Extensibility.Core** | Transport-agnostic models, handler interfaces, discriminated unions (`OneOf`), structured errors, and a fluent validation framework. |
| **Azure.Deployments.Extensibility.AspNetCore** | ASP.NET Core hosting layer — `ExtensionApplication` fluent API, version-based routing, typed handler base classes, and the behavior pipeline. |

## Audience

> [!NOTE]
> The **AspNetCore** SDK is currently intended for **first-party (Microsoft-internal) extension authors**. A wrapper SDK for third-party and local extension development will be published separately.

The **Core** package is used by both 1P and 3P extensions.

## Next steps

- [Core SDK](core.md) — models, `OneOf`, validation
- [AspNetCore SDK](aspnetcore.md) — hosting, routing, behaviors
