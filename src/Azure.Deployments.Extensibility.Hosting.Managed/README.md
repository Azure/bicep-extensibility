# Azure.Deployments.Extensibility.Hosting.Managed

Managed ASP.NET Core host for public Bicep extensions. It configures the shared
`Azure.Deployments.Extensibility.AspNetCore` runtime for one exact extension version.

Set the extension identity in the application project:

```xml
<PropertyGroup>
  <BicepExtensionName>Contoso.Widgets</BicepExtensionName>
  <BicepExtensionVersion>1.0.0</BicepExtensionVersion>
</PropertyGroup>
```

Register handlers and map the managed host:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddBicepExtension(extension => extension
    .ForResourceType("Widget", resource => resource
        .AddHandler<WidgetPreviewHandler>()
        .AddHandler<WidgetCreateOrUpdateHandler>()
        .AddHandler<WidgetGetHandler>()
        .AddHandler<WidgetDeleteHandler>()));

var app = builder.Build();
app.UseBicepExtension();
app.Run();
```

The host maps the Bicep contract at `/{extensionVersion}/...` and health checks at
`GET /ping`. The requested extension version must exactly match `BicepExtensionVersion`.
