FROM mcr.microsoft.com/dotnet/sdk:5.0

# Set up MS package feed
RUN \
    wget https://packages.microsoft.com/config/debian/10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb

# Install dotnet 3.1 (also required for functionapps for some reason)
RUN \
    apt-get update \
    && apt-get install -y apt-transport-https \
    && apt-get update \
    && apt-get install -y dotnet-sdk-3.1