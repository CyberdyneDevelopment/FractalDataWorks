# CALCULATION-PIPELINE-ARCHITECTURE.md

## Design Document: Calculation as First-Class Entity and Pipeline DataSet Boundaries

**Status (verified 1.3):**
- **IMPLEMENTED:** Calculation entity layer — `CalculationEntityTypes` MutableTypeCollection,
  `FormulaCalculationEntityType`, `WindowedCalculationEntityType`,
  `ICalculationEntityService` / `CalculationEntityService`,
  `CalculationEntityManagedConfiguration`, `DefaultCalculationEntityProvider`.
- **IMPLEMENTED (renamed):** the SQL tables exist under different names than this
  document proposed, and live in the `calc` schema of ConfigurationDb (not `cfg`).
  Authoritative names are `calc.CalculationEntity`,
  `calc.CalculationEntityInput`, `calc.FormulaCalculation`,
  `calc.WindowedCalculation`, plus `calc.CalculationStep*` (a row-level step layer
  that was added after this design). See `databases/DATABASE-MAP.md` for the
  authoritative schema list. (The `cfg.*` names used throughout the proposal body
  below are pre-migration design names; the shipped schema is `calc.*`.)
- **NOT IMPLEMENTED:** the pipeline-graph layer (`cfg.PipelineNode`,
  `cfg.PipelineEdge`, `IPipelineGraphService`, `IPipelineBoundaryValidator`)
  and the schema-only-DataSet layer (`IsSchemaOnly`, `cfg.DataSetField`,
  conformance validator) described in §2 and §6 below remain proposals — no
  code exists yet.

Treat this document as a design proposal. The §3-§5 contracts match what was
actually built (modulo table renames); §2, §6, §8.3, §8.4, §10, §12 describe
intended future work.

**Design Decision Applied:** Option B — Calculation is a separate entity sitting between DataSets in the lineage graph.

---

## 1. Architecture Overview

### 1.1 Design Principles

This document specifies the addition of three closely related features:

1. **Calculation** as a first-class configuration entity persisted in `cfg` schema, dispatched through a `MutableTypeCollection`, and linked to the lineage graph as a node type.
2. **Pipeline boundary enforcement**: every Pipeline must begin with a DataSet (the source) and end with a DataSet (the sink). Intermediate nodes may be Calculations or other DataSets. This is enforced at the service layer, not via SQL constraints.
3. **Schema-only DataSet abstractions**: a DataSet can describe its column/field schema without being bound to a physical source. These abstract DataSets serve as typed contracts for Calculation inputs and outputs — and as reusable DataSet-level "interfaces" against which Calculations can be declared.

The Calculation entity follows the existing `DataStoreTypeBase` / `MutableTypeCollection` cross-assembly pattern exactly. It does NOT add a new ServiceTypeCollection (Calculations are not runtime services; they are configured transformations). The existing `CalculationTypes` TypeCollection (single-assembly, in `Fdw.Calculations.Abstractions`) is kept for the aggregation math functions. A new `CalculationEntityTypes` MutableTypeCollection is introduced for cross-assembly dispatch of how to execute a named, configured Calculation entity.

Calculations are **reusable**: a single named Calculation entity can be referenced by multiple Pipelines as an intermediate node. The Calculation definition is stored once in `cfg.Calculation` and referenced by `cfg.PipelineNode.EntityId` from any Pipeline.

### 1.2 What Already Exists (Do Not Duplicate)

| Existing Type | Location | Purpose |
|---|---|---|
| `ICalculation<TInput, TOutput>` | `Fdw.Calculations` | Runtime execution interface (internal calculation engine) |
| `CalculationBase<TInput, TOutput>` | `Fdw.Calculations` | Base for runtime execution |
| `ICalculationType` / `CalculationTypeBase` | `Fdw.Calculations.Abstractions` | TypeOption for aggregation math (Sum, Avg, etc.) |
| `CalculationTypes` TypeCollection | `Fdw.Calculations.Abstractions` | Compile-time math function registry |
| `CalculationTransformationType` | `Fdw.Services.Transformations.Calculation` | ServiceTypeOption for transformation pipeline use |
| `CalculationDetailDto` | `Fdw.Web.Calculations.Clients.Abstractions` | API contract for existing formula-based calculations |
| `cfg.PipelineTransformCalculation` | ControlDb | Inline formula calculations within pipeline transforms |

The new design does NOT touch any of these. The new Calculation entity is a higher-level configuration concept: a named, reusable operation that consumes one or more typed inputs and produces a DataSet output.

### 1.3 Conceptual Graph

