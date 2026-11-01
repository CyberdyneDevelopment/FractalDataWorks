# The roslyn domain

Roslyn tooling: Roslyn commands, the code builder and analysis, the SQL workspace and workspace management.

This folder, `public/src/tooling/roslyn/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/tooling/roslyn/`.

## Projects

fractaldataworks:

- `Fdw.CodeBuilder.Abstractions`
- `Fdw.CodeBuilder.Analysis`
- `Fdw.CodeBuilder.Analysis.CSharp`
- `Fdw.CodeBuilder.CSharp`
- `Fdw.Roslyn.Commands`
- `Fdw.Roslyn.Commands.Abstractions`
- `Fdw.Sql.Workspace`: SQL Server Data Tools (.sqlproj) workspace abstraction: load, edit, snapshot, apply-to-disk. Parallel to Fdw.Workspace.Roslyn.
- `Fdw.Workspace.Management`: Workspace management for Roslyn-based development tools. Provides multi-workspace support, session persistence, and workspace lifecycle management.
- `Fdw.Workspace.Roslyn`

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IRoslynCommandResult` (Fdw.Roslyn.Commands.Abstractions)
- `RoslynCommandTranslatorBase` (Fdw.Roslyn.Commands.Abstractions)
- `RoslynCommandBase` (Fdw.Roslyn.Commands.Abstractions)
- `RoslynResultCodeBase` (Fdw.Roslyn.Commands.Abstractions)
- `IRoslynCommandTranslator` (Fdw.Roslyn.Commands.Abstractions)
- `IRoslynCommand` (Fdw.Roslyn.Commands.Abstractions)
- `IMethodBuilder` (Fdw.CodeBuilder.Abstractions)
- `IClassBuilder` (Fdw.CodeBuilder.Abstractions)
- `IPropertyBuilder` (Fdw.CodeBuilder.Abstractions)
- `ITranslatorRegistry` (Fdw.Roslyn.Commands.Abstractions)
- `IFieldBuilder` (Fdw.CodeBuilder.Abstractions)
- `IConstructorBuilder` (Fdw.CodeBuilder.Abstractions)
- `ISyntaxNode` (Fdw.CodeBuilder.Abstractions)
- `ICodeParser` (Fdw.CodeBuilder.Abstractions)
- `IInputInfoModel` (Fdw.CodeBuilder.Abstractions)
- `IInterfaceBuilder` (Fdw.CodeBuilder.Abstractions)
- `IEnumBuilder` (Fdw.CodeBuilder.Abstractions)
- `RoslynCommandCategoryBase` (Fdw.Roslyn.Commands.Abstractions)
- `IChangeLedger` (Fdw.Roslyn.Commands.Abstractions)
- `ICodeBuilder` (Fdw.CodeBuilder.Abstractions)
- and 19 more in the `.Abstractions` projects above

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

- `dotnet test public/tests/tooling/roslyn/Fdw.CodeBuilder.Abstractions.Tests/Fdw.CodeBuilder.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/tooling/roslyn/Fdw.CodeBuilder.Analysis.CSharp.Tests/Fdw.CodeBuilder.Analysis.CSharp.Tests.csproj -c Release`
- `dotnet test public/tests/tooling/roslyn/Fdw.CodeBuilder.Analysis.Tests/Fdw.CodeBuilder.Analysis.Tests.csproj -c Release`
- `dotnet test public/tests/tooling/roslyn/Fdw.CodeBuilder.CSharp.Tests/Fdw.CodeBuilder.CSharp.Tests.csproj -c Release`
- `dotnet test public/tests/tooling/roslyn/Fdw.Roslyn.Commands.Refactoring.Tests/Fdw.Roslyn.Commands.Refactoring.Tests.csproj -c Release`
- `dotnet test public/tests/tooling/roslyn/Fdw.Roslyn.Commands.Tests/Fdw.Roslyn.Commands.Tests.csproj -c Release`
- `dotnet test public/tests/tooling/roslyn/Fdw.Sql.Workspace.Tests/Fdw.Sql.Workspace.Tests.csproj -c Release`
- `dotnet test public/tests/tooling/roslyn/Fdw.Workspace.Management.Tests/Fdw.Workspace.Management.Tests.csproj -c Release`

## Dependencies

Depends on: `messagelogging` (11), `results` (9), `collections` (8), `abstractions` (1), `commands` (1).
Depended on by: `connections` (3), `analyzers` (2), `ui` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `roslyn-mcp`.
