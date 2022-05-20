using Azure.ResourceManager.Extensibility.Core.Exceptions;
using Json.Pointer;
using k8s.Autorest;

namespace Azure.ResourceManager.Extensibility.Providers.Kubernetes.Extensions
{
    public static class HttpOperationExceptionExtensions
    {
        public static ExtensibilityException ToExtensibilityException(this HttpOperationException exception) =>new(
            exception.Response.StatusCode.ToString(),
            JsonPointer.Empty,
            exception.Response.Content);
    }
}
