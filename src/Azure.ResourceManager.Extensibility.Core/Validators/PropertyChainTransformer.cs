using FluentValidation;
using FluentValidation.Internal;
using Json.Pointer;
using System.Text.Json;

namespace Azure.ResourceManager.Extensibility.Core.Validators
{
    public static class PropertyChainConverter
    {
        public static JsonPointer ConvertToJsonPointer(PropertyChain propertyChain, string? propertyChainSeparator = null) =>
            ConvertToJsonPointer(propertyChain.ToString(), propertyChainSeparator);

        public static JsonPointer ConvertToJsonPointer(string propertyChain, string? propertyChainSeparator = null)
        {
            if (JsonPointer.TryParse(propertyChain, out var pointer) && pointer is not null)
            {
                return pointer;
            }

            propertyChainSeparator = ValidatorOptions.Global.PropertyChainSeparator;

            var memberNames = propertyChain.ToString().Split(ValidatorOptions.Global.PropertyChainSeparator);
            var memberSegments = memberNames
                .Select(JsonNamingPolicy.CamelCase.ConvertName)
                .Select(x => PointerSegment.Create(x));

            return JsonPointer.Create(memberSegments, isUriEncoded: false);
        }
    }
}
