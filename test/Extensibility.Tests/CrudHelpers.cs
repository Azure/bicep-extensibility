namespace Extensibility.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility.Core.Contract;
    using Extensibility.Core.Data;

    public static class CrudHelper
    {
        private static IExtensibilityProvider GetResourceOperations(ExtensibleResourceBody resource)
        {
            var providerName = resource.Import?.Provider ?? throw new InvalidOperationException($"resource.Import.Provider should not be null");
            var typeString = resource.Type ?? throw new InvalidOperationException($"resource.Type should not be null");

            return Providers.TryGetProvider(providerName) ?? throw new InvalidOperationException($"Failed to find provider \"{providerName}\"");
        }

        public static async Task<ExtensibleResourceBody> Delete(ExtensibleResourceBody resource, CancellationToken cancellationToken)
        {
            var result = await GetResourceOperations(resource).Delete(new() { Body = resource }, cancellationToken);

            return result.Body!;
        }

        public static async Task<ExtensibleResourceBody> Get(ExtensibleResourceBody resource, CancellationToken cancellationToken)
        {
            var result = await GetResourceOperations(resource).Get(new() { Body = resource }, cancellationToken);

            return result.Body!;
        }

        public static async Task<ExtensibleResourceBody> PreviewSave(ExtensibleResourceBody resource, CancellationToken cancellationToken)
        {
            var result = await GetResourceOperations(resource).PreviewSave(new() { Body = resource }, cancellationToken);

            return result.Body!;
        }

        public static async Task<ExtensibleResourceBody> Save(ExtensibleResourceBody resource, CancellationToken cancellationToken)
        {
            var result = await GetResourceOperations(resource).Save(new() { Body = resource }, cancellationToken);

            return result.Body!;
        }
    }
}
