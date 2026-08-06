# FractalDataWorks (FDW)

FractalDataWorks is a .NET framework for building data-centric services — APIs, ETL pipelines,
schedulers, and UIs — on a small set of consistent mechanisms instead of ad-hoc conventions per
project. This is the **first public release** (`1.0.0-rc.1`).

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](LICENSE)

## What FDW is

- **A fail-loud result type.** `IGenericResult` / `IGenericResult<T>` is the universal return type
  for anything that can fail. Methods never return `null` and never throw for an expected failure —
  they return a result carrying a `ResultCode`, a message chain, and root-cause tracking
  (`IsSuccess` / `Value` / `Status` / `CodeChain` / `RootCause` / `IsEmpty`).
- **Extensible enums instead of `enum` + `switch`.** TypeCollections (`[TypeCollection]` /
  `[TypeOption]`) are compile-time-discovered, cross-assembly-extensible families looked up by
  `ById` / `ByName` / `All`. A failed lookup returns a `NotFound` sentinel, never `null`.
  `ServiceTypeCollections` extend the same idea to DI: a three-phase `Configure → Register →
  Initialize` lifecycle is the *only* service-registration mechanism in FDW — there are no ad-hoc
  `AddXxx()` extension methods scattered through application code.
- **One data-access path.** `IDataGateway` is the only way to reach a database, REST API, file
  store, or any other backend. Addressing (which store, path, container) travels alongside the
  command, never inside it, so the same `IDataGateway.Execute<T>()` call shape works against
  MsSql, PostgreSql, Sqlite, Http, FileSystem, and RoslynWorkspace connections without the caller
  ever branching on connection type.
- **Database-backed configuration, not scattered `appsettings.json` files.** `[ManagedConfiguration]`
  POCOs generate their own DDL, validation, and UI form metadata; a parent-header + typed-body
  pattern lets polymorphic configuration (e.g. one `Connection` header with a different body per
  connection kind) stay strongly typed. One provider mechanism
  (`DefaultConfigurationProvider<TConfig,TCommand>`) composes reads and cascades writes with
  version-on-write and tag-based cache invalidation.
- **Source generators and analyzers that enforce the rest.** Configuration DDL/validators, POCO
  row↔object mappers (`[GenerateMapper]`), MessageLogging, and TypeCollection/ServiceTypeCollection
  scaffolding are all generated, not hand-written. A `FDW001`–`FDW045` Roslyn analyzer family —
  44 rules, with no `FDW024` — enforces the structured-failure, no-raw-logging and
  TypeCollection/ServiceType conventions at build time. The generators ship with the packages;
  the analyzer assemblies are deliberately not packable, so they bind this repository rather than
  its consumers.
- **A render-agnostic UI model.** `UI.Abstractions` contracts plus an `IUIRenderer` seam let the
  same page/component model back a Spectre.Console renderer or a headless Blazor
  Context/Provider/ProviderLog triple, keeping presentation logic out of the domain.

New to FDW? **[docs/GETTING-STARTED.md](docs/GETTING-STARTED.md)** walks through installing the
packages, the three-phase bootstrap, and running your first `IDataGateway` query end to end.

See **[docs/FDW-CAPABILITIES.md](docs/FDW-CAPABILITIES.md)** for the full, source-verified feature
inventory (service domains, connection backends, hosting middleware, etc.) and
**[CHANGELOG.md](CHANGELOG.md)** for everything shipped in this release.

## Why it exists

- **Failures should be data, not control flow.** Exceptions are for the unexpected; anticipated
  failure (not found, validation failed, conflict) is a value the caller inspects, not a `catch`
  block the caller writes.
- **A plugin point should not require editing a `switch` statement.** Enums and if/else chains
  don't compose across assemblies; TypeCollections do — a downstream package can add a new
  `[TypeOption]` without touching the collection that defines it.
- **Data access should not leak the backend.** Code that queries "the Customers container in the
  Sales DataStore" shouldn't need to know or care whether that resolves to SQL Server, a REST
  endpoint, or a CSV file.
