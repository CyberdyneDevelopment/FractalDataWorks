# 14-02 Building an ETL Server

The canonical reference implementation of an FDW ETL server lives in the **reference-etl** repository.

`reference-etl/public/src/Reference.Etl.Server/Program.cs` is the authoritative shape for an FDW ETL server startup, including the three-phase ServiceTypeCollection registration (`EtlPipelineTypes`, `TransformationTypes`, plus the same connection / secret-manager / authentication / operations registrations as the API server) and the pipeline background executor (`AddFrameworkPipelineBackgroundExecutor`).

## Required Steps

1. **Create an ASP.NET Core project** (Microsoft.NET.Sdk.Web, target net10.0).
2. **Reference the ETL packages:**
   - `Fdw.Services.Etl` and `Fdw.Services.Etl.Abstractions`
   - Per-pipeline-type implementation packages (e.g. `Fdw.Services.Etl.BatchCopy`, `Fdw.Services.Etl.Streaming`)
   - Per-transformation packages (`Fdw.Services.Transformations.*`)
3. **Run the single `PlatformServices.Configure`/`Register`/`Initialize` sweep** — it drives `EtlPipelineTypes` and `TransformationTypes` (and every other `[ServiceTypeCollection]`) automatically in dependency-safe order; no hand-written per-domain calls.
4. **Register the background executor:**

   ```csharp
   builder.Services.AddFrameworkPipelineBackgroundExecutor(loggerFactory, queueCapacity: 100);
   ```

   This registers `PipelineExecutionQueue` (bounded Channel) and the `PipelineExecutionBackgroundService` `HostedService`.
5. **Reference the ETL endpoint package** (`Fdw.Services.Etl.Endpoints` or build standalone endpoints that inject `IJobExecutionService`).

## Inter-Service Communication

The ETL server consumes pipeline configuration from the API server (via `IPipelineClient` typed HTTP client) and exposes its own job-trigger endpoints for callers (typically the Scheduler or admin UI). The shape of these clients is documented in [12-03 Service Communication](12-03-Service-Communication.md).

## See Also

- [06-04 Transformations Service Domain](06-04-Transformations-Service-Domain.md)
- [12-01 Creating a Server](12-01-Creating-A-Server.md)
- [12-13 OpsDb Configuration](12-13-OpsDb-Configuration.md)
- [20-02 Service Startup Order](20-02-Service-Startup-Order.md)
