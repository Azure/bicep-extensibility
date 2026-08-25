# Build a self-hosted FirstParty extension

> [!IMPORTANT]
> `Azure.Deployments.Extensibility.Hosting.FirstParty` is a closed-source,
> Microsoft-internal SDK. Its implementation and detailed setup documentation are
> planned for the Blueprint Azure DevOps repository. It is not yet ready for extension
> teams to consume.

Use the FirstParty SDK only when a Microsoft extension is closely integrated with the
ARM deployment service and the extension team will host and operate its own service.
The Microsoft Graph extension is an example. This model does not use the managed
extension runtime.

FirstParty hosting supports team-owned service integration, Microsoft-specific
identity, and multiple extension version ranges. It also requires the extension team
to own service deployment, availability, monitoring, and operations.

Microsoft teams that do not require this model can use
[Hosting.Managed](../sdks/managed.md) and the managed extension runtime.

The internal quickstart link will be added here after the FirstParty SDK is implemented.

In the meantime, the following guidance is shared by both hosting models:

1. [Typed handlers](../tutorials/typed-handlers.md)
2. [Behaviors](../tutorials/behaviors.md)
3. [Validation](../tutorials/validators.md)
4. [Preview](../contract/preview-operation.md)
5. [Long-running operations](../contract/async-operations.md)
6. [API Explorer](../tutorials/api-explorer.md)
7. [API contract](../contract/contract.md)