- **Configuration should be validated, versioned, and queryable — not a pile of JSON files.**
  Configuration that drives runtime behavior (connections, pipelines, schedules) lives in a real
  data store with the same discipline as any other data.

## Install

FDW ships as a set of `Fdw.*` NuGet packages from nuget.org, versioned together at `1.0.0-rc.1`.
Add only the packages your project needs — most consumers start with the foundation layer plus
whichever service/data packages they use:

```bash
# Foundation: results, messaging, collections
dotnet add package Fdw.Abstractions
dotnet add package Fdw.Results
dotnet add package Fdw.Collections

# Data access
dotnet add package Fdw.Services.Data.Abstractions
dotnet add package Fdw.Commands.Data
dotnet add package Fdw.Commands.Data.Extensions

# Service infrastructure
dotnet add package Fdw.Services.Abstractions
dotnet add package Fdw.Services
```

Because this is a `-rc` prerelease, pass `--prerelease` to `dotnet add package` (or pin the
explicit version) until a stable `1.0.0` lands.

## Quickstart: run a query through `IDataGateway`

`IDataGateway` is the only data-access path in FDW. You never open a raw connection or build a SQL
string — you address a container by `DataStore` + `Path` + `Container` name, build a command with
the fluent `Query`/`DataQuery` builder, and execute it. This example queries a `SessionStateRecord`
row by user and key (the real pattern used by `Fdw.Services.SessionState`):

```csharp
using Fdw.Commands.Data;
using Fdw.Services.Data.Abstractions;

public class SessionStateReader(IDataGateway gateway)
{
    public async Task<IGenericResult<SessionStateRecord?>> GetRecord(
        Guid userId, string key, CancellationToken cancellationToken = default)
    {
        // Build the command: address (DataStore, Path, Container) + typed WHERE clause.
        var call = Query.From<SessionStateRecord>("ConfigurationDb", "settings", "SessionState")
            .Where(r => r.UserId).Equal(userId)
            .Where(r => r.StateKey).Equal(key)
            .Build();

        // Execute — no connection string, no SQL, no branching on backend type.
        var result = await gateway.Execute<IEnumerable<SessionStateRecord>>(call, cancellationToken)
            .ConfigureAwait(false);

        // IGenericResult never throws for an expected failure — check IsSuccess, not try/catch.
        if (!result.IsSuccess)
            return GenericResult<SessionStateRecord?>.Failure(result.RootCause);

        return GenericResult<SessionStateRecord?>.Success(result.Value?.FirstOrDefault());
    }
}
```

A few things worth noting:

- `Query.From<T>(dataStore, path, container)` (alias for `DataQuery.From<T>`) starts a
  `QueryCommandBuilder<T>`; `.Where(x => x.Prop).Equal(value)` gives type-safe filters, and plain
  `.Where("PropName", value)` also works.
- `.Build()` returns a `DataGatewayCall` — the command and its `DataStoreTarget` bundled together —
  which `gateway.Execute<T>(call, cancellationToken)` runs directly.
- The result is always an `IGenericResult<T>`. Check `IsSuccess` before touching `Value`; on
  failure, inspect `CodeChain` / `RootCause` for structured diagnostics instead of catching an
  exception.
- The same gateway call shape works whether `"ConfigurationDb"` resolves to MsSql, PostgreSql,
  Sqlite, or another registered connection — the caller never knows which.

## Learn more

- **[docs/GETTING-STARTED.md](docs/GETTING-STARTED.md)** — install the packages, bootstrap an app
  with the three-phase service lifecycle, and run your first `IDataGateway` query.
- **FDW Developer's Guide** — the narrative onboarding guide (mental model, `IGenericResult`,
  MessageLogging, TypeCollections, three-phase DI, DataGateway, configuration, hosting, extension
  recipes). Start with its `README.md` and `00-index.md`.
- **[docs/FDW-CAPABILITIES.md](docs/FDW-CAPABILITIES.md)** — the complete, source-verified feature
  catalog with file-level anchors into this repository.
- **[CHANGELOG.md](CHANGELOG.md)** — what shipped in `1.0.0-rc.1`.

## License

Licensed under the Apache-2.0 License. See [LICENSE](LICENSE) for details.
