# See https://hub.docker.com/_/microsoft-dotnet-runtime-deps/
FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine AS base
WORKDIR /app
EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000

# Enabling globalization
ENV \
     DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
     LC_ALL=en_US.UTF-8 \
     LANG=en_US.UTF-8
RUN apk add --no-cache icu-libs

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /src
COPY ["src/Extensibility.Core/Extensibility.Core.csproj", "src/Extensibility.Core/"]
COPY ["src/Extensibility.AzureStorage/Extensibility.AzureStorage.csproj", "src/Extensibility.AzureStorage/"]
COPY ["src/Extensibility.Kubernetes/Extensibility.Kubernetes.csproj", "src/Extensibility.Kubernetes/"]
COPY ["src/Extensibility.Standalone/Extensibility.Standalone.csproj", "src/Extensibility.Standalone/"]
RUN dotnet restore "src/Extensibility.Standalone/Extensibility.Standalone.csproj" -r linux-musl-x64 /p:PublishReadyToRun=false
COPY . .
WORKDIR "/src/src/Extensibility.Standalone"
RUN dotnet publish "Extensibility.Standalone.csproj" -c Release -o /app/publish -r linux-musl-x64 --self-contained true --no-restore /p:PublishReadyToRun=false /p:PublishSingleFile=true

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["/app/Extensibility.Standalone"]

