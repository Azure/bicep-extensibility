# Azure.Deployments.Extensibility.AspNetCore

ASP.NET Core hosting framework for building [Bicep extensions](https://github.com/Azure/bicep-extensibility). Provides a fluent API for handler registration, middleware pipeline, request routing, and behavior (decorator) chains on top of the [Core SDK](https://github.com/Azure/bicep-extensibility/tree/main/src/Azure.Deployments.Extensibility.Core).

## Quick start

```csharp
using Azure.Deployments.Extensibility.AspNetCore;

ExtensionApplication.Create(args)
    .AddExtensionVersion("1.*.*", version => version
        .ForResourceType("Fortune", type => type
            .AddHandler<FortunePreviewHandler>()
            .AddHandler<FortuneCreateOrUpdateHandler>()
            .AddHandler<FortuneGetHandler>()
            .AddHandler<FortuneDeleteHandler>()))
    .Run();
```

`ExtensionApplication` handles JSON serialization, error handling, correlation headers, culture propagation, and endpoint routing automatically.

## Key concepts

### Handler registration

Register handlers by extension version range and optional resource type. The framework auto-detects which handler interfaces each class implements (`IResourcePreviewHandler`, `IResourceCreateOrUpdateHandler`, `IResourceGetHandler`, `IResourceDeleteHandler`, `ILongRunningOperationGetHandler`).

```csharp
app.AddExtensionVersion(">=1.0.0 <2.0.0", version => version
    .AddHandler<FallbackHandler>()                    // default handler for this version range
    .ForResourceType("Employee", type => type         // resource-type-specific handlers
        .AddHandler<EmployeePreviewHandler>()
        .AddHandler<EmployeeCreateOrUpdateHandler>()));
```

### Typed handler base classes

Extend one of the typed base classes to work with strongly-typed models instead of raw `JsonObject`:

| Base class | Operation |
|-----------|-----------|
| `TypedResourcePreviewHandler<TProperties, TIdentifiers>` | Preview |
| `TypedResourceCreateOrUpdateHandler<TProperties, TIdentifiers>` | Create or update |
| `TypedResourceGetHandler<TProperties, TIdentifiers>` | Get |
| `TypedResourceDeleteHandler<TProperties, TIdentifiers>` | Delete |

Each base class automatically deserializes the request and serializes the response using the ASP.NET Core `JsonOptions`.

### Behaviors (decorators)

Behaviors wrap handler invocations for cross-cutting concerns such as validation, logging, or authorization. They execute in order: **global → version-scoped → resource-type-scoped**.

```csharp
// Global — runs on every handler invocation.
app.AddGlobalHandlerBehavior<LoggingBehavior>();

app.AddExtensionVersion("1.*.*", version => version
    // Version-scoped — runs on all handlers in this version range.
    .AddHandlerBehavior<ApiVersionValidationBehavior>()
    .ForResourceType("Fortune", type => type
        // Resource-type-scoped — runs only on Fortune handlers.
        .AddHandlerBehavior<FortuneAuthorizationBehavior>()
        .AddHandler<FortuneCreateOrUpdateHandler>()));
```

Implement `IResourcePreviewBehavior`, `IResourceCreateOrUpdateBehavior`, `IResourceGetBehavior`, `IResourceDeleteBehavior`, or `ILongRunningOperationGetBehavior` as needed. A single class can implement multiple behavior interfaces.

### Scalar API explorer

Enable a development-time API explorer powered by [Scalar](https://scalar.com/) with request/response examples:

```csharp
app.EnableDevelopmentScalarApiExplorer(explorer => explorer
    .WithTitle("My Extension API")
    .WithExtensionVersions("1.0.0", "2.0.0")
    .ConfigureExamples(examples => examples
        .ForPreview(requestExample, responseExample)
        .ForCreateOrUpdate(requestExample, responseExample)));
```

### Request headers

Read extensibility request headers using `HttpContextExtensions`:

```csharp
var clientRequestId = httpContext.GetClientRequestId();
var correlationId = httpContext.GetCorrelationRequestId();
var tenantId = httpContext.GetClientTenantId();
```

## Documentation

- [Bicep Extension API Contract v2](https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/contract.md)
- [Preview Operation](https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/preview-operation.md)
- [Asynchronous Operations](https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/async-operations.md)
- [Sample Extension (Magic Eight Ball)](https://github.com/Azure/bicep-extensibility/tree/main/sample/MagicEightBallExtension)

## License

This project is licensed under the [MIT License](https://github.com/Azure/bicep-extensibility/blob/main/LICENSE).