```
[DataSet A: "NFL_Players"] ---(input)---> [Calculation: "ComputePasserRating"] ---(output)---> [DataSet B: "NFL_PasserRatings"]
                                  ^
                             [Scalar: 0.5]    (multiplier constant input)

[DataSet B] ---(input)---> [Calculation: "ApplyTaxRate"] ---(output)---> [DataSet C]

[Abstract DataSet: "PlayerSchema"]  -- schema-only, no source, used as type contract
    ↓
[Calculation: "ComputePasserRating"] inputs must match "PlayerSchema" column types

Pipeline boundary:
  Source: DataSet A  (MUST be a DataSet)
  ...intermediate Calculations and DataSets...
  Sink:   DataSet C  (MUST be a DataSet)
```

### 1.4 Windowed / Column-Level Calculations

A **windowed calculation** applies a function to a single column (or field) of a DataSet, partitioned/grouped by one or more other fields. This is the equivalent of SQL window functions (`ROW_NUMBER() OVER (PARTITION BY X ORDER BY Y)`).

Windowed calculations are modeled as a `CalculationEntityType` with discriminator `"Windowed"`. They register in `CalculationEntityTypes` cross-assembly. The `WindowedCalculationEntityType` carries:
- `PartitionByFields`: `IReadOnlyList<string>` — the GROUP BY equivalent
- `OrderByFields`: `IReadOnlyList<WindowOrderField>` — the ORDER BY within the partition
- `TargetField`: `string` — the single column the function applies to
- `WindowFunction`: `string` — the math function name from the existing `CalculationTypes` TypeCollection (e.g., `"Avg"`, `"Sum"`, `"RowNumber"`)
- `OutputFieldName`: `string` — name of the result field written to the output DataSet

This keeps windowed calculations a first-class Calculation entity type, fully dispatched through TypeCollections with no `switch`/`if-else`.

---

## 2. Schema-Only DataSet Abstractions

### 2.1 Concept

A DataSet normally references a physical source (a container/connection). A **schema-only DataSet** describes column names and their scalar value types without any physical binding. It acts as:

- A typed contract for a Calculation's inputs (e.g., "this Calculation accepts a DataSet shaped like `PlayerSchema`")
- A reusable abstract type that multiple physical DataSets can declare conformance to
- A target output spec where the schema is declared before the physical storage is assigned

Schema-only DataSets are persisted in `cfg.DataSet` with `IsSchemaOnly = 1` (new boolean column). They have no `ConnectionName`, no `ContainerPath`. They have one or more rows in `cfg.DataSetField` (new child table).

### 2.2 cfg.DataSetField — New Table

```sql
CREATE TABLE [cfg].[DataSetField]
(
    [RowId]           UNIQUEIDENTIFIER NOT NULL DEFAULT (newsequentialid()),
    [Id]              UNIQUEIDENTIFIER NOT NULL,
    [DataSetId]       UNIQUEIDENTIFIER NOT NULL,   -- FK to cfg.DataSet.Id
    [FieldName]       NVARCHAR(200)    NOT NULL,
    [ScalarTypeName]  NVARCHAR(50)     NOT NULL,   -- maps to ScalarValueTypes TypeCollection key
    [IsNullable]      BIT              NOT NULL DEFAULT (1),
    [Ordinal]         INT              NOT NULL DEFAULT (0),
    [Description]     NVARCHAR(MAX)    NULL,
    [IsCurrent]       BIT              NOT NULL DEFAULT (1),
    [IsDeleted]       BIT              NOT NULL DEFAULT (0),
    [CreateDate]      DATETIMEOFFSET   NOT NULL DEFAULT (sysdatetimeoffset()),
    [CreateBy]        NVARCHAR(128)    NOT NULL DEFAULT (suser_sname()),
    [ModifyDate]      DATETIMEOFFSET   NOT NULL DEFAULT (sysdatetimeoffset()),
    [ModifyBy]        NVARCHAR(128)    NOT NULL DEFAULT (suser_sname()),
    CONSTRAINT [PK_DataSetField] PRIMARY KEY CLUSTERED ([RowId])
);
```

`ScalarTypeName` maps to the `ScalarValueTypes` TypeCollection (Int32, Decimal, Int64, String, CharArray). This is the same TypeCollection used by `CalculationScalarValue`.

### 2.3 DataSet Schema Conformance

A physical DataSet can declare that it conforms to an abstract DataSet's schema by referencing the abstract DataSet's `Id` in a new column `cfg.DataSet.AbstractDataSetId` (nullable GUID). Conformance is checked at the service layer — `IDataSetSchemaValidator.ValidateConformance(physicalDataSetId, abstractDataSetId)` — not via SQL. The validator reads both DataSets' field lists and checks name + type compatibility using `StringComparison.OrdinalIgnoreCase`.

