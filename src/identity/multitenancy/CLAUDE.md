# The multitenancy domain

Multitenancy: tenants and tenant scope, the multitenancy service, endpoints and clients.

This folder, `public/src/identity/multitenancy/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/identity/multitenancy/`, and its reference-servicetypes implementations under `reference-servicetypes/public/src/identity/multitenancy/`.

## Projects

fractaldataworks:

- `Fdw.Services.Multitenancy`: Multi-tenancy service domain: ServiceTypeCollection selecting SingleTenant/Sql tenant resolution per host
- `Fdw.Services.Multitenancy.Abstractions`
- `Fdw.Services.Multitenancy.Clients`: API client and registration types for tenant management operations.
- `Fdw.Services.Multitenancy.Endpoints`: HTTP endpoints for the Multitenancy domain, extracted from Fdw.Web.Api.

reference-servicetypes:

- `FdwReferenceMultitenancy`: Multitenancy over HTTP — resolving the tenant and org a request is for, from its claims and headers. Reads the Multitenancy abstractions only, so the collection that composes the request pipeline can place this without naming which provider stores the tenants.
- `FdwReferenceMultitenancy.Sql`: Sql multitenancy provider — the aggregation over FDW's Multitenancy machinery, plus its service type registration.
- `FdwReferenceMultitenancy.Sql.Registration`: The SQL multitenancy service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `ITenant` (Fdw.Services.Multitenancy.Abstractions)
- `ITenantProvider` (Fdw.Services.Multitenancy.Abstractions)
- `IOrganizationProvider` (Fdw.Services.Multitenancy.Abstractions)
- `IPlaceAncestry` (Fdw.Services.Multitenancy.Abstractions)
- `ITenantWriter` (Fdw.Services.Multitenancy.Abstractions)
- `IRetainedDocuments` (Fdw.Services.Multitenancy.Abstractions)
- `ITenantTheme` (Fdw.Services.Multitenancy.Abstractions)
- `ITenantContext` (Fdw.Services.Multitenancy.Abstractions)
- `IPlaceNames` (Fdw.Services.Multitenancy.Abstractions)
- `IFeatureEnablement` (Fdw.Services.Multitenancy.Abstractions)
- `ILegalHolds` (Fdw.Services.Multitenancy.Abstractions)
- `IOrganizationWriter` (Fdw.Services.Multitenancy.Abstractions)
- `IPlaceParentSource` (Fdw.Services.Multitenancy.Abstractions)
- `IRetentionPolicies` (Fdw.Services.Multitenancy.Abstractions)
- `ITenantOptions` (Fdw.Services.Multitenancy.Abstractions)
- `FeatureBase` (Fdw.Services.Multitenancy.Abstractions)
- `ScopeLayerBase` (Fdw.Services.Multitenancy.Abstractions)
- `IRetentionKeeper` (Fdw.Services.Multitenancy.Abstractions)
- `TenantTypeBase` (Fdw.Services.Multitenancy.Abstractions)
- `IFeature` (Fdw.Services.Multitenancy.Abstractions)
- and 7 more in the `.Abstractions` projects above

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

- `dotnet test public/tests/identity/multitenancy/Fdw.Services.Multitenancy.Abstractions.Tests/Fdw.Services.Multitenancy.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/identity/multitenancy/Fdw.Services.Multitenancy.Clients.Tests/Fdw.Services.Multitenancy.Clients.Tests.csproj -c Release`
- `dotnet test public/tests/identity/multitenancy/Fdw.Services.Multitenancy.Endpoints.Tests/Fdw.Services.Multitenancy.Endpoints.Tests.csproj -c Release`
- `dotnet test public/tests/identity/multitenancy/FdwReferenceMultitenancy.Sql.Tests/FdwReferenceMultitenancy.Sql.Tests.csproj -c Release` (in reference-servicetypes)

## Dependencies

Depends on: `messagelogging` (8), `collections` (6), `data` (6), `pipelines` (6), `services` (5), `authentication` (4), `configuration` (4), `authorization` (3), `endpoints` (3), `results` (3), `abstractions` (2), `connections` (2), `datasets` (2), `scheduling` (2), `schema` (2), `secretmanagers` (2), `security` (2), `users` (2), `calculations` (1), `catalog` (1), `commands` (1), `hosts` (1), `quality` (1), `registration` (1), `ui` (1), `validation` (1).
Depended on by: `authentication` (3), `authorization` (3), `endpoints` (2), `scheduling` (2), `telemetry` (2), `users` (2), `calculations` (1), `catalog` (1), `connections` (1), `credentials` (1), `data` (1), `hosts` (1), `pipelines` (1), `quality` (1), `schema` (1), `shells` (1), `ui` (1), `workspaces` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `multitenancy`, `access-tenancy`, `catalog-tenant`.
