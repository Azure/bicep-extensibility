using Json.Pointer;
using System.Linq.Expressions;

namespace Azure.ResourceManager.Extensibility.Core.Extensions
{
    public static class ExtensibleImportExtensions
    {
        public static JsonPointer GetJsonPointer<TConfig>(this ExtensibleImport<TConfig> import) =>
            JsonPointer.Create("imports", import.SymbolicName);

        public static JsonPointer GetJsonPointer<TConfig>(this ExtensibleImport<TConfig> import, Expression<Func<ExtensibleImport<TConfig>, object>> expression) =>
            import.GetJsonPointer().Combine(expression.ToJsonPointer());
    }
}
