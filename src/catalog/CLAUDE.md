# The catalog domain

The data catalog and lineage: catalog endpoints, clients and models, and data lineage.

This folder, `public/src/catalog/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/catalog/`, and its reference-servicetypes implementations under `reference-servicetypes/public/src/catalog/`.

## Projects

fractaldataworks:

- `Fdw.Data.Lineage`
- `Fdw.Services.Catalog.Clients`: API clients and DTOs for catalog operations.
- `Fdw.Services.Catalog.Clients.Models`: DTO models for catalog API client operations.
- `Fdw.Services.Catalog.Endpoints`: HTTP endpoints for the Catalog domain, extracted from Fdw.Web.Api.

reference-servicetypes:

- `FdwReferenceCatalog.Endpoints`: The Catalog domain HTTP endpoints - glossary terms, dataset annotations and catalog search.

## Seams

This domain has no `.Abstractions` project; its projects above are its surface.

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

- `dotnet test public/tests/catalog/Fdw.Data.Lineage.Tests/Fdw.Data.Lineage.Tests.csproj -c Release`
- `dotnet test public/tests/catalog/Fdw.Services.Catalog.Clients.Tests/Fdw.Services.Catalog.Clients.Tests.csproj -c Release`

## Dependencies

Depends on: `pipelines` (6), `collections` (4), `data` (4), `endpoints` (4), `authentication` (3), `messagelogging` (3), `authorization` (2), `calculations` (2), `connections` (2), `datasets` (2), `scheduling` (2), `schema` (2), `secretmanagers` (2), `services` (2), `users` (2), `abstractions` (1), `commands` (1), `configuration` (1), `hosts` (1), `multitenancy` (1), `quality` (1), `registration` (1), `results` (1), `security` (1), `transformations` (1), `ui` (1), `validation` (1), `workspaces` (1).
Depended on by: `endpoints` (2), `authentication` (1), `authorization` (1), `connections` (1), `data` (1), `multitenancy` (1), `pipelines` (1), `quality` (1), `scheduling` (1), `schema` (1), `ui` (1), `users` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `catalog-catalog`.
