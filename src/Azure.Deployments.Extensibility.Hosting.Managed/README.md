# Azure.Deployments.Extensibility.Hosting.Managed

Managed (third-party) hosting SDK for building Bicep extensions on ASP.NET Core.

## Overview

`Azure.Deployments.Extensibility.Hosting.Managed` provides turnkey hosting for single-version Bicep extensions. It wraps `Azure.Deployments.Extensibility.AspNetCore`, automating extension identity discovery, standard host integration, health checks (`GET /ping`), middleware configuration, and contract endpoint mapping.

## Getting Started

### 1. Configure MSBuild Properties

Set your extension's name and version in your `.csproj`:

```xml
<PropertyGroup>
  <BicepExtensionName>MyExtension</BicepExtensionName>
  <BicepExtensionVersion>1.0.0</BicepExtensionVersion>
</PropertyGroup>
```

### 2. Configure the Application

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Register application services
builder.Services.AddSingleton<IMyService, MyService>();

// Register Bicep extension handlers
builder.AddBicepExtension(extension => extension
    .ForResourceType("MyResource", resourceType => resourceType
        .AddHandler<MyResourcePreviewHandler>()
        .AddHandler<MyResourceCreateOrUpdateHandler>()
        .AddHandler<MyResourceGetHandler>()
        .AddHandler<MyResourceDeleteHandler>()));

var app = builder.Build();

// Configure the Managed Bicep extension pipeline and endpoints
app.UseBicepExtension();

await app.RunAsync();
```
