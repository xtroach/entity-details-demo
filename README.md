# entity-details-demo

## Overview
`entity-details-demo` explores how an EF (Entity Framework) → REST API → Blazor
stack can support generic CRUD implementations, with an eye toward growing into
a skeleton for rapidly building APIs and front ends for workflows currently
done in spreadsheets. The repository is split into five projects: a `Data`
class library owning the EF Core `DbContext` and entities, a `Contracts`
class library defining the wire-format types exchanged between the API and
its clients, an `Api` ASP.NET Core Web API that exposes CRUD endpoints over
`Data` in terms of `Contracts` types, an `ApiClient` class library that wraps
calls to the API in a typed HTTP client, and a `BlazorClient` WebAssembly app
that consumes `ApiClient` to render the data in the browser.

## Folder Structure
Each project is split into a `src/` folder (the project itself) and, except
for `Contracts` (see Architecture), a `test/` folder (its xUnit test
project). Within `src/`, entity-related files are grouped into one folder
per entity (e.g. `WeatherForecast/`), so a given entity's files live
together as the number of entities grows.
```
entity-details-demo/
├── .dockerignore          # Build-context exclusions for every Dockerfile (all build from the root)
├── .editorconfig          # Repo-wide formatting and analyzer (StyleCop) rules
├── .gitattributes         # Forces LF endings for shell scripts that run inside containers
├── CLAUDE.md              # Documentation and coding-standard requirements
├── Directory.Build.props  # MSBuild properties shared by every project (warnings as errors)
├── README.md              # This file
├── dev.ps1                # Builds and runs the API and Blazor client together for local dev
├── EntityDetailsDemo.slnx # Solution file referencing all 9 projects
├── Data/                          # EF Core data-access layer
│   ├── src/EntityDetails.Data/
│   │   ├── AppDbContext.cs        # The application's DbContext
│   │   └── Entities/               # EF entities, one folder per entity
│   │       └── WeatherForecast/WeatherForecast.cs
│   └── test/EntityDetails.Data.Tests/
├── Contracts/                     # Wire-format types shared by Api and ApiClient
│   └── src/EntityDetails.Contracts/
│       └── WeatherForecast/       # One folder per entity
│           ├── WeatherForecastDto.cs
│           └── WeatherForecastRequest.cs
├── Api/                           # ASP.NET Core Web API
│   ├── src/EntityDetails.Api/
│   │   ├── Program.cs             # Entry point, DI, and middleware pipeline
│   │   ├── Controllers/           # CRUD API controllers
│   │   ├── Mapping/               # Entity <-> Contracts type mapping
│   │   ├── Properties/launchSettings.json
│   │   ├── appsettings.json / appsettings.Development.json
│   │   └── Dockerfile
│   └── test/EntityDetails.Api.Tests/   # WebApplicationFactory integration tests
├── ApiClient/                     # Typed HTTP client used by BlazorClient
│   ├── src/EntityDetails.ApiClient/
│   │   ├── IWeatherForecastApiClient.cs / WeatherForecastApiClient.cs
│   │   └── ServiceCollectionExtensions.cs  # DI registration helper
│   └── test/EntityDetails.ApiClient.Tests/
└── BlazorClient/                  # Blazor WebAssembly front end
    ├── src/EntityDetails.BlazorClient/
    │   ├── Program.cs             # Registers ApiClient, configures API base URL
    │   ├── Pages/, Layout/, wwwroot/
    │   ├── wwwroot/appsettings.json    # ApiBaseUrl setting
    │   ├── Dockerfile             # Publishes the client and serves it with nginx
    │   ├── nginx.conf             # SPA fallback, precompressed assets, cache headers
    │   └── docker-entrypoint.d/40-api-base-url.sh  # Applies API_BASE_URL at container start
    └── test/EntityDetails.BlazorClient.Tests/   # bUnit component tests
```

## Setup
Prerequisites:
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A trusted HTTPS development certificate (`dotnet dev-certs https --trust`)
- PowerShell — Windows PowerShell 5.1 (built into Windows) or PowerShell 7+
  (`pwsh`, required on macOS/Linux) — to run `dev.ps1`
