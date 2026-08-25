# Managed SDK

**Package:** `Azure.Deployments.Extensibility.Hosting.Managed`

> [!WARNING]
> This SDK is under active development and is not ready for extension authors
> to consume. The package and API described here are for early evaluation only.

The Managed SDK targets extensions that run in the managed extension runtime. External
authors and Microsoft teams can both choose this model. The SDK wraps the
[AspNetCore base SDK](aspnetcore.md), accepts one exact extension version per process,
and configures the standard middleware, contract routes, and `GET /ping` health endpoint.

> [!TIP]
> If you are building your first managed extension, begin with
> [Build a managed extension](../tutorials/getting-started.md), then return here for
> hosting details.

## Configure extension identity

Set the name and exact version in the extension application's project file. The package
emits these values as assembly metadata and reads them when the application starts.

```xml
<PropertyGroup>
  <BicepExtensionName>Contoso.Widgets</BicepExtensionName>
  <BicepExtensionVersion>1.0.0</BicepExtensionVersion>
</PropertyGroup>
```

Both properties are required. `BicepExtensionVersion` is compared to the route value
using an ordinal, exact comparison; it is not a semantic-version range.

## Configure the host

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<WidgetStore>();
builder.Services.AddBicepExtensionGlobalHandlerBehavior<LoggingBehavior>();

builder.AddBicepExtension(extension => extension
    .AddHandler<LongRunningOperationGetHandler>()
    .AddHandlerBehavior<ValidationBehavior>()
    .ForResourceType("Widget", resource => resource
        .AddHandler<WidgetPreviewHandler>()
        .AddHandler<WidgetCreateOrUpdateHandler>()
        .AddHandler<WidgetGetHandler>()
        .AddHandler<WidgetDeleteHandler>()));

var app = builder.Build();

// Host-owned middleware and endpoints can be added normally.
app.UseAuthentication();
app.UseBicepExtension();
app.MapGet("/status", () => Results.Ok());

app.Run();
```

`AddBicepExtension` and `UseBicepExtension` must each be called exactly once.
The application remains a normal ASP.NET Core application, so it can register arbitrary
services, middleware, and endpoints.

## Routes

The managed host maps:

- `POST /{extensionVersion}/resource/preview`
- `POST /{extensionVersion}/resource/createOrUpdate`
- `POST /{extensionVersion}/resource/get`
- `POST /{extensionVersion}/resource/delete`
- `POST /{extensionVersion}/longRunningOperation/get`
- `GET /ping`

Requests for any version other than `BicepExtensionVersion` receive the contract's
`UnsupportedExtensionVersion` response.

## Development API explorer

The Scalar explorer remains part of the AspNetCore base SDK and can be added before
the application starts:

```csharp
app.MapBicepExtensionApiExplorer(
    configureExamples: WidgetExamples.Configure,
    title: "Widget Extension API",
    extensionVersions: ["1.0.0"]);
```

The routes are mapped only in the Development environment.
Both hosting SDKs use this shared AspNetCore API, so the explorer behavior and contract
remain consistent.
