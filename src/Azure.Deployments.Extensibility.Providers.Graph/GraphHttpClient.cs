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
        private string _graphToken;
        private CancellationToken _cancellationToken;
        private string _baseUri;
        public GraphHttpClient(string graphToken, CancellationToken cancellationToken)
        {
            _httpClient = new HttpClient();
            _graphToken = graphToken;
            _cancellationToken = cancellationToken;
            _baseUri = "https://graph.microsoft.com/v1.0";
        }

        public async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request)
        {
            return await this._httpClient.SendAsync(request);
        }

        public string CallGraph(string uri, HttpMethod method)
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(method, uri);

            HttpResponseMessage response = SendRequest(httpRequestMessage).Result;
            string responseJson = response.Content.ReadAsStringAsync().Result;

            return responseJson.ToString();
        }

        public async Task<string> GetAsync(string uri)
        {
            var completeUri = $"{this._baseUri}/{uri}";
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, completeUri);
            httpRequestMessage.Headers.Add("Authorization", $"Bearer {this._graphToken}");

            var response = await SendAsync(httpRequestMessage);
            return HandleResponse(response);
        }

        public async Task<string> PutAsync(string uri, JsonObject properties)
        {
            var completeUri = $"{this._baseUri}/{uri}";
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, completeUri);
            httpRequestMessage.Headers.Add("Authorization", $"Bearer {this._graphToken}");
            httpRequestMessage.Content = new StringContent(properties.ToJsonString(), Encoding.UTF8, "application/json");

            var response = await SendAsync(httpRequestMessage);
            return HandleResponse(response);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage)
        {
            return await _httpClient.SendAsync(httpRequestMessage, this._cancellationToken);
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

