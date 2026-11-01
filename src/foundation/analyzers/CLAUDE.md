# The analyzers domain

The FDW Roslyn analyzers, code fixes and convention checks that every project builds with, and the shared source generators.

This folder, `public/src/foundation/analyzers/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/foundation/analyzers/`.

## Projects

fractaldataworks:

- `Fdw.Analyzers` (not a package)
- `Fdw.CodeFixes` (not a package)
- `Fdw.Conventions.Analyzers` (not a package)
- `Fdw.Conventions.CodeFixes` (not a package)
- `Fdw.Conventions.FileSplitter`: CLI tool that splits multi-type C# files into one file per type (fixes FDW005)
- `Fdw.ServiceTypes.Analyzers` (not a package)
- `Fdw.ServiceTypes.CodeFixes` (not a package)
- `Fdw.SourceGenerators`

## Seams

This domain has no `.Abstractions` project; its projects above are its surface.

## Rules that matter here

- Every project except tests and the analyzers themselves builds with these analyzers (Directory.Build.props). A change here changes every package's build.
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

- `dotnet test public/tests/foundation/analyzers/Fdw.Analyzers.Tests/Fdw.Analyzers.Tests.csproj -c Release`
- `dotnet test public/tests/foundation/analyzers/Fdw.Conventions.Analyzers.Tests/Fdw.Conventions.Analyzers.Tests.csproj -c Release`
- `dotnet test public/tests/foundation/analyzers/Fdw.ServiceTypes.Analyzers.Tests/Fdw.ServiceTypes.Analyzers.Tests.csproj -c Release`

## Dependencies

Depends on: `roslyn` (2).
Depended on by: `configuration` (2).
(The number is how many references cross between the two domains' src projects.)

A foundation project that references a domain outside the foundation is a layering defect or a misfiled project:

- `Fdw.SourceGenerators` -> `Fdw.CodeBuilder.Abstractions` (roslyn)
- `Fdw.SourceGenerators` -> `Fdw.CodeBuilder.CSharp` (roslyn)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `analyzers`, `conventions`.
