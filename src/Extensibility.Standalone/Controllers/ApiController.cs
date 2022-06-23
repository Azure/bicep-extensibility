// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
        public ApiController()
        {
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] DeleteRequest request)
        {
            var provider = Providers.GetProvider(request.Body?.Import?.Provider);

            var response = await provider.Delete(request, CancellationToken.None);

            return new OkObjectResult(response);
        }

        [HttpPost("get")]
        public async Task<IActionResult> Get([FromBody] GetRequest request)
        {
            var provider = Providers.GetProvider(request.Body?.Import?.Provider);
            
            var response = await provider.Get(request, CancellationToken.None);

            return new OkObjectResult(response);
        }

        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] SaveRequest request)
        {
            var provider = Providers.GetProvider(request.Body?.Import?.Provider);

            var response = await provider.Save(request, CancellationToken.None);

            return new OkObjectResult(response);
        }

        [HttpPost("previewSave")]
        public async Task<IActionResult> PreviewSave([FromBody] PreviewSaveRequest request)
        {
            var provider = Providers.GetProvider(request.Body?.Import?.Provider);

            var response = await provider.PreviewSave(request, CancellationToken.None);

            return new OkObjectResult(response);
        }
    }
}