### 2.4 Using Schema-Only DataSets in Calculation Inputs

When a `CalculationInput` references a DataSet by name, the execution layer resolves it:
1. If the DataSet is `IsSchemaOnly = true`, the execution is blocked (`GenericResult.Failure`) with `CalculationEntityResultCodes.AbstractDataSetCannotBeUsedAsInput`.
2. The DataSet wizard allows the user to select a physical DataSet that conforms to the abstract schema, substituting the abstract reference at Pipeline configuration time.

---

## 3. Entity Contracts

### 3.1 ICalculationEntity

Lives in `Fdw.Services.Calculations.Abstractions`.

```csharp
public interface ICalculationEntity
{
    Guid Id { get; }
    string Name { get; }
    string? Description { get; }
    string CalculationEntityType { get; }   // discriminator → CalculationEntityTypes key
    IReadOnlyList<CalculationInput> Inputs { get; }
    CalculationOutputSpec Output { get; }
    bool IsEnabled { get; }
}
```

### 3.2 CalculationInput — Typed Union

```csharp
public sealed class CalculationInput
{
    public ICalculationInputKind Kind { get; init; }

    // DataSet kind: the logical DataSet name
    public string? DataSetName { get; init; }

    // Container kind: connection + container path (no DataSet layer)
    public string? ConnectionName { get; init; }
    public string? ContainerPath { get; init; }

    // Scalar kind: typed constant
    public CalculationScalarValue? ScalarValue { get; init; }

    // Alias used to reference this input inside a formula/expression
    public string InputAlias { get; init; } = string.Empty;

    public static CalculationInput FromDataSet(string dataSetName, string alias)
        => new() { Kind = CalculationInputKinds.ByName("DataSet"),
                   DataSetName = dataSetName, InputAlias = alias };

    public static CalculationInput FromContainer(string connectionName,
        string containerPath, string alias)
        => new() { Kind = CalculationInputKinds.ByName("Container"),
                   ConnectionName = connectionName,
                   ContainerPath = containerPath, InputAlias = alias };

    public static CalculationInput FromScalar(CalculationScalarValue scalar, string alias)
        => new() { Kind = CalculationInputKinds.ByName("Scalar"),
                   ScalarValue = scalar, InputAlias = alias };
}
```

`CalculationInputKinds` is a compile-time `[TypeCollection]` with three registered kinds: `DataSet`, `Container`, `Scalar`. No `switch`/`if-else` on kind anywhere.

### 3.3 CalculationScalarValue — Typed Scalar

```csharp
public sealed class CalculationScalarValue
{
    public IScalarValueType ValueType { get; init; }
    // Always stored as string in DB. Parsed by ValueType at execution time.
    public string SerializedValue { get; init; } = string.Empty;
}
```

Registered `ScalarValueTypeBase` subtypes:

| Name | CLR type materialized | DB storage |
|---|---|---|
| `Int32` | `int` | `NVARCHAR(50)` → `int.Parse` |
| `Decimal` | `decimal` | `NVARCHAR(50)` → `decimal.Parse(InvariantCulture)` |
| `Int64` | `long` | `NVARCHAR(50)` → `long.Parse` |
| `String` | `string` | `NVARCHAR(MAX)` stored directly |
| `CharArray` | `char[]` | `NVARCHAR(MAX)` → `string.ToCharArray()` |

`Span<T>` is excluded from all contracts. Callers who need `Span<char>` call `.AsSpan()` locally.

### 3.4 CalculationOutputSpec

The output of any Calculation is always a DataSet.

```csharp
public sealed class CalculationOutputSpec
{
    public string OutputDataSetName { get; init; } = string.Empty;
    public string ResultFieldName { get; init; } = string.Empty;
    public string ResultDataTypeName { get; init; } = "Decimal";
}
```

### 3.5 WindowedCalculationSpec (Extension for Windowed Type)

```csharp
public sealed class WindowedCalculationSpec
{
    // Fields to partition by (SQL PARTITION BY equivalent)
    public IReadOnlyList<string> PartitionByFields { get; init; } = [];

    // Fields to order within the partition (SQL ORDER BY within window)
    public IReadOnlyList<WindowOrderField> OrderByFields { get; init; } = [];

    // The single column the window function operates on
    public string TargetField { get; init; } = string.Empty;

    // Function name — must match a key in CalculationTypes TypeCollection
    public string WindowFunction { get; init; } = string.Empty;

    // The output field name written to the result DataSet
    public string OutputFieldName { get; init; } = string.Empty;
}

public sealed class WindowOrderField
{
    public string FieldName { get; init; } = string.Empty;
    public bool Descending { get; init; }
}
```

