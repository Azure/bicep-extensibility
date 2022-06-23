// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Extensibility.Core.Messages;

namespace Extensibility.Host
{
    public static class Save
    {
        [Function(nameof(Save))]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            return await RequestHelper.Handle(
                req,
                context,
                async (SaveRequest request) => await Providers.TryGetProvider(request.Body!.Import!.Provider!)!.Save(request, CancellationToken.None));
        }
    }
}
