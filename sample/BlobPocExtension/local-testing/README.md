# Local Testing Guide — BlobPocExtension

This guide shows how to verify the `BlobPocExtension` end-to-end on your machine.
The handlers perform **real** Azure Storage I/O via `DefaultAzureCredential`, so you
need a real storage account and data-plane RBAC before testing.

> A ready-to-deploy [`storage-account.bicep`](./storage-account.bicep) is included in this
> folder to provision the account and grant your user the required role in one step.

## 1. Prerequisites

- **Sign in** so `DefaultAzureCredential` can resolve a token:

  ```powershell
  az login
  # if you have multiple tenants/subscriptions:
  az account set --subscription "<your-sub-id>"
  ```

- **A storage account** you can reach (the extension assumes the account already exists —
  control-plane creation is out of scope for the extension itself). Deploy one with the
  bundled Bicep file (see below). Note the account name, e.g. `mystgacct`.

- **Data-plane RBAC** on that account for your user: **Storage Blob Data Contributor**
  (container create/delete/list needs data-plane, not just control-plane, permissions).
  The Bicep file assigns this for you.

### Provision a storage account with the bundled Bicep

```powershell
# create a resource group (or reuse one)
az group create --name ligar-test --location eastus

# get your own object id so the template can grant you the data-plane role
$me = az ad signed-in-user show --query id -o tsv

# deploy: account name must be globally unique, 3-24 lowercase alphanumerics
az deployment group create `
  --resource-group ligar-test `
  --template-file storage-account.bicep `
  --parameters storageAccountName=mystgacct28518 principalId=$me
```

The deployment outputs the final `storageAccountName` — use that value as `accountName`
in the requests below. Role assignments can take a few minutes to propagate.

## 2. Run the extension

```powershell
cd C:\Users\ligar\Repos\bicep-extensibility\sample\BlobPocExtension
dotnet run
```

It binds to **`EXTENSION_PORT` (8080)** and launches the Scalar API UI at
`http://localhost:8080/scalar/v2`. Leave it running; use a **second** terminal for the
calls below.

> If `dotnet run` exits with code 1, a previous instance is likely still holding port 8080.
> Run this first: `Get-Process -Name BlobPocExtension -ErrorAction SilentlyContinue | Stop-Process -Force`

## 3. Health check

```powershell
curl.exe http://localhost:8080/health
```

Returns `Healthy`. By default the storage probe is skipped. To make `/health` actually
hit storage, set the probe account before `dotnet run`:

```powershell
$env:Storage__ProbeAccountName = "mystgacct"
dotnet run
```

## 4. Exercise the endpoints

All requests are in [`requests.http`](./requests.http). Open it in VS Code (with the REST
Client extension, or the built-in HTTP support) and click **Send Request** above each
`###` block. Update the `@accountName` variable at the top to your real account name first.

The file covers, in order:

- **Resource lifecycle** — `preview` (side-effect-free), `createOrUpdate`, `get`, `delete`,
  plus the `longRunningOperation/get` stub. Cross-check with
  `az storage container list --account-name <accountName> --auth-mode login`.
- **Behavior validation** — these prove the pipeline (exception mapping -> apiVersion ->
  accountName) works without touching the handlers:
  - bad `apiVersion` -> `UnsupportedApiVersion` (`ApiVersionValidationBehavior`)
  - invalid / missing `accountName` -> `InvalidAccountName` / `MissingAccountName`
    (`AccountNameValidationBehavior`)
  - `get` against a non-existent container or unauthorized account ->
    `ContainerNotFound` / `AuthorizationFailed` / `AccountNotFound` `ErrorResponse`
    instead of a raw stack trace (`StorageExceptionHandlingBehavior`)

> All routes are `POST /{extensionVersion}/resource/{action}` with `extensionVersion = 1.0.0`.
> Prefer the command line? Each block translates directly to `Invoke-RestMethod -Method Post
> -Uri <url> -ContentType application/json -Body <json>` (add `-SkipHttpErrorCheck` to see
> error bodies instead of throwing).

## Notes

- `-SkipHttpErrorCheck` lets `Invoke-RestMethod` show the JSON error body instead of
  throwing on non-2xx.
- The Scalar UI at `/scalar/v2` lets you do all of the above interactively with the
  baked-in examples (`accountName=acct`, `containerName=container1`) — just edit `accountName` to
  your real account.
- No Azurite / connection strings: auth is always `az login` locally. If calls return 403,
  your RBAC role assignment hasn't propagated yet (can take a few minutes) or you're on the
  wrong subscription / tenant.

# Publishing the extension

This packages the extension as a **self-contained, single-file `linux-x64`** binary and
pushes it as an **OCI artifact** (via [ORAS](https://oras.land)) to an **Azure Container
Registry** with anonymous pull disabled. Run the commands from the project directory
(`sample/BlobPocExtension`) unless noted otherwise.

## 1. Publish the self-contained binary

```powershell
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

Output lands in `bin/Release/net8.0/linux-x64/publish/`. The single executable is named
`BlobPocExtension` (no extension on Linux) and bundles the .NET runtime, so the target host
needs no .NET install.

## 2. Create the Azure Container Registry

[`acr.bicep`](./acr.bicep) provisions a registry with `adminUserEnabled: false` and
`anonymousPullEnabled: false`, so every pull/push must authenticate through Azure AD.

```powershell
# reuse the existing resource group; registry name must be globally unique, 5-50 alphanumerics
$registryName = "ligartestacr$((Get-Random -Maximum 99999))"

az deployment group create `
  --resource-group ligar-test `
  --template-file acr.bicep `
  --parameters registryName=$registryName

# capture the login server (e.g. ligartestacrNNNNN.azurecr.io)
$loginServer = az acr show --name $registryName --query loginServer -o tsv
```

## 3. Push the binary as an OCI artifact

[ORAS](https://oras.land/docs/installation) pushes arbitrary files to an OCI registry
without a Dockerfile. Because anonymous pull is disabled, log ORAS in first. ORAS does
**not** need Docker — `az acr login --expose-token` returns an ACR access token you can
hand straight to `oras login` (the username is the literal all-zeros GUID):

```powershell
$token = az acr login --name $registryName --expose-token --query accessToken -o tsv
oras login $loginServer --username "00000000-0000-0000-0000-000000000000" --password $token

# push the published binary from its output folder
Push-Location bin/Release/net8.0/linux-x64/publish

oras push "$loginServer/blobpocextension:1.0.0" `
  --artifact-type application/vnd.bicep.extension.binary `
  BlobPocExtension:application/octet-stream

Pop-Location

# verify
az acr repository show-tags --name $registryName --repository blobpocextension -o table
oras manifest fetch "$loginServer/blobpocextension:1.0.0" | ConvertFrom-Json
```

To pull it later (also requires auth, since anonymous pull is off):

```powershell
$token = az acr login --name $registryName --expose-token --query accessToken -o tsv
oras login $loginServer --username "00000000-0000-0000-0000-000000000000" --password $token
oras pull "$loginServer/blobpocextension:1.0.0" --output ./pulled
```
