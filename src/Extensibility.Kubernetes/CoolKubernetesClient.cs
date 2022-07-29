// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using k8s.Models;
using Microsoft.Rest;

namespace Extensibility.Kubernetes
{
    // Like Kubernetes client but *cool*.
    //
    //
    // Seriously the problem is that GetAPIResources() doesn't accept a group-version. Wierd :-/.
    // HACK THE PLANET
    public class CoolKubernetesClient : k8s.Kubernetes
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        };

        public CoolKubernetesClient(k8s.KubernetesClientConfiguration clientConfig)
            : base(clientConfig)
        {
        }

        public async Task<V1APIResourceList> GetAPIResourcesAsync(
            string? group,
            string version,
            CancellationToken cancellationToken = default)
        {
            using (var _result = await this.GetAPIResourcesWithHttpMessagesAsync(
                group,
                version,
                null,
                cancellationToken).ConfigureAwait(false))
            {
                return _result.Body;
            }
        }

        public async Task<HttpOperationResponse<V1APIResourceList>> GetAPIResourcesWithHttpMessagesAsync(
            string? group,
            string version,
            Dictionary<string, string>? customHeaders = null,
            CancellationToken cancellationToken = default)
        {
            // Tracing
            bool _shouldTrace = ServiceClientTracing.IsEnabled;
            string? _invocationId = null;
            if (_shouldTrace)
            {
                _invocationId = ServiceClientTracing.NextInvocationId.ToString();
                Dictionary<string, object> tracingParameters = new Dictionary<string, object>();
                tracingParameters.Add("cancellationToken", cancellationToken);
                ServiceClientTracing.Enter(_invocationId, this, "GetAPIResources1", tracingParameters);
            }
            // Construct URL
            var urlSuffix = group switch {
                {} => $"/apis/{group}/{version}/",
                _ => $"/api/{version}/",
            };

            var _baseUrl = BaseUri.AbsoluteUri;
            var _url = new System.Uri(new System.Uri(_baseUrl + (_baseUrl.EndsWith("/") ? "" : "/")), urlSuffix).ToString();
            List<string> _queryParameters = new List<string>();
            if (_queryParameters.Count > 0)
            {
                _url += "?" + string.Join("&", _queryParameters);
            }
            // Create HTTP transport
            var _httpRequest = new HttpRequestMessage();
            HttpResponseMessage? _httpResponse = null;
            _httpRequest.Method = HttpMethod.Get;
            _httpRequest.RequestUri = new System.Uri(_url);
            // Set Headers

            if (customHeaders != null)
            {
                foreach(var _header in customHeaders)
                {
                    if (_httpRequest.Headers.Contains(_header.Key))
                    {
                        _httpRequest.Headers.Remove(_header.Key);
                    }
                    _httpRequest.Headers.TryAddWithoutValidation(_header.Key, _header.Value);
                }
            }

            // Serialize Request
            string? _requestContent = null;
            // Set Credentials
            if (Credentials != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Credentials.ProcessHttpRequestAsync(_httpRequest, cancellationToken).ConfigureAwait(false);
            }
            // Send Request
            if (_shouldTrace)
            {
                ServiceClientTracing.SendRequest(_invocationId, _httpRequest);
            }
            cancellationToken.ThrowIfCancellationRequested();
            _httpResponse = await HttpClient.SendAsync(_httpRequest, cancellationToken).ConfigureAwait(false);
            if (_shouldTrace)
            {
                ServiceClientTracing.ReceiveResponse(_invocationId, _httpResponse);
            }
            HttpStatusCode _statusCode = _httpResponse.StatusCode;
            cancellationToken.ThrowIfCancellationRequested();
            string? _responseContent = null;
            if ((int)_statusCode != 200 && (int)_statusCode != 201 && (int)_statusCode != 202)
            {
                var ex = new HttpOperationException(string.Format("Operation returned an invalid status code '{0}'", _statusCode));
                if (_httpResponse.Content != null) {
                    _responseContent = await _httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                else {
                    _responseContent = string.Empty;
                }
                ex.Request = new HttpRequestMessageWrapper(_httpRequest, _requestContent);
                ex.Response = new HttpResponseMessageWrapper(_httpResponse, _responseContent);
                if (_shouldTrace)
                {
                    ServiceClientTracing.Error(_invocationId, ex);
                }
                _httpRequest.Dispose();
                if (_httpResponse != null)
                {
                    _httpResponse.Dispose();
                }
                throw ex;
            }
            // Create Result
            var _result = new HttpOperationResponse<V1APIResourceList>();
            _result.Request = _httpRequest;
            _result.Response = _httpResponse;
            // Deserialize Response
            if ((int)_statusCode == 200 || (int)_statusCode == 201 || (int)_statusCode == 202)
            {
                _responseContent = await _httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                try
                {
                    _result.Body = JsonSerializer.Deserialize<V1APIResourceList>(_responseContent, JsonSerializerOptions)!;
                }
                catch (JsonException ex)
                {
                    _httpRequest.Dispose();
                    if (_httpResponse != null)
                    {
                        _httpResponse.Dispose();
                    }
                    throw new SerializationException("Unable to deserialize the response.", _responseContent, ex);
                }
            }
            if (_shouldTrace)
            {
                ServiceClientTracing.Exit(_invocationId, _result);
            }
            return _result;

        }
    }
}
