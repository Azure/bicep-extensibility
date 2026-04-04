# Azure.Deployments.Extensibility.Core

Core models, contracts, and validation utilities for building [Bicep extensions](https://github.com/Azure/bicep-extensibility).

A Bicep extension is an API abstraction that enables users to deploy Azure data-plane or non-Azure resources through Bicep files or ARM templates.

## What's in this package

| Namespace | Description |
|-----------|-------------|
| `Azure.Deployments.Extensibility.Core.V2.Contracts.Models` | Request/response models defined by the [Bicep Extension API Contract v2](../../docs/v2/contract.md): `Resource`, `ResourceSpecification`, `ResourceReference`, `ResourcePreview`, `LongRunningOperation`, `ErrorResponse`, and related types. |
| `Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers` | Handler interfaces for each operation: `IResourcePreviewHandler`, `IResourceCreateOrUpdateHandler`, `IResourceGetHandler`, `IResourceDeleteHandler`, `ILongRunningOperationGetHandler`. |
| `Azure.Deployments.Extensibility.Core.V2.Contracts` | The `OneOf<T0, T1>` and `OneOf<T0, T1, T2>` discriminated union types used as handler return types. |
| `Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions` | `ErrorResponseException` for propagating structured errors through the handler pipeline. |
| `Azure.Deployments.Extensibility.Core.V2.Validation` | A fluent model validation framework: `ModelValidator<T>`, `IPropertyRuleBuilder`, and built-in criteria (`NotNull`, `MatchesRegex`, `MatchesJsonSchema`, `Satisfies`). |
| `Azure.Deployments.Extensibility.Core.V2.Json` | JSON utilities: `JsonDefaults` (serializer options), `JsonPointerProxy`, and `JsonNodeExtensions` for navigating JSON trees with JSON Pointers. |

## Usage

This package provides the shared types. To build a complete extension with HTTP hosting, routing, and the handler pipeline, use [`Azure.Deployments.Extensibility.AspNetCore`](../Azure.Deployments.Extensibility.AspNetCore/) on top of this package.

### Defining a model validator

```csharp
using Azure.Deployments.Extensibility.Core.V2.Validation;

public class EmployeeValidator : ModelValidator<ResourceSpecification>
{
    public EmployeeValidator()
    {
        Ensure(x => x.Type).NotNull();
        Ensure(x => x.Properties).NotNull().MatchesJsonSchema(employeeSchema);
    }
}
```

### Throwing structured errors

```csharp
using Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions;

throw new ErrorResponseException("InvalidProperty", "The 'name' property is required.", "/properties/name");
```

## Documentation

- [Bicep Extension API Contract v2](../../docs/v2/contract.md)
- [Preview Operation](../../docs/v2/preview-operation.md)
- [Asynchronous Operations](../../docs/v2/async-operations.md)

## License

This project is licensed under the [MIT License](../../LICENSE).
