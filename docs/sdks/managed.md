# Managed SDK

**Package:** `Azure.Deployments.Extensibility.Hosting.Managed`

The Managed SDK is the public ASP.NET Core host for Bicep extensions running on the managed extension runtime. It's the default route for third-party, local, and internal extensions. One process hosts one exact extension version. It includes the base runtime transitively while keeping the complete `WebApplicationBuilder` and `WebApplication` surface available to your application.

## Install and declare identity

Add the package and declare the extension identity in your web project:

```bash
dotnet add package Azure.Deployments.Extensibility.Hosting.Managed
```

```xml
<PropertyGroup>
  <BicepExtensionName>Widget</BicepExtensionName>
  <BicepExtensionVersion>1.0.0</BicepExtensionVersion>
</PropertyGroup>
```

The package writes these values to assembly metadata. Both values must be present exactly once and must not be blank. The version is an opaque string: it is not parsed, normalized, trimmed, or treated as a range.

## Configure the host

```csharp
using Azure.Deployments.Extensibility.Hosting.Managed.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBicepExtension(extension => extension
    .AddGlobalHandlerBehavior<AuditBehavior>()
    .AddHandler<OperationStatusHandler>()
    .AddHandlerBehavior<SchemaValidationBehavior>()
    .ForResourceType("Widget", resourceType => resourceType
        .AddHandler<WidgetPreviewHandler>()
        .AddHandler<WidgetCreateOrUpdateHandler>()
        .AddHandler<WidgetGetHandler>()
        .AddHandler<WidgetDeleteHandler>()));

builder.Services.AddSingleton<WidgetStore>();

var app = builder.Build();

app.UseBicepExtension();
app.MapGet("/about", () => "Widget extension");

await app.RunAsync();
```

`AddBicepExtension` creates one immutable handler registration and can only be called once. `UseBicepExtension` installs the shared middleware, maps the contract routes, and can also only be called once. The application fails startup if service registration succeeds but `UseBicepExtension` is omitted.

## Handler and behavior scopes

- `AddHandler<T>()` registers a default handler, such as an LRO status handler.
- `ForResourceType(name, configure)` registers handlers and behaviors for one resource type.
- `AddGlobalHandlerBehavior<T>()` wraps every request, including an unsupported extension version.
- `AddHandlerBehavior<T>()` wraps requests after the exact extension version resolves.
- Factory overloads resolve from the current request scope and are disposed by dependency injection.

Type-based registration adds a scoped default while respecting an existing service registration. Use normal `builder.Services` APIs when a handler needs a different implementation or additional application services.

## Exact-version dispatch

The version segment in a contract route must equal `BicepExtensionVersion` using ordinal comparison. For an extension built as `1.0.0`, only `1.0.0` resolves. Values such as `1.0`, `1.0.0+build`, `1.*.*`, or casing variants do not match unless they are the exact declared string.

An unmatched value returns `UnsupportedExtensionVersion`. Missing resource handlers return `UnsupportedResourceType`, and a missing LRO handler returns `UnsupportedOperation`.

## Routes and health

`UseBicepExtension()` maps the five bare contract routes:

```text
POST /{extensionVersion}/resource/preview
POST /{extensionVersion}/resource/createOrUpdate
POST /{extensionVersion}/resource/get
POST /{extensionVersion}/resource/delete
POST /{extensionVersion}/longRunningOperation/get
```

It also maps `GET /ping`. Other methods receive `405 Method Not Allowed`.

## Scalar API explorer

Map the interactive Scalar UI and OpenAPI document for local development:

```csharp
app.MapDevelopmentApiExplorer(explorer => explorer
  .WithTitle("Widget Extension API")
  .ConfigureExamples(WidgetExamples.Configure));
```

The explorer is available at `/scalar/v2` and its OpenAPI document at `/openapi/v2.json` only when the application environment is `Development`. The Managed SDK automatically adds the exact `BicepExtensionVersion` as the route-version example.

The Scalar implementation is provided by the shared ASP.NET Core runtime. First-party hosting can use the same API and supply its supported version examples with `WithExtensionVersions(...)`.

## Standard ASP.NET Core composition

The SDK does not wrap or own the application. Configuration, logging, authentication, authorization, middleware, health checks, unrelated endpoints, `TestServer`, and `WebApplicationFactory` continue to use their standard ASP.NET Core APIs and ordering rules.

## Next steps

- [Getting Started](../tutorials/getting-started.md)
- [Typed Handlers](../tutorials/typed-handlers.md)
- [Behaviors](../tutorials/behaviors.md)
- [Managed API Reference](api-managed/)
