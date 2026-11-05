# The rowsources domain

Row sources: reading rows from delimited, fixed-width, JSON, XML, HTTP and data reader sources.

This folder, `public/src/rowsources/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/rowsources/`.

## Projects

fractaldataworks:

- `Fdw.Data.RowSources`
- `Fdw.Data.RowSources.Abstractions`
- `Fdw.Data.RowSources.DataReader`
- `Fdw.Data.RowSources.Delimited`
- `Fdw.Data.RowSources.FixedWidth`
- `Fdw.Data.RowSources.FixedWidth.Abstractions`
- `Fdw.Data.RowSources.Http`
- `Fdw.Data.RowSources.Json`
- `Fdw.Data.RowSources.Xml`

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IRecordSource` (Fdw.Data.RowSources.Abstractions)
- `IRecordCursor` (Fdw.Data.RowSources.Abstractions)
- `IRecordWriter` (Fdw.Data.RowSources.Abstractions)
- `IRowSourceReader` (Fdw.Data.RowSources.Abstractions)
- `IRecordWriterType` (Fdw.Data.RowSources.Abstractions)
- `IRowMapperFactory` (Fdw.Data.RowSources.Abstractions)
- `IRowWriter` (Fdw.Data.RowSources.Abstractions)
- `RecordSourceTypeBase` (Fdw.Data.RowSources.Abstractions)
- `IRowMapper` (Fdw.Data.RowSources.Abstractions)
- `IRowSource` (Fdw.Data.RowSources.Abstractions)
- `IRecordSourceType` (Fdw.Data.RowSources.Abstractions)
- `RowMappingContextBase` (Fdw.Data.RowSources.Abstractions)
- `RecordWriterTypeBase` (Fdw.Data.RowSources.Abstractions)
- `RowMapperTypeBase` (Fdw.Data.RowSources.Abstractions)
- `IAsyncRowSourceReader` (Fdw.Data.RowSources.Abstractions)
- `IRowMapperType` (Fdw.Data.RowSources.Abstractions)
- and 3 more in the `.Abstractions` projects above

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

- `dotnet test public/tests/rowsources/Fdw.Data.RowSources.Abstractions.Tests/Fdw.Data.RowSources.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/rowsources/Fdw.Data.RowSources.RoundTrip.Tests/Fdw.Data.RowSources.RoundTrip.Tests.csproj -c Release`
- `dotnet test public/tests/rowsources/Fdw.Data.RowSources.Tests/Fdw.Data.RowSources.Tests.csproj -c Release`

## Dependencies

Depends on: `collections` (9), `messagelogging` (3), `data` (2), `results` (2), `abstractions` (1).
Depended on by: `connections` (6), `data` (3), `pipelines` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.
