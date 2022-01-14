using System;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using System.IO;
using Newtonsoft.Json;
using System.Net;

namespace Extensibility.Host
{
    public static class RequestHelper
    {
        public static async Task<HttpResponseData> Handle<TRequest, TResponse>(
            HttpRequestData request,
            FunctionContext context,
            Func<TRequest, Task<TResponse>> handleFunc)
        {
            var logger = context.GetLogger(context.FunctionDefinition.Name);
            ServiceClientTracing.IsEnabled = true;
            ServiceClientTracing.AddTracingInterceptor(new LoggerTracingInterceptor(logger));

            logger.LogInformation($"Received request {context.FunctionDefinition.Name}");

            var requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            var requestObj = JsonConvert.DeserializeObject<TRequest>(requestBody);

            var responseObj = await handleFunc(requestObj);

            var response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(responseObj);

            return response;
        }
    }
}