`WindowedCalculationEntityType` stores a `WindowedCalculationSpec` in `cfg.WindowedCalculation` (type-specific child table).

---

## 4. CalculationEntityTypes MutableTypeCollection

### 4.1 Cross-Assembly Pattern

Follows `DataStoreTypes` exactly.

**Interface** (`Fdw.Services.Calculations.Abstractions`):

```csharp
public interface ICalculationEntityType : ITypeOption<Guid, CalculationEntityTypeBase>
{
    Type ConfigurationType { get; }
    void Configure(IServiceCollection services, IConfiguration configuration);
    IGenericResult ValidateConfiguration(ICalculationEntityConfiguration configuration);
    Task<IGenericResult<string>> Execute(
        ICalculationEntity entity,
        IReadOnlyList<ResolvedCalculationInput> inputs,
        ICalculationContext context,
        CancellationToken cancellationToken);
}
```

**Base Class** (`Fdw.Services.Calculations.Abstractions`):

```csharp
public abstract class CalculationEntityTypeBase :
    TypeOptionBase<Guid, CalculationEntityTypeBase>,
    ICalculationEntityType
{
    protected CalculationEntityTypeBase(string name, string displayName,
        string description, string? category = null)
        : base(GenerateId(name), name, ...) { }

    // GenerateId: MD5 deterministic, same as DataStoreTypeBase pattern
}
```

**Collection** (`Fdw.Services.Calculations`):

```csharp
[MutableTypeCollection(typeof(CalculationEntityTypeBase), typeof(ICalculationEntityType),
    typeof(CalculationEntityTypes))]
public abstract partial class CalculationEntityTypes
    : TypeCollectionBase<CalculationEntityTypeBase, ICalculationEntityType>
{
    public static string ServiceCategory => "Calculation";
}
```

**First-party TypeOptions** registered in `Fdw.Services.Calculations`:

| Key | Description |
|---|---|
| `"Formula"` | C#/SQL formula evaluator |
| `"Windowed"` | Column-level windowed function (Avg/Sum/RowNumber partitioned by fields) |

Cross-assembly packages (Python executor, R executor, etc.) register their own TypeOptions via module initializer.

### 4.2 Reusability

Because `CalculationEntityTypes` is a `MutableTypeCollection` and `cfg.Calculation` has a durable logical `Id`, a single Calculation entity can be referenced from multiple `cfg.PipelineNode` rows across different Pipelines. The Calculation is defined once; Pipelines reference it. This is the cross-pipeline reuse model.

---

## 5. CalculationBase (CRTP for Entity Execution)

```csharp
public abstract class CalculationEntityBase<TConfiguration> : CalculationEntityTypeBase
    where TConfiguration : class, ICalculationEntityConfiguration
{
    public sealed override Type ConfigurationType => typeof(TConfiguration);

    public sealed override void Configure(IServiceCollection services, IConfiguration configuration)
        => services.Configure<List<TConfiguration>>(
               configuration.GetSection($"Calculations:{Name}"));

    public override IGenericResult ValidateConfiguration(ICalculationEntityConfiguration config)
    {
        if (config is not TConfiguration typed)
            return GenericResult.Failure(
                CalculationEntityResultCodes.ByName("ConfigurationTypeMismatch"));
        return ValidateTypedConfiguration(typed);
    }

    public sealed override Task<IGenericResult<string>> Execute(
        ICalculationEntity entity,
        IReadOnlyList<ResolvedCalculationInput> inputs,
        ICalculationContext context,
        CancellationToken cancellationToken)
        => ExecuteTyped(entity, inputs, context, cancellationToken);

    protected abstract IGenericResult ValidateTypedConfiguration(TConfiguration config);
    protected abstract Task<IGenericResult<string>> ExecuteTyped(
        ICalculationEntity entity,
        IReadOnlyList<ResolvedCalculationInput> inputs,
        ICalculationContext context,
        CancellationToken cancellationToken);
}
```

---

## 6. Pipeline Entity Changes and Boundary Enforcement

### 6.1 New Tables

**`cfg.PipelineNode`**:

