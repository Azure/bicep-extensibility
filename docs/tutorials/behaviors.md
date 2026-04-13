# Behaviors

Behaviors are pipeline decorators that wrap handler invocations. Use them for cross-cutting concerns like validation, logging, authorization, or error handling.

## How behaviors work

A behavior receives the request and a `next` delegate. Call `next` to continue the pipeline, or return early to short-circuit:

```
Request → [Behavior A] → [Behavior B] → [Handler] → Response
                ↑              ↑              ↑
            can inspect    can inspect    does the work
            can modify     can modify
            can short-     can short-
            circuit        circuit
```

Behaviors execute in registration order: **global → version-scoped → resource-type-scoped**.

## Operation-specific interfaces

Each operation has a dedicated behavior interface. A single class can implement multiple interfaces to apply the same logic across operations.

| Interface | Operation | Request type | Response type |
|-----------|-----------|-------------|---------------|
| `IResourcePreviewBehavior` | Preview | `ResourcePreviewSpecification` | `OneOf<ResourcePreview, ErrorResponse>` |
| `IResourceCreateOrUpdateBehavior` | Create/Update | `ResourceSpecification` | `OneOf<Resource, LRO, ErrorResponse>` |
| `IResourceGetBehavior` | Get | `ResourceReference` | `OneOf<Resource?, ErrorResponse>` |
| `IResourceDeleteBehavior` | Delete | `ResourceReference` | `OneOf<Resource?, LRO, ErrorResponse>` |
| `ILongRunningOperationGetBehavior` | LRO poll | `JsonObject` | `OneOf<LRO, ErrorResponse>` |

## Example: API version validation

This behavior validates that the incoming `apiVersion` is one of the accepted values. It implements four operation interfaces and short-circuits with an error if validation fails.

```csharp
public sealed class ApiVersionValidationBehavior :
    IResourcePreviewBehavior,
    IResourceCreateOrUpdateBehavior,
    IResourceGetBehavior,
    IResourceDeleteBehavior
{
    private readonly IReadOnlySet<string> acceptedApiVersions;

    public ApiVersionValidationBehavior(params string[] acceptedApiVersions)
    {
        this.acceptedApiVersions = new HashSet<string>(
            acceptedApiVersions, StringComparer.OrdinalIgnoreCase);
    }

    Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> IResourceCreateOrUpdateBehavior.HandleAsync(
        ResourceSpecification request,
        ResourceCreateOrUpdateHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        if (this.Validate(request.ApiVersion) is { } error)
        {
            return Task.FromResult<OneOf<Resource, LongRunningOperation, ErrorResponse>>(error);
        }

        return next(request);
    }

    // Implement the same pattern for IResourcePreviewBehavior,
    // IResourceGetBehavior, and IResourceDeleteBehavior...

    private ErrorResponse? Validate(string? apiVersion) =>
        apiVersion is null || !this.acceptedApiVersions.Contains(apiVersion)
            ? new ErrorResponse(new Error
            {
                Code = "UnsupportedApiVersion",
                Message = $"API version '{apiVersion}' is not supported.",
                Target = Json.Pointer.JsonPointer.Parse("/apiVersion"),
            })
            : null;
}
```

## Example: Response logging

This behavior logs the result of every handler invocation. It wraps the call (doesn't short-circuit):

```csharp
public sealed partial class ResponseLoggingBehavior :
    IResourcePreviewBehavior,
    IResourceCreateOrUpdateBehavior,
    IResourceGetBehavior,
    IResourceDeleteBehavior,
    ILongRunningOperationGetBehavior
{
    private readonly ILogger<ResponseLoggingBehavior> logger;

    public ResponseLoggingBehavior(ILogger<ResponseLoggingBehavior> logger)
    {
        this.logger = logger;
    }

    async Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> IResourceCreateOrUpdateBehavior.HandleAsync(
        ResourceSpecification request,
        ResourceCreateOrUpdateHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        var response = await next(request);

        response.Switch(
            resource => this.logger.LogInformation("Created '{Type}'.", request.Type),
            lro => this.logger.LogInformation("Started LRO for '{Type}'.", request.Type),
            error => this.logger.LogWarning("Failed '{Type}': {Error}.", request.Type, error.Error.Message));

        return response;
    }

    // Implement the same pattern for other interfaces...
}
```

## Registration

### Global behaviors

Run for **every handler** across all extension versions:

```csharp
app.AddGlobalHandlerBehavior<ResponseLoggingBehavior>();
```

### Version-scoped behaviors

Run for handlers in a specific version range:

```csharp
app.AddExtensionVersion("1.*.*", version => version
    .AddHandlerBehavior(sp => new ApiVersionValidationBehavior("2024-01-01", "2024-01-01-preview"))
    ...);
```

### Resource-type-scoped behaviors

Run only for handlers of a specific resource type:

```csharp
.ForResourceType("Widget", type => type
    .AddHandlerBehavior<WidgetAuthorizationBehavior>()
    .AddHandler<WidgetCreateOrUpdateHandler>()
    ...);
```

### Factory registration

Use the factory overload when your behavior needs constructor parameters that aren't in DI:

```csharp
.AddHandlerBehavior(sp => new ApiVersionValidationBehavior("2024-01-01"))
```

Or use the generic overload when all dependencies are in DI:

```csharp
.AddHandlerBehavior<ResponseLoggingBehavior>()
```

## Execution order

Given this registration:

```csharp
app.AddGlobalHandlerBehavior<LoggingBehavior>();

app.AddExtensionVersion("1.*.*", version => version
    .AddHandlerBehavior<ApiVersionBehavior>()
    .ForResourceType("Widget", type => type
        .AddHandlerBehavior<WidgetAuthBehavior>()
        .AddHandler<WidgetCreateOrUpdateHandler>()));
```

A create-or-update request for version `1.0.0` and type `Widget` executes:

```
LoggingBehavior → ApiVersionBehavior → WidgetAuthBehavior → WidgetCreateOrUpdateHandler
```

## Next steps

- [Typed Handlers](typed-handlers.md) — use strongly-typed models in your handlers.
- [Validators](validators.md) — validate request models with a fluent DSL.
