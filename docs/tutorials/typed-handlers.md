# Typed Handlers

This tutorial shows how to use the typed handler base classes to work with strongly-typed C# models instead of raw `JsonObject` in your handlers.

## Why typed handlers?

The raw handler interfaces (`IResourceCreateOrUpdateHandler`, etc.) use `JsonObject` for properties and identifiers. This works but requires manual JSON navigation:

```csharp
var name = request.Properties["name"]?.GetValue<string>();
```

The typed handler base classes deserialize the request into your C# models automatically and serialize the response back. You work with strongly-typed records throughout.

## 1. Define your models

Create a record for your resource's **properties** and one for its **identifiers**:

```csharp
public record WidgetProperties
{
    public required string Name { get; init; }
    public string? Color { get; init; }
    public int? Size { get; init; }
    public string? CreatedAt { get; init; }
}

public record WidgetIdentifiers
{
    public required string Name { get; init; }
}
```

> [!TIP]
> If your extension uses `System.Text.Json` source generation, register a `JsonSerializerContext` for your model types. See the [Magic 8-Ball sample](https://github.com/Azure/bicep-extensibility/tree/main/sample/MagicEightBallExtension) for an example.

## 2. Implement the handlers

Inherit from the typed base class and override `HandleAsync`. Each base class provides nested type aliases (`TypedResource`, `TypedResourceSpecification`, etc.) scoped to your type parameters.

### Create or Update

```csharp
public class WidgetCreateOrUpdateHandler
    : TypedResourceCreateOrUpdateHandler<WidgetProperties, WidgetIdentifiers>
{
    private readonly WidgetStore store;

    public WidgetCreateOrUpdateHandler(
        IOptions<JsonOptions> jsonOptions,
        WidgetStore store)
        : base(jsonOptions)
    {
        this.store = store;
    }

    protected override Task<OneOf<TypedResource, LongRunningOperation, ErrorResponse>> HandleAsync(
        TypedResourceSpecification request, CancellationToken cancellationToken)
    {
        // request.Properties is WidgetProperties, not JsonObject.
        var resource = new TypedResource
        {
            Type = request.Type,
            ApiVersion = request.ApiVersion,
            Identifiers = new WidgetIdentifiers { Name = request.Properties.Name },
            Properties = request.Properties with
            {
                CreatedAt = DateTimeOffset.UtcNow.ToString("o"),
            },
        };

        this.store.Save(resource.Identifiers.Name, this.ToResource(resource));

        return Task.FromResult<OneOf<TypedResource, LongRunningOperation, ErrorResponse>>(resource);
    }
}
```

### Get

```csharp
public class WidgetGetHandler
    : TypedResourceGetHandler<WidgetProperties, WidgetIdentifiers>
{
    private readonly WidgetStore store;

    public WidgetGetHandler(IOptions<JsonOptions> jsonOptions, WidgetStore store)
        : base(jsonOptions)
    {
        this.store = store;
    }

    protected override Task<OneOf<TypedResource?, ErrorResponse>> HandleAsync(
        TypedResourceReference request, CancellationToken cancellationToken)
    {
        // request.Identifiers is WidgetIdentifiers, not JsonObject.
        var resource = this.store.TryGet(request.Identifiers.Name);

        // Returning null signals 404 Not Found.
        TypedResource? result = resource is not null ? this.ToTypedResource(resource) : null;

        return Task.FromResult<OneOf<TypedResource?, ErrorResponse>>(result);
    }
}
```

### Delete

```csharp
public class WidgetDeleteHandler
    : TypedResourceDeleteHandler<WidgetProperties, WidgetIdentifiers>
{
    private readonly WidgetStore store;

    public WidgetDeleteHandler(IOptions<JsonOptions> jsonOptions, WidgetStore store)
        : base(jsonOptions)
    {
        this.store = store;
    }

    protected override Task<OneOf<TypedResource?, LongRunningOperation, ErrorResponse>> HandleAsync(
        TypedResourceReference request, CancellationToken cancellationToken)
    {
        var removed = this.store.Remove(request.Identifiers.Name);

        // Returning null signals 204 No Content (already deleted).
        TypedResource? result = removed is not null ? this.ToTypedResource(removed) : null;

        return Task.FromResult<OneOf<TypedResource?, LongRunningOperation, ErrorResponse>>(result);
    }
}
```

### Preview

```csharp
public class WidgetPreviewHandler
    : TypedResourcePreviewHandler<WidgetProperties, WidgetIdentifiers>
{
    public WidgetPreviewHandler(IOptions<JsonOptions> jsonOptions)
        : base(jsonOptions)
    {
    }

    protected override Task<OneOf<TypedResourcePreview, ErrorResponse>> HandleAsync(
        TypedResourcePreviewSpecification request, CancellationToken cancellationToken)
    {
        var preview = new TypedResourcePreview
        {
            Type = request.Type,
            ApiVersion = request.ApiVersion,
            Identifiers = new WidgetIdentifiers { Name = request.Properties.Name },
            Properties = request.Properties with
            {
                CreatedAt = DateTimeOffset.UtcNow.ToString("o"),
            },
            Metadata = new ResourcePreviewMetadata
            {
                Calculated = [JsonPointer.Parse("/properties/createdAt")],
                ReadOnly = [JsonPointer.Parse("/properties/createdAt")],
                Unevaluated = request.Metadata?.Unevaluated,
            },
        };

        return Task.FromResult<OneOf<TypedResourcePreview, ErrorResponse>>(preview);
    }
}
```

## 3. Register the handlers

```csharp
ExtensionApplication.Create(args)
    .AddExtensionVersion("1.*.*", version => version
        .ForResourceType("Widget", type => type
            .AddHandler<WidgetPreviewHandler>()
            .AddHandler<WidgetCreateOrUpdateHandler>()
            .AddHandler<WidgetGetHandler>()
            .AddHandler<WidgetDeleteHandler>()))
    .Run();
```

## Typed configuration

If your extension uses typed configuration, use the 3-type-parameter variant:

```csharp
public record MyConfig
{
    public required string Endpoint { get; init; }
    public string? ApiKey { get; init; }
}

public class WidgetCreateOrUpdateHandler
    : TypedResourceCreateOrUpdateHandler<WidgetProperties, WidgetIdentifiers, MyConfig>
{
    // request.Config is now MyConfig instead of JsonObject?
}
```

## Conversion helpers

The base class exposes conversion methods for when you need to cross between typed and untyped models (e.g., storing resources in a shared store):

| Method | Direction |
|--------|-----------|
| `ToResource(TypedResource)` | Typed → Untyped |
| `ToTypedResource(Resource)` | Untyped → Typed |
| `ToNullableResource(TypedResource?)` | Typed → Untyped (nullable) |
| `ToResourceSpecification(TypedResourceSpecification)` | Typed → Untyped |

## Next steps

- [Behaviors](behaviors.md) — add cross-cutting validation or logging around your handlers.
- [Validators](validators.md) — validate request models with a fluent DSL.
