# Concepts

This page defines the terms and building blocks used throughout the documentation. If you are new to Bicep extensions, read this after [Getting Started](tutorials/getting-started.md) to solidify the vocabulary.

## The big picture

```
Deployment (Microsoft.Resources/deployments)
        │
        ▼
Bicep Extensibility Host  ──HTTP (Extension API Contract)──►  Your extension
                                                                  │
                                                          Handlers + Behaviors
                                                                  │
                                                          Data-plane / non-Azure API
```

The **Host** owns the deployment lifecycle and speaks the [Extension API Contract](contract/contract.md) over HTTP. Your **extension** is the service on the other side of that contract. The SDKs exist to help you implement that service in .NET.

## Core terms

| Term | Meaning |
|------|---------|
| **Extension** | An HTTP API service that implements the resource operations for one or more resource types. |
| **Extensibility Host** | The component of `Microsoft.Resources/deployments` that routes deployment requests to an extension and manages the operation lifecycle. |
| **Handler** | The unit of code that implements a single resource operation (for example, create-or-update) for a resource type. |
| **Behavior** | Cross-cutting logic that wraps handlers, such as auditing, schema validation, or error shaping. Behaviors compose around handlers in a pipeline. |
| **Identity & version** | Every extension declares a name and an exact, opaque version. One process hosts exactly one version; the version is never parsed, ranged, or normalized. |

## Contract vs. SDK

These are two different things and it helps to keep them separate:

- The **[API Contract](contract/contract.md)** is the wire protocol: the HTTP shapes the Host and your extension exchange. It is language-agnostic and authoritative.
- The **SDKs** ([Core](sdks/core.md), [Managed](sdks/managed.md)) are the .NET libraries that implement the contract for you, so you write handlers instead of HTTP.

If you build in a language other than .NET, you implement the Contract directly. If you build in .NET, the SDKs are the fast path.

## Resource operations

An extension implements up to five operations per resource type:

| Operation | Purpose |
|-----------|---------|
| **Preview** | Return the projected result of a deployment without applying it (supports What-If and unevaluated expressions). |
| **Create or Update** | Create or reconcile a resource to the desired state. |
| **Get** | Read the current state of a resource. |
| **Delete** | Remove a resource. |
| **Operation Get** | Poll the status of an in-progress long-running operation. |

## Long-running operations: RELO vs. LRO

Some operations can't finish within the request timeout. The contract supports two async patterns. See [Async Operations](contract/async-operations.md) for full detail.

| Pattern | Name | How the Host polls | Use when |
|---------|------|--------------------|----------|
| **RELO** | Resource-based long-running operation | Calls **get** on the resource until status is terminal | Preferred. Use whenever the underlying API allows it. |
| **LRO** | Stepwise long-running operation | Calls a separate **operation get** endpoint with an operation handle | Only when RELO is not feasible for the underlying API. |

A status is **terminal** (`Succeeded`, `Failed`, `Canceled`) or **non-terminal** (any other value, such as `Running`). The status field is an open union, so extensions may define their own non-terminal values.

## Where to go next

- Build an extension: [Getting Started](tutorials/getting-started.md)
- Structure handlers: [Typed Handlers](tutorials/typed-handlers.md)
- Add cross-cutting logic: [Behaviors](tutorials/behaviors.md)
- Validate input: [Validators](tutorials/validators.md)
- Protocol reference: [API Contract](contract/contract.md)
