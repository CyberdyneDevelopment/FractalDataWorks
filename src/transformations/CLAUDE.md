# The transformations domain

Transformations: field transformations (boolean, conditional, date-time, injection, numeric, text) and the transformation service abstractions.

This folder, `public/src/transformations/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/transformations/`.

## Projects

fractaldataworks:

- `Fdw.Data.Transformations`
- `Fdw.Data.Transformations.Boolean`
- `Fdw.Data.Transformations.Conditional`
- `Fdw.Data.Transformations.DateTime`
- `Fdw.Data.Transformations.Injection`
- `Fdw.Data.Transformations.Numeric`
- `Fdw.Data.Transformations.Text`
- `Fdw.Services.Transformations.Abstractions`

## Seams

This domain has no `.Abstractions` project; its projects above are its surface.

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

- `dotnet test public/tests/transformations/Fdw.Data.Transformations.Tests/Fdw.Data.Transformations.Tests.csproj -c Release`

## Dependencies

Depends on: `messagelogging` (21), `collections` (16), `data` (9), `results` (8), `calculations` (7), `services` (2), `abstractions` (1), `configuration` (1).
Depended on by: `datasets` (7), `calculations` (1), `catalog` (1), `pipelines` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `catalog-transform`.
