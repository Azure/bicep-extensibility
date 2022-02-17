using System.Threading;
using System.Threading.Tasks;
using Extensibility.Core.Messages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Newtonsoft.Json;

namespace Extensibility.Standalone
{
    [ApiController]
    [Route("[controller]")]
    public class ApiController
    {
        private ILogger<ApiController> _logger;

        public ApiController(ILogger<ApiController> logger)
        {
            _logger = logger;
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] DeleteRequest request)
        {
            ServiceClientTracing.IsEnabled = true;
            ServiceClientTracing.AddTracingInterceptor(new LoggerTracingInterceptor(_logger));
            _logger.LogInformation($"Received request Delete");
            var response = await Providers.TryGetProvider(request.Body!.Import!.Provider!)!.Delete(request, CancellationToken.None);

            return new OkObjectResult(response);
        }

        [HttpPost("get")]
        public async Task<IActionResult> Get([FromBody] GetRequest request)
        {
            ServiceClientTracing.IsEnabled = true;
            ServiceClientTracing.AddTracingInterceptor(new LoggerTracingInterceptor(_logger));
            _logger.LogInformation($"Received request Get");
            var response = await Providers.TryGetProvider(request.Body!.Import!.Provider!)!.Get(request, CancellationToken.None);

            return new OkObjectResult(response);
        }

        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] SaveRequest request)
        {
            ServiceClientTracing.IsEnabled = true;
            ServiceClientTracing.AddTracingInterceptor(new LoggerTracingInterceptor(_logger));
            _logger.LogInformation($"Received request Save");
            var response = await Providers.TryGetProvider(request.Body!.Import!.Provider!)!.Save(request, CancellationToken.None);

            return new OkObjectResult(response);
        }

        [HttpPost("previewSave")]
        public async Task<IActionResult> PreviewSave([FromBody] PreviewSaveRequest request)
        {
            ServiceClientTracing.IsEnabled = true;
            ServiceClientTracing.AddTracingInterceptor(new LoggerTracingInterceptor(_logger));
            _logger.LogInformation($"Received request PreviewSave");
            var response = await Providers.TryGetProvider(request.Body!.Import!.Provider!)!.PreviewSave(request, CancellationToken.None);

            return new OkObjectResult(response);
        }
    }
}
