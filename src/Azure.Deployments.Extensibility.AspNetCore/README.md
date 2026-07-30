# Azure.Deployments.Extensibility.AspNetCore

Version-free ASP.NET Core runtime for [Bicep extension](https://github.com/Azure/bicep-extensibility) hosting packages. It provides immutable handler registration, middleware, request routing, typed handlers, and behavior dispatch on top of the Core SDK.

Third-party and local extension authors should use `Azure.Deployments.Extensibility.Hosting.Managed`, which configures this package with project identity, exact-version dispatch, startup validation, and health checks.

## Hosting package composition

```csharp
builder.Services.AddBicepExtensionServices();

var registration = BicepExtensionRegistration.Create(
    builder.Services,
    extension => extension
        .ForResourceType("Fortune", resourceType => resourceType
            .AddHandler<FortunePreviewHandler>()
            .AddHandler<FortuneCreateOrUpdateHandler>()
            .AddHandler<FortuneGetHandler>()
            .AddHandler<FortuneDeleteHandler>()));

builder.Services.AddSingleton<IBicepExtensionResolver>(
    new HostingPolicyResolver(registration));

var app = builder.Build();
app.UseBicepExtensionMiddlewares();
app.MapBicepExtensionEndpoints();
```

The host owns version policy through `IBicepExtensionResolver`. The base runtime treats the route version as an opaque string.

## Key concepts

### Handler registration

Register handlers in one immutable, version-independent registration. The runtime detects which handler interfaces each class implements (`IResourcePreviewHandler`, `IResourceCreateOrUpdateHandler`, `IResourceGetHandler`, `IResourceDeleteHandler`, `ILongRunningOperationGetHandler`).

```csharp
var registration = BicepExtensionRegistration.Create(services, extension => extension
    .AddHandler<FallbackHandler>()
    .ForResourceType("Employee", type => type
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

Behaviors wrap handler invocations for cross-cutting concerns such as validation, logging, or authorization. They execute in order: **global → registration-scoped → resource-type-scoped**.

```csharp
services.AddBicepExtensionGlobalHandlerBehavior<LoggingBehavior>();

var registration = BicepExtensionRegistration.Create(services, extension => extension
    .AddHandlerBehavior<ApiVersionValidationBehavior>()
    .ForResourceType("Fortune", type => type
        .AddHandlerBehavior<FortuneAuthorizationBehavior>()
        .AddHandler<FortuneCreateOrUpdateHandler>()));
```

Implement `IResourcePreviewBehavior`, `IResourceCreateOrUpdateBehavior`, `IResourceGetBehavior`, `IResourceDeleteBehavior`, or `ILongRunningOperationGetBehavior` as needed. A single class can implement multiple behavior interfaces.

### Scalar API explorer

Hosting packages can reuse the shared development-time Scalar UI and OpenAPI document:

```csharp
BicepExtensionApiExplorer.MapDevelopment(app, explorer => explorer
    .WithTitle("My Extension API")
    .WithExtensionVersions("1.0.0", "2.0.0")
    .ConfigureExamples(MyExtensionExamples.Configure));
```

The host supplies the version examples according to its routing policy. Managed hosting supplies its exact assembly version automatically; FirstParty hosting can supply all supported version examples.

### Request headers

Read extensibility request headers using `HttpContextExtensions`:

```csharp
var clientRequestId = httpContext.GetClientRequestId();
var correlationId = httpContext.GetCorrelationRequestId();
var tenantId = httpContext.GetClientTenantId();
```

## Documentation

- [Bicep Extension API Contract](https://github.com/Azure/bicep-extensibility/blob/main/docs/contract/contract.md)
- [Preview Operation](https://github.com/Azure/bicep-extensibility/blob/main/docs/contract/preview-operation.md)
- [Asynchronous Operations](https://github.com/Azure/bicep-extensibility/blob/main/docs/contract/async-operations.md)
- [Sample Extension (Magic Eight Ball)](https://github.com/Azure/bicep-extensibility/tree/main/sample/MagicEightBallExtension)

## License

This project is licensed under the [MIT License](https://github.com/Azure/bicep-extensibility/blob/main/LICENSE).
