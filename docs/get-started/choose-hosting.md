# Choose a hosting SDK

> [!WARNING]
> These SDKs are under active development and are not yet ready for extension authors
> to consume. Use this guide to understand the planned hosting model only.

Choose based on who operates the extension service. Microsoft affiliation alone does
not require FirstParty hosting.

## Decision guide

### Use Hosting.Managed when the platform runs the extension

Choose `Azure.Deployments.Extensibility.Hosting.Managed` when you want to use the
managed extension runtime. This is the standard choice for external extension authors.
Microsoft teams can also choose it when they do not need to operate a dedicated
extension service.

The Managed SDK:

- hosts one exact extension version per process;
- reads the extension name and version from project metadata;
- configures the standard middleware and contract endpoints;
- exposes `GET /ping`;
- supports the shared Bicep Extension API Explorer.

Continue to [Build a managed extension](../tutorials/getting-started.md).

### Use Hosting.FirstParty when your team runs the extension service

Choose `Azure.Deployments.Extensibility.Hosting.FirstParty` only for a Microsoft
extension that is closely integrated with the ARM deployment service and must be
hosted and operated by the extension team. The Microsoft Graph extension is an example
of this model. FirstParty hosting supports team-owned service integration, first-party
identity, and version-range selection without the managed extension runtime.

If the managed runtime meets the team's needs, use Hosting.Managed instead.

Continue to [Build a self-hosted FirstParty extension](first-party.md).

### Use AspNetCore only to build a host

`Azure.Deployments.Extensibility.AspNetCore` is the shared foundation beneath both
hosting SDKs. Use it directly only when you are implementing another hosting SDK or
need full control over version resolution, middleware ordering, route groups, and
endpoint mapping.

It does not provide a complete hosting policy by itself. See the
[AspNetCore base SDK](../sdks/aspnetcore.md).

## Shared authoring model

All three choices use the same handlers, typed handlers, behaviors, contract models,
validation framework, and API Explorer. Choosing a host does not change how resource
operations are implemented.
