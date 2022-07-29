// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.DevHost.Swagger;
using Azure.Deployments.Extensibility.DevHost.Extensions;
using Azure.Deployments.Extensibility.DevHost.Middlewares;
using Azure.Deployments.Extensibility.DevHost.Registries;
using Json.Path;
using Json.Pointer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Deployments.Extensibility.Core.Json;

var builder = WebApplication.CreateBuilder(args);

// Set default JsonOptions.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    var properties = typeof(JsonSerializerOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    foreach (var property in properties)
    {
        if (property is not null && property.CanWrite)
        {
            var defaultValue = property.GetValue(ExtensibilityJsonSerializer.Default.Options);
            property.SetValue(options.JsonSerializerOptions, defaultValue);
        }
    }
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Deployments Extensibility Provider API",
        Description = "Deployments extensibility provider OpenAPI specification.",
    });

    options.EnableAnnotations();
    options.SupportNonNullableReferenceTypes();
    options.ExampleFilters();
    options.UseAllOfToExtendReferenceSchemas();
    options.UseAllOfForInheritance();
    options.UseOneOfForPolymorphism();
    options.DocumentFilter<SchemaPolishDocumentFilter>();
    options.CustomSchemaIds(type =>
    {
        if (type.Name == typeof(ExtensibleImport<JsonElement>).Name)
        {
            return nameof(ExtensibleImport<JsonElement>);
        }

        if (type.Name == typeof(ExtensibleResource<JsonElement>).Name)
        {
            return nameof(ExtensibleResource<JsonElement>);
        }

        return type.Name;
    });

    options.MapType<JsonElement>(() => new OpenApiSchema { Type = "object" });
    options.MapType<JsonPath>(() => new OpenApiSchema
    {
        Reference = new OpenApiReference
        {
            ExternalResource = "#/components/schemas/JsonPath",
        },
    });
    options.MapType<JsonPointer>(() => new OpenApiSchema
    {
        Reference = new OpenApiReference
        {
            ExternalResource = "#/components/schemas/JsonPointer",
        },
    });

    if (Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) is { } directory)
    {
        foreach (var filePath in Directory.GetFiles(directory, "*.xml"))
        {
            options.IncludeXmlComments(filePath);
        }
    }
});

builder.Services.AddSwaggerExamplesFromAssemblyOf<ExtensibilityOperationResponseExampleProvider>();
builder.Services.AddSingleton<IExtensibilityProviderRegistry, FirstPartyExtensibilityProviderRegistry>();

var app = builder.Build();

app.UseReDoc(options =>
{
    options.DocumentTitle = "Deployment Extensibility Provider API Documentation";
    options.SpecUrl = "/swagger/v1/swagger.json";
});

app.UsePathBase("/api");
app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseExtensibilityExceptionHandler();
app.UseEndpoints(endpoints => endpoints.MapControllers());

app.Run();
