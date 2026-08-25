# Managed Hosting SDK

**Package:** `Azure.Deployments.Extensibility.Hosting.Managed`

> [!IMPORTANT]
> **Work in Progress**: This SDK is under active development. Extension authors should not yet consume or build production workloads with this SDK until official release.

The Managed Hosting SDK is the official public-facing hosting layer for third-party (3P) containerized Bicep extensions. It wraps the base AspNetCore hosting layer to provide an idiomatic, single-extension hosting model with MSBuild metadata generation, exact-version routing, and built-in health checks.

## Architecture

The Bicep Extensibility platform uses a layered SDK architecture:

- **Core SDK** (`Azure.Deployments.Extensibility.Core`): Transport-agnostic data contracts, handler interfaces, and validation engine.
- **AspNetCore SDK** (`Azure.Deployments.Extensibility.AspNetCore`): Base ASP.NET Core hosting library providing endpoint routing, JSON formatting, request correlation, culture handling, and the handler/behavior pipeline.
- **Hosting SDKs**:
  - **Managed SDK** (`Azure.Deployments.Extensibility.Hosting.Managed`): Public-facing SDK for managed/containerized 3P extensions.
  - **First-Party SDK**: Internal SDK for Microsoft 1P services hosted within Azure control planes.

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

## Getting Started

### 1. Configure the project file

Add the `Azure.Deployments.Extensibility.Hosting.Managed` package and specify your extension's name and version properties in your `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>

    <!-- Extension identity metadata -->
    <BicepExtensionName>MyCustomExtension</BicepExtensionName>
    <BicepExtensionVersion>1.0.0</BicepExtensionVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Azure.Deployments.Extensibility.Hosting.Managed" Version="1.0.0-*" />
  </ItemGroup>
</Project>
```

The MSBuild build targets automatically generate assembly metadata attributes (`BicepExtensionNameAttribute` and `BicepExtensionVersionAttribute`) during compilation.

### 2. Implement handlers

Implement handler interfaces from `Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers`:

```csharp
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using System.Text.Json.Nodes;

public class WidgetPreviewHandler : IResourcePreviewHandler
{
    public Task<OneOf<ResourcePreview, ErrorResponse>> HandleAsync(
        ResourcePreviewSpecification request,
        CancellationToken cancellationToken)
    {
        var preview = new ResourcePreview
        {
            Type = request.Type,
            Identifiers = new JsonObject { ["name"] = request.Properties?["name"]?.DeepClone() ?? "preview-widget" },
            Properties = request.Properties?.DeepClone().AsObject() ?? new JsonObject(),
            Metadata = ResourcePreviewMetadata.NewBuilder()
                .WithMetadataFromSpec(request.Metadata)
                .Build(),
        };

        return Task.FromResult<OneOf<ResourcePreview, ErrorResponse>>(preview);
    }
}

public class WidgetCreateOrUpdateHandler : IResourceCreateOrUpdateHandler
{
    public Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> HandleAsync(
        ResourceSpecification request,
        CancellationToken cancellationToken)
    {
        var resource = new Resource
        {
            Type = request.Type,
            Identifiers = new JsonObject { ["name"] = request.Properties?["name"]?.DeepClone() ?? "default-widget" },
            Properties = request.Properties?.DeepClone().AsObject() ?? new JsonObject(),
        };

        return Task.FromResult<OneOf<Resource, LongRunningOperation, ErrorResponse>>(resource);
    }
}

public class WidgetGetHandler : IResourceGetHandler
{
    public Task<OneOf<Resource?, ErrorResponse>> HandleAsync(
        ResourceReference request,
        CancellationToken cancellationToken)
    {
        var resource = new Resource
        {
            Type = request.Type,
            Identifiers = request.Identifiers,
            Properties = new JsonObject { ["name"] = request.Identifiers["name"]?.DeepClone() },
        };

        return Task.FromResult<OneOf<Resource?, ErrorResponse>>(resource);
    }
}

