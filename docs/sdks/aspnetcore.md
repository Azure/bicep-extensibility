# AspNetCore SDK

**Package:** `Azure.Deployments.Extensibility.AspNetCore`

> [!NOTE]
> This SDK is currently intended for **first-party (Microsoft-internal) extension authors**. A wrapper SDK for third-party and local extension development will be published separately.

The AspNetCore SDK provides the hosting layer for Bicep extensions. It handles JSON serialization, correlation headers, culture propagation, endpoint routing, and the behavior pipeline automatically.

## ExtensionApplication

The entry point for every extension. A thin wrapper around `WebApplicationBuilder` that adds Bicep-specific middleware and routing.

```csharp
ExtensionApplication.Create(args)
    .AddExtensionVersion("1.*.*", version => version
        .ForResourceType("MyResource", type => type
            .AddHandler<MyPreviewHandler>()
            .AddHandler<MyCreateOrUpdateHandler>()
            .AddHandler<MyGetHandler>()
            .AddHandler<MyDeleteHandler>()))
    .Run();
```

### Key methods

| Method | Description |
|--------|-------------|
| `AddExtensionVersion(range, configure)` | Registers handlers for a semantic version range (e.g., `">=1.0.0 <2.0.0"` or `"1.*.*"`). |
| `ForResourceType(name, configure)` | Scopes handlers to a specific resource type. |
| `AddHandler<T>()` | Registers a handler. The SDK detects the operation from the interface the handler implements. |
| `ConfigureServices(configure)` | Access the underlying `IServiceCollection` for DI registration. |
| `AddGlobalHandlerBehavior<T>()` | Registers a behavior that runs for every handler across all versions. |
| `EnableDevelopmentScalarApiExplorer(configure)` | Enables the Scalar API explorer UI in development mode. |
| `Build()` | Builds and returns the `WebApplication` without starting it. Use this when hosting inside Service Fabric or other custom hosts. |
| `Run()` / `RunAsync()` | Builds and runs the application. Does not return until shutdown. |

### Handler resolution

When a request arrives, the SDK:

1. Extracts the extension version from the URL path (`/{version}/resource/{operation}`).
2. Matches the version against registered `SemVersionRange` patterns.
3. Looks up a handler for the request's resource type and operation.
4. Falls back to version-scoped (non-resource-type-specific) handlers if no resource-type handler is found.

### Generic (fallback) handlers

Handlers registered with `AddHandler<T>()` on the `ExtensionVersionBuilder` (outside `ForResourceType`) act as **generic handlers** — they match any resource type that doesn't have a type-specific handler:

```csharp
app.AddExtensionVersion("1.*.*", version => version
    // Generic handler — handles all resource types without a specific handler.
    .AddHandler<FallbackHandler>()

    // Resource-type-specific — takes priority for "Widget" resources.
    .ForResourceType("Widget", type => type
        .AddHandler<WidgetCreateOrUpdateHandler>()));
```

This is useful for operations that are resource-type-agnostic, such as LRO polling handlers.

## Typed handler base classes

For extensions with strongly-typed resource models, the SDK provides abstract base classes that handle JSON serialization automatically:

| Base class | Operation | Input | Output |
|-----------|-----------|-------|--------|
| `TypedResourcePreviewHandler<TProperties, TIdentifiers>` | Preview | `TypedResourcePreviewSpecification` | `TypedResourcePreview` |
| `TypedResourceCreateOrUpdateHandler<TProperties, TIdentifiers>` | Create/Update | `TypedResourceSpecification` | `TypedResource` |
| `TypedResourceGetHandler<TProperties, TIdentifiers>` | Get | `TypedResourceReference` | `TypedResource?` |
| `TypedResourceDeleteHandler<TProperties, TIdentifiers>` | Delete | `TypedResourceReference` | `TypedResource?` |

All four also have a 3-type-parameter variant (`<TProperties, TIdentifiers, TConfig>`) when you need typed configuration.

See the [Typed Handlers tutorial](../tutorials/typed-handlers.md) for a step-by-step guide.

## Behavior pipeline

Behaviors are decorators that wrap handler invocations. They execute in registration order: **global → version-scoped → resource-type-scoped**.

Each operation has a dedicated behavior interface:

| Interface | Operation |
|-----------|-----------|
| `IResourcePreviewBehavior` | Preview |
| `IResourceCreateOrUpdateBehavior` | Create/Update |
| `IResourceGetBehavior` | Get |
| `IResourceDeleteBehavior` | Delete |
| `ILongRunningOperationGetBehavior` | LRO polling |

A single class can implement multiple interfaces to apply the same logic across operations.

### Registration scopes

```csharp
// Global — runs for every handler, every version.
app.AddGlobalHandlerBehavior<LoggingBehavior>();

// Version-scoped — runs for handlers in this version range.
app.AddExtensionVersion("1.*.*", version => version
    .AddHandlerBehavior<ApiVersionValidationBehavior>()
    ...);

// Resource-type-scoped — runs only for handlers of this resource type.
    .ForResourceType("MyResource", type => type
        .AddHandlerBehavior<MyResourceSpecificBehavior>()
        ...);
```

See the [Behaviors tutorial](../tutorials/behaviors.md) for a full walkthrough.

## Built-in behaviors

| Behavior | Scope | Description |
|----------|-------|-------------|
| `ErrorResponseExceptionHandlingBehavior` | Global (auto-registered) | Catches `ErrorResponseException` and converts it to `ErrorResponse`. |
| `FakeValueSubstitutionPreviewRewriter` | — | Preview rewriter that substitutes unevaluated expressions with fake placeholders. |
| `PreviewRewriteBehavior` | — | Adapter that wraps an `IResourcePreviewRewriter` as an `IResourcePreviewBehavior`. |
| `UnevaluatedPreviewNotSupportedBehavior` | — | Rejects preview requests containing unevaluated expressions. |

## Scalar API explorer

Enable an interactive API explorer in development mode:

```csharp
app.EnableDevelopmentScalarApiExplorer(explorer => explorer
    .WithTitle("My Extension API")
    .WithExtensionVersions("1.0.0", "2.0.0")
    .ConfigureExamples(examples => { ... }));
```

The explorer serves the embedded OpenAPI spec and renders it with [Scalar](https://scalar.com/).
