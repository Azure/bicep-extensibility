namespace Azure.Deployments.Extensibility.Core
{
    public interface IExtensibilityProviderRegistry
    {
        IExtensibilityProvider? TryGetExtensibilityProvider(string providerName);
    }
}
