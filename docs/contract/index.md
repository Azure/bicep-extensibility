# How Extensions Work

A Bicep extension is an HTTP API service that sits between Azure and the resources your users want to deploy: Azure data-plane resources, or resources outside Azure entirely. When a user deploys a Bicep file that references your resource types, the **Bicep Extensibility Host** (a component of `Microsoft.Resources/deployments`) routes the deployment operations to your extension and manages its lifecycle.

```
Bicep file > deployments RP > Extensibility Host > your extension > resource provider
```

Your extension implements up to five operations defined by the Extension API Contract:

| Operation | Purpose |
|-----------|---------|
| **Preview** | Simulates a create or update without persisting changes. Powers preflight validation and What-If. |
| **Create or Update** | Creates or updates a resource, synchronously or via a long-running operation. |
| **Get** | Retrieves the current state of a resource. |
| **Delete** | Deletes a resource, synchronously or via a long-running operation. |
| **Get Long-Running Operation** | Polls the status of an in-flight operation. |

Each request carries an **extension version** in its route (`/{extensionVersion}/resource/...`) and a **resource API version** in its body. A managed extension hosts exactly one extension version per process; the resource API version is validated by your own handlers.

## Documents

| Document | Description |
|----------|-------------|
| [API Contract](contract.md) | The complete V2 contract: operations, models, HTTP binding, authentication, and limits. |
| [Preview Operation](preview-operation.md) | Deep-dive on the preview operation: unevaluated expressions, preview metadata, What-If integration. |
| [Async Operations](async-operations.md) | Long-running operation patterns: RELO (recommended) and LRO, with sequence diagrams and examples. |

## Next step

- [Getting started](../tutorials/getting-started.md): build and run your first extension.
