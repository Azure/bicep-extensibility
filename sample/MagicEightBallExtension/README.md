# Magic 8-Ball Extension — Sample Bicep Extension

A fun sample extension that demonstrates how to build a Bicep extension using the AspNetCore SDK. It implements a **Magic 8-Ball** resource provider: you ask a question, shake the 8-ball, and get a fortune!

This sample exercises **all 5 API endpoints** defined by the [V2 contract](../../docs/v2/contract.md):

| Endpoint | What it does in this sample |
|---|---|
| **Preview** | Returns a preview fortune without persisting anything. Demonstrates handling of `unevaluated` expressions and preview metadata (`readOnly`, `calculated`). |
| **Create or Update** | Shakes the 8-ball, generates a random fortune, and stores it. One special fortune ("The cosmos need more time to decide...") triggers a **long-running operation** (202 Accepted). Includes a **v2 handler** that adds `confidence` and `mood` fields when extensionVersion >= 2.0.0. |
| **Get** | Retrieves a stored fortune by name. Returns 404 if it doesn't exist. |
| **Delete** | Removes a fortune. Returns 204 if it was already gone. |
| **Get LRO** | Polls the status of a "cosmic contemplation" operation. Completes after 5 seconds. |

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
    "apiVersion": "2026-01-01",
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
  "apiVersion": "2026-01-01",
  "identifiers": { "name": "my-fortune" },
  "properties": {
    "name": "my-fortune",
    "question": "Will this sample work?",
    "fortune": "Preview: The stars are aligning... (actual fortune generated on create)",
    "answeredAt": "2026-03-06T12:00:00.0000000+00:00"
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
    "apiVersion": "2026-01-01",
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
  "apiVersion": "2026-01-01",
  "identifiers": { "name": "my-fortune" },
  "properties": {
    "name": "my-fortune",
    "question": "Should I use Bicep?",
    "fortune": "It is certain.",
    "answeredAt": "2026-03-06T12:00:00.0000000+00:00"
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
    "apiVersion": "2026-01-01",
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
    "apiVersion": "2026-01-01",
    "identifiers": { "name": "my-fortune" }
  }'
```

**If deleted (200 OK):** returns the deleted resource. **If already gone (204 No Content).**

## How It Works

The entire extension is wired up in `Program.cs` using the fluent `ExtensionApplication` API:

```csharp
ExtensionApplication.Create(args)
    .ConfigureServices(services => services.AddSingleton<FortuneStore>())
    .AddHandler<FortunePreviewHandler>()
    .AddHandler<FortuneCreateOrUpdateHandler>()
    .AddHandler<FortuneCreateOrUpdateHandlerV2>()
    .AddHandler<FortuneGetHandler>()
    .AddHandler<FortuneDeleteHandler>()
    .AddHandler<FortuneLongRunningOperationGetHandler>()
    .EnableDevelopmentScalarApiExplorer(FortuneExamples.Configure)
    .Run();
```

Each handler extends one of the SDK's base classes (`ResourceCreateOrUpdateHttpHandler`, `ResourceGetHttpHandler`, etc.) and overrides a single method. The SDK handles:
- JSON serialization/deserialization
- Request header extraction and correlation
- Error handling and `ErrorResponseException` mapping
- Model validation (if validators are registered)
- Extension version routing via `[SupportedExtensionVersions(...)]` — e.g., `FortuneCreateOrUpdateHandler` handles `>=1.0.0 <2.0.0` while `FortuneCreateOrUpdateHandlerV2` handles `>=2.0.0`

## Project Structure

```
sample/MagicEightBallExtension/
├── Program.cs                                    # Entry point
├── FortuneExamples.cs                            # OpenAPI examples for Scalar explorer
├── MagicEightBallExtension.csproj                # Project file
├── README.md                                     # This file
├── Data/
│   └── FortuneStore.cs                           # Thread-safe in-memory store
└── Handlers/
    ├── FortunePreviewHandler.cs                  # Preview endpoint
    ├── FortuneCreateOrUpdateHandler.cs           # Create/Update (v1, >=1.0.0 <2.0.0)
    ├── FortuneCreateOrUpdateHandlerV2.cs         # Create/Update (v2, >=2.0.0) — adds confidence & mood
    ├── FortuneGetHandler.cs                      # Get endpoint
    ├── FortuneDeleteHandler.cs                   # Delete endpoint
    └── FortuneLongRunningOperationGetHandler.cs  # LRO polling endpoint
```
