# The pipelines domain

Pipelines and ETL: orchestration, operations, ETL services and projects, mappers, job trigger sources, the ETL gateway and the pipeline UI clients.

This folder, `public/src/pipelines/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/pipelines/`, and its reference-servicetypes implementations under `reference-servicetypes/public/src/pipelines/`.

## Projects

fractaldataworks:

- `Fdw.Etl.Abstractions`
- `Fdw.Operations`
- `Fdw.Operations.Abstractions`
- `Fdw.Operations.Clients`: API clients and DTOs for operations (lineage, dataflow, configuration).
- `Fdw.Operations.Endpoints`: Generic endpoint base classes and DTOs for lineage/impact analysis and configuration metadata via FastEndpoints.
- `Fdw.Orchestration.Abstractions`
- `Fdw.Orchestration.Pipelines`
- `Fdw.Services.Etl`: ETL pipeline services for Fdw.
- `Fdw.Services.Etl.Abstractions`: Abstractions for Fdw ETL pipeline services.
- `Fdw.Services.Etl.JobTriggerSources`: The JobTriggerSource collection and its members — what caused an ETL job to run.
- `Fdw.Services.Etl.Projects`: Concrete writers, validators, providers, orchestrator runtime, completion signaling, and lineage extensions for ETL project orchestration (Project → Stage → Step → Pipeline).
- `Fdw.Services.Etl.Projects.Clients`: API clients and registration types for ETL project and orchestration-node operations.
- `Fdw.Services.Etl.Projects.Registration`: The ETL projects service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `Fdw.Services.EtlGateway`: The ETL host's own path to a data store's connection, built for running pipelines rather than serving a console: it resolves the connection and runs the command on it, without the data gateway's per-call caching, limit counting or request authorization. Scripted commands first.
- `Fdw.Services.EtlGateway.Registration`: The ETL gateway service-type registration: the [Implementation] only. References the gateway, so referencing THIS package brings the gateway with it and registers it; the ETL host references it.
- `Fdw.Services.EtlMappers`
- `Fdw.Services.EtlMappers.Abstractions`
- `Fdw.Services.EtlMappers.Dynamic`: Dynamic ETL row mapper — the aggregation over FDW's EtlMappers machinery, plus its service type registration.
- `Fdw.Services.EtlMappers.Pooled`: Pooled-dictionary ETL row mapper — the aggregation over FDW's EtlMappers machinery, plus its service type registration.
- `Fdw.Services.Pipelines`
- `Fdw.Services.Pipelines.Abstractions`
- `Fdw.Services.Pipelines.Clients`: API clients and registration types for pipeline and pipeline job operations.
- `Fdw.Services.Pipelines.Endpoints`: HTTP endpoints for the Pipelines domain, extracted from Fdw.Web.Api.
- `Fdw.UI.Pipelines.Clients`: API client contract, implementation and registration for the pipeline designer, over Fdw.UI.Pipelines.Clients.Models.
- `Fdw.UI.Pipelines.Clients.Models`: Pipeline editor UI DTOs for Fdw Management UIs.
- `Fdw.UI.Pipelines.Endpoints`: Generic endpoint base classes for the pipeline designer API — visual graph CRUD, task type discovery, and server-side validation via FastEndpoints.

reference-servicetypes:

