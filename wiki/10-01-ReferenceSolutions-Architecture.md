# Reference Applications Architecture

FDW reference applications live in their own repositories — they are not part of this framework repo:

| Repo | Purpose |
|------|---------|
| `reference-api` | REST API server (FastEndpoints + JWT + FDW hosting) |
| `reference-etl` | ETL pipeline server (`EtlPipelineTypes` + background executor) |
| `reference-scheduler` | Schedule evaluation server (`SchedulerTypes` + `IPipelineJobClient`) |
| `reference-ui` | Tailwind + InteractiveServer Blazor skin |
| `reference-aui` | MudBlazor + InteractiveAuto Blazor skin |

Each repo has its own `Directory.Packages.props` with a `<FdwVersion>` centrally pinning the FDW framework version, its own `build-all.sh` / CI pipeline, and its own deploy story (typically dotnet publish + rsync to a VM, with systemd unit files for runtime).

## Repository Boundary

This framework repository ships:

- The FDW NuGet packages (`FractalDataWorks.*`)
- The wiki and developer docs
- Build scripts for packing the framework

The reference repos consume the FDW packages and demonstrate concrete usage patterns. They are not built from this repo.

## Solution / Project Shape

Each reference repo follows the same general shape:

- `public/` — source root, mirrored to the corresponding GitHub repo via a CI subtree push
- `public/src/<App>/` — entry-point project (Microsoft.NET.Sdk.Web)
- `public/tests/<App>.Tests/` — xUnit v3 tests
- `public/Directory.Packages.props` — central package versions, pins `<FdwVersion>`
- `public/scripts/` — build/test scripts
- `Program.cs` — the canonical startup shape (see [12-01 Creating a Server](12-01-Creating-A-Server.md) and [20-02 Service Startup Order](20-02-Service-Startup-Order.md))

## Inter-Service Communication

The reference apps demonstrate the typed-client communication pattern (`IPipelineClient`, `IPipelineJobClient`, `IScheduleClient`, `AuthenticationApiClient`) documented in [12-03 Service Communication](12-03-Service-Communication.md).

## See Also

- [12-01 Creating a Server](12-01-Creating-A-Server.md) — general server startup
- [12-02 Deployment Guide](12-02-Deployment-Guide.md) — production deployment
- [12-03 Service Communication](12-03-Service-Communication.md) — typed-client patterns
- [14-01 Building an API Server](14-01-Building-An-API-Server.md) — `reference-api` pointer
- [14-02 Building an ETL Server](14-02-Building-An-ETL-Server.md) — `reference-etl` pointer
- [14-03 Building a Scheduler Server](14-03-Building-A-Scheduler-Server.md) — `reference-scheduler` pointer
- [15-01 Building a Blazor Server UI](15-01-Building-A-Blazor-Server-UI.md) — `reference-ui` pointer
- [15-02 Building a Blazor Auto UI](15-02-Building-A-Blazor-Auto-UI.md) — `reference-aui` pointer