```sql
CREATE TABLE [cfg].[PipelineNode]
(
    [RowId]       UNIQUEIDENTIFIER NOT NULL DEFAULT (newsequentialid()),
    [Id]          UNIQUEIDENTIFIER NOT NULL,
    [PipelineId]  UNIQUEIDENTIFIER NOT NULL,
    [NodeType]    NVARCHAR(50)     NOT NULL,  -- 'DataSet' | 'Calculation'
    [EntityId]    UNIQUEIDENTIFIER NOT NULL,
    [EntityName]  NVARCHAR(200)    NOT NULL,
    [Ordinal]     INT              NOT NULL DEFAULT (0),
    [IsCurrent]   BIT              NOT NULL DEFAULT (1),
    [IsDeleted]   BIT              NOT NULL DEFAULT (0),
    [CreateDate]  DATETIMEOFFSET   NOT NULL DEFAULT (sysdatetimeoffset()),
    [CreateBy]    NVARCHAR(128)    NOT NULL DEFAULT (suser_sname()),
    [ModifyDate]  DATETIMEOFFSET   NOT NULL DEFAULT (sysdatetimeoffset()),
    [ModifyBy]    NVARCHAR(128)    NOT NULL DEFAULT (suser_sname()),
    CONSTRAINT [PK_PipelineNode] PRIMARY KEY CLUSTERED ([RowId])
);
```

**`cfg.PipelineEdge`**:

```sql
CREATE TABLE [cfg].[PipelineEdge]
(
    [RowId]          UNIQUEIDENTIFIER NOT NULL DEFAULT (newsequentialid()),
    [Id]             UNIQUEIDENTIFIER NOT NULL,
    [PipelineId]     UNIQUEIDENTIFIER NOT NULL,
    [SourceNodeId]   UNIQUEIDENTIFIER NOT NULL,
    [TargetNodeId]   UNIQUEIDENTIFIER NOT NULL,
    [EdgeLabel]      NVARCHAR(200)    NULL,
    [IsCurrent]      BIT              NOT NULL DEFAULT (1),
    [IsDeleted]      BIT              NOT NULL DEFAULT (0),
    [CreateDate]     DATETIMEOFFSET   NOT NULL DEFAULT (sysdatetimeoffset()),
    [CreateBy]       NVARCHAR(128)    NOT NULL DEFAULT (suser_sname()),
    [ModifyDate]     DATETIMEOFFSET   NOT NULL DEFAULT (sysdatetimeoffset()),
    [ModifyBy]       NVARCHAR(128)    NOT NULL DEFAULT (suser_sname()),
    CONSTRAINT [PK_PipelineEdge] PRIMARY KEY CLUSTERED ([RowId])
);
```

No hard SQL foreign key constraints — follows version-on-write pattern.

### 6.2 cfg.Pipeline New Columns

```sql
-- Added to cfg.Pipeline (nullable for migration)
[SourceDataSetId]    UNIQUEIDENTIFIER NULL,   -- FK to cfg.DataSet.Id
[SinkDataSetId]      UNIQUEIDENTIFIER NULL,   -- FK to cfg.DataSet.Id
```

### 6.3 Boundary Validation

```csharp
public interface IPipelineBoundaryValidator
{
    IGenericResult ValidateBoundaries(IPipelineGraphDefinition graph);
}
```

Implementation uses LINQ on nodes/edges. Root nodes (in-degree=0) must all be `NodeType == "DataSet"`. Leaf nodes (out-degree=0) must all be `NodeType == "DataSet"`. No Calculation→Calculation direct edges. All comparisons use `StringComparison.OrdinalIgnoreCase`. No `switch`/`if-else`.

---

## 7. Database Schema: cfg.Calculation

**`cfg.Calculation`** — header table:

```sql
CREATE TABLE [cfg].[Calculation]
(
    [RowId]                  UNIQUEIDENTIFIER NOT NULL DEFAULT (newsequentialid()),
    [Id]                     UNIQUEIDENTIFIER NOT NULL,
    [Name]                   NVARCHAR(200)    NOT NULL,
    [CalculationEntityType]  NVARCHAR(100)    NOT NULL,
    [Description]            NVARCHAR(MAX)    NULL,
    [OutputDataSetName]      NVARCHAR(200)    NOT NULL,
    [ResultFieldName]        NVARCHAR(200)    NOT NULL,
    [ResultDataTypeName]     NVARCHAR(50)     NOT NULL DEFAULT ('Decimal'),
    [IsEnabled]              BIT              NOT NULL DEFAULT (1),
    [IsCurrent]              BIT              NOT NULL DEFAULT (1),
    [IsDeleted]              BIT              NOT NULL DEFAULT (0),
    [CreateDate]             DATETIMEOFFSET   NOT NULL DEFAULT (sysdatetimeoffset()),
    [CreateBy]               NVARCHAR(128)    NOT NULL DEFAULT (suser_sname()),
    [ModifyDate]             DATETIMEOFFSET   NOT NULL DEFAULT (sysdatetimeoffset()),
    [ModifyBy]               NVARCHAR(128)    NOT NULL DEFAULT (suser_sname()),
    CONSTRAINT [PK_Calculation] PRIMARY KEY CLUSTERED ([RowId])
);
```

