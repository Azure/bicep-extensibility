// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.AspNetCore.Builders;

/// <summary>
/// A builder for providing extension-specific request and response examples
/// that are injected into the OpenAPI specification served by the Scalar API explorer.
/// </summary>
public class OpenApiExamplesBuilder
{
    internal record OperationExample(object? Request, object? Response);

    internal class OperationExamples
    {
        private readonly string _responseStatusCode;

        public OperationExamples(string responseStatusCode)
        {
            _responseStatusCode = responseStatusCode;
        }

        public string ResponseStatusCode => _responseStatusCode;

        public Dictionary<string, OperationExample> Examples { get; } = [];
    }

    internal Dictionary<string, OperationExamples> Operations { get; } = [];

    /// <summary>
    /// Adds named request and response examples for the resource preview operation.
    /// </summary>
    public OpenApiExamplesBuilder ForPreview(string name, object request, object response)
    {
        GetOrAdd("previewResource", "200").Examples[name] = new(request, response);
        return this;
    }

    /// <summary>
    /// Adds request and response examples for the resource preview operation.
    /// </summary>
    public OpenApiExamplesBuilder ForPreview(object request, object response)
        => ForPreview("default", request, response);

    /// <summary>
    /// Adds named request and response examples for the resource create or update operation.
    /// The response example is used for the 200 (synchronous completion) response.
    /// </summary>
    public OpenApiExamplesBuilder ForCreateOrUpdate(string name, object request, object response)
    {
        GetOrAdd("createOrUpdateResource", "200").Examples[name] = new(request, response);
        return this;
    }

    /// <summary>
    /// Adds request and response examples for the resource create or update operation.
    /// The response example is used for the 200 (synchronous completion) response.
    /// </summary>
    public OpenApiExamplesBuilder ForCreateOrUpdate(object request, object response)
        => ForCreateOrUpdate("default", request, response);

    /// <summary>
    /// Adds named request and response examples for the resource get operation.
    /// </summary>
    public OpenApiExamplesBuilder ForGet(string name, object request, object response)
    {
        GetOrAdd("getResource", "200").Examples[name] = new(request, response);
        return this;
    }

    /// <summary>
    /// Adds request and response examples for the resource get operation.
    /// </summary>
    public OpenApiExamplesBuilder ForGet(object request, object response)
        => ForGet("default", request, response);

    /// <summary>
    /// Adds named request and response examples for the resource delete operation.
    /// Pass <c>null</c> for <paramref name="response"/> to indicate a 204 No Content response.
    /// </summary>
    public OpenApiExamplesBuilder ForDelete(string name, object request, object? response = null)
    {
        GetOrAdd("deleteResource", response is not null ? "200" : "204").Examples[name] = new(request, response);
        return this;
    }

    /// <summary>
    /// Adds request and response examples for the resource delete operation.
    /// Pass <c>null</c> for <paramref name="response"/> to indicate a 204 No Content response.
    /// </summary>
    public OpenApiExamplesBuilder ForDelete(object request, object? response = null)
        => ForDelete("default", request, response);

    /// <summary>
    /// Adds named request and response examples for the long-running operation get endpoint.
    /// </summary>
    public OpenApiExamplesBuilder ForLongRunningOperationGet(string name, object request, object response)
    {
        GetOrAdd("getLongRunningOperation", "200").Examples[name] = new(request, response);
        return this;
    }

    /// <summary>
    /// Adds request and response examples for the long-running operation get endpoint.
    /// </summary>
    public OpenApiExamplesBuilder ForLongRunningOperationGet(object request, object response)
        => ForLongRunningOperationGet("default", request, response);

    private OperationExamples GetOrAdd(string operationId, string responseStatusCode)
    {
        if (!this.Operations.TryGetValue(operationId, out var op))
        {
            this.Operations[operationId] = op = new OperationExamples(responseStatusCode);
        }

        return op;
    }
}
