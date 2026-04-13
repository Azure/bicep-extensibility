# Preview Operation

This document provides detailed guidance and examples for implementing the preview resource operation. For the core contract, see [contract.md](contract.md).

## Overview

The preview operation simulates a resource creation or update without persisting any changes. It enables the deployment engine to show users what a deployment *would* produce (read-only properties, default values, validation errors, etc.) before any real changes are made.

Preview is a **best-effort operation**. Extensions should implement it as completely as possible, but are not required
to handle every scenario. When a preview cannot be meaningfully produced — for example, because the identifiers,
configuration, or other key properties are unevaluated — the extension may return a `PreviewNotSupported` error instead
of a partial or misleading response. See [Unprocessable Preview](#unprocessable-preview) for details.

| | Type |
|---|---|
| **Input** | `ResourcePreviewSpecification` |
| **Output** | `ResourcePreview \| ErrorResponse` |

### How the Deployment Engine Uses Preview

The deployment engine calls the preview operation in two contexts:

1. **Preflight validation.** Triggered by both deployment validate and PUT requests. The deployment engine calls preview to surface validation errors early, before any resources are created.
2. **[What-If](https://learn.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-what-if).** Computes a diff between the current and desired state of each resource in the template. The deployment engine:
   1. Calls **preview** to obtain the *future state* of the resource (what the resource would look like after the deployment).
   2. Calls **get** to obtain the *current state* of the resource (or determines that the resource does not yet exist).
   3. Processes both states using the preview metadata to exclude noise (e.g., `calculated` properties that change on every operation, `unknown` properties whose values are indeterminate).
   4. Runs a diff algorithm on the processed states to produce the What-If output shown to the user.

The quality of the What-If output depends directly on the accuracy and completeness of the preview response and its metadata. See [What-If Walkthrough](#what-if-walkthrough) for an end-to-end example.

## Request Model

```typespec
model ResourcePreviewSpecification extends ResourceSpecification {
  metadata?: ResourcePreviewSpecificationMetadata;
}

model ResourcePreviewSpecificationMetadata {
  unevaluated: JsonPointer[];
}
```

The `ResourcePreviewSpecification` extends the standard `ResourceSpecification` with an optional `metadata` object. When present, `metadata.unevaluated` lists the JSON Pointer paths to properties whose values are unresolved ARM template language expressions.

## Unevaluated Expressions

At preview time, the deployment engine may not be able to evaluate all ARM template language expressions. For example, an expression like `[reference('orgSetup').defaultDepartment]` cannot be resolved if the referenced resource has not been deployed yet.

When this happens:

1. The deployment engine sends the raw expression string as the property value.
2. The paths to all such properties are listed in `metadata.unevaluated`.

The extension does **not** need to parse or detect expression syntax. It only needs to handle the fact that a property value may be a string containing an expression instead of the expected type.

### Example: Request with Unevaluated Expressions

Consider an employee resource where the `department` depends on an organizational setup resource that has not yet been deployed:

```json
{
  "type": "Contoso.HR/employees",
  "apiVersion": "2024-04-01",
  "properties": {
    "firstName": "Jane",
    "lastName": "Doe",
    "department": "[reference('orgSetup').defaultDepartment]",
    "role": "Engineer"
  },
  "config": {
    "endpoint": "https://hr-api.contoso.com"
  },
  "metadata": {
    "unevaluated": [
      "/properties/department"
    ]
  }
}
```

In this example:
- `department` contains an ARM expression rather than an actual department name.
- The path `/properties/department` appears in `metadata.unevaluated`, so the extension knows to skip validation for that property.
- All other properties (`firstName`, `lastName`, `role`) are fully resolved and should be validated normally.

For the full expression syntax, see https://aka.ms/ArmTemplateExpressions.

### Handling Strategies

When an extension encounters an unevaluated property, it should:

1. **Skip validation** for that property. Do not reject the request because a value has the wrong type.
2. **Echo the expression back** as-is in the response properties.
3. **Classify the path** using the most specific metadata category the extension can determine. If the extension knows the property is read-only, immutable, or calculated regardless of its input value, mark it accordingly. Only list the path under `unevaluated` if no more specific category applies.

If an unevaluated expression affects a property that the extension would normally transform or compute from, mark the dependent output properties using the appropriate metadata category (`unknown`, `calculated`, etc.).

## Response Model

```typespec
model ResourcePreview {
  type: string;
  apiVersion?: string;
  identifiers: Record<unknown>;
  properties: Record<unknown>;
  status?: OperationStatus;
  config?: Record<unknown>;
  configId?: string;
  metadata?: ResourcePreviewMetadata;
}

model ResourcePreviewMetadata {
  readOnly?: JsonPointer[];
  immutable?: JsonPointer[];
  unknown?: JsonPointer[];
  calculated?: JsonPointer[];
  unevaluated?: JsonPointer[];
}
```

The response must represent the resource state as if the create or update operation had been performed, equivalent to what a subsequent get operation would return.

### Response Requirements

| Requirement | Details |
|-------------|---------|
| Read-only properties | Include properties managed by the service (e.g., `provisioningState`, `id`). |
| Default values | Fill in properties that the service would assign defaults to. |
| Write-only properties | Exclude secrets such as passwords and connection strings. |
| Unevaluated expressions | Echo back the raw expression string as-is. |
| Identifiers | Include the `identifiers` object with the best available values. |

## Preview Metadata

The metadata object enables the deployment engine to give users a richer preview experience by distinguishing between different categories of property values.

### `readOnly`

Properties managed entirely by the service. The user cannot set these.

**Example:** An `employeeId` and `onboardingState` assigned by the service.

```json
{
  "metadata": {
    "readOnly": ["/properties/employeeId", "/properties/onboardingState"]
  }
}
```

### `immutable`

Properties that can be set at creation but cannot be changed on subsequent updates.

**Example:** A `startDate` that is fixed after creation.

```json
{
  "metadata": {
    "immutable": ["/properties/startDate"]
  }
}
```

### `calculated`

Properties whose values are computed at operation time and would change if the operation were performed later.

**Example:** A `badgeNumber` generated at onboarding time.

```json
{
  "metadata": {
    "calculated": ["/properties/badgeNumber"]
  }
}
```

### `unknown`

Properties whose values cannot be determined at preview time. This typically applies when a value depends on an unevaluated expression or external state that is unavailable during preview.

**Example:** An `email` that depends on a domain configuration resource that has not been deployed yet.

```json
{
  "metadata": {
    "unknown": ["/properties/email"]
  }
}
```

### `unevaluated`

Properties containing ARM template language expressions that the deployment engine could not resolve. The extension must account for every path received in the request's `metadata.unevaluated`, but should **reclassify** a path into a more specific category whenever possible. For example, if the unevaluated path points to a property the extension knows is always read-only, it should appear under `readOnly`, not `unevaluated`. Only list a path under `unevaluated` when the extension cannot determine a more specific category.

```json
{
  "metadata": {
    "unevaluated": ["/properties/department"]
  }
}
```

## Unprocessable Preview

When the extension cannot produce any meaningful preview — for instance because the identifiers, configuration, or other
essential inputs are unevaluated — it should return an `ErrorResponse` with error code `PreviewNotSupported` and an
error message explaining the reason.

### Effect on the Deployment Engine

| Context                  | Behavior                                                                                                                                   |
|--------------------------|--------------------------------------------------------------------------------------------------------------------------------------------|
| **Preflight validation** | The error is ignored. Preflight does **not** fail.                                                                                         |
| **What-If**              | The resource is reported with change type **Unsupported**, and the `PreviewNotSupported` error message is shown as the unsupported reason. |

### Example: Unprocessable Preview

**Request** (identifiers are unevaluated):

```json
{
  "type": "Contoso.HR/employees",
  "apiVersion": "2024-04-01",
  "properties": {
    "firstName": "Jane",
    "lastName": "Doe",
    "employeeId": "[reference('idProvider').nextEmployeeId]",
    "role": "Engineer"
  },
  "config": {
    "endpoint": "https://hr-api.contoso.com"
  },
  "metadata": {
    "unevaluated": ["/properties/employeeId"]
  }
}
```

**Response** (HTTP 422):

```json
{
  "error": {
    "code": "PreviewNotSupported",
    "message": "Preview cannot be performed because the identifier 'employeeId' is unevaluated."
  }
}
```

## HTTP Binding

| Scenario                                      | Status Code                 |
|-----------------------------------------------|-----------------------------|
| Preview succeeded                             | `200 OK`                    |
| Preview not supported (`PreviewNotSupported`) | `422 Unprocessable Content` |
| Validation error or other client error        | `400 Bad Request`           |

## Configuration Handling

If the request includes a `config` object, the extension must:

1. **Echo back the configuration** in the response, excluding any secret properties. Any property not echoed back is treated as a secret by the deployment engine.
2. **Return a `configId`**, a value that uniquely identifies the deployment control plane. The format is determined by the extension (e.g., a hash of the endpoint URL).
3. **Validate the `configId`** if one is provided in the request.

## End-to-End Examples

### Example 1: All Properties Resolved

**Request:**

```json
{
  "type": "Contoso.HR/employees",
  "apiVersion": "2024-04-01",
  "properties": {
    "firstName": "John",
    "lastName": "Smith",
    "department": "Engineering",
    "role": "Engineer"
  },
  "config": {
    "endpoint": "https://hr-api.contoso.com",
    "apiKey": "secret-key-123"
  }
}
```

**Response:**

```json
{
  "type": "Contoso.HR/employees",
  "apiVersion": "2024-04-01",
  "identifiers": {
    "employeeId": "emp-00042"
  },
  "properties": {
    "firstName": "John",
    "lastName": "Smith",
    "department": "Engineering",
    "role": "Engineer",
    "employeeId": "emp-00042",
    "onboardingState": "Succeeded",
    "startDate": "2024-02-01",
    "badgeNumber": "B-1234",
    "email": "john.smith@contoso.com"
  },
  "config": {
    "endpoint": "https://hr-api.contoso.com"
  },
  "configId": "sha256:a1b2c3d4",
  "metadata": {
    "readOnly": ["/properties/employeeId", "/properties/onboardingState", "/properties/email"],
    "immutable": ["/properties/startDate"],
    "calculated": ["/properties/badgeNumber"]
  }
}
```

Key points:
- The `apiKey` is a secret, so it is excluded from the echoed `config`.
- `employeeId`, `onboardingState`, and `email` are read-only (service-managed).
- `startDate` is immutable (cannot be changed after creation).
- `badgeNumber` is calculated (would differ if the operation ran later).

### Example 2: Resource with Unevaluated Expressions

**Request:**

```json
{
  "type": "Contoso.HR/employees",
  "apiVersion": "2024-04-01",
  "properties": {
    "firstName": "Jane",
    "lastName": "Doe",
    "department": "[reference('orgSetup').defaultDepartment]",
    "role": "Manager"
  },
  "config": {
    "endpoint": "https://hr-api.contoso.com"
  },
  "metadata": {
    "unevaluated": ["/properties/department"]
  }
}
```

**Response:**

```json
{
  "type": "Contoso.HR/employees",
  "apiVersion": "2024-04-01",
  "identifiers": {
    "employeeId": "emp-00043"
  },
  "properties": {
    "firstName": "Jane",
    "lastName": "Doe",
    "department": "[reference('orgSetup').defaultDepartment]",
    "role": "Manager",
    "employeeId": "emp-00043",
    "onboardingState": "Succeeded",
    "startDate": "2024-03-01",
    "email": "jane.doe@contoso.com"
  },
  "config": {
    "endpoint": "https://hr-api.contoso.com"
  },
  "configId": "sha256:a1b2c3d4",
  "metadata": {
    "readOnly": ["/properties/employeeId", "/properties/onboardingState", "/properties/email"],
    "immutable": ["/properties/startDate"],
    "unevaluated": ["/properties/department"]
  }
}
```

Key points:
- `department` contains an ARM expression and is echoed back as-is.
- The path `/properties/department` remains in `metadata.unevaluated` in the response.
- `email` is a read-only property the service computes from the employee's name.
- Other properties (`firstName`, `lastName`, `role`) are fully resolved and validated normally.

### Example 3: Preview Returns an Error

When the extension cannot produce a meaningful preview, it should return an `ErrorResponse`:

**Request:**

```json
{
  "type": "Contoso.HR/employees",
  "apiVersion": "2024-04-01",
  "properties": {
    "firstName": "John",
    "lastName": "Smith",
    "department": "Engineering",
    "role": "invalid-role"
  },
  "config": {
    "endpoint": "https://hr-api.contoso.com"
  }
}
```

**Response:**

```json
{
  "error": {
    "code": "InvalidRole",
    "message": "The role 'invalid-role' is not valid. Supported values are: 'Engineer', 'Manager', 'Director'.",
    "target": "/properties/role"
  }
}
```

Key points:
- The `target` field uses a JSON Pointer to identify the offending property.
- The `message` provides actionable guidance for the user.
- Validation errors should be returned eagerly during preview, even when the create or update operation would also reject the input.

## What-If Walkthrough

This section walks through how the deployment engine uses the preview operation to produce What-If output. The example below covers the **update** case. The same flow applies to **create** (where the get call returns a not-found error, so the entire resource is shown as new). For **delete**, it is only possible when an extensible resource is managed by Deployment Stacks, and the support is still a work in progress.

### Scenario

An employee already exists with `role: "Engineer"`. The user submits a template that changes `role` to `"Manager"` and adds `department: "Marketing"`.

### Step 1: Call Preview (Future State)

The deployment engine calls preview with the desired state from the template.

**Request:**

```json
{
  "type": "Contoso.HR/employees",
  "apiVersion": "2024-04-01",
  "properties": {
    "firstName": "John",
    "lastName": "Smith",
    "department": "Marketing",
    "role": "Manager"
  },
  "config": {
    "endpoint": "https://hr-api.contoso.com"
  }
}
```

**Response (future state):**

```json
{
  "type": "Contoso.HR/employees",
  "apiVersion": "2024-04-01",
  "identifiers": {
    "employeeId": "emp-00042"
  },
  "properties": {
    "firstName": "John",
    "lastName": "Smith",
    "department": "Marketing",
    "role": "Manager",
    "employeeId": "emp-00042",
    "onboardingState": "Succeeded",
    "startDate": "2024-02-01",
    "badgeNumber": "B-5678",
    "email": "john.smith@contoso.com"
  },
  "config": {
    "endpoint": "https://hr-api.contoso.com"
  },
  "configId": "sha256:a1b2c3d4",
  "metadata": {
    "readOnly": ["/properties/employeeId", "/properties/onboardingState", "/properties/email"],
    "immutable": ["/properties/startDate"],
    "calculated": ["/properties/badgeNumber"]
  }
}
```

### Step 2: Call Get (Current State)

The deployment engine calls get to retrieve the resource's current state.

**Response (current state):**

```json
{
  "type": "Contoso.HR/employees",
  "apiVersion": "2024-04-01",
  "identifiers": {
    "employeeId": "emp-00042"
  },
  "properties": {
    "firstName": "John",
    "lastName": "Smith",
    "department": "Engineering",
    "role": "Engineer",
    "employeeId": "emp-00042",
    "onboardingState": "Succeeded",
    "startDate": "2024-02-01",
    "badgeNumber": "B-1234",
    "email": "john.smith@contoso.com"
  },
  "config": {
    "endpoint": "https://hr-api.contoso.com"
  },
  "configId": "sha256:a1b2c3d4"
}
```

### Step 3: Process States Using Preview Metadata

> [!NOTE]
> This section is under development and will be finalized in a future update.

### Step 4: Diff and Produce What-If Output

> [!NOTE]
> This section is under development and will be finalized in a future update.
