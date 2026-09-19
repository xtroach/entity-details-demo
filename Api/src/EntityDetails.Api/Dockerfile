# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# The SQLite database lives in /data rather than next to the app in /app, so a volume mounted at
# /data persists it without hiding the published files. The directory is created owned by the
# non-root app user because an empty named volume takes the ownership of the path it's mounted on.
RUN mkdir /data && chown $APP_UID:$APP_UID /data
ENV ConnectionStrings__AppDbContext="Data Source=/data/entitydetails.db"
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
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
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EntityDetails.Api.dll"]