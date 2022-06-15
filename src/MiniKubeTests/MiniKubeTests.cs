using Azure.Deployments.Extensibility.Providers.Kubernetes.Models;
using FluentAssertions;
using k8s;
using Xunit;
using Xunit.Abstractions;

namespace MiniKubeTests
{
    public class MiniKubeTests
    {
        private readonly ITestOutputHelper testOutput;

        public MiniKubeTests(ITestOutputHelper testOutput)
        {
            this.testOutput = testOutput;
        }

        [Fact]
        public async Task TestMiniKubeConnection()
        {
            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var kubeConfigPath = Path.Combine(homeDirectory, ".kube", "config");

            var kubeConfigContent = File.ReadAllText(kubeConfigPath);

            this.testOutput.WriteLine(kubeConfigContent);

            var kubeConfigBytes = File.ReadAllBytes(kubeConfigPath);
            var kubernetesConfig = new KubernetesConfig("default", kubeConfigBytes, null);
            var kubernetes = CreateKubernetes(kubernetesConfig);

            var namespaces = await kubernetes.CoreV1.ListNamespaceAsync();

            namespaces.Items.Should().NotBeEmpty();
        }

        private static IKubernetes CreateKubernetes(KubernetesConfig config) => new k8s.Kubernetes(
            KubernetesClientConfiguration.BuildConfigFromConfigFile(
                new MemoryStream(config.KubeConfig),
                currentContext: config.Context));
    }
}