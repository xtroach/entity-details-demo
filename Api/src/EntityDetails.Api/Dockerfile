# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
# Base images are pinned by digest so every build uses exactly the same image. The tag stays for
# readability; Dependabot (.github/dependabot.yml) opens a PR when a new digest is published.
FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:2d584d8147faddb0d678c5748d47953e5b8e18621ed4fb7049a91381d9d7746f AS base
# The image holds no data: the database is a separate PostgreSQL server, and its connection string
# (ConnectionStrings__AppDbContext) must be supplied when the container starts.
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:35d40304542c8689331f8cab17c65926cdf48fe711e289321d71924b230a7d29 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Repo-wide build settings: central package versions (required to restore), the SDK pin and
# warnings-as-errors. Copied before restore so they apply to it.
COPY ["Directory.Build.props", "Directory.Packages.props", "global.json", "./"]
COPY ["Api/src/EntityDetails.Api/EntityDetails.Api.csproj", "Api/src/EntityDetails.Api/"]
COPY ["Data/src/EntityDetails.Data/EntityDetails.Data.csproj", "Data/src/EntityDetails.Data/"]
COPY ["Contracts/src/EntityDetails.Contracts/EntityDetails.Contracts.csproj", "Contracts/src/EntityDetails.Contracts/"]
RUN dotnet restore "./Api/src/EntityDetails.Api/EntityDetails.Api.csproj"
COPY . .
WORKDIR "/src/Api/src/EntityDetails.Api"
RUN dotnet build "./EntityDetails.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./EntityDetails.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
# Links the published GHCR package to this repository (shown on the repo page; lets its workflows push).
LABEL org.opencontainers.image.source="https://github.com/xtroach/entity-details-demo"
WORKDIR /app
COPY --from=publish /app/publish .
# The runtime image has no curl or wget, so the API probes itself: --probe requests /health/live and
# exits 0 (healthy) or 1 (unhealthy). See HealthProbe.cs.
HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=5 CMD ["dotnet", "EntityDetails.Api.dll", "--probe"]
ENTRYPOINT ["dotnet", "EntityDetails.Api.dll"]
