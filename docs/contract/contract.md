# Bicep Extension API Contract v2.0.0

This document defines the contract between the Bicep Extensibility Host, a component of the `Microsoft.Resources/deployments` resource provider, and Bicep extensions. All teams onboarding a Bicep extension must conform to this contract.

A Bicep extension is an API abstraction that enables users to deploy Azure data-plane or non-Azure resources (extensible resources) through Bicep files or ARM templates.

## Table of Contents
- [API Operations Overview](#api-operations-overview)
- [Core Models](#core-models)
  - [Error Models](#error-models)
  - [Resource Reference](#resource-reference)
  - [Resource Specification](#resource-specification)
  - [Resource](#resource)
  - [Resource Preview](#resource-preview)
  - [Long-Running Operation](#long-running-operation)
- [Resource Operations](#resource-operations)
  - [Configuration Handling](#configuration-handling)
  - [Preview Resource](#preview-resource)
  - [Create or Update Resource](#create-or-update-resource)
  - [Get Resource](#get-resource)
  - [Delete Resource](#delete-resource)
- [Long-Running Operations](#long-running-operations)
- [HTTP Binding](#http-binding)
  - [OpenAPI Specification](#openapi-specification)
  - [Authentication](#authentication)
  - [Limits and Constraints](#limits-and-constraints)
- [Operational Requirements for 3P Extensions](#operational-requirements-for-3p-extensions)
  - [Health Check](#health-check)

## API Operations Overview

| Operation                                               | Required | Description                                                                                                                            |
|---------------------------------------------------------|----------|----------------------------------------------------------------------------------------------------------------------------------------|
| [Preview Resource](#preview-resource)                   | Yes      | Simulates a create or update without persisting changes. Used for preflight validation and What-If.                                    |
| [Create or Update Resource](#create-or-update-resource) | Yes      | Creates or updates a resource, synchronously or via a long-running operation.                                                          |
| [Get Resource](#get-resource)                           | Yes      | Retrieves the current state of a resource.                                                                                             |
| [Delete Resource](#delete-resource)                     | Yes      | Deletes a resource, synchronously or via a long-running operation.                                                                     |
| [Get Long-Running Operation](#long-running-operations)  | No       | Polls the status of a long-running operation. Only required if the extension supports the [stepwise LRO pattern](async-operations.md). |

## Core Models

The models below are defined in [`core.tsp`](https://github.com/Azure/bicep-extensibility/blob/main/spec/core.tsp) and are transport-agnostic. A `?` suffix on a property name indicates the property is optional.

### Error Models

Errors use the OData v4 error response format. The ARM Template deployment service converts this format into the ARM error response format before returning it to the client.

```typespec
model ErrorResponse {
  error: Error;
}

model Error {
  code: string;
  message: string;
  target?: JsonPointer;       // JSON Pointer to the location that caused the error
  details?: ErrorDetail[];
  innererror?: Record<unknown>;
}

model ErrorDetail {
  code: string;
  message: string;
  target?: JsonPointer;
}
```

### Resource Reference

Input to **get** and **delete** operations. Contains the information needed to uniquely identify a resource.

```typespec
model ResourceReference {
  type: string;
  apiVersion?: string;
  identifiers: Record<unknown>;
  config?: Record<unknown>;
  configId?: string;
}
```

- `identifiers`: Properties that, combined with the resource type and configuration, form a globally unique key for the resource. The structure is defined by the extension and is opaque to the deployment engine.
- `config`: Extension configuration, if applicable.
- `configId`: A checksum that identifies the configuration. If present, the extension must validate it. Required for delete operations when a configuration is present.

### Resource Specification

Input to **create or update** and **preview** operations. Represents the desired state of a resource as declared in the user's template.

```typespec
model ResourceSpecification {
  type: string;
  apiVersion?: string;
  properties: Record<unknown>;
  config?: Record<unknown>;
  configId?: string;
}
```

The preview operation extends this model with optional metadata indicating which properties contain unevaluated expressions:

```typespec
model ResourcePreviewSpecification extends ResourceSpecification {
  metadata?: ResourcePreviewSpecificationMetadata;
}

model ResourcePreviewSpecificationMetadata {
  unevaluated: JsonPointer[];  // paths to property values with unevaluated ARM expressions
}
```

### Resource

Returned on success from **create or update**, **get**, and **delete** operations.

```typespec
model Resource {
  type: string;
  apiVersion?: string;
  identifiers: Record<unknown>;
  properties: Record<unknown>;
  config?: Record<unknown>;
  configId?: string;
  status?: OperationStatus;
  error?: Error;
}
```

- `identifiers`: Must always be included in successful responses.
- `config` / `configId`: Required if the resource has a configuration.
  See [Configuration Handling](#configuration-handling).
- `status` / `error`: Used in RELO long-running operations. A non-terminal status (e.g., `"Running"`) indicates the operation is in progress. Set `error` only when `status` is `"Failed"`.

### Resource Preview

Returned on success from the **preview** operation. The preview should reflect what a get response would return if the create or update operation had been performed.

```typespec
model ResourcePreview {
  type: string;
  apiVersion?: string;
  identifiers: Record<unknown>;
  properties: Record<unknown>;
  status?: OperationStatus;
  config?: Record<unknown>;
  configId?: string;
  metadata?: ResourcePreviewMetadata;
}

model ResourcePreviewMetadata {
  readOnly?: JsonPointer[];
  immutable?: JsonPointer[];
  unknown?: JsonPointer[];
  calculated?: JsonPointer[];
  unevaluated?: JsonPointer[];
}
```

See [Preview Operation](preview-operation.md) for the meaning of each metadata field.

### Long-Running Operation

Returned when an operation is long-running. Used for stepwise LRO polling.

```typespec
model LongRunningOperation {
  status: OperationStatus;
  retryAfterSeconds?: int32;
  operationHandle?: Record<unknown>;
  error?: Error;
}
```

- `status`: Terminal values are `"Succeeded"`, `"Failed"`, and `"Canceled"`. Extensions may define custom non-terminal values (e.g., `"Running"`, `"Provisioning"`).
- `retryAfterSeconds`: Recommended polling interval in seconds. Defaults to 60 if not specified.
- `operationHandle`: An opaque handle for tracking the operation. Required when `status` is non-terminal.
- `error`: Required when `status` is `"Failed"` or `"Canceled"`.

## Resource Operations

This section describes the contract requirements for each resource operation.

### Configuration Handling

The following configuration rules apply to **all** resource operations that accept or return a `config` object:

The extension must include the `config` it received in the response, excluding any secret properties. Any property not
echoed back is treated as a secret by the deployment engine.

#### Configuration Identifier (`configId`)

##### Return a `configId`

If a configuration is present, the extension must return a `configId`, a value that uniquely identifies the deployment
target of the extension. The `configId` must be based on the `config`. This value serves as a checksum for future
operations such as deletion. An example of a deployment target could be a specific cloud region or environment.

The format is determined by the extension, and it could be anything from a human-readable string, an actual ID, or a
hash of various inputs. However, the `configId` should not depend on unstable inputs for reasons explained in the
compatibility section below. If unstable inputs must be used to compute it, such as a URL, map it to a stable GUID
instead. Even if an extension can only deploy to one deployment target or the configuration does not affect the
deployment target, a `configId` is required to be returned. A `configId` is case-sensitive, so inputs to calculate it
should be normalized. See the [Examples](#configid-examples) section below for some ideas.

Do not return a `configId` when the customer passed no configuration to the extension.

##### Preserve `configId` compatibility

The `configId` value must be both backward and forward compatible across extension versions. Backward compatibility is
required because an extension can receive a `configId` generated by any older version of the extension. Forward
compatibility requires keeping the returned `configId` stable for the same deployment target across version upgrades.

Forward compatibility is critical for customers that deploy their resources with Deployment stacks. Deployment stacks
use it as part of a resource's unique identity, which determines whether that resource remains managed on future stack
updates.

Deployment stacks execute the following workflow when a customer updates their stack:

1. Deploys the customer's latest deployment template.
2. Uses the pre-existing state of the stack combined with the deployment results to calculate which resources to
   unmanage from the stack.
3. If the customer sets their action-on-unmanage setting to delete resources, the stack will then delete unmanaged
   resources after deployment completes.

Here is an example of what will happen if forward compatibility is broken:

Suppose an extension returns `configId` of `"prod.com"` for a resource. A customer then deploys a stack with that
resource. Later an extension developer changes the `configId` to `"renamedProd.com"` for the same configuration the
customer is providing and releases a new extension version. The customer decides to update their deployment extension to
the new version and redeploys their stack with action-on-unmanage set to delete resources but with no deployment
template or input changes (what should be a no-op). When the resource is deployed, the `configId` of `"renamedProd.com"`
will cause a resource record mismatch between the deployment and the state persisted on the stack. This results in the
resource being deployed again, then deleted shortly after!

If breaking forward compatibility is unavoidable, the extension must document it as a **breaking change** and instruct
customers to use an **action-on-unmanage** setting to **detach resources** for the first deployment update of a stack
with the breaking change effective. Subsequent updates to the stack do not require this setting override.

Another possible scenario that doesn't require an extension version update is when the customer updates their config to
point to the new URL and extension returns the URL the customer provided in the config for the `configId`. If the URL
changes, this will cause stack resources to be inadvertently deleted.

##### Validate `configId`

If a `configId` is provided in the request, the extension should validate it when possible. This should always be
present on a deletion request for an extension resource that was deployed with a configuration and should always be
validated for deletion requests. The other resource operations have this as nullable, and the extension can choose to
validate it or not. The validation check ensures the operation being performed is being performed on the same exact
resource from a previous record of it.

##### configId Examples

Here are some examples of a `configId` calculation:

| Scenario                                                                                                                   | `config`                                                                                         | `configId`                                                                                            |
|----------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------|
| Deploying secrets to an Azure key vault (cannot be renamed)                                                                | `{ "keyVaultUri": ".../myKeyVault" }`<br/>`{ "keyVaultUri": ".../MYKEYVAULT" }`                  | `".../mykeyvault"` (normalized)                                                                       |
| Deploying to a renamed resource                                                                                            | `{ "resourceId": ".../myResource" }`<br/>then later, `{ "resourceId": ".../myRenamedResource" }` | `"00000000-0000-0000-0000-000000000001"` (use a stable GUID to track it)                              |
| Use a new authorization mechanism to deploy                                                                                | `{ "authorization": "Old" }`<br/>then later, `{ "authorization": "New" }`                        | `"default"` (we're still deploying to the same target, so the configId should be the same)            |
| Introduce possibility to deploy to a new environment                                                                       | `{ "environment": "Prod" }`<br/>then later, `{ "environment": "Test" }`                          | `"Prod"` for production, `"Test"` for test                                                            |
| Introduce possibility to deploy to a new environment, no config previously, defaulted to prod                              | `null`<br/>then later `{ "environment": "Test" }`                                                | `null` for production resources as it was, `"Test"` for test resources                                |
| Introduce possibility to deploy to a new environment, no config previously, defaulted to prod, now requires prod specified | `null`<br/>then later `{ "environment": "Prod" }`                                                | **BREAKING CHANGE**: prod resources previously had no config or configId, and now it will be `"Prod"` |

Here are some examples of bad practices to avoid:

| Example                      | Reason it's not ideal                                                                    | Suggested alternative                                                                                                                                                                                                   |
|------------------------------|------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `prod-0001-asdf.company.com` | A URL will likely change. Only use a URL if you are confident the URL will never change. | GUID. Maintain a mapping of URLs to GUIDs, that way the URL can be updated or aliased to the same GUID. A user could update the URL in their config when it changes, but the extension would still return the same GUID |

Given these examples, a good approach would be to maintain a mapping of extension config possibilities to stable GUIDs
and use the GUID as the `configId`. If your `configId` is sensitive in nature, normalize the GUID and hash it.

### Preview Resource

Simulates a resource creation or update without persisting any changes. The deployment engine uses preview for two purposes: **preflight validation** (triggered by both template deployment validate and PUT requests) and **[What-If](https://learn.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-what-if)** (computing a diff between the current and desired state of each resource).

|            | Type                               |
|------------|------------------------------------|
| **Input**  | `ResourcePreviewSpecification`     |
| **Output** | `ResourcePreview \| ErrorResponse` |

The request may contain ARM template language expressions that the deployment engine could not evaluate at preview time. The paths to these properties are listed in `metadata.unevaluated`. The extension must handle expressions at any JSON node, echo them back as-is, and return a best-effort preview with the available information.

On success, the response must represent the resource state as if the create or update had been performed, equivalent to what a subsequent get operation would return. The extension should also supply preview metadata (`readOnly`, `immutable`, `calculated`, `unknown`, `unevaluated`) to enable a richer preview experience.

Preview is a **best-effort operation**. If the extension cannot produce a meaningful preview (e.g., because the
identifiers or configuration are unevaluated), it should return an `ErrorResponse` with error code
`PreviewNotSupported`. When this happens, preflight validation does not fail, and What-If reports the resource with
change type **Unsupported**.

For detailed guidance, metadata field definitions, and end-to-end examples, see [Preview Operation](preview-operation.md).

---

### Create or Update Resource

Creates or updates a resource. The operation may complete synchronously and return the resource, or initiate a long-running operation.

|            | Type                                                |
|------------|-----------------------------------------------------|
| **Input**  | `ResourceSpecification`                             |
| **Output** | `Resource \| LongRunningOperation \| ErrorResponse` |

The extension must include `identifiers` in the response. Its sub-properties, combined with the resource type and configuration, form a *globally unique* key for the resource. The structure is defined by the extension and is opaque to the deployment engine.

---

### Get Resource

Retrieves the current state of a resource given its type, identifiers, and configuration.

|            | Type                        |
|------------|-----------------------------|
| **Input**  | `ResourceReference`         |
| **Output** | `Resource \| ErrorResponse` |

If the resource does not exist, the extension must return an error indicating the resource was not found.

---

### Delete Resource

Deletes a resource given its type, identifiers, configuration, and configuration ID.

|            | Type                                                |
|------------|-----------------------------------------------------|
| **Input**  | `ResourceReference`                                 |
| **Output** | `Resource \| LongRunningOperation \| ErrorResponse` |

The operation may complete synchronously and return the deleted resource, indicate that the resource was already deleted, or initiate a long-running operation.

---

## Long-Running Operations

When an operation cannot complete synchronously, either because it would exceed the request timeout or because the underlying API is itself asynchronous, the extension may initiate a long-running operation. The Extensibility Host supports two patterns:

| Pattern                   | Polling Mechanism                     | When to Use                            |
|---------------------------|---------------------------------------|----------------------------------------|
| **RELO** (resource-based) | Poll via the get operation            | Preferred. Use whenever possible.      |
| **LRO** (stepwise)        | Poll via a dedicated operation handle | Alternative when RELO is not feasible. |

Both patterns support create, update, and delete operations. For detailed flows, examples, and guidance on choosing between them, see [Asynchronous Operations](async-operations.md). For general background, see the [Azure REST API Guidelines](https://github.com/microsoft/api-guidelines/blob/vNext/azure/Guidelines.md#long-running-operations--jobs) and [Microsoft Graph REST API Guidelines](https://github.com/microsoft/api-guidelines/blob/vNext/graph/patterns/long-running-operations.md).

The Extensibility Host retrieves the current status of a stepwise long-running operation via the **Get Long-Running Operation** endpoint:

|            | Type                                    |
|------------|-----------------------------------------|
| **Input**  | `operationHandle: Record<unknown>`      |
| **Output** | `LongRunningOperation \| ErrorResponse` |

An error response from this endpoint indicates a problem retrieving the status (e.g., a network error), **not** a failure of the operation itself. Operation failures are reported through `status: "Failed"` and the accompanying `error` property.

## HTTP Binding

The core contract above is transport-agnostic. This section describes the HTTP protocol binding used by the Extensibility Host.

> **Note:** Bicep Local Deploy currently uses gRPC as its transport. This is an experimental feature and subject to change. No gRPC binding specification is provided at this time.

### OpenAPI Specification

For the complete HTTP binding (routes, methods, request/response headers, and status codes), refer to the generated [OpenAPI specification](v2/openapi.yaml). A rendered version is available via the [OpenAPI Explorer](../api-explorer/index.html).

The HTTP binding is defined in [`http.tsp`](https://github.com/Azure/bicep-extensibility/blob/main/spec/http.tsp).

### Authentication

Authentication between the Extensibility Host and extensions depends on the extension type:

| Extension Type                           | Authentication Mechanism                                                                                                                                                                                                                                                                                                                               |
|------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **First-party (1P, Microsoft-internal)** | OAuth 2.0 On-Behalf-Of (OBO) flow. The Extensibility Host acquires a token on behalf of the caller and passes it to the extension. See [OAuth 2.0 On-Behalf-Of flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-on-behalf-of-flow) for details. Teams should reach out to the Azure Deployments team for onboarding guidance. |
| **Third-party (3P)**                     | Handled by the platform. Extension authors do not need to implement authentication.                                                                                                                                                                                                                                                                    |

> **Note:** The third-party (3P) extensibility platform is a work in progress. Details are subject to change.

### Limits and Constraints

#### Request Timeout

Each request has a 60-second timeout. If the extension does not respond within this window, the Extensibility Host returns a timeout error to the `Microsoft.Resources/deployments` resource provider and discards the extension's response.

#### Request Throttling

The Extensibility Host does not throttle incoming requests from the Deployments RP; extensions are responsible for enforcing their own throttling limits. Because extensible resources are deployed through ARM, the [general ARM request throttling limits](https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/request-limits-and-throttling) still apply to deployments containing extensible resources.

#### Maximum Request Body Size

The maximum size of a request body that ARM will accept is 4 MB. Any request with a body larger than 4 MB will not be forwarded to the extensibility host and extensions.

#### Maximum Response Size

To keep it consistent with ARM's limit, the extensibility host does not accept any single resource response that exceeds 20 MB in size. When this happens, the response body will be dropped.

## Operational Requirements for 3P Extensions

This section documents operational requirements that apply to **Bicep third-party (3P) and local extensions**. These requirements are enforced by the Extensibility Host but are separate from the public API contract.

### Health Check

> [!NOTE]
> Health check API documentation is coming soon.
