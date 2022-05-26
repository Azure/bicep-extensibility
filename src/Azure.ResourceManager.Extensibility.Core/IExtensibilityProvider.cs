namespace Azure.ResourceManager.Extensibility.Core
{
    public delegate Task<ExtensibilityOperationResponse> ExtensibilityOperation(ExtensibilityOperationRequest request, CancellationToken cancellationToken);

    public interface IExtensibilityProvider
    {
        Task<ExtensibilityOperationResponse> SaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken);

        Task<ExtensibilityOperationResponse> PreviewSaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken);

        Task<ExtensibilityOperationResponse> GetAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken);

        Task<ExtensibilityOperationResponse> DeleteAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken);
    }
}