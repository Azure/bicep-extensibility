# AspNetCore runtime reference

> [!NOTE]
> This page is a technical reference for the shared AspNetCore runtime implementation. It is not a public authoring guide for extension authors. Extension authors should use the [Hosting SDK](hosting.md) instead.

The AspNetCore runtime provides the shared hosting layer for Bicep extensions. It handles JSON serialization, correlation headers, culture propagation, endpoint routing, and the behavior pipeline automatically.

> [!WARNING]
> The extensibility platform is still a work in progress. The shared runtime and SDKs are evolving quickly and are not yet ready for broad extension-author consumption.

## Who should read this?

Read this page if you are maintaining or extending the shared hosting/runtime implementation, or if you are part of a Microsoft-internal (1P) team that needs to understand the underlying infrastructure the SDKs build on.

This is not the primary entry point for public extension authors. Third-party authors should start with the [Hosting SDK](hosting.md) and [Getting Started](../tutorials/getting-started.md).

## What it provides

- Middleware and request correlation/culture handling
- Shared endpoint mapping for extension contract routes
- Shared handler invocation and behavior pipeline support
- Typed handler base classes and related runtime helpers

## Relationship to the public SDK

The public Hosting SDK wraps this runtime and exposes the standard-host entry points that extension authors should use.
