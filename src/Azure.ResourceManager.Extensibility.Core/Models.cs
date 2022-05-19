using Json.Path;
using Json.Pointer;
using System.Text.Json;

namespace Azure.ResourceManager.Extensibility.Core
{
    public record ExtensibleImport(string Provider, string Version, JsonElement Config);

    public record ExtensibleResource(string Type, JsonElement Properties);

    public record ExtensibleResourceMetadata(
        IEnumerable<JsonPath> ReadOnlyProperties,
        IEnumerable<JsonPath> ImmutableProperties,
        IEnumerable<JsonPath> DynamicProperties);

    public record ExtensibilityError(string Code, JsonPointer Target, string Message);

    public record ExtensibilityRequest(ExtensibleImport Import, ExtensibleResource Resource);

    public abstract record ExtensibilityResponse(ExtensibleResource? Resource, ExtensibleResourceMetadata? ResourceMetadata, IEnumerable<ExtensibilityError>? Errors);

    public record ExtensibilitySuccessResponse(ExtensibleResource Resource, ExtensibleResourceMetadata? ResourceMetadata = null)
        : ExtensibilityResponse(Resource, ResourceMetadata, null);

    public record ExtensibilityErrorResponse(IEnumerable<ExtensibilityError> Errors)
        : ExtensibilityResponse(null, null, Errors);
}
