// ---------------------------------------------------------------------------
// Bicep Extensibility Provider API — Core Contract
// ---------------------------------------------------------------------------
// Transport-agnostic models and operation signatures for the V2 API.
//
// Models mirror the C# V2 contract models defined in
// src/Azure.Deployments.Extensibility.Core/V2/Contracts/Models/
//
// Operation interfaces define the logical API surface independent of any
// transport protocol. The HTTP binding in http.tsp layers on routes,
// methods, headers, and status codes.
// ---------------------------------------------------------------------------

// ===========================================================================
// Scalars & unions
// ===========================================================================

/**
 * An RFC 6901 JSON Pointer string (e.g., "/properties/name").
 * Used throughout the API to reference specific locations in a JSON document.
 */
scalar JsonPointer extends string;

/**
 * Known terminal status values for resource and long-running operations.
 *
 * The `string` member keeps the union open — extensions may define their own
 * non-terminal statuses (e.g., "Running", "Provisioning").
 */
union OperationStatus {
  string,

  /** The operation completed successfully. */
  Succeeded: "Succeeded",

  /** The operation failed. */
  Failed: "Failed",

  /** The operation was canceled. */
  Canceled: "Canceled",
}

// ===========================================================================
// Error models
// ===========================================================================

/**
 * Represents detailed information about an error, including a code, message,
 * and an optional target reference.
 */
model ErrorDetail {
  /** A service-defined error code that classifies the error. */
  code: string;

  /** A human-readable description of the error. */
  message: string;

  /**
   * The target of the error.
   * This should be a JSON Pointer that indicates the location in the request
   * payload that caused the error.
   */
  target?: JsonPointer;
}

/**
 * Error model based on the OData v4 error response format.
 *
 * The ARM Template deployment service converts this format to the ARM error
 * response format before returning it to the client.
 */
model Error {
  /** A service-defined error code that classifies the error. */
  code: string;

  /** A human-readable description of the error. */
  message: string;

  /**
   * The target of the error.
   * This should be a JSON Pointer that indicates the location in the request
   * payload that caused the error.
   */
  target?: JsonPointer;

  /** An array of additional error details about specific errors that led to this reported error. */
  details?: ErrorDetail[];

  /**
   * An object containing more specific information about the error.
   * The contents of this object are service-defined and may contain additional
   * nested error information.
   */
  innererror?: Record<unknown>;
}

/** A wrapper for Error to be used as an extensibility API error response. */
model ErrorResponse {
  /** The error object containing detailed error information. */
  error: Error;
}

// ===========================================================================
// Request models
// ===========================================================================

/**
 * Represents a reference to a resource, containing the information needed to
 * uniquely identify it (type, API version, identifiers, and optional config).
 * Used as input to get and delete operations.
 */
model ResourceReference {
  /** The type of the resource. */
  type: string;

  /** The API version of the resource. */
  apiVersion?: string;

  /**
   * The identifier properties that, combined with the resource type and
   * configuration, form a globally unique key for the resource.
   * The structure is defined by the extension and is opaque to the
   * deployment engine.
   */
  identifiers: Record<unknown>;

  /** The extension configuration for the resource, if any. */
  config?: Record<unknown>;

  /**
   * A checksum identifying the configuration.
   * If present, the extension should validate it against the config.
   * Required for delete operations when a configuration is present.
   */
  configId?: string;
}

/**
 * Represents what the user declares in their template to specify the desired
 * state of a resource. Used as input to create or update operations.
 */
model ResourceSpecification {
  /** The type of the resource. */
  type: string;

  /** The API version of the resource. */
  apiVersion?: string;

  /** The desired properties of the resource. */
  properties: Record<unknown>;

  /**
   * The extension configuration.
   * The extension should echo this back in the response, minus any secret
   * properties. Properties not echoed back are treated as secrets by the
   * deployment engine.
   */
  config?: Record<unknown>;

  /**
   * A checksum identifying the configuration.
   *
   * This is only set when the request is sent from the Deployment Stacks service.
   * Extensions should calculate the config ID based on the config and compare it
   * with this value. If they don't match, the extension should reject the request.
   */
  configId?: string;
}

/**
 * Represents the specification for a resource preview request.
 * Extends ResourceSpecification with optional preview metadata.
 */
model ResourcePreviewSpecification extends ResourceSpecification {
  /**
   * Optional metadata for the preview specification, such as properties that
   * could not be evaluated.
   */
  metadata?: ResourcePreviewSpecificationMetadata;
}

/**
 * Contains metadata for a resource preview specification, indicating which
 * properties could not be evaluated at the time of the preview request.
 */
model ResourcePreviewSpecificationMetadata {
  /**
   * JSON Pointers to the properties that could not be evaluated during the
   * preview. The values of these properties are ARM template expressions
   * (e.g., [reference(...)]).
   */
  unevaluated: JsonPointer[];
}

// ===========================================================================
// Response models
// ===========================================================================

/**
 * Represents an instance of a resource returned from the create or update,
 * get, or delete operations.
 */
model Resource {
  /** The type of the resource. */
  type: string;

  /** The API version of the resource. */
  apiVersion?: string;

  /**
   * The identifiers that, combined with the resource type and configuration,
   * form a globally unique key for the resource. The extension must always
   * include this in successful responses.
   */
  identifiers: Record<unknown>;

  /** The properties of the resource. */
  properties: Record<unknown>;

  /**
   * The configuration echoed back, with secret properties removed.
   * Must be set if the resource has a configuration. Properties not echoed
   * back are treated as secrets by the deployment engine.
   */
  config?: Record<unknown>;

  /**
   * A checksum identifying the configuration.
   * Must be set if the resource has a configuration. Used as a checksum
   * for subsequent operations such as deletion.
   */
  configId?: string;

  /**
   * The status of the resource. Used in RELO long-running operations where
   * a non-terminal status (e.g., "Running") indicates the operation is still
   * in progress. Once the operation completes, this transitions to a terminal
   * state ("Succeeded", "Failed", or "Canceled").
   */
  status?: OperationStatus;

  /**
   * Error information if the long-running resource operation failed.
   * This should only be set if `status` is "Failed".
   */
  error?: Error;
}

