# The telemetry domain

Telemetry and logging: the telemetry and logging services and web analytics clients.

This folder, `public/src/observability/telemetry/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/observability/telemetry/`, and its reference-servicetypes implementations under `reference-servicetypes/public/src/observability/telemetry/`.

## Projects

fractaldataworks:

- `Fdw.Services.Logging`
- `Fdw.Services.Logging.Abstractions`
- `Fdw.Services.Telemetry`
- `Fdw.Services.Telemetry.Abstractions`
- `Fdw.Web.Analytics.Clients`: API clients and DTOs for analytics, profiling, and promotion operations.
- `Fdw.Web.Analytics.Clients.Abstractions`: DTO models for analytics, profiling, and promotion API client operations.

reference-servicetypes:

- `FdwReferenceLogging.Serilog`: Serilog logging pipeline — the aggregation over FDW's Logging machinery, plus its service type registration.
- `FdwReferenceLogging.Serilog.Registration`: The Email notification service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceTelemetry.OpenTelemetry`: Email notification service — the aggregation over FDW's Notifications machinery, plus its service type registration.
- `FdwReferenceTelemetry.OpenTelemetry.Registration`: The Email notification service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `ILoggingService` (Fdw.Services.Logging.Abstractions)
- `ITelemetryService` (Fdw.Services.Telemetry.Abstractions)
- `IAnalyticsService` (Fdw.Web.Analytics.Clients.Abstractions)
- `ILoggingFactory` (Fdw.Services.Logging.Abstractions)
- `ILoggingImplementationConfiguration` (Fdw.Services.Logging.Abstractions)
- `ITelemetryFactory` (Fdw.Services.Telemetry.Abstractions)
- `ITelemetryImplementationConfiguration` (Fdw.Services.Telemetry.Abstractions)
- `ILoggingType` (Fdw.Services.Logging.Abstractions)
- `ITelemetryType` (Fdw.Services.Telemetry.Abstractions)
- `ILoggingConfigurationProvider` (Fdw.Services.Logging.Abstractions)
- `ITelemetryConfigurationProvider` (Fdw.Services.Telemetry.Abstractions)
- `ILoggingConfiguration` (Fdw.Services.Logging.Abstractions)
- `ITelemetryConfiguration` (Fdw.Services.Telemetry.Abstractions)

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

- `dotnet test public/tests/observability/telemetry/Fdw.Web.Analytics.Clients.Tests/Fdw.Web.Analytics.Clients.Tests.csproj -c Release`

## Dependencies

Depends on: `data` (10), `messagelogging` (10), `services` (8), `collections` (7), `configuration` (6), `commands` (4), `results` (4), `endpoints` (3), `abstractions` (2), `multitenancy` (2), `pipelines` (2), `secretmanagers` (2), `validation` (2), `authentication` (1).
Depended on by: `pipelines` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `catalog-otel`, `catalog-log`.
