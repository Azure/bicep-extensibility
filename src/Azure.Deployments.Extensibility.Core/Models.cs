// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Path;
using Json.Pointer;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Azure.Deployments.Extensibility.Core
{
    /// <summary>
    /// Provides information about an imported extensibility provider.
    /// </summary>
    /// <typeparam name="T">The provider configuration type.</typeparam>
    /// <param name="Provider">The unique extensibility provider name.</param>
    /// <param name="Version">The version of the named extensibility provider.</param>
    /// <param name="Config">Provider-specific configuration.</param>
    public record ExtensibleImport<T>(string Provider, string Version, T Config);

    /// <summary>
    /// Provides information about an extensible resource.
    /// </summary>
    /// <typeparam name="T">The resource properties type parameter.</typeparam>
    /// <param name="Type">The type of the extensible resource.</param>
    /// <param name="Properties">The properties of the extensible resource.</param>
    public record ExtensibleResource<T>(string Type, T Properties);

    /// <summary>
    /// Provides metadata of an extensible resource.
    /// </summary>
    /// <param name="ReadOnlyProperties">Read-only properties of the extensible resource. A read-only property cannot be set by users.</param>
    /// <param name="ImmutableProperties">Immutable properties of the extensible resource. An immutable property cannot be updated after the resource is created.</param>
    /// <param name="DynamicProperties">Dynamic properties of the resource. A dynamic property's value is calculated at runtime.</param>
    public record ExtensibleResourceMetadata(
        IEnumerable<JsonPath>? ReadOnlyProperties,
        IEnumerable<JsonPath>? ImmutableProperties,
        IEnumerable<JsonPath>? DynamicProperties);

    /// <summary>
    /// Provides error details of a failed extensibility operation.
    /// </summary>
    /// <param name="Code">The error code.</param>
    /// <param name="Target">
    /// The JSON Pointer to the property causing the error. Must start with "/import" or "/resource" depending on
    /// which top level request property the error is on. When the error response is received by the deployments
    /// extensibility host, the JSON Pointer will be augumented with the import alias or resource symbolic name
    /// to make it easier for users to map the error to a location in the deployment template. For example,
    /// "/resource/properties/foo" will be converted to "resources/myResource/properties/foo".
    /// </param>
    /// <param name="Message">The error message.</param>
    public record ExtensibilityError(string Code, JsonPointer Target,  string Message);

    /// <summary>
    /// Provides information about an extensibility operation.
    /// </summary>
    /// <typeparam name="TConfig">The extensible config type parameter.</typeparam>
    /// <typeparam name="TProperties">The extensible resource properties type parameter.</typeparam>
    /// <param name="Import">The extensible import.</param>
    /// <param name="Resource">The extensible resource.</param>
    public record ExtensibilityOperationRequest<TConfig, TProperties>(ExtensibleImport<TConfig> Import, ExtensibleResource<TProperties> Resource);

    /// <summary>
    /// Provides information about an extensibility operation request.
    /// </summary>
    /// <param name="Import">The extensible import.</param>
    /// <param name="Resource">The extensible resource.</param>
    public record ExtensibilityOperationRequest(ExtensibleImport<JsonElement> Import, ExtensibleResource<JsonElement> Resource)
        : ExtensibilityOperationRequest<JsonElement, JsonElement>(Import, Resource);

    public abstract record ExtensibilityOperationResponse();

    /// <summary>
    /// Provides information about a successful extensibility operation.
    /// </summary>
    /// <param name="Resource">The updated extensible resource after the operation is complete.</param>
    /// <param name="ResourceMetadata">The extensible resource metadata. The metadata is needed by ARM template What-If to produce clean results. It should only be set for the "previewSave" operation.</param>
    public record ExtensibilityOperationSuccessResponse(ExtensibleResource<JsonElement> Resource, ExtensibleResourceMetadata? ResourceMetadata = null)
        : ExtensibilityOperationResponse();

    /// <summary>
    /// Provides information about a failed extensibility operation.
    /// </summary>
    public record ExtensibilityOperationErrorResponse : ExtensibilityOperationResponse
    {
        [JsonConstructor]
        public ExtensibilityOperationErrorResponse(IEnumerable<ExtensibilityError> errors)
        {
            this.Errors = errors;
        }

        /// <summary>
        /// The extensibility operation error response.
        /// </summary>
        /// <param name="error">The error to return.</param>
        /// <param name="additionalErrors">Additional errors to return.</param>
        public ExtensibilityOperationErrorResponse(ExtensibilityError error, params ExtensibilityError[] additionalErrors)
            : this((new[] { error }).Concat(additionalErrors))
        {
        }

        public IEnumerable<ExtensibilityError> Errors { get; }
    }
}
