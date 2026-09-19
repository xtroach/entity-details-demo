# Builds the Blazor WebAssembly client and serves the published static files with nginx.
# Build from the repository root so the ApiClient and Contracts projects are in the context:
#   docker build -f BlazorClient/src/EntityDetails.BlazorClient/Dockerfile -t entitydetails-blazorclient .

# This stage is used to publish the client. A Blazor WebAssembly app is plain static files once
# published, so only this stage needs the .NET SDK.
# Base images are pinned by digest so every build uses exactly the same image. The tag stays for
# readability; Dependabot (.github/dependabot.yml) opens a PR when a new digest is published.
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:2fa828c68761b1b8c23d7662dc134421b9d3b59fe1425fdbc80804e390cdb24d AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Repo-wide build settings: central package versions (required to restore), the SDK pin and
# warnings-as-errors. Copied before restore so they apply to it.
COPY ["Directory.Build.props", "Directory.Packages.props", "global.json", "./"]
COPY ["BlazorClient/src/EntityDetails.BlazorClient/EntityDetails.BlazorClient.csproj", "BlazorClient/src/EntityDetails.BlazorClient/"]
COPY ["ApiClient/src/EntityDetails.ApiClient/EntityDetails.ApiClient.csproj", "ApiClient/src/EntityDetails.ApiClient/"]
COPY ["Contracts/src/EntityDetails.Contracts/EntityDetails.Contracts.csproj", "Contracts/src/EntityDetails.Contracts/"]
RUN dotnet restore "./BlazorClient/src/EntityDetails.BlazorClient/EntityDetails.BlazorClient.csproj"
COPY . .
WORKDIR "/src/BlazorClient/src/EntityDetails.BlazorClient"
RUN dotnet publish "./EntityDetails.BlazorClient.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore

# This stage serves the published wwwroot. API_BASE_URL, when set, overrides the ApiBaseUrl the
# browser reads from appsettings.json (see docker-entrypoint.d/40-api-base-url.sh).
FROM nginx:alpine@sha256:62ff2089abf5a9ed33bd232895bef5e22f7bb4b200675cec49a5ebc48e3d4ac8 AS final
# Links the published GHCR package to this repository (shown on the repo page; lets its workflows push).
LABEL org.opencontainers.image.source="https://github.com/xtroach/entity-details-demo"
COPY BlazorClient/src/EntityDetails.BlazorClient/nginx.conf /etc/nginx/conf.d/default.conf
COPY BlazorClient/src/EntityDetails.BlazorClient/docker-entrypoint.d/40-api-base-url.sh /docker-entrypoint.d/
RUN sed -i 's/\r$//' /docker-entrypoint.d/40-api-base-url.sh && chmod +x /docker-entrypoint.d/40-api-base-url.sh
COPY --from=publish /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 8080
# busybox wget ships with nginx:alpine.
HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=5 CMD wget -q --spider http://127.0.0.1:8080/ || exit 1
