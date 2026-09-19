# Builds the Blazor WebAssembly client and serves the published static files with nginx.
# Build from the repository root so the ApiClient and Contracts projects are in the context:
#   docker build -f BlazorClient/src/EntityDetails.BlazorClient/Dockerfile -t entitydetails-blazorclient .

# This stage is used to publish the client. A Blazor WebAssembly app is plain static files once
# published, so only this stage needs the .NET SDK.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["BlazorClient/src/EntityDetails.BlazorClient/EntityDetails.BlazorClient.csproj", "BlazorClient/src/EntityDetails.BlazorClient/"]
COPY ["ApiClient/src/EntityDetails.ApiClient/EntityDetails.ApiClient.csproj", "ApiClient/src/EntityDetails.ApiClient/"]
COPY ["Contracts/src/EntityDetails.Contracts/EntityDetails.Contracts.csproj", "Contracts/src/EntityDetails.Contracts/"]
RUN dotnet restore "./BlazorClient/src/EntityDetails.BlazorClient/EntityDetails.BlazorClient.csproj"
COPY . .
WORKDIR "/src/BlazorClient/src/EntityDetails.BlazorClient"
RUN dotnet publish "./EntityDetails.BlazorClient.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore

# This stage serves the published wwwroot. API_BASE_URL, when set, overrides the ApiBaseUrl the
# browser reads from appsettings.json (see docker-entrypoint.d/40-api-base-url.sh).
FROM nginx:alpine AS final
COPY BlazorClient/src/EntityDetails.BlazorClient/nginx.conf /etc/nginx/conf.d/default.conf
COPY BlazorClient/src/EntityDetails.BlazorClient/docker-entrypoint.d/40-api-base-url.sh /docker-entrypoint.d/
RUN sed -i 's/\r$//' /docker-entrypoint.d/40-api-base-url.sh && chmod +x /docker-entrypoint.d/40-api-base-url.sh
COPY --from=publish /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 8080
