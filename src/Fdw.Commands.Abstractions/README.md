# Fdw.Commands.Abstractions

The root command contracts: `IGenericCommand`, the runtime command instance every gateway executes.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (5)

| Type | Kind | Purpose |
|---|---|---|
| `IGenericCommandCategory` | interface | Interface for command categories that define command behavior and requirements. |
| `IGenericCommandTranslator` | interface | Interface for command translators that convert between different command formats. |
| `IGenericCommandType` | interface | Interface for command type definitions with metadata and capabilities. |
| `ITranslationContext` | interface | Provides context information for command translation. |
| `ITranslatorType` | interface | Interface for command translator type definitions. |

## Base types (7)

| Type | Kind | Purpose |
|---|---|---|
| `CommandCategories` | class | Collection of command categories. |
| `CommandCategoryBase` | class | Base class for command categories. |
| `CommandMessage` | class | Base class for all command-related messages. |
| `CommandTypeBase` | class | Base class for command type definitions. |
| `CommandTypes` | class | Collection of command types. |
| `TranslatorTypeBase` | class | Base class for translator type definitions. |
| `TranslatorTypes` | class | Collection of translator types. |

## Models and supporting types (10)

| Type | Kind | Purpose |
|---|---|---|
| `CommandCostEstimate` | class | Represents the estimated cost of executing a command. |
| `CommandExecution` | record | Represents a command execution instance with tracking metadata. Separates command TYPE (static… |
| `CommandLogger` | class | Static logger class for command operations. |
| `CommandNullMessage` | class | Message for when a command is null. |
| `OrderByClause` | class | Represents an ORDER BY clause extracted from a query. |
| `TranslationCapabilities` | class | Defines the capabilities of a command translator. |
| `TranslationFailedMessage` | class | Message for when command translation fails. |
| `TranslatorNotFoundMessage` | class | Message for when a translator cannot be found for the specified formats. |
| `UnsupportedCommandMessage` | class | Message for when a command type is not supported. |
| `WhereClause` | class | Represents a single WHERE condition extracted from a query. |

## Installation

```bash
dotnet add package Fdw.Commands.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Orchestration.Abstractions` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
