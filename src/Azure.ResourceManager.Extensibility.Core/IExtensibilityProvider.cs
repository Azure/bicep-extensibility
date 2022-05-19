namespace Azure.ResourceManager.Extensibility.Core
{
    public delegate Task<ExtensibilityResponse> ExtensibilityOperation(ExtensibilityRequest request, CancellationToken cancellationToken);

    public interface IExtensibilityProvider
    {
        Task<ExtensibilityResponse> SaveAsync(ExtensibilityRequest request, CancellationToken cancellationToken);

        Task<ExtensibilityResponse> PreviewSaveAsync(ExtensibilityRequest request, CancellationToken cancellationToken);

        Task<ExtensibilityResponse> GetAsync(ExtensibilityRequest request, CancellationToken cancellationToken);

        Task<ExtensibilityResponse> DeleteAsync(ExtensibilityRequest request, CancellationToken cancellationToken);
    }
}