using Json.Pointer;
using System.Linq.Expressions;

namespace Azure.ResourceManager.Extensibility.Core.Extensions
{
    public static class ExtensibleResourceExtensions
    {
        public static JsonPointer GetJsonPointer<TProperty>(this ExtensibleResource<TProperty> resource) =>
            JsonPointer.Create("resources", resource.SymbolicName);

        public static JsonPointer GetJsonPointer<TProperty>(this ExtensibleResource<TProperty> resource, Expression<Func<ExtensibleResource<TProperty>, object>> expression) =>
            resource.GetJsonPointer().Combine(expression.ToJsonPointer());

    }
}
