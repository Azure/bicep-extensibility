// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Core
{
    public delegate Task<TResponse> ExtensibilityOperation<TResponse>(ExtensibilityOperationRequest request, CancellationToken cancellationToken);

    public interface IExtensibilityProvider
    {
        /// <summary>
        /// Saves an extensible resource.
        /// </summary>
        /// <param name="request">The save operation request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<object> SaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Previews the result of saving an extensible resource.
        /// </summary>
        /// <param name="request">The preview save operation request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<object> PreviewSaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Gets an extensible resource.
        /// </summary>
        /// <param name="request">The get operation request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<object> GetAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes an extensible resource.
        /// </summary>
        /// <param name="request">The delete operation request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<object> DeleteAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken);
    }
}