**`cfg.CalculationInput`** — child inputs (one row per input slot):

```sql
CREATE TABLE [cfg].[CalculationInput]
(
    [RowId]              UNIQUEIDENTIFIER NOT NULL DEFAULT (newsequentialid()),
    [Id]                 UNIQUEIDENTIFIER NOT NULL,
    [CalculationId]      UNIQUEIDENTIFIER NOT NULL,
    [InputAlias]         NVARCHAR(100)    NOT NULL,
    [InputKind]          NVARCHAR(50)     NOT NULL,   -- 'DataSet' | 'Container' | 'Scalar'
    [DataSetName]        NVARCHAR(200)    NULL,
    [ConnectionName]     NVARCHAR(200)    NULL,
    [ContainerPath]      NVARCHAR(500)    NULL,
    [ScalarValueTypeName] NVARCHAR(50)   NULL,
    [ScalarValue]        NVARCHAR(MAX)   NULL,
    [Ordinal]            INT             NOT NULL DEFAULT (0),
    [IsCurrent]          BIT             NOT NULL DEFAULT (1),
    [IsDeleted]          BIT             NOT NULL DEFAULT (0),
    [CreateDate]         DATETIMEOFFSET  NOT NULL DEFAULT (sysdatetimeoffset()),
    [CreateBy]           NVARCHAR(128)   NOT NULL DEFAULT (suser_sname()),
    [ModifyDate]         DATETIMEOFFSET  NOT NULL DEFAULT (sysdatetimeoffset()),
    [ModifyBy]           NVARCHAR(128)   NOT NULL DEFAULT (suser_sname()),
    CONSTRAINT [PK_CalculationInput] PRIMARY KEY CLUSTERED ([RowId])
);
```

**`cfg.FormulaCalculation`** — type-specific child for Formula type:

```sql
CREATE TABLE [cfg].[FormulaCalculation]
(
    [Id]              UNIQUEIDENTIFIER NOT NULL,
    [CalculationId]   UNIQUEIDENTIFIER NOT NULL,
    [FormulaLanguage] NVARCHAR(50)     NOT NULL DEFAULT ('CSharp'),
    [FormulaBody]     NVARCHAR(MAX)    NOT NULL,
    [TimeoutSeconds]  INT              NOT NULL DEFAULT (30),
    [IsCurrent]       BIT             NOT NULL DEFAULT (1),
    [IsDeleted]       BIT             NOT NULL DEFAULT (0),
    [CreateDate]      DATETIMEOFFSET  NOT NULL DEFAULT (sysdatetimeoffset()),
    [CreateBy]        NVARCHAR(128)   NOT NULL DEFAULT (suser_sname()),
    [ModifyDate]      DATETIMEOFFSET  NOT NULL DEFAULT (sysdatetimeoffset()),
    [ModifyBy]        NVARCHAR(128)   NOT NULL DEFAULT (suser_sname()),
    CONSTRAINT [PK_FormulaCalculation] PRIMARY KEY CLUSTERED ([Id])
);
```

**`cfg.WindowedCalculation`** — type-specific child for Windowed type:

```sql
CREATE TABLE [cfg].[WindowedCalculation]
(
    [Id]                    UNIQUEIDENTIFIER NOT NULL,
    [CalculationId]         UNIQUEIDENTIFIER NOT NULL,
    [TargetField]           NVARCHAR(200)    NOT NULL,
    [WindowFunction]        NVARCHAR(100)    NOT NULL,  -- maps to CalculationTypes key
    [OutputFieldName]       NVARCHAR(200)    NOT NULL,
    [PartitionByFields]     NVARCHAR(MAX)    NOT NULL,  -- JSON array of field names
    [OrderByFields]         NVARCHAR(MAX)    NOT NULL,  -- JSON array of {Field, Descending}
    [IsCurrent]             BIT             NOT NULL DEFAULT (1),
    [IsDeleted]             BIT             NOT NULL DEFAULT (0),
    [CreateDate]            DATETIMEOFFSET  NOT NULL DEFAULT (sysdatetimeoffset()),
    [CreateBy]              NVARCHAR(128)   NOT NULL DEFAULT (suser_sname()),
    [ModifyDate]            DATETIMEOFFSET  NOT NULL DEFAULT (sysdatetimeoffset()),
    [ModifyBy]              NVARCHAR(128)   NOT NULL DEFAULT (suser_sname()),
    CONSTRAINT [PK_WindowedCalculation] PRIMARY KEY CLUSTERED ([Id])
);
```

