# Hosting SDK

The Hosting SDK is the public-facing wrapper for third-party and local Bicep extension authors. It builds on the shared AspNetCore runtime and provides a standard-host experience with one exact extension version, health checks, and a fixed `/ping` endpoint.

> [!WARNING]
> This SDK is still a work in progress. The public authoring experience is not yet ready for broad extension-author consumption.

## Package

- Package: `Azure.Deployments.Extensibility.Hosting`
- Audience: third-party and local extension authors
- Base dependency: `Azure.Deployments.Extensibility.AspNetCore`

## Who should read this?

Read this page if you are building a public or local Bicep extension and want the standard-host entry points for registering handlers and wiring the extension into ASP.NET Core.

If you are on a Microsoft-internal (1P) team, this page is still useful as background on the public authoring model. Teams that self-host their extension service should use `Azure.Deployments.Extensibility.Hosting.FirstParty`, while teams that want the managed runtime should use `Azure.Deployments.Extensibility.Hosting.Managed`. Your primary implementation reference should be the shared [AspNetCore runtime reference](aspnetcore.md) plus the relevant internal wrapper docs when they are available.

## Configure the host

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

## Development API explorer

You can also enable the development-time Scalar API explorer for local authoring and testing:

```csharp
var app = builder.Build();
app.UseBicepExtension();
app.EnableDevelopmentScalarApiExplorer(explorer => explorer
    .WithTitle("My Extension API")
    .ConfigureExamples(examples => examples
        .ForPreview(new { type = "MyResource" }, new { type = "MyResource" })));
await app.RunAsync();
```

The explorer is only served when the app is running in the Development environment.

## Notes

- The Hosting SDK uses one exact extension version and compares route versions ordinally.
- The aggregate host wires shared middleware, the bare contract endpoints, `/healthz`, and `/ping` for you.
