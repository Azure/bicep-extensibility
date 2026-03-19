#!/usr/bin/env pwsh
# PostToolUse hook — after any file-edit tool, injects a reminder to keep comments
# in sync with the updated code.
#
# Triggers on: replace_string_in_file, multi_replace_string_in_file, create_file
# Does nothing for other tools (exits 0 silently).

$payload = $null
try {
    $raw = [Console]::In.ReadToEnd()
    $payload = $raw | ConvertFrom-Json
} catch {
    exit 0
}

$editTools = @(
    'replace_string_in_file',
    'multi_replace_string_in_file',
    'create_file'
)

$toolName = $payload.tool_name ?? $payload.toolName

if ($toolName -notin $editTools) {
    exit 0
}

$message = @'
Code was just edited. Before finishing your response, verify that all comments remain accurate:

- **XML doc comments** (`<summary>`, `<param>`, `<returns>`, `<remarks>`, etc.) must reflect any renamed parameters, changed return values, or revised logic.
- **Inline comments** must not describe what the code no longer does.
- **`<list>` items** inside `<summary>` blocks that enumerate steps or behaviors must be updated if those steps changed.

Do NOT add new comments for self-evident code. Only update comments that are now stale or misleading.
'@

[ordered]@{ systemMessage = $message } | ConvertTo-Json -Compress
