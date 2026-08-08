# Azure Deployments Extensibility Provider Contract

## Extensible Resource Operation API Reference

The following APIs must be implemented for each resource type by deployments extensibility providers:

- `/get` - Gets a resource.
- `/save` - performs an idempotent upsert on a resource.
- `/previewSave` - Previews `/save` for a resource.
- `/delete` - Deletes a resource.

> For extensibility public preview, the APIs will have to be implemented in C# by first party extensibility providers. The deployments team has created a nuget package ([Azure.Deployments.Extensibility.Core](https://www.nuget.org/packages/Azure.Deployments.Extensibility.Core)) to help first party providers with the development of the APIs. The [Kubernetes provider](../src/Azure.Deployments.Extensibility.Providers.Kubernetes/) can be used as a reference implementation that demonstrates how the extensibility core package can be used.

To learn more the details of the APIs, check the [Swagger definition](extensibility-provider-api.json) or got to [bicep-extensibility-api-docs](https://bicep-extensibility-dev-host.azurewebsites.net/api-docs/index.html) for a better reading experience.
