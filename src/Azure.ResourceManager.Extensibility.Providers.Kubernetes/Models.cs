using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azure.ResourceManager.Extensibility.Providers.Kubernetes
{
    public record KubernetesConfig(string Namespace, byte[] KubeConfig, string? Context);
}
