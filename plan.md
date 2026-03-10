# ExtensionApplication – Improvement Plan

## Overview

This document outlines targeted improvements to the `ExtensionApplication` class in
`Azure.Deployments.Extensibility.AspNetCore`. The class is well-structured overall, but
the issues below range from real bugs to documentation polish.

---

## 1. `ConfigureMiddlewares` Should Support Chaining, Not Replacement

**Severity:** 🟠 Medium

Calling `ConfigureMiddlewares` twice silently discards the first callback, which is a real
bug waiting to happen.

### Before

```csharp
private Action<WebApplication>? middlewareConfigurator;

public ExtensionApplication ConfigureMiddlewares(Action<WebApplication> configure)
{
    this.middlewareConfigurator = configure;  // ❌ overwrites previous
    return this;
}
```

### After

```csharp
public ExtensionApplication ConfigureMiddlewares(Action<WebApplication> configure)
{
    this.middlewareConfigurator += configure;  // ✅ delegate chaining
    return this;
}
```

---

## 2. `Build()` Should Guard Against Double-Build

**Severity:** 🟡 Low

`WebApplicationBuilder.Build()` throws on a second call, but nothing in the API prevents
calling `Run()` + `RunAsync()`, or `Build()` twice. A guard makes the failure surface
earlier with a clear message.

### After

```csharp
private WebApplication? _builtApp;

private WebApplication Build()
{
    if (_builtApp is not null)
        throw new InvalidOperationException("ExtensionApplication has already been built.");

    this.handlerRegistry.Validate();
    this.RegisterDefaultServices();
    // ...
}
```

---

## 3. Hidden Ordering Side Effect in `RegisterDefaultServices`

**Severity:** 🟡 Low

`ErrorResponseExceptionHandlingBehavior` is silently prepended as the outermost pipeline
layer inside `Build()`. This is a hidden side effect with no documentation contract. At a
minimum, a `<remarks>` block should make this explicit.

### After

```csharp
/// <remarks>
/// <see cref="ErrorResponseExceptionHandlingBehavior"/> is always registered
/// as the outermost pipeline layer and cannot be removed.
/// </remarks>
private void RegisterDefaultServices() { ... }
```

Longer term, consider exposing a sealed pipeline position concept so it is enforced by the
type system rather than a comment.

---

## 4. `EnableDevelopmentScalarApiExplorer` – Fragile No-Op Sentinel

**Severity:** 🟡 Low

The no-op lambda `(_ => { })` is used as a sentinel to distinguish "enabled without
examples" from "not enabled". This implicit contract is easy to break accidentally.

### Before

```csharp
// The comment explains the trick, but it's easy to break accidentally.
this.apiExplorerConfigurator = configureExamples ?? (_ => { });
```

### After

```csharp
private bool _apiExplorerEnabled;

public ExtensionApplication EnableDevelopmentScalarApiExplorer(
    Action<OpenApiExamplesBuilder>? configureExamples = null,
    string? title = null,
    string[]? extensionVersions = null)
{
    _apiExplorerEnabled = true;
    this.apiExplorerConfigurator = configureExamples;
    this.apiExplorerTitle = title;
    this.apiExplorerExtensionVersions = extensionVersions;
    return this;
}

// In MapExtensionEndpoints:
if (_apiExplorerEnabled) { ... }
```

---

## 5. Missing `ArgumentNullException` Guards on Public Methods

**Severity:** 🟠 Medium

Public methods such as `AddExtensionVersion` do not validate their parameters before use.
This can produce confusing `NullReferenceException`s deep in the call stack.

### After

```csharp
public ExtensionApplication AddExtensionVersion(
    string versionRange,
    Action<ExtensionVersionBuilder> configure)
{
    ArgumentException.ThrowIfNullOrEmpty(versionRange);
    ArgumentNullException.ThrowIfNull(configure);  // ✅ add this

    var range = SemVersionRange.Parse(versionRange);
    // ...
}
```

Apply the same pattern to `ConfigureServices`, `ConfigureMiddlewares`, and
`AddGlobalPipelineBehavior`.

---

## 6. XML Doc HTML-Escaping in `<code>` Block

**Severity:** 🟢 Minor

The `<example>` block in the class-level XML doc uses `&amp;gt;` / `&amp;lt;`, which
renders as escaped HTML in IDEs and documentation generators. Wrapping in `<![CDATA[...]]>`
fixes this cleanly.

### Before

```xml
/// <code>
/// ExtensionApplication.Create(args)
///     .AddExtensionVersion("&gt;=1.0.0", v =&gt; v
///         .ForResourceType("apps/Deployment", rt =&gt; rt
///             .AddHandler&lt;DeploymentHandler&gt;())
///         .AddHandler&lt;FallbackHandler&gt;())
///     .Run();
/// </code>
```

### After

```xml
/// <code><![CDATA[
/// ExtensionApplication.Create(args)
///     .AddExtensionVersion(">=1.0.0", v => v
///         .ForResourceType("apps/Deployment", rt => rt
///             .AddHandler<DeploymentHandler>())
///         .AddHandler<FallbackHandler>())
///     .Run();
/// ]]></code>
```

---

## Summary

| # | Issue                                              | Severity    | Fix                                  |
|---|----------------------------------------------------|-------------|--------------------------------------|
| 1 | `ConfigureMiddlewares` overwrites on second call   | 🟠 Medium   | Use `+=` delegate chaining           |
| 2 | No double-build guard                              | 🟡 Low      | Add built flag + guard               |
| 3 | Hidden ordering side effect in `RegisterDefaultServices` | 🟡 Low | Add `<remarks>` or type enforcement  |
| 4 | Sentinel no-op lambda for API explorer             | 🟡 Low      | Use explicit `bool` flag             |
| 5 | Missing null guards on public methods              | 🟠 Medium   | `ArgumentNullException.ThrowIfNull`  |
| 6 | XML doc HTML-escaping in `<code>` block            | 🟢 Minor    | Use `CDATA`                          |

> **Most impactful fix:** Issue #1 — the silent overwrite in `ConfigureMiddlewares` is a
> real bug that can manifest in production when the method is called more than once.

