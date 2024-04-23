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

The Extensibility Host supports two types of long-running operations: resource based long-running operations (RELO) and stepwise long-running operations (LRO). Comprehensive details regarding each of these operation types can be found in the [Azure REST API Guideline](https://github.com/microsoft/api-guidelines/blob/vNext/azure/Guidelines.md#long-running-operations--jobs) and [Microsoft Graph REST API Guidelines](https://github.com/microsoft/api-guidelines/blob/vNext/graph/patterns/long-running-operations.md). Extensions should align any other long-running operation patterns implemented by the underlying API with these two classifications.

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

1. The Deployments RP initiates a DELETE request to the Extensibility Host, signaling the intention to delete a resource.
2. The Extensibility Host, upon receiving the DELETE request, determines the corresponding extension to call and forwards the DELETE request to the identified extension.
3. In response, the extension issues a 202 Accepted status along with a `Location` header, and optionally a `Retry-After` header. The Location header encompasses an absolute URL pointing to an operation resource.
4. The Extensibility Host then modifies the `Location` URL provided by the extension, replacing the extension's endpoint (including the FQDN and port) with its own, and adds the extension's name and version at the beginning of the URL path. For instance, a `Location` URL like `https://{extensionApiEndpoint}/resourceOperations/123` is transformed to `https://{extensibilityHostEndpoint}/{extensionName}/{extensionVersion}/resourceOperations/123`.
5. The Extensibility Host sends back the DELETE response to the Deployments RP.
6. The Deployments RP employs a GET request on the `Location` URL to track the operation's progress.
7. When this GET request is received, the Extensibility Host reverts the `Location` URL to its original format by substituting back the extension's endpoint for its own and removing the extension's name and version from the path.
9. The Extensibility Host makes a GET request to the original `Location` URL and relays the response received from the extension back to the Deployments Resource Provider.
10. The Deployments RP then periodically polls for the operation's status, adhering to the `Retry-After` header if specified, or defaulting to a 60-second interval otherwise.
11. While the delete operation remains incomplete, the extension will return a 200 OK response containing a non-terminal `status` property in the response body.
12. Upon the operation's completion, the extension will issue a 200 OK response with a terminal `status` property (`Succeeded`, `Failed`, or `Canceled`). If the status is `Failed` or `Canceled`, an `error` property must be included in the response.
