# Azure.Deployments.Extensibility.AspNetCore

Shared ASP.NET Core base SDK for Bicep extension hosts. It provides immutable handler
registration, request dispatch, middleware, contract endpoint mapping, typed handlers,
behaviors, and a development Scalar explorer.

Public extension applications should normally reference
`Azure.Deployments.Extensibility.Hosting.Managed`. Managed and first-party hosting
packages wrap this base SDK and own their respective version-selection and hosting
policies.

Custom hosts can compose the base directly:

```csharp
builder.Services.AddBicepExtensionServices();

var registration = BicepExtensionRegistration.Create(
    builder.Services,
    extension => extension.ForResourceType(
        "Widget",
        resource => resource.AddHandler<WidgetGetHandler>()));

builder.Services.AddSingleton(registration);
builder.Services.AddSingleton<IBicepExtensionResolver, CustomResolver>();

var app = builder.Build();
app.UseBicepExtensionMiddlewares();
app.MapBicepExtensionEndpoints();
app.Run();
```

The host-provided `IBicepExtensionResolver` selects an immutable registration for the
exact route version. The base SDK itself does not impose a version-range policy.

See the [SDK documentation](https://azure.github.io/bicep-extensibility/sdks/aspnetcore.html)
and [API contract](https://azure.github.io/bicep-extensibility/contract/contract.html).
