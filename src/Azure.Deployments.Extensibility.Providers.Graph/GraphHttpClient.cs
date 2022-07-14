// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Azure.Deployments.Extensibility.Providers.Graph
{
    public class GraphHttpClient
    {
        private HttpClient _httpClient;
        private string _baseUri;

        public GraphHttpClient()
        {
            _httpClient = new HttpClient();
            _baseUri = "https://graph.microsoft.com/v1.0";
        }

        public virtual async Task<string> GetAsync(string uri, string graphToken, CancellationToken cancellationToken)
        {
            var completeUri = $"{this._baseUri}/{uri}";
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, completeUri);
            httpRequestMessage.Headers.Add("Authorization", $"Bearer {graphToken}");

            var response = await SendAsync(httpRequestMessage, cancellationToken);
            return HandleResponse(response);
        }

        public virtual async Task<string> PutAsync(string uri, JsonObject properties, string graphToken, CancellationToken cancellationToken)
        {
            var completeUri = $"{this._baseUri}/{uri}";
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, completeUri);
            httpRequestMessage.Headers.Add("Authorization", $"Bearer {graphToken}");
            httpRequestMessage.Content = new StringContent(properties.ToJsonString(), Encoding.UTF8, "application/json");

            var response = await SendAsync(httpRequestMessage, cancellationToken);
            return HandleResponse(response);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken)
        {
            return await _httpClient.SendAsync(httpRequestMessage, cancellationToken);
        }

        private string HandleResponse(HttpResponseMessage response)
        {
            var content = response.Content.ReadAsStringAsync().Result;
            if (response.IsSuccessStatusCode)
            {
                return content;
            }
            else
            {
                throw new GraphHttpException((int)response.StatusCode, content);
            }
        }
    }
}

