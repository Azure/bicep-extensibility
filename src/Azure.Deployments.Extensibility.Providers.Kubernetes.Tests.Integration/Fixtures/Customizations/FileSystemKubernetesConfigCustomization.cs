using AutoFixture;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Models;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Integration.Fixtures.Customizations
{
    public class FileSystemKubernetesConfigCustomization : ICustomization
    {
        private static readonly byte[] KubeConfig = LoadKubeConfig();

        private readonly string @namespace;

        public FileSystemKubernetesConfigCustomization(string @namespace)
        {
            this.@namespace = @namespace;
        }

        public void Customize(IFixture fixture)
        {
            fixture.Customize<KubernetesConfig>(composer => composer
                .With(x => x.Namespace, this.@namespace)
                .With(x => x.KubeConfig, KubeConfig)
                .With(x => x.Context, value: null));
        }

        private static byte[] LoadKubeConfig()
        {
            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var kubeConfigPath = Path.Combine(homeDirectory, ".kube", "config");

            return File.ReadAllBytes(kubeConfigPath);
        }
    }
}
