using Azure.ResourceManager.Extensibility.Core;
using FluentValidation;
using Json.Pointer;

namespace Azure.ResourceManager.Extensibility.Registries
{
    public class FirstPartyExtensibilityProviderProxy : IExtensibilityProvider
    {
        private readonly IExtensibilityProvider provider;

        public FirstPartyExtensibilityProviderProxy(IExtensibilityProvider provider)
        {
            this.provider = provider;
        }

        public Task<ExtensibilityResponse> DeleteAsync(ExtensibilityRequest request, CancellationToken cancellationToken) =>
            HandleValidationException(this.provider.DeleteAsync)(request, cancellationToken);

        public Task<ExtensibilityResponse> GetAsync(ExtensibilityRequest request, CancellationToken cancellationToken) =>
            HandleValidationException(this.provider.GetAsync)(request, cancellationToken);

        public Task<ExtensibilityResponse> PreviewSaveAsync(ExtensibilityRequest request, CancellationToken cancellationToken) =>
            HandleValidationException(this.provider.PreviewSaveAsync)(request, cancellationToken);

        public Task<ExtensibilityResponse> SaveAsync(ExtensibilityRequest request, CancellationToken cancellationToken) =>
            HandleValidationException(this.provider.SaveAsync)(request, cancellationToken);
        

        private static ExtensibilityOperation HandleValidationException(ExtensibilityOperation operation)
        {
            return async (ExtensibilityRequest request, CancellationToken cancellationToken) =>
            {
                try
                {
                    return await operation.Invoke(request, cancellationToken);
                }
                catch (ValidationException validationException)
                {
                    var extensibilityErrors = new List<ExtensibilityError>();

                    foreach (var error in validationException.Errors)
                    {
                        var target = JsonPointer.Parse(error.PropertyName);
                        extensibilityErrors.Add(new(error.ErrorCode, target, error.ErrorMessage));
                    }

                    return new ExtensibilityErrorResponse(extensibilityErrors.ToArray());
                }
            };
        }
    }
}