public class WidgetDeleteHandler : IResourceDeleteHandler
{
    public Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> HandleAsync(
        ResourceReference request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<OneOf<Resource?, LongRunningOperation, ErrorResponse>>((Resource?)null);
    }
}
```

### 3. Wire up in `Program.cs`

Use the standard ASP.NET Core `WebApplicationBuilder` pattern:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register the Bicep extension using metadata from project properties
builder.AddBicepExtension(extension => extension
    .ForResourceType("Widget", resourceType => resourceType
        .AddHandler<WidgetPreviewHandler>()
        .AddHandler<WidgetCreateOrUpdateHandler>()
        .AddHandler<WidgetGetHandler>()
        .AddHandler<WidgetDeleteHandler>()));

var app = builder.Build();

// Configure the Bicep extension pipeline, health check, and endpoints
app.UseBicepExtension();

app.Run();
```

## Key Features

### Assembly Metadata Discovery

When using `builder.AddBicepExtension(extension => ...)`, the extension metadata (`Name` and `Version`) is automatically discovered from the entry assembly at startup via reflection. If metadata is missing or invalid, an informative `InvalidOperationException` is thrown.

You can also provide explicit metadata:

```csharp
var metadata = new BicepExtensionMetadata("MyCustomExtension", "1.0.0");
builder.AddBicepExtension(metadata, extension => { ... });
```

### Exact Version Resolution

Managed containerized extensions represent a single immutable deployment unit with an exact version. The Managed SDK enforces exact-version matching (using ordinal string equality) rather than range parsing. If an incoming request targets a different version, the host returns HTTP 400 with `UnsupportedExtensionVersion`.

### Built-in Health Check (`/ping`)

`app.UseBicepExtension()` automatically maps a health check endpoint at `/ping` which responds with HTTP 200 `Healthy`. This endpoint satisfies container orchestration readiness and liveness probes.

### Development API Explorer (Scalar)

During local development and testing, you can enable an interactive OpenAPI specification endpoint (`/openapi/v2.json`) and API explorer UI (`/scalar/v1`) using `app.MapBicepExtensionApiExplorer()`:

```csharp
var app = builder.Build();

// Enable Scalar API explorer (only active in Development environment)
app.MapBicepExtensionApiExplorer(explorer => explorer
    .WithTitle("Widget Extension API")
    .ConfigureExamples(examples =>
    {
        examples.ForCreateOrUpdate(
            request: new { type = "Widget", properties = new { name = "my-widget" } },
            response: new { type = "Widget", identifiers = new { name = "my-widget" }, properties = new { name = "my-widget" } });
    }));

app.UseBicepExtension();
app.Run();
```

### Integration with Custom Middleware and Endpoints

Because the Managed SDK extends standard `WebApplicationBuilder` and `WebApplication`, you can freely register your own services, middlewares, and additional endpoints alongside the extension:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Custom dependency injection services
builder.Services.AddSingleton<IMyExternalClient, MyExternalClient>();

builder.AddBicepExtension(extension => extension
    .ForResourceType("Widget", type => type.AddHandler<WidgetCreateOrUpdateHandler>()));

var app = builder.Build();

// Custom ASP.NET Core middleware
app.UseAuthentication();
app.UseAuthorization();

// Custom routes alongside Bicep endpoints
app.MapGet("/metrics", () => Results.Ok(new { status = "running" }));

app.UseBicepExtension();

app.Run();
```

### Safety and Validation

- **Single Extension Registration**: Attempting to call `AddBicepExtension` multiple times on the same service collection throws an `InvalidOperationException`.
- **Single Pipeline Setup**: Calling `UseBicepExtension` more than once throws an `InvalidOperationException`.
- **Order Enforcement**: Calling `UseBicepExtension` before `AddBicepExtension` fails fast with an informative exception.
