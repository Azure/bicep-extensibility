# Get started

> [!WARNING]
> The extensibility SDKs are not yet ready for extension-author adoption. The paths in
> this section describe the intended experience and are provided for early evaluation,
> not production development.

Choose the path that matches how your extension will be hosted.

| Hosting model | Start here | Hosting SDK |
|---|---|---|
| The platform runs the extension | [Build a managed extension](../tutorials/getting-started.md) | `Azure.Deployments.Extensibility.Hosting.Managed` |
| Your Microsoft team runs a service that integrates directly with ARM deployments | [Build a self-hosted FirstParty extension](first-party.md) | `Azure.Deployments.Extensibility.Hosting.FirstParty` |
| A hosting SDK or custom ASP.NET Core host | [AspNetCore base SDK](../sdks/aspnetcore.md) | `Azure.Deployments.Extensibility.AspNetCore` |

If you are unsure, see [Choose a hosting SDK](choose-hosting.md). Most extension
authors should not use the AspNetCore package directly.

After completing the appropriate quickstart, continue with the shared
[authoring guides](../tutorials/index.md). Handler, behavior, validation, preview,
and long-running-operation concepts apply to both hosting models.
