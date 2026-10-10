# Azure.Deployments.Extensibility.Hosting.Managed

ASP.NET Core hosting SDK for third-party and local Bicep extensions.

Declare the extension name and version in the application project:

```xml
<PropertyGroup>
  <BicepExtensionName>Widget</BicepExtensionName>
  <BicepExtensionVersion>1.0.0</BicepExtensionVersion>
</PropertyGroup>
```

Register one immutable handler set and map the managed extension endpoints:

```csharp
using Azure.Deployments.Extensibility.Hosting.Managed.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBicepExtension(extension => extension
    .ForResourceType("Widget", resourceType => resourceType
        .AddHandler<WidgetGetHandler>()));

var app = builder.Build();
app.MapBicepExtension();
app.MapManagedScalarApiExplorer();
await app.RunAsync();
```
