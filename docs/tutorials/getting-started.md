# Getting Started

> **Audience**: Third-Party (3P) Managed Extension Authors  
> **Package**: `Azure.Deployments.Extensibility.Hosting.Managed`  
> [!IMPORTANT]
> **Work in Progress**: The Bicep Extensibility platform and SDKs are in active development and not yet ready for production or general consumption by extension authors.

This guide walks you through building, running, and testing your first containerized Bicep extension using the public **Managed Hosting SDK**.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Any HTTP testing tool (`curl`, `httpie`, or browser)

## 1. Create a new project

```bash
dotnet new web -n MyExtension
cd MyExtension
dotnet add package Azure.Deployments.Extensibility.Hosting.Managed
```

In your `MyExtension.csproj` file, specify the extension's name and version:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>

    <!-- Extension identity metadata -->
    <BicepExtensionName>MyExtension</BicepExtensionName>
    <BicepExtensionVersion>1.0.0</BicepExtensionVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Azure.Deployments.Extensibility.Hosting.Managed" Version="1.0.0-*" />
  </ItemGroup>
</Project>
```

## 2. Define a handler

Each resource operation is implemented as a handler. At a minimum, you need a create-or-update handler, a get handler, a delete handler, and a preview handler.

Here's a minimal create-or-update handler:

```csharp
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using System.Text.Json.Nodes;

public class MyResourceCreateOrUpdateHandler : IResourceCreateOrUpdateHandler
{
    public Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> HandleAsync(
        ResourceSpecification request,
        CancellationToken cancellationToken)
    {
        var resource = new Resource
        {
            Type = request.Type,
            ApiVersion = request.ApiVersion,
            Identifiers = request.Properties?["name"] is { } name
                ? new JsonObject { ["name"] = name.DeepClone() }
                : new JsonObject(),
            Properties = request.Properties?.DeepClone().AsObject(),
        };

        return Task.FromResult(OneOf.FromT0<Resource, LongRunningOperation, ErrorResponse>(resource));
    }
}
```

## 3. Wire up the application

Replace the contents of `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddBicepExtension(extension => extension
    .ForResourceType("MyResource", type => type
        .AddHandler<MyResourceCreateOrUpdateHandler>()
        .AddHandler<MyResourceGetHandler>()
        .AddHandler<MyResourceDeleteHandler>()
        .AddHandler<MyResourcePreviewHandler>()));

var app = builder.Build();

app.UseBicepExtension();

app.Run();
```

Key concepts:

- **`AddBicepExtension`** — registers the extension using the metadata defined in the project file.
- **`ForResourceType`** — scopes handlers to a specific resource type name.
- **`AddHandler<T>`** — registers a handler. The SDK infers the operation (create, get, delete, preview, LRO) from the interface the handler implements.
- **`UseBicepExtension`** — configures standard middleware, error handling, the `/ping` health check endpoint, and contract routes.

## 4. Run and test

```bash
dotnet run
```

The extension starts at `http://localhost:5000`. Test a preview request:

```bash
curl -X POST http://localhost:5000/1.0.0/resource/preview \
  -H "Content-Type: application/json" \
  -H "x-ms-client-request-id: test-001" \
  -H "x-ms-correlation-request-id: corr-001" \
  -H "Referer: http://localhost" \
  -H "traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" \
  -H "tracestate: congo=t61rcWkgMzE" \
  -d '{
    "type": "MyResource",
    "apiVersion": "2024-01-01",
    "properties": {
      "name": "example"
    }
  }'
```

You can also test the health check endpoint:

```bash
curl http://localhost:5000/ping
```

## Next Steps

- [Typed Handlers](typed-handlers.md) — use strongly-typed models instead of raw `JsonObject`.
- [Behaviors](behaviors.md) — add cross-cutting concerns like validation and logging.
- [Validators](validators.md) — validate requests with a fluent DSL.
- Read the [API Contract](../contract/contract.md) for the complete protocol specification.
- Explore the [Managed Hosting SDK Guide](../sdks/managed.md) for hosting options, metadata, and custom middleware integration.
- Explore the [Magic 8-Ball sample](https://github.com/Azure/bicep-extensibility/tree/main/sample/MagicEightBallExtension) for a full working example covering all 5 endpoints.
