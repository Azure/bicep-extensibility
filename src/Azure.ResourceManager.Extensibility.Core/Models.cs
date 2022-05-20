using Json.Path;
using Json.Pointer;
using System.Text.Json;

namespace Azure.ResourceManager.Extensibility.Core
{
    public record ExtensibleImport<T>(string Provider, string Version, T Config);

    public record ExtensibleResource<T>(string Type, T Properties);

    public record ExtensibleResourceMetadata(
        IEnumerable<JsonPath> ReadOnlyProperties,
        IEnumerable<JsonPath> ImmutableProperties,
        IEnumerable<JsonPath> DynamicProperties);

    public record ExtensibilityError(string Code, JsonPointer Target, string Message);

    public record ExtensibilityRequest<TImport, TConfig>(ExtensibleImport<TImport> Import, ExtensibleResource<TConfig> Resource);

    public record ExtensibilityRequest(ExtensibleImport<JsonElement> Import, ExtensibleResource<JsonElement> Resource)
        : ExtensibilityRequest<JsonElement, JsonElement>(Import, Resource);

    public abstract record ExtensibilityResponse(ExtensibleResource<JsonElement>? Resource, ExtensibleResourceMetadata? ResourceMetadata, IEnumerable<ExtensibilityError>? Errors);

    public record ExtensibilitySuccessResponse(ExtensibleResource<JsonElement> Resource, ExtensibleResourceMetadata? ResourceMetadata = null)
        : ExtensibilityResponse(Resource, ResourceMetadata, null);

    public record ExtensibilityErrorResponse(IEnumerable<ExtensibilityError> Errors)
        : ExtensibilityResponse(null, null, Errors);
}
