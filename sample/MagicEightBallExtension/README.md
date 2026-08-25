# Magic 8-Ball managed extension

This public sample demonstrates the Managed SDK with one exact extension version,
`1.0.0`. It implements preview, create or update, get, delete, and long-running
operation polling for a `Fortune` resource.

## Run

```bash
cd sample/MagicEightBallExtension
dotnet run
```

The project declares its managed identity in `MagicEightBallExtension.csproj`:

```xml
<BicepExtensionName>MagicEightBall</BicepExtensionName>
<BicepExtensionVersion>1.0.0</BicepExtensionVersion>
```

`Program.cs` uses the standard ASP.NET Core host:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddBicepExtension(extension => extension
    .AddHandler<FortuneLongRunningOperationGetHandler>()
    .ForResourceType("Fortune", type => type
        .AddHandler<FortunePreviewHandler>()
        .AddHandler<FortuneCreateOrUpdateHandler>()
        .AddHandler<FortuneGetHandler>()
        .AddHandler<FortuneDeleteHandler>()));

var app = builder.Build();
app.UseBicepExtension();
app.Run();
```

The managed host exposes `GET /ping` and accepts contract requests beneath
`/1.0.0/`. In Development, the sample also exposes the Scalar API explorer at
`/scalar/v2`.

## Try a preview

```bash
curl -X POST http://localhost:5000/1.0.0/resource/preview \
  -H "Content-Type: application/json" \
  -H "x-ms-client-request-id: test-001" \
  -H "x-ms-correlation-request-id: corr-001" \
  -d '{
    "type": "Fortune",
    "apiVersion": "2024-01-01",
    "properties": {
      "name": "my-fortune",
      "question": "Will this sample work?"
    }
  }'
```

This executable intentionally hosts one exact version. Multi-version selection is a
FirstParty hosting concern and is documented internally with that SDK.
