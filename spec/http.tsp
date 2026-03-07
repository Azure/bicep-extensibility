// ---------------------------------------------------------------------------
// Bicep Extensibility Provider API — HTTP Binding
// ---------------------------------------------------------------------------
// Defines the HTTP routes and request/response mappings for the V2 API.
// Maps to the route table in WebApplicationExtensions.cs and the handler
// dispatch logic in HandlerDispatcher.cs.
//
// Transport-agnostic models and operation interfaces are defined in core.tsp.
// This file layers on HTTP-specific concerns: routes, methods, headers, and
// status-code–specific response envelopes.
// ---------------------------------------------------------------------------

import "@typespec/http";
import "@typespec/openapi";

import "./core.tsp";

using TypeSpec.Http;
using OpenAPI;

@service(#{ title: "Bicep Extensibility Provider API" })
@info(#{ version: "2.0.0" })
@server("{endpoint}", "Extension endpoint", {
  endpoint: string = "http://localhost:8080",
})
namespace BicepExtensibility;

// ---------------------------------------------------------------------------
// Request headers
// ---------------------------------------------------------------------------

model RequestHeaders {
  /** Client-generated request ID for tracing. */
  @header("x-ms-client-request-id")
  clientRequestId: string;

  /** Correlation request ID for cross-service tracing. */
  @header("x-ms-correlation-request-id")
  correlationRequestId: string;

  /** The tenant ID of the client. */
  @header("x-ms-client-tenant-id")
  clientTenantId?: string;

  /** The home tenant ID of the client. */
  @header("x-ms-home-tenant-id")
  homeTenantId?: string;

  /**
   * Specifies the preferred language for the response.
   * Extensions should use this when generating client-facing text such as error messages.
   */
  @header("Accept-Language")
  acceptLanguage?: string;

  /**
   * The full URL of the extensibility host service instance from which the request is sent.
   * Extensions should use this to track the source of the request.
   */
  @header("Referer")
  referer: string;

  /**
   * Standard W3C trace context header for distributed tracing.
   * Extensions should propagate this header if they support OpenTelemetry.
   */
  @header("traceparent")
  traceparent: string;

  /**
   * Standard W3C trace state header for vendor-specific trace data.
   * Extensions should propagate this header if they support OpenTelemetry.
   */
  @header("tracestate")
  tracestate: string;
}

// ---------------------------------------------------------------------------
// Response headers
// ---------------------------------------------------------------------------

model ResponseHeaders {
  /** Server-generated request ID. */
  @header("x-ms-request-id")
  requestId: string;

  /** The date the request was processed, in RFC 7231 format. */
  @header("Date")
  date: string;
}

// ---------------------------------------------------------------------------
// Common parameters for all endpoints
// ---------------------------------------------------------------------------

model RequestParams {
  /** The semantic version of the extension. */
  @path extensionVersion: string;

  ...RequestHeaders;
}

// ---------------------------------------------------------------------------
// Response templates
// ---------------------------------------------------------------------------

/** Standard 200 OK response. */
model Ok<T> {
  @statusCode statusCode: 200;
  ...ResponseHeaders;
  @body body: T;
}

/** Standard 202 Accepted response for long-running operations. */
model Accepted<T> {
  @statusCode statusCode: 202;
  ...ResponseHeaders;
  @body body: T;
}

/** Standard 204 No Content response. */
model NoContent {
  @statusCode statusCode: 204;
  ...ResponseHeaders;
}

/** Standard 404 Not Found error response. */
model NotFound {
  @statusCode statusCode: 404;
  ...ResponseHeaders;
  @body body: ErrorResponse;
}

/** Generic error response for 4xx and 5xx status codes. */
model Failure {
  @minValue(400)
  @maxValue(599)
  @statusCode
  statusCode: int32;

  ...ResponseHeaders;
  @body body: ErrorResponse;
}

// ---------------------------------------------------------------------------
// Resource endpoints
// ---------------------------------------------------------------------------

@route("/{extensionVersion}/resource")
interface HttpResourceActions {
  /**
   * Preview a resource creation or update operation by simulating the process.
   *
   * See the [contract documentation](https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/contract.md#preview-resource)
   * for detailed requirements including handling of unevaluated expressions, preview metadata,
   * configuration echoing, and configuration ID.
   */
  @route("/preview")
  @post
  @operationId("previewResource")
  @externalDocs(
    "https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/contract.md#preview-resource",
    "Preview Resource Contract"
  )
  previewResource(
    ...RequestParams,
    @body body: ResourcePreviewSpecification,
  ): Ok<ResourcePreview> | Failure;

  /**
   * Create or update a resource.
   *
   * Returns 200 OK with the resource if the operation completes synchronously,
   * or 202 Accepted with a LongRunningOperation if it is long-running.
   *
   * See the [contract documentation](https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/contract.md#create-or-update-resource)
   * for detailed requirements including identifiers, configuration echoing, and configuration ID.
   */
  @route("/createOrUpdate")
  @post
  @operationId("createOrUpdateResource")
  @externalDocs(
    "https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/contract.md#create-or-update-resource",
    "Create or Update Resource Contract"
  )
  createOrUpdateResource(
    ...RequestParams,
    @body body: ResourceSpecification,
  ): Ok<Resource> | Accepted<LongRunningOperation> | Failure;

  /**
   * Get a resource given its type, identifiers, and configuration.
   *
   * Returns the current state of the resource, or 404 Not Found if it does not exist.
   *
   * See the [contract documentation](https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/contract.md#get-resource)
   * for detailed requirements.
   */
  @route("/get")
  @post
  @operationId("getResource")
  @externalDocs(
    "https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/contract.md#get-resource",
    "Get Resource Contract"
  )
  getResource(
    ...RequestParams,
    @body body: ResourceReference,
  ): Ok<Resource> | NotFound | Failure;

  /**
   * Delete a resource given its type, identifiers, configuration, and configuration ID.
   *
   * Returns 200 OK with a Resource if the operation completes synchronously or if the
   * extension uses the RELO pattern (the resource will have a non-terminal status),
   * 204 No Content if the resource is deleted synchronously or does not exist, or
   * 202 Accepted with a LongRunningOperation if the deletion uses the LRO pattern.
   *
   * See the [contract documentation](https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/contract.md#delete-resource)
   * for detailed requirements.
   */
  @route("/delete")
  @post
  @operationId("deleteResource")
  @externalDocs(
    "https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/contract.md#delete-resource",
    "Delete Resource Contract"
  )
  deleteResource(...RequestParams, @body body: ResourceReference):
    | Ok<Resource>
    | NoContent
    | Accepted<LongRunningOperation>
    | Failure;
}

// ---------------------------------------------------------------------------
// Long-running operation endpoints
// ---------------------------------------------------------------------------

@route("/{extensionVersion}/longRunningOperation")
interface HttpLongRunningOperationActions {
  /**
   * Retrieve the status of a long-running operation.
   *
   * A Failure response indicates an issue retrieving the operation status
   * (e.g., network errors), NOT a failure of the operation itself.
   * Operation failures are reported via the operation's status field.
   *
   * See the [contract documentation](https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/contract.md#get-long-running-operation)
   * for detailed requirements.
   */
  @route("/get")
  @post
  @operationId("getLongRunningOperation")
  @externalDocs(
    "https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/contract.md#get-long-running-operation",
    "Get Long-Running Operation Contract"
  )
  getLongRunningOperation(
    ...RequestParams,
    @body body: Record<unknown>,
  ): Ok<LongRunningOperation> | Failure;
}
