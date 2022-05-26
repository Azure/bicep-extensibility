using Json.Path;
using Json.Pointer;
using System.Text.Json;

namespace Azure.ResourceManager.Extensibility.Core
{
    public record ExtensibleImport<T>(string SymbolicName, string Provider, string Version, T Config);

    public record ExtensibleResource<T>(string SymbolicName, string Type, T Properties);

    public record ExtensibleResourceMetadata(
        IEnumerable<JsonPath> ReadOnlyProperties,
        IEnumerable<JsonPath> ImmutableProperties,
        IEnumerable<JsonPath> DynamicProperties);

    public record ExtensibilityError(string Code, JsonPointer Target, string Message);

    public record ExtensibilityOperationRequest<TImport, TConfig>(ExtensibleImport<TImport> Import, ExtensibleResource<TConfig> Resource);

    public record ExtensibilityOperationRequest(ExtensibleImport<JsonElement> Import, ExtensibleResource<JsonElement> Resource)
        : ExtensibilityOperationRequest<JsonElement, JsonElement>(Import, Resource);

    public abstract record ExtensibilityOperationResponse(ExtensibleResource<JsonElement>? Resource, ExtensibleResourceMetadata? ResourceMetadata, IEnumerable<ExtensibilityError>? Errors);

    public record ExtensibilityOperationSuccessResponse(ExtensibleResource<JsonElement> Resource, ExtensibleResourceMetadata? ResourceMetadata = null)
        : ExtensibilityOperationResponse(Resource, ResourceMetadata, null);

    public record ExtensibilityOperationErrorResponse(IEnumerable<ExtensibilityError> Errors)
        : ExtensibilityOperationResponse(null, null, Errors)
    {
        public ExtensibilityOperationErrorResponse(ExtensibilityError error, params ExtensibilityError[] additionalErrors)
            : this((new[] { error }).Concat(additionalErrors))
        {
        }
    }
}
