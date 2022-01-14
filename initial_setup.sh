#!/bin/bash

# This file contains the commands I ran to set GitHub Actions up for the first time

az login

az account set --subscription 61e0a28a-63ed-4afc-9827-2ed09b7b30f3
az group create -n "bicep-extensibility-svc" -l "East US 2"

rbacScope=$(az group show -n "bicep-extensibility-svc" --query id -o tsv)
az ad sp create-for-rbac --name "Bicep Extensibility Service GitHub CD" --role contributor --scopes $rbacScope --sdk-auth > credentials.json

# Now - copy + paste contents of credentials.json and add to this repo as a GitHub Secret with name AZURE_CREDENTIALS