/**
 * Represents a preview of a resource, returned from the preview operation.
 * The preview should mirror what a get response would return as if the
 * create or update operation had been performed, including read-only
 * properties and properties with default values, and excluding write-only
 * properties such as passwords.
 */
model ResourcePreview {
  /** The type of the resource. */
  type: string;

  /** The API version of the resource. */
  apiVersion?: string;

  /** The identifiers that uniquely identify the resource. */
  identifiers: Record<unknown>;

  /**
   * The properties of the previewed resource. Should include read-only
   * properties and default values. Write-only properties (e.g., passwords)
   * should be excluded. Unevaluated expressions should be echoed as-is.
   */
  properties: Record<unknown>;

  /** The status of the resource. */
  status?: OperationStatus;

  /**
   * The configuration echoed back, with secret properties removed.
   * Must be set if the resource has a configuration.
   */
  config?: Record<unknown>;

  /**
   * A checksum identifying the configuration.
   * Must be set if the resource has a configuration.
   */
  configId?: string;

  /**
   * Metadata describing which properties in the preview are read-only,
   * immutable, unknown, calculated, or unevaluated. Providing this metadata
   * enables a more accurate and detailed preview experience for the end user.
   */
  metadata?: ResourcePreviewMetadata;
}

/**
 * Contains metadata about a resource preview, indicating which properties
 * are read-only, immutable, unknown, calculated, or unevaluated.
 */
model ResourcePreviewMetadata {
  /**
   * JSON Pointers to properties that are read-only.
   * These properties are managed by the service and cannot be set by the client.
   */
  readOnly?: JsonPointer[];

  /**
   * JSON Pointers to properties that are immutable.
   * These properties can be set during resource creation but cannot be
   * changed later.
   */
  immutable?: JsonPointer[];

  /**
   * JSON Pointers to properties whose values cannot be determined at preview
   * time (e.g., because they depend on unevaluated expressions or external
   * state).
   */
  `unknown`?: JsonPointer[];

  /**
   * JSON Pointers to properties whose values are calculated at operation time
   * and would change if the operation were performed later (e.g., timestamps).
   */
  calculated?: JsonPointer[];

  /**
   * JSON Pointers to properties containing unevaluated ARM template language
   * expressions. The extension must echo these back from the request.
   * A property in `unevaluated` in the request may be reclassified in the
   * response (e.g., as `readOnly` or `immutable`) if the extension can
   * determine the property's nature despite the unevaluated expression.
   */
  unevaluated?: JsonPointer[];
}

// ===========================================================================
// Long-running operation model
// ===========================================================================

/**
 * Represents the result of a long-running operation (LRO), including its
 * current status and optional retry, handle, and error information.
 * Used for stepwise long-running operations where the caller polls for
 * status using the operation handle.
 */
model LongRunningOperation {
  /**
   * The current status of the operation (e.g., "Succeeded", "Failed",
   * "Canceled", or a non-terminal status like "Running").
   */
  status: OperationStatus;

  /**
   * The recommended number of seconds to wait before polling the operation
   * status again. If not specified, the caller defaults to a 60-second
   * polling interval.
   */
  retryAfterSeconds?: int32;

  /**
   * An opaque handle used to track and poll the operation.
   * Must be specified if `status` is not terminal (i.e., not "Succeeded",
   * "Failed", or "Canceled").
   */
  operationHandle?: Record<unknown>;

  /**
   * Error information if the operation failed.
   * Must be set if `status` is "Failed" or "Canceled".
   */
  error?: Error;
}

// ===========================================================================
// Response type aliases
// ===========================================================================

/** Result of a preview operation: preview data or an error. */
alias PreviewResult = ResourcePreview | ErrorResponse;

/** Result of a get operation: resource data or an error. */
alias ResourceResult = Resource | ErrorResponse;

/** Result of a create/update or delete: resource, LRO, or an error. */
alias ResourceOrLroResult = Resource | LongRunningOperation | ErrorResponse;

// ===========================================================================
// Operations
// ===========================================================================

/**
 * Operations for managing extensibility resources.
 */
interface ResourceActions {
  /**
   * Preview a resource creation or update by simulating the process without
   * persisting any changes. The response should mirror what a get would
   * return as if the operation had been performed.
   */
  previewResource(specification: ResourcePreviewSpecification): PreviewResult;

  /**
   * Create or update a resource. May complete synchronously and return the
   * resource, or initiate a long-running operation.
   */
  createOrUpdateResource(
    specification: ResourceSpecification,
  ): ResourceOrLroResult;

  /**
   * Retrieve the current state of a resource. Returns an error if the
   * resource does not exist.
   */
  getResource(reference: ResourceReference): ResourceResult;

  /**
   * Delete a resource. May complete synchronously, indicate the resource was
   * already deleted, or initiate a long-running operation.
   */
  deleteResource(reference: ResourceReference): ResourceOrLroResult;
}

/**
 * Operations for polling stepwise long-running operations.
 */
interface LongRunningOperationActions {
  /**
   * Retrieve the current status of a long-running operation. An error
   * response indicates an issue retrieving the status (e.g., network errors),
   * NOT a failure of the operation itself. Operation failures are reported
   * via the `status` field set to "Failed" and the `error` property.
   */
  getLongRunningOperation(
    operationHandle: Record<unknown>,
  ): LongRunningOperation;
}
