# The users domain

Users: the user service, its endpoints and clients.

This folder, `public/src/identity/users/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/identity/users/`.

## Projects

fractaldataworks:

- `Fdw.Services.Users`
- `Fdw.Services.Users.Abstractions`
- `Fdw.Services.Users.Clients`: API client and registration types for user management operations.
- `Fdw.Services.Users.Endpoints`: HTTP endpoints for the Users domain, extracted from Fdw.Web.Api.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IUserCredentialService` (Fdw.Services.Users.Abstractions)
- `UserResultCodeBase` (Fdw.Services.Users.Abstractions)
- `IUser` (Fdw.Services.Users.Abstractions)
- `IUserTenantsHeld` (Fdw.Services.Users.Abstractions)
- `IUserConfiguration` (Fdw.Services.Users.Abstractions)

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

- `dotnet test public/tests/identity/users/Fdw.Services.Users.Abstractions.Tests/Fdw.Services.Users.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/identity/users/Fdw.Services.Users.Clients.Tests/Fdw.Services.Users.Clients.Tests.csproj -c Release`
- `dotnet test public/tests/identity/users/Fdw.Services.Users.Tests/Fdw.Services.Users.Tests.csproj -c Release`

## Dependencies

Depends on: `data` (8), `pipelines` (6), `collections` (5), `messagelogging` (5), `results` (5), `commands` (4), `configuration` (4), `secretmanagers` (4), `services` (4), `abstractions` (3), `authentication` (3), `authorization` (3), `credentials` (3), `endpoints` (3), `connections` (2), `datasets` (2), `multitenancy` (2), `scheduling` (2), `schema` (2), `security` (2), `calculations` (1), `catalog` (1), `hosts` (1), `notifications` (1), `quality` (1), `registration` (1), `ui` (1), `validation` (1).
Depended on by: `authentication` (5), `authorization` (3), `endpoints` (3), `catalog` (2), `connections` (2), `credentials` (2), `data` (2), `multitenancy` (2), `pipelines` (2), `quality` (2), `scheduling` (2), `schema` (2), `ui` (2), `shells` (1), `workspaces` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `catalog-usr`.
