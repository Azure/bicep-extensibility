using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Extensibility.Core.Messages;

namespace Extensibility.Host
{
    public static class PreviewSave
    {
        [Function(nameof(PreviewSave))]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            return await RequestHelper.Handle(
                req,
                context,
                async (PreviewSaveRequest request) => await Providers.TryGetProvider(request.Body!.Import!.Provider!)!.PreviewSave(request, CancellationToken.None));
        }
    }
}