`PartitionByFields` and `OrderByFields` stored as JSON arrays (`NVARCHAR(MAX)`) — deserialized at service layer, not parsed by SQL. This avoids a many-to-many join table for ordered field lists.

---

## 8. Service Layer

### 8.1 New Projects

| Package | Purpose | Target |
|---|---|---|
| `Fdw.Services.Calculations.Abstractions` | All interfaces, base classes, input/output types, TypeCollections | `netstandard2.0` |
| `Fdw.Services.Calculations` | `CalculationEntityTypes`, `FormulaCalculationEntityType`, `WindowedCalculationEntityType`, `DefaultCalculationEntityProvider`, `CalculationEntityService` | `netstandard2.0;net10.0` |

### 8.2 ICalculationEntityService

```csharp
public interface ICalculationEntityService
{
    Task<IGenericResult<ICalculationEntity>> GetCalculation(
        string name, CancellationToken cancellationToken = default);

    Task<IGenericResult<ICalculationEntity>> GetCalculationById(
        Guid id, CancellationToken cancellationToken = default);

    Task<IGenericResult<IReadOnlyList<ICalculationEntity>>> ListCalculations(
        CancellationToken cancellationToken = default);

    Task<IGenericResult> ValidateCalculation(
        ICalculationEntity entity, CancellationToken cancellationToken = default);

    Task<IGenericResult<string>> ExecuteCalculation(
        string calculationName,
        ICalculationContext context,
        CancellationToken cancellationToken = default);
}
```

All data access via `IDataGateway.Execute()` with `QueryCommand<T>`. No raw ADO.NET.

### 8.3 IDataSetSchemaService (new)

```csharp
public interface IDataSetSchemaService
{
    Task<IGenericResult<IReadOnlyList<DataSetFieldDefinition>>> GetSchema(
        Guid dataSetId, CancellationToken cancellationToken = default);

    Task<IGenericResult> ValidateConformance(
        Guid physicalDataSetId, Guid abstractDataSetId,
        CancellationToken cancellationToken = default);

    Task<IGenericResult> SaveSchema(
        Guid dataSetId,
        IReadOnlyList<DataSetFieldDefinition> fields,
        CancellationToken cancellationToken = default);
}
```

### 8.4 IPipelineGraphService

```csharp
public interface IPipelineGraphService
{
    Task<IGenericResult<IPipelineGraphDefinition>> GetGraph(
        Guid pipelineId, CancellationToken cancellationToken = default);

    Task<IGenericResult> SaveGraph(
        Guid pipelineId,
        IPipelineGraphDefinition graph,
        CancellationToken cancellationToken = default);

    Task<IGenericResult> ValidateGraph(
        Guid pipelineId, CancellationToken cancellationToken = default);
}
```

---

## 9. Lineage Graph Changes

**New node type** (id=3): `CalculationNodeType`
**New edge types**: `InputsFrom` (id=5) — Calculation←DataSet; `ProducesDataSet` (id=6) — Calculation→DataSet

---

## 10. Wizard Step Implications

Three wizards are updated or created:

### 10.1 DataSet Wizard — Extended

The existing `DataSetWizardSteps` gains a new step:

```
Wizards/DataSet/
  ConfigureSchemaStep.cs              -- [TypeOption(DataSetWizardSteps, "ConfigureSchema", RestrictToCurrentCompilation = true)]
  ConfigureSchemaComponent.razor      -- Add/remove/edit field definitions (name + ScalarValueType)
```

This step is shown for both physical DataSets (optional schema annotation) and schema-only DataSets (required). The `DataSetWizardContext` gains:
- `bool IsSchemaOnly` — toggles physical source steps off
- `IList<DataSetFieldDefinition> Fields` — the schema being configured

### 10.2 Calculation Wizard — New

