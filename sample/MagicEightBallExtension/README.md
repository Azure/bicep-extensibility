# Magic 8-Ball Extension — Sample Bicep Extension

A fun sample extension that demonstrates how to build a Bicep extension using the AspNetCore SDK. It implements a **Magic 8-Ball** resource provider: you ask a question, shake the 8-ball, and get a fortune!

This sample exercises **all 5 API endpoints** defined by the [V2 contract](../../docs/v2/contract.md), with two extension versions:

| Endpoint | What it does in this sample |
|---|---|
| **Preview** | Returns a preview fortune without persisting anything. Demonstrates handling of `unevaluated` expressions and preview metadata (`readOnly`, `calculated`). The v2 preview adds placeholder `confidence` and `mood` values. |
| **Create or Update** | Shakes the 8-ball, generates a random fortune, and stores it. One special fortune ("The cosmos need more time to decide...") triggers a **long-running operation** (202 Accepted). The v2 handler adds `confidence` and `mood` fields. |
| **Get** | Retrieves a stored fortune by name. Returns 404 if it doesn't exist. |
| **Delete** | Removes a fortune. Returns 204 if it was already gone. |
| **Get LRO** | Polls the status of a "cosmic contemplation" operation. Completes after 5 seconds. Shared across both extension versions. |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build & Run

```bash
cd sample/MagicEightBallExtension
dotnet run
```

The server starts at `http://localhost:5000` by default. Open the **Scalar API explorer** at:

```
http://localhost:5000/scalar/v2
```

## Try It Out

All endpoints require `x-ms-client-request-id` and `x-ms-correlation-request-id` headers, plus `Referer`, `traceparent`, and `tracestate`.

### 1. Preview a Fortune

```bash
curl -X POST http://localhost:5000/1.0.0/resource/preview \
  -H "Content-Type: application/json" \
  -H "x-ms-client-request-id: test-001" \
  -H "x-ms-correlation-request-id: corr-001" \
  -H "Referer: http://localhost" \
  -H "traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" \
  -H "tracestate: congo=t61rcWkgMzE" \
  -d '{
    "type": "Fortune",
    "apiVersion": "2024-01-01",
    "properties": {
      "name": "my-fortune",
      "question": "Will this sample work?"
    }
  }'
```

**Expected response (200 OK):**
```json
{
  "type": "Fortune",
  "apiVersion": "2024-01-01",
  "identifiers": { "name": "my-fortune" },
  "properties": {
    "name": "my-fortune",
    "question": "Will this sample work?",
    "fortune": "Preview: The stars are aligning... (actual fortune generated on create)",
    "answeredAt": "2024-10-01T12:00:00.0000000+00:00"
  },
  "metadata": {
    "calculated": ["/properties/fortune", "/properties/answeredAt"],
    "readOnly": ["/properties/fortune"]
  }
}
```

### 2. Create a Fortune

```bash
curl -X POST http://localhost:5000/1.0.0/resource/createOrUpdate \
  -H "Content-Type: application/json" \
  -H "x-ms-client-request-id: test-002" \
  -H "x-ms-correlation-request-id: corr-002" \
  -H "Referer: http://localhost" \
  -H "traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" \
  -H "tracestate: congo=t61rcWkgMzE" \
  -d '{
    "type": "Fortune",
    "apiVersion": "2024-01-01",
    "properties": {
      "name": "my-fortune",
      "question": "Should I use Bicep?"
    }
  }'
```

**Expected response — synchronous (200 OK):**
```json
{
  "type": "Fortune",
  "apiVersion": "2024-01-01",
  "identifiers": { "name": "my-fortune" },
  "properties": {
    "name": "my-fortune",
    "question": "Should I use Bicep?",
    "fortune": "It is certain.",
    "answeredAt": "2024-10-01T12:00:00.0000000+00:00"
  }
}
```

**Or — long-running (202 Accepted):** if the cosmos need more time:
```json
{
  "status": "CosmicContemplation",
  "retryAfterSeconds": 3,
  "operationHandle": { "operationId": "abc123..." }
}
```

### 3. Poll a Long-Running Operation

If you got a 202, poll the LRO endpoint:

```bash
curl -X POST http://localhost:5000/1.0.0/longRunningOperation/get \
  -H "Content-Type: application/json" \
  -H "x-ms-client-request-id: test-003" \
  -H "x-ms-correlation-request-id: corr-003" \
  -H "Referer: http://localhost" \
  -H "traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" \
  -H "tracestate: congo=t61rcWkgMzE" \
  -d '{ "operationId": "abc123..." }'
```

**While contemplating:**
```json
{
  "status": "CosmicContemplation",
  "retryAfterSeconds": 3,
  "operationHandle": { "operationId": "abc123..." }
}
```

**After 5 seconds:**
```json
{
  "status": "Succeeded"
}
```

### 4. Get a Fortune

```bash
curl -X POST http://localhost:5000/1.0.0/resource/get \
  -H "Content-Type: application/json" \
  -H "x-ms-client-request-id: test-004" \
  -H "x-ms-correlation-request-id: corr-004" \
  -H "Referer: http://localhost" \
  -H "traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" \
  -H "tracestate: congo=t61rcWkgMzE" \
  -d '{
    "type": "Fortune",
    "apiVersion": "2024-01-01",
    "identifiers": { "name": "my-fortune" }
  }'
```

**If found (200 OK):** returns the resource. **If not found (404).**

### 5. Delete a Fortune

