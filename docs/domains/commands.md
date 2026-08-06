# Domain: Commands

## Purpose

The Commands domain provides a **uniform interface** for operating on any specific implementation of a given type through a single set of commands. The core idea: one command set, multiple translators. A "navigate to definition" command works the same whether the underlying implementation is Roslyn (C#), a Java analyzer, a C++ tool, or a Rust language server. A "query data" command works the same whether the target is SQL Server, PostgreSQL, or an HTTP API.

Commands define **what** to do. Translators handle **how** to do it for each implementation.

## Projects

| Project | Role |
|---------|------|
| `Commands.Abstractions` | Core framework: `CommandTypes`, `CommandCategories`, `TranslatorTypes`, `IGenericCommandTranslator`, clauses |
| `Commands.Data` | Data commands: `QueryCommand<T>`, `InsertCommand<T>`, `UpdateCommand<T>`, `DeleteCommand` |
| `Commands.Data.Abstractions` | Data command interfaces, `IDataGateway` |
| `Commands.Data.Extensions` | Fluent builder extensions for data command construction |
| `Commands.Development.Abstractions` | Development commands: 9 categories (Analysis, CodeSearch, Compilation, Formatting, Generation, Navigation, Project, Refactoring, Workspace) that work across any language toolchain |

## Key Types

### Core Framework (`Commands.Abstractions`)

- **`CommandTypes`** -- TypeCollection of all command types. Source-generated, supports routing and discovery.
- **`CommandCategories`** -- TypeCollection of command categories. Each category defines execution characteristics:
  - `RequiresTransaction` -- whether commands need transactional execution
  - `SupportsStreaming` -- whether results can be streamed
  - `IsCacheable` -- whether results can be cached
  - `IsMutation` -- whether the command modifies state
  - `ExecutionPriority` -- ordering for batched execution
- **`IGenericCommandTranslator`** -- The bridge between uniform commands and specific implementations. Each implementation type (MsSql, PostgreSql, Roslyn, etc.) provides its own translator that converts the universal command into implementation-specific operations.
- **`TranslatorTypes`** -- TypeCollection of available translators. New language/tool support means adding a new translator, not new commands.
- **Clauses** -- `WhereClause`, `OrderByClause` for composable filtering and sorting.

### Data Commands (`Commands.Data`)

One set of data commands, translated to SQL Server, PostgreSQL, HTTP, etc.:

- **`QueryCommand<T>`** -- Read operations with fluent filtering, sorting, paging.
- **`InsertCommand<T>`** / **`UpdateCommand<T>`** / **`DeleteCommand`** -- Write operations.
- **`IDataGateway`** -- Central execution point. Resolves the appropriate translator and connection, executes the command.

### Development Commands (`Commands.Development.Abstractions`)

One set of development tool commands, translated to Roslyn, and in future to Java analyzers, C++ tools, Rust language servers, etc.:

- **`DevelopmentCommands`** / **`DevelopmentCommandCategories`** -- TypeCollections for development operations.
- **9 Command Categories**: Analysis, CodeSearch, Compilation, Formatting, Generation, Navigation, Project, Refactoring, Workspace.
- **`IDevelopmentCommandTranslator`** -- Each language toolchain implements this to handle the universal command set.
- Today: Roslyn translator. Future: Java, C++, Rust, etc. -- all through the same commands.

## Patterns

### Uniform Commands, Implementation-Specific Translators

This is the central pattern. Commands are defined once per domain. Translators map them to specific implementations.

```
                          +--> MsSql Translator --> SQL Server
QueryCommand<T> ----+--> PostgreSql Translator --> PostgreSQL
                          +--> Http Translator --> REST API

                                      +--> Roslyn Translator --> C# workspace
NavigationCommand --+--> (future) Java Translator --> Java workspace
                                      +--> (future) Rust Translator --> Rust workspace
```

Adding support for a new database or a new language means adding a translator. The command interface and all consumers remain unchanged.

### Data Command Builder (Fluent API)

Entry points live in `Fdw.Commands.Data.Extensions`
(`Query`, `Insert`, `Update`, `Delete`, `DataQuery`, `Find`):

```csharp
var command = Query.From<ConnectionDto>("ConfigurationDb", "conn", "Connection")
    .Where(c => c.Name).Equal(connectionName)
    .OrderBy(c => c.Name)
    .Skip(0).Take(50)
    .Build();

var result = await dataGateway.Execute(command, cancellationToken);
```

`Query.From<T>(dataStoreName, pathName, containerName)` returns a
`QueryCommandBuilder<T>` (see
`public/src/Fdw.Commands.Data.Extensions/Query.cs`).

### DataGateway-Only Data Access

All data operations MUST go through `IDataGateway`. The gateway resolves the correct connection, selects the appropriate translator, and executes.

**Never:**
- `new SqlConnection()` outside connection implementations
- `SqlCommand`, `ExecuteReaderAsync`, `ExecuteScalarAsync` outside connection projects
- Raw connection strings in application code
- Pass raw `"schema.table"` strings above the connection layer

### Container Abstraction

Containers abstract physical storage location. A container maps to a schema+table in SQL, a path in HTTP, or a collection in document stores. Commands reference containers, not raw table names.

## Rules

1. **DataGateway is the ONLY data access path.** No exceptions for data commands.
2. **Commands are immutable after construction.** Use the builder to compose, then execute.
3. **Implementation type is invisible to command consumers.** The translator handles dialect/tool differences. Consumers issue uniform commands.
4. **No `switch`/`if-else` on implementation type.** Translator selection is handled by `TranslatorTypes` and `SupportedTranslators`.
5. **New implementations = new translators, not new commands.** Adding PostgreSQL or Java support means implementing a translator, not changing the command interfaces.
6. **New command categories** must define all five characteristics (transaction, streaming, caching, mutation, priority).

## Related Domains

- **Services.Connections** -- Provides the connections that DataGateway routes data commands through
- **Services.Data** -- Service-layer wrapper around DataGateway for DataStores/DataSets
- **Configuration** -- ManagedConfiguration uses data commands for config writes
- **Schema** -- Schema discovery uses data commands to query database metadata
- **Roslyn** -- Roslyn.Commands provides the translator for development commands against C# workspaces
- **Core** -- `IGenericResult`, `IGenericCommand`, expressions
