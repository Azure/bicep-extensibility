# Build a managed extension

> [!WARNING]
> The managed extensibility platform and Managed SDK are under active development and
> are not ready for extension authors to adopt. This quickstart demonstrates the
> intended experience for early evaluation only.

This quickstart is for teams that want the platform to run their extension. This
includes external extension authors and Microsoft teams that do not need to operate a
self-hosted extension service. You will build a single-version extension with the
Managed hosting SDK.

> [!NOTE]
> Microsoft teams should use the [FirstParty SDK handoff](../get-started/first-party.md)
> only when they need a self-hosted service that integrates closely with ARM
> deployments. If you are unsure, see
> [Choose a hosting SDK](../get-started/choose-hosting.md).

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## 1. Create a new project

```bash
dotnet new web -n MyExtension
cd MyExtension
dotnet add package Azure.Deployments.Extensibility.Hosting.Managed
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

Add the extension identity to `MyExtension.csproj`:

```xml
<PropertyGroup>
  <BicepExtensionName>MyExtension</BicepExtensionName>
  <BicepExtensionVersion>1.0.0</BicepExtensionVersion>
</PropertyGroup>
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

- **`BicepExtensionVersion`** declares the one exact version served by the managed process.
- **`AddBicepExtension`** registers the extension's handlers and shared hosting services.
- **`ForResourceType`** scopes handlers to a specific resource type name.
- **`AddHandler<T>`** registers a handler. The SDK infers the operation (create, get, delete, preview, LRO) from the interface the handler implements.

Handlers registered directly on the extension builder (outside `ForResourceType`) act as **generic handlers** that match any resource type without a type-specific handler. This is useful for resource-type-agnostic operations like LRO polling.

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
builder.AddBicepExtension(extension => extension
    .AddHandlerBehavior<MyValidationBehavior>()
    .ForResourceType("MyResource", type => type
        .AddHandler<MyResourceCreateOrUpdateHandler>()));
```

Behaviors can be registered at three levels:
- **Global:** `builder.Services.AddBicepExtensionGlobalHandlerBehavior<T>()` also wraps unsupported-version responses.
- **Extension-scoped:** `extension.AddHandlerBehavior<T>()` runs for handlers in this managed extension.
- **Resource-type-scoped:** `type.AddHandlerBehavior<T>()` runs only for handlers of that resource type.

## Next Steps

- [Typed Handlers](typed-handlers.md): use strongly typed models instead of raw `JsonObject`.
- [Behaviors](behaviors.md): add cross-cutting concerns like validation and logging.
- [Validators](validators.md): validate requests with a fluent DSL.
- Read the [API Contract](../contract/contract.md) for the complete protocol specification.
- Explore the [Magic 8-Ball sample](https://github.com/Azure/bicep-extensibility/tree/main/sample/MagicEightBallExtension) for a full working example covering all 5 endpoints.
- Review [Managed hosting](../sdks/managed.md) for identity, routes, and application integration.
