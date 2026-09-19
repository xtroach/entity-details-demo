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
├── .editorconfig          # Repo-wide formatting and analyzer (StyleCop) rules
├── CLAUDE.md              # Documentation and coding-standard requirements
├── README.md              # This file
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
    │   └── wwwroot/appsettings.json    # ApiBaseUrl setting
    └── test/EntityDetails.BlazorClient.Tests/   # bUnit component tests
```

## Setup
Prerequisites:
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker Desktop (optional, only needed to run the API's `Container (Dockerfile)` launch profile)

Get a dev environment running (two terminals — the API must be running for
the Blazor client to fetch data):
```bash
git clone <repo-url>
cd entity-details-demo

# Terminal 1 — API (creates/seeds a local SQLite database on first run)
cd Api/src/EntityDetails.Api
dotnet run

# Terminal 2 — Blazor client
cd BlazorClient/src/EntityDetails.BlazorClient
dotnet run
```
The API starts on `http://localhost:5148` / `https://localhost:7178`; the
OpenAPI document is available at `/openapi/v1.json` in Development. The
Blazor client starts on `http://localhost:5286` / `https://localhost:7137`
and calls the API at the `ApiBaseUrl` configured in its
`wwwroot/appsettings.json` (defaults to `https://localhost:7178`). Run the
whole solution with `dotnet build` / `dotnet test` from the repo root using
`EntityDetailsDemo.slnx`.

## CodeConventions
- Formatting and language conventions (indentation, brace style, `var` usage,
  file-scoped namespaces, etc.) are defined in `.editorconfig` and apply
  repo-wide.
- `Nullable` and `ImplicitUsings` are enabled in every project.
- XML documentation comments (`///`) are required on public and internal
  types, methods, properties, and events in all `src/` projects, enforced at
  build time via `StyleCop.Analyzers` and the compiler's `CS1591` check
  (configured in `.editorconfig`). Test projects are exempt — they don't
  enable documentation generation or reference StyleCop.Analyzers. See
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
- `Api`'s `Dockerfile` builds from the repository root (`DockerfileContext`
  is set to the repo root) since it needs the `Api`, `Data`, and `Contracts`
  project files to restore.

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
