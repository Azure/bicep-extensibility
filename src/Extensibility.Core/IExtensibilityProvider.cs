// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Extensibility.Core.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace Extensibility.Core.Contract
{
    public interface IExtensibilityProvider
    {
        Task<SaveResponse> Save(SaveRequest request, CancellationToken cancellationToken);

        Task<PreviewSaveResponse> PreviewSave(PreviewSaveRequest request, CancellationToken cancellationToken);

        Task<GetResponse> Get(GetRequest request, CancellationToken cancellationToken);

        Task<DeleteResponse> Delete(DeleteRequest request, CancellationToken cancellationToken);
    }
}
