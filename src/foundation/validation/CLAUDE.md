# The validation domain

Validation: the validation abstractions and the FastEndpoints validation integration.

This folder, `public/src/foundation/validation/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/foundation/validation/`.

## Projects

fractaldataworks:

- `Fdw.Validation`
- `Fdw.Validation.Abstractions`
- `Fdw.Validation.FastEndpoints`

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IEntityValidator` (Fdw.Validation.Abstractions)

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

- `dotnet test public/tests/foundation/validation/Fdw.Validation.Tests/Fdw.Validation.Tests.csproj -c Release`

## Dependencies

Depends on: `abstractions` (1), `collections` (1), `results` (1).
Depended on by: `pipelines` (4), `connections` (3), `authentication` (2), `authorization` (2), `calculations` (2), `quality` (2), `scheduling` (2), `telemetry` (2), `ui` (2), `catalog` (1), `configuration` (1), `credentials` (1), `data` (1), `endpoints` (1), `hosts` (1), `multitenancy` (1), `notifications` (1), `schema` (1), `secretmanagers` (1), `users` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.
