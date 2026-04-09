# Getting Started

> [!NOTE]
> These tutorials use the **AspNetCore SDK**, which is currently intended for **first-party (Microsoft-internal) extension authors**. A wrapper SDK for third-party and local extension development will be published separately.

This guide walks you through building your first Bicep extension using the AspNetCore SDK.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## 1. Create a new project

```bash
dotnet new web -n MyExtension
cd MyExtension
dotnet add package Azure.Deployments.Extensibility.AspNetCore
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
using Azure.Deployments.Extensibility.AspNetCore;

ExtensionApplication.Create(args)
    .AddExtensionVersion("1.*.*", version => version
        .ForResourceType("MyResource", type => type
            .AddHandler<MyResourceCreateOrUpdateHandler>()
            .AddHandler<MyResourceGetHandler>()
            .AddHandler<MyResourceDeleteHandler>()
            .AddHandler<MyResourcePreviewHandler>()))
    .Run();
```

Key concepts:

- **`AddExtensionVersion`** — registers handlers for a semantic version range. The extension host routes requests to the matching version.
- **`ForResourceType`** — scopes handlers to a specific resource type name.
- **`AddHandler<T>`** — registers a handler. The SDK infers the operation (create, get, delete, preview, LRO) from the interface the handler implements.

Handlers registered directly on the version builder (outside `ForResourceType`) act as **generic handlers** that match any resource type without a type-specific handler. This is useful for resource-type-agnostic operations like LRO polling.

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

## 5. Add behaviors (optional)

Behaviors are pipeline decorators that run before and after handlers. Use them for cross-cutting concerns like validation, logging, or retry logic.

```csharp
app.AddExtensionVersion("1.*.*", version => version
    .AddHandlerBehavior<MyValidationBehavior>()
    .ForResourceType("MyResource", type => type
        .AddHandler<MyResourceCreateOrUpdateHandler>()));
```

Behaviors can be registered at three levels:
- **Global** — `app.AddGlobalHandlerBehavior<T>()` runs for every handler across all versions.
- **Version-scoped** — `version.AddHandlerBehavior<T>()` runs for handlers in that version range.
- **Resource-type-scoped** — `type.AddHandlerBehavior<T>()` runs only for handlers of that resource type.

## Next Steps

- [Typed Handlers](typed-handlers.md) — use strongly-typed models instead of raw `JsonObject`.
- [Behaviors](behaviors.md) — add cross-cutting concerns like validation and logging.
- [Validators](validators.md) — validate requests with a fluent DSL.
- Read the [API Contract](../contract/contract.md) for the complete protocol specification.
- Explore the [Magic 8-Ball sample](https://github.com/Azure/bicep-extensibility/tree/main/sample/MagicEightBallExtension) for a full working example covering all 5 endpoints.
