FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /pushserver
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore "DexieNETCloudPushServer/DexieNETCloudPushServer.csproj" -a $TARGETARCH
# Build and publish a release
RUN dotnet publish "DexieNETCloudPushServer/DexieNETCloudPushServer.csproj" -c $BUILD_CONFIGURATION -o out -a $TARGETARCH

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /pushserver
COPY --from=build-env /pushserver/out .
ENTRYPOINT ["dotnet", "DexieNETCloudPushServer.dll"]