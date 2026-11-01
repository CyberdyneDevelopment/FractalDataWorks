# The quality domain

Data quality: quality rules, their service, endpoints and clients.

This folder, `public/src/observability/quality/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/observability/quality/`, and its reference-servicetypes implementations under `reference-servicetypes/public/src/observability/quality/`.

## Projects

fractaldataworks:

- `Fdw.Services.Quality`
- `Fdw.Services.Quality.Abstractions`
- `Fdw.Services.Quality.Clients`: API client and registration types for data-quality rule and dashboard operations.
- `Fdw.Services.Quality.Endpoints`: HTTP endpoints for the Quality domain, extracted from Fdw.Web.Api.

reference-servicetypes:

- `FdwReferenceQuality.Endpoints`: The Quality domain HTTP endpoints - quality rules, results and promotion.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `QualityRuleTypeBase` (Fdw.Services.Quality.Abstractions)
- `QualitySeverityTypeBase` (Fdw.Services.Quality.Abstractions)
- `IQualitySeverityType` (Fdw.Services.Quality.Abstractions)
- `IQualityRuleType` (Fdw.Services.Quality.Abstractions)

## Rules that matter here

- No fallbacks: a missing value fails with a MessageLogging error and a failed result. The only `??` allowed is `logger ?? NullLogger<T>.Instance` in a constructor.
- Log through MessageLogging (source-generated messages), never raw `ILogger` calls.
- Return `IGenericResult` with a ResultCode; a failure never carries a plain string.
- All data access goes through `IDataGateway.Execute()` with command objects. No `SqlConnection`, `SqlCommand` or connection string outside the connection projects.
- The connection type is invisible above the connection layer: no `is`/`as`/`typeof` on a connection type, no switch on connection type names.
- Services register only through the ServiceTypeCollection three-phase lifecycle; no `AddXxx` extension methods and no ad-hoc `services.Add*` in applications.
- Every async method takes `CancellationToken cancellationToken = default` and passes it on. Compare strings with an explicit `StringComparison`.
- One type per file. Before adding a type, search the repo for one with the same name.

## Tests

Run a test project from the repository root:

- `dotnet test public/tests/observability/quality/Fdw.Services.Quality.Abstractions.Tests/Fdw.Services.Quality.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/observability/quality/Fdw.Services.Quality.Clients.Tests/Fdw.Services.Quality.Clients.Tests.csproj -c Release`
- `dotnet test public/tests/observability/quality/Fdw.Services.Quality.Tests/Fdw.Services.Quality.Tests.csproj -c Release`

## Dependencies

Depends on: `data` (6), `messagelogging` (6), `pipelines` (6), `collections` (5), `results` (4), `services` (4), `authentication` (3), `configuration` (3), `endpoints` (3), `authorization` (2), `commands` (2), `connections` (2), `datasets` (2), `scheduling` (2), `schema` (2), `secretmanagers` (2), `users` (2), `validation` (2), `calculations` (1), `catalog` (1), `hosts` (1), `multitenancy` (1), `registration` (1), `security` (1), `ui` (1).
Depended on by: `endpoints` (2), `authentication` (1), `authorization` (1), `catalog` (1), `connections` (1), `data` (1), `multitenancy` (1), `pipelines` (1), `scheduling` (1), `schema` (1), `shells` (1), `ui` (1), `users` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `catalog-quality`.
