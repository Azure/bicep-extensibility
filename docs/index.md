# Bicep Extensibility

[![codecov](https://codecov.io/gh/Azure/bicep-extensibility/branch/main/graph/badge.svg)](https://codecov.io/gh/Azure/bicep-extensibility)

The Bicep Extensibility platform lets you build **Bicep extensions**: HTTP services that connect Bicep deployments to custom resources.

> [!WARNING]
> The platform is still a work in progress. The SDKs and docs are changing quickly, and the public authoring experience is not ready for broad extension-author use yet.

## How it works

A Bicep extension implements the resource operations your service needs: preview, create or update, get, delete. It exposes those operations through an HTTP API that follows the [Extension API Contract](contract/contract.md). The Bicep Extensibility Host, part of `Microsoft.Resources/deployments`, routes deployment requests to your extension and manages the lifecycle.

## SDKs

| Package | Audience | Description |
|---------|----------|-------------|
| **Azure.Deployments.Extensibility.Core** | All extensions | Transport-agnostic models, handler interfaces, discriminated unions, structured errors, and a fluent validation framework. |
| **Azure.Deployments.Extensibility.Hosting** | Third-party and local extensions | Public Hosting SDK for standard-host extensions with an exact-version resolver, health checks, and `/ping`. |
| **Azure.Deployments.Extensibility.Hosting.FirstParty** | Microsoft-internal extensions | First-party hosting wrapper for teams that self-host their extension service and need the ARM-integrated hosting experience. |
| **Azure.Deployments.Extensibility.Hosting.Managed** | Microsoft-internal extensions | Managed-runtime hosting option for teams that want the shared managed environment rather than self-hosting their extension service. |

## Start here

### Third-party extension authors
If you are building a public or local extension, start here:

1. [Getting Started](tutorials/getting-started.md) for a first walkthrough.
2. [Hosting SDK](sdks/hosting.md) for the public host entry points.
3. [Core SDK](sdks/core.md) for the shared contract models, handlers, and validation building blocks.
4. [API Contract](contract/contract.md) as the reference for request and response behavior.

### Microsoft-internal teams
If you are working on an internal implementation, start here:

1. [Core SDK](sdks/core.md) for the shared contract and validation layer.
2. [AspNetCore runtime reference](sdks/aspnetcore.md) for the shared hosting and runtime implementation.
3. [Hosting SDK](sdks/hosting.md) for context on the public authoring model and shared host behavior.
4. Choose the hosting package that fits your runtime model:
   - `Azure.Deployments.Extensibility.Hosting.FirstParty` for teams like Microsoft Graph that are deeply integrated with ARM and self-host their extension service.
   - `Azure.Deployments.Extensibility.Hosting.Managed` for teams that want the managed runtime instead of self-hosting.

> [!NOTE]
> For public and local authors, the Hosting SDK is the main entry point. For Microsoft-internal work, the shared runtime and the appropriate hosting wrapper package are the primary references.

## Quick links

- [Getting Started](tutorials/getting-started.md) for a simple first walkthrough
- [API Contract](contract/contract.md) for the full extension protocol
- [Async Operations](contract/async-operations.md) for long-running operation patterns
- [Preview Operation](contract/preview-operation.md) for preview and What-If behavior
- [Core SDK](sdks/core.md) for the shared models and validation framework
- [Hosting SDK](sdks/hosting.md) for the public host entry points
- <xref:Azure.Deployments.Extensibility.Core.V2.Contracts.Models> for the Core API reference
- <xref:Azure.Deployments.Extensibility.AspNetCore> for the AspNetCore API reference
- [Sample Extension](https://github.com/Azure/bicep-extensibility/tree/main/sample/MagicEightBallExtension) for the Magic 8-Ball demo