- Docker Desktop (optional, only needed to build the container images or run
  the API's `Container (Dockerfile)` launch profile)

Get a dev environment running with one command from the repo root:
```powershell
git clone <repo-url>
cd entity-details-demo
./dev.ps1              # add -OpenBrowser to open the client once it's up
```
`dev.ps1` builds the API and the Blazor client, starts both with their
`https` launch profiles in the same terminal, waits until both accept
connections, and prints their URLs. Ctrl+C stops both. If either app exits
on its own, the script stops the other too. On first run the API
creates and seeds a local SQLite database. To run the apps separately
instead, run `dotnet run --launch-profile https` in
`Api/src/EntityDetails.Api` and `BlazorClient/src/EntityDetails.BlazorClient`
in two terminals.

The API starts on `https://localhost:7178` / `http://localhost:5148`; the
OpenAPI document is available at `/openapi/v1.json` in Development. The
Blazor client starts on `https://localhost:7137` / `http://localhost:5286`
and calls the API at the `ApiBaseUrl` configured in its
`wwwroot/appsettings.json` (defaults to `https://localhost:7178`). Run the
whole solution with `dotnet build` / `dotnet test` from the repo root using
`EntityDetailsDemo.slnx`.

Build and run the container images (both build from the repo root):
```bash
docker build -f Api/src/EntityDetails.Api/Dockerfile -t entitydetails-api .
docker build -f BlazorClient/src/EntityDetails.BlazorClient/Dockerfile -t entitydetails-blazorclient .

# Serves the client on http://localhost:8080, pointed at the API address given
docker run --rm -p 8080:8080 -e API_BASE_URL=https://localhost:7178 entitydetails-blazorclient
```

## CodeConventions
- Formatting and language conventions (indentation, brace style, `var` usage,
  file-scoped namespaces, etc.) are defined in `.editorconfig` and apply
  repo-wide.
- `Nullable` and `ImplicitUsings` are enabled in every project.
- Every project builds with `TreatWarningsAsErrors` (set once in
  `Directory.Build.props`), so any compiler, analyzer, or NuGet warning —
  including nullable-reference warnings and NuGet vulnerability-audit
  warnings — fails the build. Diagnostics configured below `warning`
  severity in `.editorconfig` (`suggestion`/`silent`) are unaffected.
- XML documentation comments (`///`) are required on public and internal
  types, methods, properties, and events in all `src/` projects, enforced at
  build time via `StyleCop.Analyzers` and the compiler's `CS1591` check
  (configured in `.editorconfig`; a missing comment fails the build). Test
  projects are exempt — they don't enable documentation generation or
  reference StyleCop.Analyzers. See
  `CLAUDE.md` for the full documentation standard.
- Wire-format types (e.g. `WeatherForecastDto`/`WeatherForecastRequest`) live in
  `Contracts`, referenced by both `Api` and `ApiClient`, so the two can never
  drift apart on the shape of data exchanged between them. Neither `Contracts`
  nor `ApiClient` reference `Data`, so the Blazor client never depends on EF Core.

## Configuration
- **Api/appsettings.json** — logging levels, `AllowedHosts`, the
  `ConnectionStrings:AppDbContext` SQLite connection string (default
  `Data Source=entitydetails.db`), and `BlazorClientOrigins` (the CORS
  allow-list, defaulting to the Blazor client's dev URLs).
- **ASPNETCORE_ENVIRONMENT** — controls the OpenAPI endpoint (Development
  only) and whether the API seeds sample weather forecasts on startup
  (Development only; see `Program.SeedDevelopmentData`).
- **Api/Properties/launchSettings.json** — local run profiles: `http` (port
  5148), `https` (ports 7178/5148), and `Container (Dockerfile)` (ports
  8080/8081).
- **BlazorClient/wwwroot/appsettings.json** — `ApiBaseUrl`, the address the
  Blazor client's `ApiClient` registration points at.
- **API_BASE_URL** (Blazor client container only) — when set, the
  container's entrypoint hook rewrites the served `appsettings.json` so
  `ApiBaseUrl` points at this address. If it isn't set, the image keeps the
  published default (`https://localhost:7178`). The API must also allow the
  client container's origin through CORS, e.g.
  `BlazorClientOrigins__0=http://localhost:8080` on the API.
- **dev.ps1 parameters** — `-OpenBrowser` (open the client once it's ready;
  off by default) and `-StartupTimeoutSeconds` (how long to wait for each
  app to accept connections; default 60). Ports come from each project's
  `https` launch profile.
- **User secrets** — `Api`'s project has a `UserSecretsId` configured for
  storing local secrets outside source control via `dotnet user-secrets`.

## Architecture
- **Data** (`EntityDetails.Data`) is a class library owning `AppDbContext`
  and the EF entities (e.g. `WeatherForecast`). It has no knowledge of HTTP
  or the API and is referenced only by `Api`.
- **Contracts** (`EntityDetails.Contracts`) is a class library defining the
  wire-format types exchanged between `Api` and its clients (e.g.
  `WeatherForecastDto`/`WeatherForecastRequest`). It has no dependency on
  anything beyond the BCL — no EF Core, no HTTP — and is referenced by both
  `Api` and `ApiClient`, so neither can drift from the other on the shape of
  exchanged data. It deliberately has no validation logic and no `test/`
  project: it holds plain data shapes with no behavior to test today (see
  the open validation-layer issue for planned changes to this).
- **Api** (`EntityDetails.Api`) is an ASP.NET Core Web API referencing
  `Data` and `Contracts`. `Program.cs` registers the DbContext (SQLite), a
  CORS policy for the Blazor client's origin, and ensures/seeds the database
  on startup. `Controllers/WeatherForecastController` exposes full CRUD
  (`GET`, `GET/{id}`, `POST`, `PUT/{id}`, `DELETE/{id}`) in terms of
  `Contracts` types, not the EF entity directly; `Mapping/WeatherForecastMapper`
  converts between the `WeatherForecast` entity and its `Contracts`
  representation.
- **ApiClient** (`EntityDetails.ApiClient`) is a class library referencing
  only `Contracts` — no dependency on `Data`. It defines
  `IWeatherForecastApiClient`/`WeatherForecastApiClient`, a typed
  `HttpClient` wrapper around the API's endpoints using `Contracts` types,
  plus a `AddEntityDetailsApiClient(Uri)` DI extension.
- **BlazorClient** (`EntityDetails.BlazorClient`) is a standalone Blazor
  WebAssembly app referencing only `ApiClient`. It runs entirely in the
  browser, so it cannot access `Data`/EF Core directly — all data access
  goes through `ApiClient` calling the `Api` over HTTP. `Pages/Weather.razor`
  demonstrates this by injecting `IWeatherForecastApiClient` to render
  forecasts fetched from the API.
- Each project's `test/` folder holds its xUnit tests (`Contracts` excepted,
  see above): `Api.Tests` uses `WebApplicationFactory` with the EF Core
  InMemory provider for integration tests, asserting against `Contracts`
  types the same way a real client would; `Data.Tests` tests `AppDbContext`
  directly against InMemory; `ApiClient.Tests` fakes `HttpMessageHandler` to
  test the typed client in isolation; `BlazorClient.Tests` uses bUnit to
  render `Weather.razor` against a fake `IWeatherForecastApiClient`.
- Both Dockerfiles build from the repository root, because each app needs
  sibling projects' files to restore (`Api` needs `Data` and `Contracts`;
  `BlazorClient` needs `ApiClient` and `Contracts`). `Api`'s
  `DockerfileContext` is set to the repo root for Visual Studio. Docker
  only reads `.dockerignore` from the root of the build context, so the
  single `.dockerignore` lives at the repo root. It keeps host `bin/`/`obj/`,
  IDE state, and local SQLite databases out of every image build.
