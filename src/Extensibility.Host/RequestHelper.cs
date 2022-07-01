// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using System.Text.Json.Serialization;

namespace Extensibility.Host
{
    public static class RequestHelper
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        };

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
            var requestObj = JsonSerializer.Deserialize<TRequest>(requestBody, JsonSerializerOptions)!;

            var responseObj = await handleFunc(requestObj);

            var response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(JsonSerializer.Serialize(responseObj, JsonSerializerOptions));

            return response;
        }
    }
}
