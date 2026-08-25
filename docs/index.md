# Bicep Extensibility

> [!WARNING]
> The Bicep extensibility platform and its SDKs are still in development. They are
> available for design review and early evaluation, but are not ready for extension
> authors to adopt. APIs, packages, and hosting requirements may change.

Bicep extensions enable Bicep files and ARM templates to deploy Azure data-plane and
non-Azure resources. Each extension implements resource operations behind an HTTP API,
and the Bicep Extensibility Host sends deployment requests to that API.

## Choose how the extension will run

### Use the managed extension runtime

Choose this path when the platform should run your extension. This is the standard
model for external authors and is also available to Microsoft teams.

[Build a managed extension](tutorials/getting-started.md)

### Host your own extension service

Choose this path only for a Microsoft extension that must run as a team-owned service
and integrate closely with the ARM deployment service, such as the Microsoft Graph
extension.

[Build a self-hosted FirstParty extension](get-started/first-party.md)

## Not sure where to begin?

[Choose a hosting SDK](get-started/choose-hosting.md) explains the difference between
Managed hosting, FirstParty hosting, and the advanced AspNetCore base SDK.

After choosing a host, use the [authoring guides](tutorials/index.md) to implement
typed handlers, behaviors, validation, preview, long-running operations, and local API
exploration.

## Reference

Use the [API contract](contract/contract.md) for protocol requirements. Package details
and generated API references are available under [Hosting SDKs](sdks/index.md).

The [Magic 8-Ball sample](https://github.com/Azure/bicep-extensibility/tree/main/sample/MagicEightBallExtension)
shows the planned managed extension authoring model.
