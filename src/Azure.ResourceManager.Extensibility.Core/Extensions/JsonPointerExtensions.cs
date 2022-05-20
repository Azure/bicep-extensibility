using Json.Pointer;
using System.Text.Json;

namespace Azure.ResourceManager.Extensibility.Core.Extensions
{
    public static class JsonPointerExtensions
    {
        public static JsonPointer CamelCase(this JsonPointer pointer) => JsonPointer.Create(
            pointer.Segments
                .Select(x => JsonNamingPolicy.CamelCase.ConvertName(x.Source))
                .Select(x => PointerSegment.Create(x)),
            isUriEncoded: false);
    }
}
