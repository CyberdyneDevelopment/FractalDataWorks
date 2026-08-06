# 14-01 Building an API Server

The canonical reference implementation of an FDW API server lives in the **reference-api** repository.

`reference-api/public/src/Reference.Api/Program.cs` is the authoritative shape for an FDW API server startup, including the three-phase ServiceTypeCollection registration, hosting extensions (`AddFrameworkSerilog`, `AddFrameworkOpenTelemetry`, `AddFrameworkOperations`, `AddFrameworkHealthMonitoring`, `AddFrameworkDesignerPipelines`, `AddFrameworkSignalR`, `AddFrameworkRateLimiting`, `AddFrameworkCors`, `AddFrameworkConfigurationWriters`), and FastEndpoints wiring.

## Required Steps

To build a new FDW API server:

1. **Create an ASP.NET Core project** (Microsoft.NET.Sdk.Web, target net10.0).
2. **Reference the FDW hosting packages:**
   - `Fdw.MessageLogging.Abstractions`
   - `Fdw.MessageLogging.SourceGenerators` (analyzer-only)
   - `Fdw.Registration.SourceGenerators` (analyzer-only — emits ModuleInitializer to register `[ServiceTypeOption]` at assembly load)
   - `Fdw.Hosting`, `Fdw.Hosting.MsSql`
   - FastEndpoints
3. **Add a `configurationSchema.json`** file declaring the ConfigurationDb connection (loaded at the very start of `Program.cs`).
4. **Run the single `PlatformServices.Configure`/`Register`/`Initialize` sweep** — it drives every `[ServiceTypeCollection]` (and `[PlatformServiceProvider]` sibling) in dependency-safe order automatically; see [20-02 Service Startup Order](20-02-Service-Startup-Order.md).
5. **Reference per-domain `*.Endpoints` packages** for the API surfaces you want (`Fdw.Services.Connections.Endpoints`, `Fdw.Services.Data.Endpoints`, etc.) and create thin closure endpoints as documented in [12-08 Customizing Endpoints](12-08-Customizing-Endpoints.md).

## Key Wiring Patterns

For the canonical examples — including JWT auth, RBAC policies, FastEndpoints route prefix, OpenTelemetry, Scalar UI, SignalR — read `reference-api/public/src/Reference.Api/Program.cs` directly.

## See Also

- [12-01 Creating a Server](12-01-Creating-A-Server.md) — general server startup concepts
- [12-07 API Endpoints](12-07-API-Endpoints.md) — endpoint architecture overview
- [12-08 Customizing Endpoints](12-08-Customizing-Endpoints.md) — thin-closure endpoint pattern
- [12-11 JWT Authentication Architecture](12-11-JWT-Authentication-Architecture.md) — auth setup
- [20-02 Service Startup Order](20-02-Service-Startup-Order.md) — three-phase registration