- The `BlazorClient` image is a two-stage build. The .NET SDK publishes the
  app, and `nginx:alpine` serves the published `wwwroot`. A Blazor
  WebAssembly app is plain static files once published, so the final image
  needs no .NET runtime. `nginx.conf` falls back to `index.html` for
  client-side routes and serves the `.gz` files publish already writes. The
  browser downloads `appsettings.json` at startup, so the API address is set
  when the container starts (`API_BASE_URL`), not baked in at build time.
  One image can target any API.
- `dev.ps1` builds both app projects one after the other before starting
  either with `dotnet run --no-build`. Both depend on `Contracts`, so
  parallel builds would race on its `obj/` folder. On shutdown it kills
  each `dotnet run` process tree, not just the `dotnet` process, because the
  app runs as a child process and would otherwise keep holding its port.

## Development Process
Changes to this repo go through a structured process, not ad-hoc prompting:

- **Design decisions get a paper trail.** Before any non-trivial change,
  the assistant either scopes it into a GitHub issue first (decision,
  reasoning, and enough detail to implement later with no memory of the
  conversation) or, if implementing immediately, writes that same issue
  before touching any code. Trivial changes (typos, formatting) skip
  this. Full rules in [`CLAUDE.md`](./CLAUDE.md).
- **`main` is protected, not just by convention.** All changes land via
  pull request — enforced by GitHub branch protection (`enforce_admins`
  on, so this applies to the repo owner too, not just contributors), not
  merely documented as a policy. Force-pushes and branch deletion on
  `main` are blocked outright.
- **Merges never rewrite history.** PRs merge via "Create a merge
  commit" exclusively — squash and rebase merge are disabled at the
  repository level, the only GitHub method that never rewrites
  already-pushed commits. Merged branches auto-delete, which also keeps
  GitHub's PR-stacking mechanics reliable.
- **Nothing merges without local verification.** Every PR touching code
  has had `dotnet build`/`dotnet test` run locally first, with new or
  changed logic accompanied by tests in the same PR.
- **Supply-chain and secrets hygiene is on by default.** Dependabot
  security updates, secret scanning, and push protection are enabled;
  a committed secret is treated as compromised on sight, not just
  deleted.
- **Enforced where possible, self-enforced where not.** Where GitHub can
  enforce a rule structurally (branch protection, merge method,
  auto-delete), it does; where it can't yet (build/test verification,
  test-writing discipline), the assistant follows it consistently and
  flags genuinely ambiguous cases rather than deciding silently.

The complete, current rule set the assistant follows in this repo lives
in [`CLAUDE.md`](./CLAUDE.md).