```
Wizards/Calculation/
  CalculationWizardStepBase.cs        -- extends WizardStepBase, adds string? CalculationEntityType
  CalculationWizardSteps.cs           -- [MutableTypeCollection]
  CalculationWizardContext.cs         -- state: Name, SelectedType, Inputs, OutputSpec, WindowedSpec?
  SelectCalculationTypeStep.cs        -- id=1, shared, RestrictToCurrentCompilation = true
  SelectCalculationTypeComponent.razor
  ConfigureInputsStep.cs              -- id=2, shared, add DataSet/Container/Scalar inputs
  ConfigureInputsComponent.razor
  ConfigureOutputStep.cs              -- id=3, shared, output DataSet name + result field
  ConfigureOutputComponent.razor
  ReviewCalculationStep.cs            -- id=100, shared
  ReviewCalculationComponent.razor
  SaveCalculationStep.cs              -- id=101, shared
  SaveCalculationComponent.razor
  Formula/
    FormulaConfigureStep.cs           -- [TypeOption(CalculationWizardSteps, "FormulaConfig")], id=10
    FormulaConfigureComponent.razor   -- formula body editor, language selector
  Windowed/
    WindowedConfigureStep.cs          -- [TypeOption(CalculationWizardSteps, "WindowedConfig")], id=10
    WindowedConfigureComponent.razor  -- target field, window function, partition/order fields
```

### 10.3 Pipeline Wizard — Extended

The existing `PipelineWizardSteps` / `FdwPipelineBuilder.razor` canvas gains:
- Calculation nodes in the node palette
- `NodeType` discriminator on `PipelineTaskDescriptor`
- Boundary validation error display in the canvas

---

## 11. EventId Allocation

| Range | Domain | Log Class |
|---|---|---|
| **4140-4179** | Calculation Entity Service | `CalculationEntityLog` |
| **4180-4199** | Calculation Entity Endpoints | `CalculationEntityEndpointLog` |
| **7020-7049** | Pipeline Graph Service | `PipelineGraphLog` |
| **7050-7069** | Pipeline Boundary Validator | `PipelineBoundaryLog` |

---

## 12. Migration Path

**Phase 1** (additive, non-breaking):
1. Add `cfg.Calculation`, `cfg.CalculationInput`, `cfg.FormulaCalculation`, `cfg.WindowedCalculation`
2. Add `cfg.PipelineNode`, `cfg.PipelineEdge`
3. Add `cfg.DataSetField` + `IsSchemaOnly` column on `cfg.DataSet`
4. Add `SourceDataSetId`, `SinkDataSetId` columns to `cfg.Pipeline` (nullable)
5. Add `CalculationNodeType` (id=3), `InputsFromEdgeType` (id=5), `ProducesDataSetEdgeType` (id=6) to seed data

**Phase 2** (data migration):
- Populate `SourceDataSetId`/`SinkDataSetId` from existing string-based `SourceDataSet`/`DestinationDataSet`

**Phase 3** (cleanup):
- Deprecate string-based pipeline source/destination columns

---

## 13. New Package Layout Summary

| Package | Action |
|---|---|
| `Fdw.Services.Calculations.Abstractions` | CREATE — interfaces, base classes, input/output types |
| `Fdw.Services.Calculations` | CREATE — `CalculationEntityTypes`, Formula + Windowed implementations |
| `Fdw.Services.Pipelines.Abstractions` | EXTEND — add IPipelineBoundaryValidator, IPipelineGraphService, IPipelineGraphDefinition |
| `Fdw.Services.Pipelines` | EXTEND — PipelineBoundaryValidator, PipelineGraphService |
| `Fdw.Services.Data.Abstractions` | EXTEND — add IDataSetSchemaService, DataSetFieldDefinition |
| `Fdw.Data.Lineage` | EXTEND — CalculationNodeType, InputsFromEdgeType, ProducesDataSetEdgeType |
| `Fdw.UI.Components.Blazor` | EXTEND — Wizards/Calculation/ (new), DataSet wizard schema step, Pipeline wizard canvas extension |
| `databases/ControlDb` | EXTEND — 6 new tables, 3 new columns, seed data |

---

## Appendix A: Design Decisions

**Why not ServiceTypeCollection for CalculationEntityTypes?** Calculations are stateless per-execution — they don't hold resources between calls. MutableTypeCollection (same as DataStoreTypes) provides cross-assembly dispatch without three-phase DI overhead.

**Why CalculationInputKinds as a TypeCollection instead of an enum?** Future kinds (StreamedInput, CachedInput) should not require modifying a central enum. TypeCollection enables behavior attachment (e.g., `Resolve(input, context)`) without `switch`.

**Why are PartitionByFields and OrderByFields stored as JSON?** An ordered list of field names with direction flags has variable length and ordering matters. A separate child table adds a join and complicates ordering. JSON in NVARCHAR(MAX) is deserialized at the service layer where `System.Text.Json` handles it cleanly. The service layer never lets raw SQL see the JSON structure.

**Why keep cfg.PipelineTransformCalculation unchanged?** It represents row-level inline formula evaluation within a transform step — a different abstraction level than the new entity-level Calculation that operates between full DataSets. Merging them would break existing pipelines.
