namespace Azure.ResourceManager.Extensibility.Core
{
    public interface IExtensibilityProviderRegistry
    {
        IExtensibilityProvider? TryGetExtensibilityProvider(string providerName);
    }
}
