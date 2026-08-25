# Bicep Extension API Explorer

The AspNetCore base SDK provides a shared development-time API Explorer used by both
Hosting.Managed and Hosting.FirstParty. It serves the embedded OpenAPI contract and a
Scalar UI for exercising extension endpoints.

## Map the explorer

Call `MapBicepExtensionApiExplorer` after building the application:

```csharp
var app = builder.Build();

app.UseBicepExtension();
app.MapBicepExtensionApiExplorer(
    configureExamples: WidgetExamples.Configure,
    title: "Widget Extension API",
    extensionVersions: ["1.0.0"]);

app.Run();
```

The explorer routes are mapped only when the ASP.NET Core environment is
`Development`. The default UI is available at `/scalar/v2`, and the generated OpenAPI
document is available at `/openapi/v2.json`.

For a FirstParty host, list every version that developers should be able to select in
the explorer. For a Managed host, pass the exact version declared by
`BicepExtensionVersion`.

## Add operation examples

Use `OpenApiExamplesBuilder` to add extension-specific request and response examples:

```csharp
internal static class WidgetExamples
{
    public static void Configure(OpenApiExamplesBuilder examples)
    {
        examples.ForGet(
            name: "existing widget",
            request: getRequest,
            response: getResponse);
    }
}
```

Examples improve local testing but do not change runtime request handling.
