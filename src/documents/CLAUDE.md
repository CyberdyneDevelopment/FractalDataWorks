# The documents domain

Documents: the document abstractions.

This folder, `public/src/documents/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/documents/`.

## Projects

fractaldataworks:

- `Fdw.Documents.Abstractions`: What a connection that stores whole files needs: the document path and scope, the document command family, the capabilities a connection declares, and the result codes and logging of anticipated failures. Abstractions only: a consumer supplies the connection implementations.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IDocumentCommand` (Fdw.Documents.Abstractions)
- `DocumentCommandBase` (Fdw.Documents.Abstractions)
- `DocumentCapabilityBase` (Fdw.Documents.Abstractions)
- `DocumentResultCodeBase` (Fdw.Documents.Abstractions)
- `IDocumentCapability` (Fdw.Documents.Abstractions)

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

- `dotnet test public/tests/documents/Fdw.Documents.Abstractions.Tests/Fdw.Documents.Abstractions.Tests.csproj -c Release`

## Dependencies

Depends on: `messagelogging` (3), `collections` (2), `results` (2), `abstractions` (1).
Depended on by: `connections` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.
