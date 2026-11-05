# The scheduling domain

Scheduling: schedules, the scheduling service, endpoints and clients.

This folder, `public/src/scheduling/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/scheduling/`, and its reference-servicetypes implementations under `reference-servicetypes/public/src/scheduling/`.

## Projects

fractaldataworks:

- `Fdw.Services.Scheduling`
- `Fdw.Services.Scheduling.Abstractions`
- `Fdw.Services.Scheduling.Clients`: API client and registration types for schedule management operations.
- `Fdw.Services.Scheduling.Endpoints`: HTTP endpoints for the Scheduling domain, extracted from Fdw.Web.Api.

reference-servicetypes:

- `FdwReferenceSchedulers.EtlDispatch`: Scheduled execution handler that runs a firing schedule's pipeline by triggering it on the ETL server (FDW-902).
- `FdwReferenceSchedulers.EtlDispatch.Registration`: The ETL-dispatch scheduled execution handler registration: the [Implementation] only. References the implementation, so referencing THIS package brings the handler with it and registers it.
- `FdwReferenceSchedulers.Quartz`: Quartz.NET scheduler — the aggregation over FDW's Scheduling machinery, plus its service type registration.
- `FdwReferenceSchedulers.Quartz.Registration`: The Quartz scheduler service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceSchedules.Endpoints`: The Schedule domain HTTP endpoints, served by the scheduler host.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IGenericTrigger` (Fdw.Services.Scheduling.Abstractions)
- `TriggerTypeBase` (Fdw.Services.Scheduling.Abstractions)
- `IScheduleImplementationConfiguration` (Fdw.Services.Scheduling.Abstractions)
- `SchedulingResultCodeBase` (Fdw.Services.Scheduling.Abstractions)
- `ISchedulerImplementationConfiguration` (Fdw.Services.Scheduling.Abstractions)
- `SchedulingMessageCollectionBase` (Fdw.Services.Scheduling.Abstractions)
- `IFrameworkSchedulingService` (Fdw.Services.Scheduling.Abstractions)
- `IGenericSchedule` (Fdw.Services.Scheduling.Abstractions)
- `IScheduleConfigurationProvider` (Fdw.Services.Scheduling.Abstractions)
- `IScheduleClient` (Fdw.Services.Scheduling.Abstractions)
- `IGenericScheduledExecutionHandler` (Fdw.Services.Scheduling.Abstractions)
- `ISchedulingCommand` (Fdw.Services.Scheduling.Abstractions)
- `SchedulingCommandBase` (Fdw.Services.Scheduling.Abstractions)
- `ISchedulingFactory` (Fdw.Services.Scheduling.Abstractions)
- `ISchedulingService` (Fdw.Services.Scheduling.Abstractions)
- `ISchedulerConfigurationProvider` (Fdw.Services.Scheduling.Abstractions)
- `ISchedulerServiceProvider` (Fdw.Services.Scheduling.Abstractions)
- `ISchedulerType` (Fdw.Services.Scheduling.Abstractions)
- `IFrameworkSchedulingServiceFactory` (Fdw.Services.Scheduling.Abstractions)
- `ITriggerType` (Fdw.Services.Scheduling.Abstractions)
- and 9 more in the `.Abstractions` projects above

## Rules that matter here

- No fallbacks: a `??` never falls back to a value that could be a real one, and a missing value fails with a MessageLogging error and a failed result. Allowed: a null logger, `?? string.Empty`, `?? []`, `??=` on a field or local, a chain of failure results or messages, a `[PlaceholderText]` constant, a `[PlatformDefault]` member, and a member Mike approved with `[ApprovedFallback]`. Never a default message. FDW055 enforces it.
- Log through MessageLogging (source-generated messages), never raw `ILogger` calls.
- Return `IGenericResult` with a ResultCode; a failure never carries a plain string.
- All data access goes through `IDataGateway.Execute()` with command objects. No `SqlConnection`, `SqlCommand` or connection string outside the connection projects.
- The connection type is invisible above the connection layer: no `is`/`as`/`typeof` on a connection type, no switch on connection type names.
- Services register only through the ServiceTypeCollection three-phase lifecycle; no `AddXxx` extension methods and no ad-hoc `services.Add*` in applications.
- Every async method takes `CancellationToken cancellationToken = default` and passes it on. Compare strings with an explicit `StringComparison`.
- One type per file. Before adding a type, search the repo for one with the same name.

## Tests

Run a test project from the repository root:

- `dotnet test public/tests/scheduling/Fdw.Services.Scheduling.Abstractions.Tests/Fdw.Services.Scheduling.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/scheduling/Fdw.Services.Scheduling.Clients.Tests/Fdw.Services.Scheduling.Clients.Tests.csproj -c Release`
- `dotnet test public/tests/scheduling/Fdw.Services.Scheduling.Tests/Fdw.Services.Scheduling.Tests.csproj -c Release`
- `dotnet test public/tests/scheduling/FdwReferenceSchedulers.Tests/FdwReferenceSchedulers.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/scheduling/FdwReferenceSchedules.Endpoints.Tests/FdwReferenceSchedules.Endpoints.Tests.csproj -c Release` (in reference-servicetypes)

## Dependencies

Depends on: `data` (9), `pipelines` (8), `messagelogging` (7), `collections` (5), `services` (5), `authentication` (4), `configuration` (4), `commands` (3), `endpoints` (3), `results` (3), `secretmanagers` (3), `authorization` (2), `connections` (2), `datasets` (2), `multitenancy` (2), `schema` (2), `users` (2), `validation` (2), `abstractions` (1), `calculations` (1), `catalog` (1), `hosts` (1), `quality` (1), `registration` (1), `security` (1), `ui` (1).
Depended on by: `endpoints` (4), `pipelines` (3), `authentication` (2), `authorization` (2), `catalog` (2), `connections` (2), `data` (2), `multitenancy` (2), `quality` (2), `schema` (2), `ui` (2), `users` (2), `resiliency` (1), `shells` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `scheduling`, `catalog-sched`.
