using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Extensibility.Core.Messages;

namespace Extensibility.Host
{
    public static class Get
    {
        [Function(nameof(Get))]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            return await RequestHelper.Handle(
                req,
                context,
                async (GetRequest request) => await Providers.TryGetProvider(request.Body!.Import!.Provider!)!.Get(request, CancellationToken.None));
        }
    }
}
