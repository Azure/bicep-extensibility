# AspNetCore SDK

**Package:** `Azure.Deployments.Extensibility.AspNetCore`

> [!WARNING]
> This base SDK is under active development and is not ready for hosting SDK or
> extension authors to adopt. Its public surface may change before release.

The AspNetCore SDK is the shared base for Bicep extension hosting. It provides
version-independent handler registrations, dispatch, contract endpoints, middleware,
typed handler base classes, behaviors, and development tooling.

> [!IMPORTANT]
> This package is for hosting SDK and custom-host authors. Extension teams should use
> [Hosting.Managed](managed.md) when the platform runs the extension. Microsoft teams
> should use Hosting.FirstParty only for a
> [self-hosted FirstParty service](../get-started/first-party.md).

Hosting.Managed wraps these APIs with exact-version selection and standard hosting
defaults. The closed-source FirstParty SDK also builds on this package and owns its
first-party version-range and identity policies.

## Host composition

Custom hosts compose the base APIs explicitly:

```csharp
builder.Services.AddBicepExtensionServices();
builder.Services.AddBicepExtensionGlobalHandlerBehavior<LoggingBehavior>();

var registration = BicepExtensionRegistration.Create(
    builder.Services,
    extension => extension
        .AddHandler<LongRunningOperationGetHandler>()
        .ForResourceType("Widget", resource => resource
            .AddHandler<WidgetGetHandler>()));

builder.Services.AddSingleton(registration);
builder.Services.AddSingleton<IBicepExtensionResolver, CustomExtensionResolver>();

var app = builder.Build();
app.UseBicepExtensionMiddlewares();
app.MapBicepExtensionEndpoints();
```

An `IBicepExtensionResolver` supplies the immutable registration for a requested
route version. Version-selection policy belongs to the wrapping host; AspNetCore
does not require a semantic-version range strategy.

The granular helpers (`AddBicepExtensionJsonOptions`,
`AddBicepExtensionHandlerRuntime`, `UseBicepExtensionRequestCorrelation`,
`MapResourceActions`, and others) support hosts that need precise ordering or route
groups.

## Handler registration

`BicepExtensionRegistration.Create` accepts an `IBicepExtensionBuilder`.

```csharp
var registration = BicepExtensionRegistration.Create(services, extension => extension
    .AddHandler<FallbackGetHandler>()
    .AddHandlerBehavior<ValidationBehavior>()
    .ForResourceType("Widget", resource => resource
        .AddHandler<WidgetCreateOrUpdateHandler>()
        .AddHandlerBehavior<WidgetAuthorizationBehavior>()));
```

Handlers directly on the extension builder are fallbacks. Resource-type handlers
take precedence and resource type matching is case-insensitive. Long-running-operation
handlers are always extension-scoped.

## Typed handlers

| Base class | Operation |
|-----------|-----------|
| `TypedResourcePreviewHandler<TProperties, TIdentifiers>` | Preview |
| `TypedResourceCreateOrUpdateHandler<TProperties, TIdentifiers>` | Create or update |
| `TypedResourceGetHandler<TProperties, TIdentifiers>` | Get |
| `TypedResourceDeleteHandler<TProperties, TIdentifiers>` | Delete |

Each also has a three-type-parameter form for typed configuration. See
[Typed Handlers](../tutorials/typed-handlers.md).

## Behaviors

Behaviors decorate operation handlers. Global behaviors are registered on
`IServiceCollection` and also wrap unsupported-version results. Registration-level
and resource-type behaviors are configured through `IBicepExtensionBuilder`.
They execute in this order:

1. Global behaviors
2. Extension registration behaviors
3. Resource type behaviors
4. Handler

See [Behaviors](../tutorials/behaviors.md).

## Scalar API explorer

Custom and managed hosts can map the shared development explorer:

```csharp
app.MapBicepExtensionApiExplorer(
    configureExamples: WidgetExamples.Configure,
    title: "Widget Extension API",
    extensionVersions: ["1.0.0"]);
```

It maps the embedded contract and Scalar UI only in the Development environment.
Both `Hosting.Managed` and `Hosting.FirstParty` should expose this shared AspNetCore
extension directly so their explorer behavior and OpenAPI document remain consistent.
