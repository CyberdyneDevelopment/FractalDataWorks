# Fdw.Services.Calculations.Abstractions

Contracts for calculation entities and their configuration.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (16)

| Type | Kind | Purpose |
|---|---|---|
| `ICalculationCacheService` | interface | Service for caching calculation results. |
| `ICalculationCatalogProvider` | interface | Surfaces the unified calculation catalog — the union of every registered option's entries — through one… |
| `ICalculationEntity` | interface | Represents a configured calculation entity — a named, typed computation with declared inputs and an… |
| `ICalculationEntityProvider` | interface | In-memory registry of loaded calculation entities. Supports lookup by name or id and supports dynamic… |
| `ICalculationEntityResultCode` | interface | Marker interface for calculation entity result codes. |
| `ICalculationEntityService` | interface | Service for managing and executing calculation entities. |
| `ICalculationEntityType` | interface | Defines a calculation entity type that can be registered in the CalculationEntityTypes collection. |
| `ICalculationInputKind` | interface | Represents a calculation input kind — describes the source of data fed into a calculation. |
| `ICalculationInputResolver` | interface | Resolves calculation inputs by fetching data from their declared sources. |
| `ICalculationOperation` | interface | Represents a composable calculation operation that can be used as a step in a calculation pipeline. Each… |
| `ICalculationSourceType` | interface | A calculation catalog origin (e.g. "Default", "Configuration") that owns its own resolution — each… |
| `ICalculationStepExecutor` | interface | Executes a calculation entity's ordered steps over the registered calculation operations. |
| `ICalculationTraceRecorder` | interface | Collects the per-step derivation of a single calculation execution as it happens. |
| `ICalculationTypedConfiguration` | interface | Marker interface for a calculation entity's polymorphic typed body (Formula / Windowed). |
| `IOperationParameterKind` | interface | Represents a kind of parameter that a calculation operation accepts. Describes the shape of the… |
| `IScalarValueType` | interface | Represents a scalar value type supported as a calculation input. |

## Base types (13)

| Type | Kind | Purpose |
|---|---|---|
| `CalculationEntityBase<TConfiguration>` | class | Generic CRTP base for calculation entity types that use a typed configuration. Seals the dispatch… |
| `CalculationEntityResultCodeBase` | class | Base class for calculation entity result codes. |
| `CalculationEntityResultCodes` | class | TypeCollection for calculation entity result codes. EventId range: 4140-4179 (Calculations domain) |
| `CalculationEntityTypeBase` | class | Abstract base class for all calculation entity types. Uses MD5-based deterministic Guid for stable… |
| `CalculationInputKindBase` | class | Base class for calculation input kinds (CRTP pattern). |
| `CalculationInputKinds` | class | TypeCollection for calculation input kinds. Source generator discovers all types decorated with… |
| `CalculationOperationBase` | class | Abstract base class for all calculation operations (CRTP pattern). Provides common metadata (category,… |
| `CalculationSourceTypeBase` | class | Base class for a option. Each concrete source owns its own resolution strategy — see… |
| `CalculationSourceTypes` | class | Extensible registry of calculation catalog origins. Built-in options are "Default" (codified, ships with… |
| `OperationParameterKindBase` | class | Base class for operation parameter kinds (CRTP pattern). Each subclass represents a distinct shape of… |

## Models and supporting types (34)

| Type | Kind | Purpose |
|---|---|---|
| `AbstractDataSetCannotBeUsedAsInputCode` | class | An abstract DataSet cannot be used as a calculation input. |
| `CachedCalculationResult` | class | Represents a cached calculation result. |
| `CalculationCacheEntryOptions` | class | Options for a specific calculation cache entry. |
| `CalculationCacheOptions` | class | Configuration options for calculation result caching. |
| `CalculationCacheStatistics` | class | Statistics about calculation cache performance. |
| `CalculationCatalogItem` | class | A single entry in the unified calculation catalog, tagged with the that produced it (provenance, not a… |
| `CalculationInput` | class | Declares a named input to a calculation entity, specifying its kind and source. |
| `CalculationNotFoundCode` | class | The requested calculation entity was not found. |
| `CalculationOperandTrace` | class | Records where one operand of a calculation step got its value. |
| `CalculationOutputSpec` | class | Describes where a calculation result should be written and what field/type it produces. |
| `CalculationScalarValue` | class | A typed scalar value used as a literal calculation input. |
| `CalculationSourceContext` | record | Execution context handed to each option so it can resolve its own catalog entries. |

## Installation

```bash
dotnet add package Fdw.Services.Calculations.Abstractions --prerelease
```

## Dependencies

`Fdw.Calculations.Abstractions` · `Fdw.Collections` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Services.Data.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
