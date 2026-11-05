# The endpoints domain

HTTP endpoints: the web API base, endpoint and REST endpoint bases, HTTP abstractions and web clients.

This folder, `public/src/endpoints/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/endpoints/`, and its reference-servicetypes implementations under `reference-servicetypes/public/src/endpoints/`.

## Projects

fractaldataworks:

- `Fdw.Web.Api`
- `Fdw.Web.Clients`: Concrete HTTP implementation classes for Fdw API clients.
- `Fdw.Web.Clients.Abstractions`: Interfaces and contracts for Fdw API clients.
- `Fdw.Web.Endpoints`: Shared DTOs, base classes, and infrastructure for Fdw REST API endpoints.
- `Fdw.Web.Http.Abstractions`
- `Fdw.Web.RestEndpoints`

reference-servicetypes:

- `FdwReferenceEndpoints`: The endpoint ServiceTypeCollection — the aggregation that gathers the endpoint groups a host joined and composes its request pipeline. The bases, security and CRUD machinery it composes stay in Fdw.Web.RestEndpoints.
- `FdwReferenceProxy.Endpoints`: Proxy endpoints for the API host. These FORWARD over HTTP to the ETL server, which owns the orchestration queue - they are not the endpoints that do the work. They live in their own package because a proxy and the endpoint it proxies declare the SAME route, so hosting both in one assembly makes registration order decide which one answers.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IRateLimitPolicy` (Fdw.Web.Http.Abstractions)
- `RateLimitPolicyBase` (Fdw.Web.Http.Abstractions)
- `SecurityMethodBase` (Fdw.Web.Http.Abstractions)
- `EndpointTypeBase` (Fdw.Web.Http.Abstractions)
- `ICalculationTypeStats` (Fdw.Web.Clients.Abstractions)
- `IDataPreviewRequest` (Fdw.Web.Clients.Abstractions)
- `IDataSetField` (Fdw.Web.Clients.Abstractions)
- `ISecurityMethod` (Fdw.Web.Http.Abstractions)
- `IColumnSchema` (Fdw.Web.Clients.Abstractions)
- `IFieldDiscovery` (Fdw.Web.Clients.Abstractions)
- `IContainerDiscovery` (Fdw.Web.Clients.Abstractions)
- `IDataPreviewResponse` (Fdw.Web.Clients.Abstractions)
- `IEnvironmentInfo` (Fdw.Web.Clients.Abstractions)
- `IEndpointType` (Fdw.Web.Http.Abstractions)
- `IBrowserCache` (Fdw.Web.Clients.Abstractions)
- `IResourceQueryClient` (Fdw.Web.Clients.Abstractions)
- `IStreamingResponse` (Fdw.Web.Http.Abstractions)
- `ITriggerClient` (Fdw.Web.Clients.Abstractions)

## Rules that matter here

- An endpoint is one class per file and a pure HTTP orchestrator: validate, call a service, map, send. It never holds `IDataGateway` or business logic.
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

- `dotnet test public/tests/endpoints/Fdw.Web.Api.Tests/Fdw.Web.Api.Tests.csproj -c Release`
- `dotnet test public/tests/endpoints/Fdw.Web.Clients.Abstractions.Tests/Fdw.Web.Clients.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/endpoints/Fdw.Web.Http.Abstractions.Tests/Fdw.Web.Http.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/endpoints/Fdw.Web.RestEndpoints.Tests/Fdw.Web.RestEndpoints.Tests.csproj -c Release`

## Dependencies

Depends on: `messagelogging` (13), `collections` (9), `pipelines` (9), `data` (7), `services` (7), `authentication` (6), `connections` (5), `results` (5), `scheduling` (4), `authorization` (3), `configuration` (3), `schema` (3), `users` (3), `abstractions` (2), `catalog` (2), `datasets` (2), `multitenancy` (2), `quality` (2), `secretmanagers` (2), `ui` (2), `calculations` (1), `commands` (1), `hosts` (1), `mcp` (1), `registration` (1), `security` (1), `validation` (1).
Depended on by: `pipelines` (11), `data` (6), `authentication` (5), `schema` (5), `authorization` (4), `calculations` (4), `catalog` (4), `connections` (4), `notifications` (4), `ui` (4), `multitenancy` (3), `quality` (3), `scheduling` (3), `secretmanagers` (3), `telemetry` (3), `users` (3), `agents` (2), `configuration` (2), `workspaces` (2), `shells` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `endpoints`, `middleware`.
