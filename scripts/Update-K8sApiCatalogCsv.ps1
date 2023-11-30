# Pre-requisites: Install Docker and minikube.
# The minimul supported Kubernetes version depends on the version of minikube you installed.
# Usage example:
#    .\Update-K8sApiCatalogCsv.ps1 -MajorMinorServerVersions 1.16,1.17
param(
    [Parameter(Mandatory = $true)][string[]] $MajorMinorServerVersions
)

$MajorMinorServerVersions | ForEach-Object {
    Write-Host "Generating API cataglog for Kubernetes sever version $_..."
    minikube start --kubernetes-version $_
    dotnet run --project "$PSScriptRoot\..\src\Azure.Deployment.Extensibility.Tools.K8sApiCatalogGenerator" --Configuration Release
    minikube delete
}