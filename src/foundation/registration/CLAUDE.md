# The registration domain

Service registration: the three-phase ServiceTypeCollection registration and its source generators.

This folder, `public/src/foundation/registration/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/foundation/registration/`.

## Projects

fractaldataworks:

- `Fdw.Registration.SourceGenerators`
- `Fdw.Services.Registration`
- `Fdw.Services.Registration.SourceGenerators`

## Seams

This domain has no `.Abstractions` project; its projects above are its surface.

## Rules that matter here

- Registration is three-phase: AddFrameworkServiceTypes, the `.AddXxx()` chain, InitializeFrameworkServiceTypes.
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

- `dotnet test public/tests/foundation/registration/Fdw.Registration.SourceGenerators.Tests/Fdw.Registration.SourceGenerators.Tests.csproj -c Release`
- `dotnet test public/tests/foundation/registration/Fdw.Services.Registration.SourceGenerators.Tests/Fdw.Services.Registration.SourceGenerators.Tests.csproj -c Release`
- `dotnet test public/tests/foundation/registration/Fdw.Services.Registration.Tests/Fdw.Services.Registration.Tests.csproj -c Release`

## Dependencies

Depends on: `abstractions` (1), `authentication` (1), `services` (1).
Depended on by: `shells` (3), `authentication` (2), `pipelines` (2), `authorization` (1), `catalog` (1), `connections` (1), `data` (1), `devsession` (1), `endpoints` (1), `multitenancy` (1), `quality` (1), `scheduling` (1), `schema` (1), `ui` (1), `users` (1).
(The number is how many references cross between the two domains' src projects.)

A foundation project that references a domain outside the foundation is a layering defect or a misfiled project:

- `Fdw.Services.Registration` -> `Fdw.Services.Authentication.Abstractions` (authentication)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `registration`.
