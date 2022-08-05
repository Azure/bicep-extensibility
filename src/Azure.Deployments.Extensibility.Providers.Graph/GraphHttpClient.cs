// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
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

        public virtual async Task<HttpResponseMessage> GetAsync(string uri, string graphInternalData, CancellationToken cancellationToken)
        {
            var completeUri = $"{this._baseUri}/{uri}";
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, completeUri);
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", graphInternalData);

            return await SendAsync(httpRequestMessage, cancellationToken);
        }

        public virtual async Task<HttpResponseMessage> PatchAsync(string uri, JsonObject properties, string graphInternalData, CancellationToken cancellationToken)
        {
            var completeUri = $"{this._baseUri}/{uri}";
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Patch, completeUri);
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", graphInternalData);
            httpRequestMessage.Content = new StringContent(properties.ToJsonString(), Encoding.UTF8, "application/json");

            return await SendAsync(httpRequestMessage, cancellationToken);
        }

        public virtual async Task<HttpResponseMessage> PostAsync(string uri, JsonObject properties, string graphInternalData, CancellationToken cancellationToken)
        {
            var completeUri = $"{this._baseUri}/{uri}";
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, completeUri);
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", graphInternalData);
            httpRequestMessage.Content = new StringContent(properties.ToJsonString(), Encoding.UTF8, "application/json");

            return await SendAsync(httpRequestMessage, cancellationToken);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken)
        {
            return await _httpClient.SendAsync(httpRequestMessage, cancellationToken);
        }
    }
}

