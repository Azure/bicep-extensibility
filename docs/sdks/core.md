# Core SDK

**Package:** `Azure.Deployments.Extensibility.Core`

The Core SDK provides the transport-agnostic building blocks shared by all Bicep extensions: contract models, handler interfaces, discriminated union types, structured error helpers, and a fluent validation framework.

## Namespaces

| Namespace | Description |
|-----------|-------------|
| `Contracts.Models` | Request/response models defined by the V2 contract: `Resource`, `ResourceSpecification`, `ResourceReference`, `ResourcePreview`, `ResourcePreviewSpecification`, `LongRunningOperation`, `ErrorResponse`, and related types. |
| `Contracts.Handlers` | Handler interfaces for each operation: `IResourcePreviewHandler`, `IResourceCreateOrUpdateHandler`, `IResourceGetHandler`, `IResourceDeleteHandler`, `ILongRunningOperationGetHandler`. |
| `Contracts` | `OneOf<T0, T1>` and `OneOf<T0, T1, T2>` — lightweight discriminated union structs used as handler return types. |
| `Contracts.Exceptions` | `ErrorResponseException` for propagating structured errors through the handler pipeline. |
| `Validation` | Fluent model validation: `ModelValidator<T>`, `IPropertyRuleBuilder`, and built-in criteria. |
| `Json` | JSON utilities: `JsonDefaults`, `JsonPointerProxy`, and `JsonNodeExtensions`. |

All namespaces live under the `Azure.Deployments.Extensibility.Core.V2` root.

## Contract models

The models map directly to the [API Contract](../contract/contract.md). A few highlights:

### ResourceSpecification

The input to **Create or Update** — carries the resource `Type`, `ApiVersion`, `Properties` (as `JsonObject`), and
optional `Config`/`ConfigId`.

### ResourcePreviewSpecification

The input to **Preview** -- carries the resource `Type`, `ApiVersion`, `Properties` (as `JsonObject`), `Metadata`, and
optional `Config`/`ConfigId`.

### Resource

The output of successful **Create or Update** and **Get** operations — adds `Identifiers` to the specification fields.

### OneOf

Handler return types use `OneOf` to express mutually exclusive outcomes without exceptions:

```csharp
// Create or update can succeed, start an LRO, or fail.
Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> HandleAsync(...)
```

Use `Match` to handle each case, or `Switch` for side-effect-only consumption.

## Structured errors

Throw an `ErrorResponseException` anywhere in the handler pipeline to short-circuit with a structured error:

```csharp
throw new ErrorResponseException("InvalidProperty", "The 'name' property is required.", "/properties/name");
```

The SDK's built-in error-handling behavior catches these and converts them into `ErrorResponse` results with the appropriate HTTP status code.

## Validation framework

The `ModelValidator<T>` base class provides a fluent DSL for defining validation rules. See the [Validators tutorial](../tutorials/validators.md) for a full walkthrough.

Quick example:

```csharp
public class MySpecificationValidator : ModelValidator<ResourceSpecification>
{
    public MySpecificationValidator()
    {
        Ensure(x => x.Type).NotNull();
        Ensure(x => x.Properties).NotNull().MatchesJsonSchema(schema);
    }
}
```

### Built-in criteria

| Criterion | Description |
|-----------|-------------|
| `NotNull()` | Fails when the property value is `null`. |
| `MatchesRegex(regex)` | Fails when a string does not match the given regex. |
| `MatchesJsonSchema(schema)` | Fails when a JSON value does not conform to a JSON Schema (Draft 7 by default). |
| `Satisfies(predicate)` | Fails when the property does not satisfy a custom predicate. |

All criteria support custom error codes and messages via `configureError`:

```csharp
Ensure(x => x.Type)
    .NotNull(e => e.WithCode("MissingType").WithMessage("Resource type is required."));
```
