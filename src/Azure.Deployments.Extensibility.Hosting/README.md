# Azure.Deployments.Extensibility.Hosting

The Hosting SDK provides a public, standard-host wrapper around the shared Bicep extensibility ASP.NET Core runtime for third-party and local extension authors.

## Quick start

```csharp
using Microsoft.AspNetCore.Builder;
using Azure.Deployments.Extensibility.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddBicepExtension("1.0.0", extension => extension
    .ForResourceType("MyResource", resourceType => resourceType
        .AddHandler<MyCreateOrUpdateHandler>()
        .AddHandler<MyGetHandler>()
        .AddHandler<MyDeleteHandler>()
        .AddHandler<MyPreviewHandler>()));

var app = builder.Build();
app.UseBicepExtension();
await app.RunAsync();
```

## Assembly metadata

The Hosting SDK reads the extension identity from the entry assembly's `BicepExtensionIdentity` assembly metadata. Add the following to your extension project:

```xml
<ItemGroup>
  <AssemblyMetadata Include="BicepExtensionIdentity" Value="MyExtension" />
</ItemGroup>
```