```bash
curl -X POST http://localhost:5000/1.0.0/resource/delete \
  -H "Content-Type: application/json" \
  -H "x-ms-client-request-id: test-005" \
  -H "x-ms-correlation-request-id: corr-005" \
  -H "Referer: http://localhost" \
  -H "traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" \
  -H "tracestate: congo=t61rcWkgMzE" \
  -d '{
    "type": "Fortune",
    "apiVersion": "2024-01-01",
    "identifiers": { "name": "my-fortune" }
  }'
```

**If deleted (200 OK):** returns the deleted resource. **If already gone (204 No Content).**

## How It Works

The extension is wired up in `Program.cs` using the `ExtensionApplication` builder API:

```csharp
var app = ExtensionApplication.Create(args);

// Register application services.
app.ConfigureServices(services =>
{
    services.AddSingleton<FortuneStore>();
    services.Configure<JsonOptions>(options =>
    {
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, FortuneModelSerializerContext.Default);
    });
});

// Global behaviors — run for every handler invocation.
app.AddGlobalHandlerBehavior<ResponseLoggingBehavior>();
app.AddGlobalHandlerBehavior<NameValidationBehavior>();
app.AddGlobalHandlerBehavior<PreviewMetadataProcessingBehavior>();

// v1 handlers
app.AddExtensionVersion("1.*.*", version => version
    .AddHandlerBehavior(sp => new ApiVersionValidationBehavior("2024-01-01", "2024-01-01-preview"))
    .AddHandler<FortuneLongRunningOperationGetHandler>()
    .ForResourceType("Fortune", type => type
        .AddHandler<V1.FortunePreviewHandler>()
        .AddHandler<V1.FortuneCreateOrUpdateHandler>()
        .AddHandler<V1.FortuneGetHandler>()
        .AddHandler<V1.FortuneDeleteHandler>()));

// v2 handlers
app.AddExtensionVersion("2.*.*", version => version
    .AddHandlerBehavior(sp => new ApiVersionValidationBehavior("2025-01-01", "2025-01-01-preview"))
    .AddHandler<FortuneLongRunningOperationGetHandler>()
    .ForResourceType("Fortune", type => type
        .AddHandler<V2.FortunePreviewHandler>()
        .AddHandler<V2.FortuneCreateOrUpdateHandler>()
        .AddHandler<V2.FortuneGetHandler>()
        .AddHandler<V2.FortuneDeleteHandler>()));

app.EnableDevelopmentScalarApiExplorer(explorer => explorer
    .WithTitle("Magic Eight Ball Extension API")
    .WithExtensionVersions("1.0.0", "2.0.0")
    .ConfigureExamples(FortuneExamples.Configure));

await app.RunAsync();
```

Each handler extends one of the SDK's typed base classes (`TypedResourceCreateOrUpdateHandler<TProperties, TIdentifiers>`, `TypedResourceGetHandler<TProperties, TIdentifiers>`, etc.) and overrides a single `HandleAsync` method. The SDK handles:
- JSON serialization/deserialization (with support for custom `JsonSerializerContext` via `JsonOptions`)
- Request header extraction and correlation
- Error handling and `ErrorResponse` mapping
- Extension version routing via `AddExtensionVersion` — e.g., `V1.*` handlers use `FortuneProperties` while `V2.*` handlers use `FortunePropertiesV2` (which adds `confidence` and `mood`)
- **Behaviors** (decorators) that run cross-cutting logic before/after handlers — both global (e.g., `ResponseLoggingBehavior`) and version-scoped (e.g., `ApiVersionValidationBehavior`)
- Resource-type routing via `ForResourceType` — handlers are scoped to specific resource types like `"Fortune"`

## Project Structure

```
sample/MagicEightBallExtension/
├── Program.cs                                          # Entry point — wires up versions, handlers, and behaviors
├── FortuneExamples.cs                                  # OpenAPI examples for Scalar explorer
├── MagicEightBallExtension.csproj                      # Project file
├── README.md                                           # This file
├── Behaviors/
│   ├── ApiVersionValidationBehavior.cs                 # Version-scoped: validates resource API version
│   ├── NameValidationBehavior.cs                       # Global: validates required "name" property/identifier
│   ├── PreviewMetadataProcessingBehavior.cs            # Global: handles unevaluated ARM expressions in preview
│   └── ResponseLoggingBehavior.cs                      # Global: logs handler results
├── Data/
│   └── FortuneStore.cs                                 # Thread-safe in-memory store for resources and LROs
├── Handlers/
│   ├── FortuneLongRunningOperationGetHandler.cs        # LRO polling (shared across versions)
│   ├── V1/
│   │   ├── FortunePreviewHandler.cs                    # Preview (v1)
│   │   ├── FortuneCreateOrUpdateHandler.cs             # Create/Update (v1)
│   │   ├── FortuneGetHandler.cs                        # Get (v1)
│   │   └── FortuneDeleteHandler.cs                     # Delete (v1)
│   └── V2/
│       ├── FortunePreviewHandler.cs                    # Preview (v2) — adds confidence & mood placeholders
│       ├── FortuneCreateOrUpdateHandler.cs             # Create/Update (v2) — adds confidence & mood
│       ├── FortuneGetHandler.cs                        # Get (v2) — deserializes v2 properties
│       └── FortuneDeleteHandler.cs                     # Delete (v2) — deserializes v2 properties
└── Models/
    ├── FortuneModels.cs                                # FortuneProperties, FortunePropertiesV2, FortuneIdentifiers
    └── FortuneModelSerializerContext.cs                 # Source-generated JSON serializer context
```
