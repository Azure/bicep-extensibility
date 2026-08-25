# Hosting SDKs

> [!WARNING]
> The hosting SDKs are a work in progress and are not ready for extension-author
> consumption. Their APIs and package behavior may change before release.

Choose a complete hosting SDK based on who operates the extension service. The
AspNetCore package is a shared base, not the default starting point for extension
applications.

| Package | Choose it when | Documentation |
|---|---|---|
| `Azure.Deployments.Extensibility.Hosting.Managed` | The platform will run the extension. External authors and Microsoft teams can use this model. | [Managed hosting](managed.md) |
| `Azure.Deployments.Extensibility.Hosting.FirstParty` | A Microsoft team will host and operate a service that integrates closely with ARM deployments. | [FirstParty handoff](../get-started/first-party.md); internal SDK link pending |
| `Azure.Deployments.Extensibility.AspNetCore` | You are implementing a hosting SDK or custom host and need to own hosting policy. | [AspNetCore base SDK](aspnetcore.md) |
| `Azure.Deployments.Extensibility.Core` | You need the shared models, handler contracts, errors, or validation APIs. | [Core SDK](core.md) |

## Recommended reading

### Managed extension runtime

1. [Build a managed extension](../tutorials/getting-started.md)
2. [Managed hosting](managed.md)
3. [Authoring guides](../tutorials/index.md)

### Self-hosted FirstParty service

1. [FirstParty handoff](../get-started/first-party.md)
2. Internal FirstParty SDK quickstart when available
3. [Authoring guides](../tutorials/index.md)

### Hosting SDK authors

1. [AspNetCore base SDK](aspnetcore.md)
2. [API contract](../contract/contract.md)
3. <xref:Azure.Deployments.Extensibility.AspNetCore>
