# Validators

The Core SDK includes a fluent validation framework built around `ModelValidator<T>`. Use it to validate request models (e.g., `ResourceSpecification`) with composable, declarative rules.

## Defining a validator

Subclass `ModelValidator<T>` and define rules in the constructor using `Ensure`:

```csharp
using Azure.Deployments.Extensibility.Core.V2.Validation;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

public class WidgetSpecificationValidator : ModelValidator<ResourceSpecification>
{
    public WidgetSpecificationValidator()
    {
        Ensure(x => x.Type).NotNull();
        Ensure(x => x.ApiVersion).NotNull();
        Ensure(x => x.Properties).NotNull();
    }
}
```

## Built-in criteria

| Method | Description | Applies to |
|--------|-------------|-----------|
| `NotNull()` | Value must not be `null`. | Any type |
| `MatchesRegex(regex)` | String must match a regular expression. | `string?` |
| `MatchesJsonSchema(schema)` | JSON value must conform to a JSON Schema (Draft 7 default). | `JsonNode?`, `JsonElement` |
| `Satisfies(predicate)` | Value must pass a custom predicate. | Any type |

## Chaining criteria

Criteria chain left to right. If an earlier criterion fails, later criteria on the same rule are not evaluated:

```csharp
Ensure(x => x.Type)
    .NotNull()
    .MatchesRegex(new Regex(@"^[A-Za-z][A-Za-z0-9]*$"));
```

## Custom error messages

Override the default error code and message for any criterion:

```csharp
Ensure(x => x.Type)
    .NotNull(e => e
        .WithCode("MissingResourceType")
        .WithMessage("The resource type is required."));
```

## Custom predicates

Use `Satisfies` for validation logic that doesn't fit a built-in criterion:

```csharp
Ensure(x => x.ApiVersion)
    .NotNull()
    .Satisfies(
        v => v.StartsWith("2024-"),
        e => e.WithCode("UnsupportedYear")
              .WithMessage("Only 2024 API versions are supported."));
```

## JSON Schema validation

Validate `Properties` against a JSON Schema:

```csharp
private static readonly JsonSchema WidgetSchema = JsonSchema.FromText("""
{
  "type": "object",
  "required": ["name"],
  "properties": {
    "name": { "type": "string", "minLength": 1 },
    "color": { "type": "string", "enum": ["red", "green", "blue"] },
    "size": { "type": "integer", "minimum": 1, "maximum": 100 }
  },
  "additionalProperties": false
}
""");

public class WidgetSpecificationValidator : ModelValidator<ResourceSpecification>
{
    public WidgetSpecificationValidator()
    {
        Ensure(x => x.Type).NotNull();
        Ensure(x => x.Properties).NotNull().MatchesJsonSchema(WidgetSchema);
    }
}
```

Schema violations produce one `ErrorDetail` per violation, each with a JSON Pointer `target` identifying the failing property.

## Rule dependencies

Use `DependsOn` to skip a rule when an earlier rule has already failed. This prevents cascading errors:

```csharp
public class WidgetSpecificationValidator : ModelValidator<ResourceSpecification>
{
    public WidgetSpecificationValidator()
    {
        var typeRule = Ensure(x => x.Type).NotNull();
        var propertiesRule = Ensure(x => x.Properties).NotNull();

        // Only validate schema if Properties is not null.
        Ensure(x => x.Properties)
            .DependsOn(propertiesRule)
            .MatchesJsonSchema(WidgetSchema);
    }
}
```

## Using validators

### In a handler

```csharp
public class WidgetCreateOrUpdateHandler : IResourceCreateOrUpdateHandler
{
    private static readonly WidgetSpecificationValidator validator = new();

    public Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> HandleAsync(
        ResourceSpecification request, CancellationToken cancellationToken)
    {
        if (validator.Validate(request) is { } error)
        {
            return Task.FromResult<OneOf<Resource, LongRunningOperation, ErrorResponse>>(
                new ErrorResponse(error));
        }

        // Proceed with handler logic...
    }
}
```

### In a behavior

```csharp
public class ValidationBehavior : IResourceCreateOrUpdateBehavior
{
    private static readonly WidgetSpecificationValidator validator = new();

    public Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> HandleAsync(
        ResourceSpecification request,
        ResourceCreateOrUpdateHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        if (validator.Validate(request) is { } error)
        {
            return Task.FromResult<OneOf<Resource, LongRunningOperation, ErrorResponse>>(
                new ErrorResponse(error));
        }

        return next(request);
    }
}
```

## Error aggregation

When multiple rules fail, `ModelValidator` aggregates them into a single error with code `MultipleErrorsOccurred` and a `details` array:

```json
{
  "error": {
    "code": "MultipleErrorsOccurred",
    "message": "Multiple errors occurred. Please refer to details for more information.",
    "details": [
      { "code": "ValueMustNotBeNull", "message": "Value must not be null.", "target": "/type" },
      { "code": "ValueMustNotBeNull", "message": "Value must not be null.", "target": "/properties" }
    ]
  }
}
```

When a single rule fails, the error is returned directly without wrapping.

## Next steps

- [Typed Handlers](typed-handlers.md) — use strongly-typed models in your handlers.
- [Behaviors](behaviors.md) — add cross-cutting concerns around your handlers.
