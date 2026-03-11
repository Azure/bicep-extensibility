#!/usr/bin/env pwsh
# Injects C# coding style rules as a system message at the start of each agent session.
# Rules come from two sources: .editorconfig and explicit user preferences.

$message = @'
C# coding style rules for this workspace (must be followed in ALL generated or edited C# code):

## From .editorconfig

### File structure
- Every .cs file must start with the copyright header (two comment lines):
    // Copyright (c) Microsoft Corporation.
    // Licensed under the MIT License.
- `using` directives go OUTSIDE the namespace declaration.
- Use block-scoped namespaces (not file-scoped).
- Always add a final newline at the end of the file.

### Braces & control flow
- Always use braces for control flow statements (`if`, `else`, `for`, `foreach`, `while`, etc.) — never omit them.

### Expression bodies
- Do NOT use expression-bodied methods, constructors, operators, or local functions.
- Expression-bodied properties, indexers, accessors, and lambdas are allowed.

### Naming
- Interfaces must be prefixed with `I` (PascalCase after the prefix).
- Types (class, struct, interface, enum) and non-field members (properties, events, methods) use PascalCase.

### Variable declarations
- Use `var` only when the type is apparent from the right-hand side.
- Use explicit types everywhere else.

### Misc style
- Prefer `is null` over `== null` / `ReferenceEquals`.
- Prefer null-coalescing (`??`, `??=`) and null-conditional (`?.`) operators.
- Prefer pattern matching over `as`+null-check or `is`+cast.
- Prefer switch expressions over switch statements where idiomatic.
- Prefer `_` discard for unused values/assignments.

## From user preferences (override .editorconfig where they conflict)
- Do NOT use primary constructors. Always use traditional constructors with explicit private fields.
- Private fields use plain camelCase — NO underscore prefix (e.g., `count`, not `_count`).
- All instance member access (fields, properties, method calls) must be qualified with `this.` — e.g., `this.field`, `this.Property`, `this.Method()`.
'@

$output = [ordered]@{
    systemMessage = $message
} | ConvertTo-Json -Compress

Write-Output $output
