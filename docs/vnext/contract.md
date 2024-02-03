# Bicep Extension API Contract

This document defines the contract of the API that every team who wants to onboard to Bicep exension must adhere to. An Bicep extension is an API abstraction, allowing users to do the following operations:
1. Deploy Azure data-plane or non-Azure resources (a.k.a, extensible resources) via Bicep files or ARM templates;
2. [Future Plan] Perform resource-level or extension-level queries that have no side effects through functions in Bicep files or ARM templates, e.g., list secrets of a resource.

## Table of Contents
- [Api Limits and Constraints](#api-limits-and-constraints)
- [Resource API Reference](#resource-api-reference)
- [Long-running Operation Reference](#long-running-operation-reference)

## API Limits and Constraints

### Request Timeout

The timeout value for each request is 60 seconds. If the extension failed to respond within 60 seconds, the Extensibility Host will return a 504 Gateway Timeout to the deployments RP and ignore the response from the extension.

### Request Throttling

The Extensibility Host does not implement throttling for incoming request from the deployments RP, and it's up to extensions to apply their own throttling limits. However, since the extensible resources are deployed through ARM, the [general ARM request throttling limits](https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/request-limits-and-throttling) still apply to deployments that contain extensible resources.

### Maximum Request Body Size

> This is something that the deployments team has been trying to improve. The limit may change in the future.

The maximum size of a request body that ARM will accept is 4 MB. Any request with a body larger than 4 MB will not be sent to the extensibility host and extensions, and a 413 Payload Too Large will be returned to the client.

### Maximum Response Size

To keep it consistent with ARM's limit, the extensibility host does not accept any single resource response that exceeds 20MB in size. When this happens, the response body will be dropped and a 502 Bad Gateway error will be returned to the deployments RP.

## Resource API Reference

For details about resource APIs, Refer to the OpenAPI 3.0 based resource API [specification](openapi.yaml). You may view it in the ReDoc UI [here](https://redocly.github.io/redoc/?url=https://raw.githubusercontent.com/Azure/bicep-extensibility/main/docs/vnext/openapi.yaml&nocors) for better experience.

## Long-running Operation Reference

The Extensibility Host supports two types of long-running operations: resource based long-running operations (RELO) and stepwise long-running operations (LRO). Comprehensive details regarding each of these operation types can be found in the [Microsoft REST API Guideline](https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#13-long-running-operations). Extensions should align any other long-running operation patterns implemented by the underlying API with these two classifications.

> As highlighted in [Microsoft REST API Guideline](https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#1441-responsiveness), it is recommended that invocations taking longer than 1 second to respond in the 99th percentile SHOULD employ the long-running operation pattern. However, this guideline is not mandatory for extensions, as it might not perfectly align with the nuances of the underlying API implementation. Nonetheless, extensions are strongly encouraged to enhance their overall system responsiveness. For instance, a slow synchronous API can be converted into an asynchronous LRO counterpart. This can be achieved by exposing an operation resource dedicated to the Extensibility Host, facilitating real-time tracking of operation status and progress. In this case, the operation resource is managed by the extension, leaving the underlying API unchanged.

### Resource based long-running operations (RELO)

#### When to Use This Pattern

The RELO pattern is the preferred pattern for long-running resource create or update operations and should be used whenever possible. The pattern cannot be used with long-running resource delete operations.

#### API Flow

1. The Extensibility Host initiates a PUT request to the extension, triggering the creation or update of a resource.
2. In response to the PUT request, the extension issues a 201 Created response if the resource is being created or 200 OK if the resource is being updated.
3. As the operation remains ongoing, the PUT response payload should include a status property set to a non-terminal value (e.g., "Running"), adhering to the guidelines specified in the resource API [specification](openapi.yaml).
4. Subsequent GET requests targeting the same resource, which underwent creation or update, should consistently yield a 200 OK response accompanied by a non-terminal status property. This pattern continues as long as the provisioning process remains in progress.
5. Upon the completion of provisioning, the status property is expected to transition to one of the terminal states. If an update to existing resource properties encounters failure, it's advisable to revert those properties to their previous state if such a restoration best reflects the final state of the resource post the unsuccessful operation.
6. The status property's presence should persist across all future GET requests, even after the operation concludes, until the occurrence of another operation (e.g., PUT or DELETE) triggers a transition to a non-terminal state.

### Stepwise Long-running Operations (LRO)

#### When to Use This Pattern

The LRO pattern serves as an alternative for executing asynchronous resource creation or update operations when implementing the RELO pattern is not feasible. Notably, it is the only long-running operation pattern that works with asynchronous resource deletion. This is attributed to the fact that reading the `status` property of a deleted resource becomes impractical.

#### API Flow

1. The Extensibility Host initiates a DELETE request to the extension, signaling the intention to delete a resource.
2. In response, the extension issues a 202 Accepted status along with a `Location` header, and optionally a `Retry-After` header. The Location header encompasses an absolute URL pointing to an operation resource that the Extensibility Host will poll.
3. The Extensibility Host employs a GET request on the URL specified within the `Location` header.
4. Subsequent polling by the Extensibility Host should respect the `Retry-After` interval if it was provided, or adhere to the default interval of 60 seconds if not.
5. While the delete operation remains incomplete, the extension returns a 200 OK response containing a non-terminal `status` property in the response body.
6. Upon the operation's completion, the extension issues a 200 OK response with a terminal `status` property (`Succeeded`, `Failed`, or `Canceled`). If the status is `Failed` or `Canceled`, an `error` property must be included in the response.