- `FdwReferenceEtl.Endpoints`: The Etl domain HTTP endpoints - jobs, executions, nodes, lineage, projects, transformations and pipelines. Schedules and the API-host proxies live in their own packages.
- `FdwReferenceOperations.Endpoints`: The Operations domain HTTP endpoints - health, audit, analytics and escalation.
- `FdwReferenceTypeCollections.Endpoints`: The type collection HTTP endpoints: the collections a host serves and the options each holds.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IAdministrativeAudit` (Fdw.Operations.Abstractions)
- `IFlowComponent` (Fdw.Services.Etl.Abstractions)
- `IExecutionItem` (Fdw.Operations.Abstractions)
- `IExecutionTracker` (Fdw.Operations.Abstractions)
- `IPipelineImplementationConfiguration` (Fdw.Services.Pipelines.Abstractions)
- `IFlowStep` (Fdw.Services.Etl.Abstractions)
- `IPipelineConfigurationProvider` (Fdw.Services.Pipelines.Abstractions)
- `ComponentTypeBase` (Fdw.Services.Etl.Abstractions)
- `IHealthState` (Fdw.Etl.Abstractions)
- `IEtlRowMapper` (Fdw.Services.EtlMappers.Abstractions)
- `ITransformContext` (Fdw.Services.Etl.Abstractions)
- `IEscalationPolicy` (Fdw.Operations.Abstractions)
- `IOrchestrationStepResult` (Fdw.Orchestration.Abstractions)
- `IEtlPipeline` (Fdw.Services.Etl.Abstractions)
- `IOrchestrationStep` (Fdw.Orchestration.Abstractions)
- `IOrchestrationContext` (Fdw.Orchestration.Abstractions)
- `TransformTypeBase` (Fdw.Services.Etl.Abstractions)
- `IEtlRowMapperFactory` (Fdw.Services.EtlMappers.Abstractions)
- `ITransformOperationSpec` (Fdw.Services.Etl.Abstractions)
- `IEtlRowMapperType` (Fdw.Services.EtlMappers.Abstractions)
- and 210 more in the `.Abstractions` projects above

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

- `dotnet test public/tests/pipelines/Fdw.Operations.Clients.Tests/Fdw.Operations.Clients.Tests.csproj -c Release`
- `dotnet test public/tests/pipelines/Fdw.Operations.Endpoints.Tests/Fdw.Operations.Endpoints.Tests.csproj -c Release`
- `dotnet test public/tests/pipelines/Fdw.Operations.Tests/Fdw.Operations.Tests.csproj -c Release`
- `dotnet test public/tests/pipelines/Fdw.Orchestration.Abstractions.Tests/Fdw.Orchestration.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/pipelines/Fdw.Orchestration.Pipelines.Abstractions.Tests/Fdw.Orchestration.Pipelines.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/pipelines/Fdw.Orchestration.Pipelines.Tests/Fdw.Orchestration.Pipelines.Tests.csproj -c Release`
- `dotnet test public/tests/pipelines/Fdw.Services.Etl.Abstractions.Tests/Fdw.Services.Etl.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/pipelines/Fdw.Services.Etl.Projects.Tests/Fdw.Services.Etl.Projects.Tests.csproj -c Release`
- `dotnet test public/tests/pipelines/Fdw.Services.Etl.Tests/Fdw.Services.Etl.Tests.csproj -c Release`
- `dotnet test public/tests/pipelines/Fdw.Services.EtlMappers.Abstractions.Tests/Fdw.Services.EtlMappers.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/pipelines/Fdw.Services.EtlMappers.Dynamic.Tests/Fdw.Services.EtlMappers.Dynamic.Tests.csproj -c Release`
- `dotnet test public/tests/pipelines/Fdw.Services.EtlMappers.Pooled.Tests/Fdw.Services.EtlMappers.Pooled.Tests.csproj -c Release`
- `dotnet test public/tests/pipelines/Fdw.Services.EtlMappers.Tests/Fdw.Services.EtlMappers.Tests.csproj -c Release`
- `dotnet test public/tests/pipelines/Fdw.Services.Pipelines.Abstractions.Tests/Fdw.Services.Pipelines.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/pipelines/Fdw.Services.Pipelines.Clients.Tests/Fdw.Services.Pipelines.Clients.Tests.csproj -c Release`
- `dotnet test public/tests/pipelines/Fdw.Services.Pipelines.Tests/Fdw.Services.Pipelines.Tests.csproj -c Release`
- `dotnet test public/tests/pipelines/FdwReferenceEtl.Endpoints.Tests/FdwReferenceEtl.Endpoints.Tests.csproj -c Release` (in reference-servicetypes)

## Dependencies

Depends on: `data` (35), `collections` (33), `messagelogging` (29), `configuration` (19), `services` (16), `results` (13), `endpoints` (11), `authentication` (10), `commands` (10), `connections` (7), `abstractions` (4), `calculations` (4), `notifications` (4), `validation` (4), `authorization` (3), `datasets` (3), `scheduling` (3), `secretmanagers` (3), `hosts` (2), `registration` (2), `schema` (2), `users` (2), `agents` (1), `catalog` (1), `multitenancy` (1), `quality` (1), `resiliency` (1), `rowsources` (1), `security` (1), `telemetry` (1), `transformations` (1), `ui` (1), `workspaces` (1).
Depended on by: `data` (9), `endpoints` (9), `scheduling` (8), `ui` (8), `authentication` (6), `authorization` (6), `catalog` (6), `connections` (6), `multitenancy` (6), `quality` (6), `schema` (6), `users` (6), `shells` (2), `telemetry` (2), `calculations` (1), `commands` (1), `configuration` (1), `credentials` (1), `hosts` (1), `notifications` (1), `secretmanagers` (1), `services` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `pipelines`, `catalog-pipe`, `catalog-workflow`.
