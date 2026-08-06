# Fdw.Commands.Data.Abstractions

The command contracts a gateway executes and a translator consumes.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (20)

| Type | Kind | Purpose |
|---|---|---|
| `ICompensationHandler` | interface | Handles compensation (rollback) when a composite command fails. |
| `ICompositeDataCommand` | interface | Represents a composite data command that orchestrates multiple child commands. A composite command… |
| `ICompositeErrorHandling` | interface | Defines error handling behavior for composite commands. |
| `IConfigurationSaveCommand` | interface | Marker interface for ConfigurationSaveCommand&lt;T&gt;. Used by the DataGateway cascade handler to… |
| `IConnectionCommand` | interface | Marker interface for connection-specific commands. These are the OUTPUT of IDataCommandTranslator… |
| `IDataCommand` | interface | Base interface for all data commands. Data commands extend IGenericCommand and can be submitted anywhere… |
| `IDataCommand<TResult>` | interface | Data command with typed result. Use this interface for commands that return a specific result type… |
| `IDataCommand<TResult, TInput>` | interface | Data command with typed input and typed result. Use this interface for commands that require input data… |
| `IDataCommandTranslator<TCommand>` | interface | Interface for data command translators. Translators convert universal IDataCommand to domain-specific… |
| `IDataCommandWithInput` | interface | Non-generic interface for commands that carry input data. Provides untyped access to Data for… |
| `IFederationStrategy` | interface | Defines a strategy for executing federated queries. |
| `IFieldValueExtractor` | interface | Extracts field values from records (handles various record types). |
| `IFilterableCommand` | interface | Interface for commands that support filtering via WHERE clause. Provides strongly-typed access to Filter… |
| `IJoinDefinition` | interface | Defines a join relationship between two containers. Used by CompoundQueryCommand to specify JOIN clauses. |
| `IJoinExecutor` | interface | Defines a join execution strategy for merging two record sets. |
| `IQualifiedNameParser` | interface | Parses qualified field names (e.g., "Customers.Id" -> "Id"). |

## Base types (12)

| Type | Kind | Purpose |
|---|---|---|
| `DataCommandBase` | class | Abstract base class for all data commands (non-generic). This base class is used by TypeCollection… |
| `DataCommandBase<TResult>` | class | Abstract base class for data commands with typed result. |
| `DataCommandBase<TResult, TInput>` | class | Abstract base class for data commands with typed input and typed result. |
| `DataCommandMessage` | class | Base class for data command messages. |
| `DataCommandMessageCollectionBase` | class | Collection base for data command messages. Generates static factory methods in DataCommandMessages class. |
| `DataCommandTranslatorBase<TCommand>` | class | Abstract base class for data command translators. This base class is used by TypeCollection source… |
| `DataCommandTranslators` | class | Hybrid collection of data command translators. Combines compile-time discovery (TypeCollection) with… |
| `DataCommands` | class | TypeCollection for all data command types. Source generator will create static properties for each… |
| `ExecutionStrategies` | class | TypeCollection for composite command execution strategies. Source generator will create static… |
| `ExecutionStrategyBase` | class | Base class for composite command execution strategies. Replaces CompositeExecutionStrategy enum to add… |

## Models and supporting types (11)

| Type | Kind | Purpose |
|---|---|---|
| `CachePolicy` | class | Typed accessor over IDataCommand.Metadata for command-level caching. The DataGateway caching decorator… |
| `CommandRequiredMessage` | class | Message indicating that a command is required. |
| `ContainerNameRequiredMessage` | class | Message indicating that a container name is required. |
| `DataCommandLog` | class | MessageLogging methods for data command operations. EventId range: 2001-2050. |
| `HttpConnectionCommand` | class | Command for executing HTTP requests that have been translated from data commands. |
| `ParallelExecutionStrategy` | class | Execute commands in parallel where possible. Requires commands to be independent (no data dependencies). |
| `SequentialContinueOnFailureExecutionStrategy` | class | Execute all commands even if some fail, then aggregate results. Useful for best-effort scenarios where… |
| `SequentialExecutionStrategy` | class | Execute commands sequentially, one after another. Output of command N becomes input to command N+1. |
| `SequentialStopOnFailureExecutionStrategy` | class | Execute commands sequentially but stop on first failure. Useful for critical pipelines where a failure… |
| `TranslationFailedMessage` | class | Message indicating that command translation failed. |
| `TranslatorNotFoundMessage` | class | Message indicating that a translator was not found. |

## Installation

```bash
dotnet add package Fdw.Commands.Data.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Commands.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
