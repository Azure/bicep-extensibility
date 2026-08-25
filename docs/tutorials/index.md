# Authoring guides

These guides explain how to implement extension behavior after the host is configured.
They apply to extensions using the managed runtime and to self-hosted FirstParty
extension services.

## Implement resources

| Guide | Use it to |
|---|---|
| [Typed handlers](typed-handlers.md) | Implement resource operations with strongly typed properties and identifiers. |
| [Behaviors](behaviors.md) | Add validation, logging, authorization, or other cross-cutting logic around handlers. |
| [Validation](validators.md) | Define reusable validation rules with the Core SDK. |

## Implement protocol behavior

| Guide | Use it to |
|---|---|
| [Preview](../contract/preview-operation.md) | Produce accurate validation and What-If results, including unevaluated values. |
| [Long-running operations](../contract/async-operations.md) | Choose between resource-based and stepwise asynchronous operation patterns. |
| [API Explorer](api-explorer.md) | Inspect the contract and exercise your extension during development. |

For wire-level requirements, limits, authentication, and model definitions, use the
[API contract](../contract/contract.md).
