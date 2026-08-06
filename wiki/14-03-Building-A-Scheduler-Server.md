# 14-03 Building a Scheduler Server

The canonical reference implementation of an FDW scheduler server lives in the **reference-scheduler** repository.

`reference-scheduler/public/src/Reference.Scheduler.Server/Program.cs` is the authoritative shape for an FDW scheduler startup, including the three-phase ServiceTypeCollection registration (`SchedulerTypes`, plus the standard connection / secret-manager / authentication / operations registrations) and the scheduling background executor.

## Required Steps

1. **Create an ASP.NET Core project** (Microsoft.NET.Sdk.Web, target net10.0).
2. **Reference the scheduling packages:**
   - `Fdw.Services.Scheduling` and `Fdw.Services.Scheduling.Abstractions`
   - Per-trigger-type packages (cron, interval, once, manual)
3. **Run the single `PlatformServices.Configure`/`Register`/`Initialize` sweep** — it drives `SchedulerTypes` (and every other `[ServiceTypeCollection]`) automatically in dependency-safe order; no hand-written `SchedulerTypes.Configure(...)` call.
4. **Reference the scheduling endpoint package** (`Fdw.Services.Scheduling.Endpoints`) for the standard CRUD shape, or build custom endpoints that inject `IScheduleService` / `IScheduleExecutor`.

## Trigger Types

The shipped trigger types are registered as `[ServiceTypeOption(typeof(TriggerTypes), "...")]` instances. See the `scheduling` skill bundle for the Cron / Interval / Once / Manual specifics.

## Inter-Service Communication

The scheduler triggers jobs by invoking the ETL server's `IPipelineJobClient` (typed HTTP client). The shape of inter-service clients is documented in [12-03 Service Communication](12-03-Service-Communication.md).

## See Also

- [12-01 Creating a Server](12-01-Creating-A-Server.md)
- [12-03 Service Communication](12-03-Service-Communication.md)
- [12-13 OpsDb Configuration](12-13-OpsDb-Configuration.md)
- [20-02 Service Startup Order](20-02-Service-Startup-Order.md)
