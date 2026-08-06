# Fdw.Data.DataSets.Abstractions

The DataSet contracts and the configuration classes its rows are read into.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (9)

| Type | Kind | Purpose |
|---|---|---|
| `IDataField` | interface | Represents metadata for a field within a dataset. Provides schema information including type,… |
| `IDataQuery` | interface | Represents a query against a dataset with LINQ expression support. This interface captures query… |
| `IDataQuery<TSource>` | interface | Generic version of IDataQuery with strong typing for the source dataset. |
| `IDataSetCatalog` | interface | Represents a catalog of datasets that provides type-safe lookup and enumeration capabilities. This… |
| `IDataSetCategory` | interface | Marker interface for DataSet category type options. Implemented by and any compile-time… |
| `IDataSetExecutionContext` | interface | Thin execution context handed to a strategy's so the strategy can run a command against the dataset's… |
| `IDataSetSourceMapperType` | interface | Represents a data set source mapper type definition. Mapper types extract raw records from structured… |
| `IDataSetType` | interface | Represents a dataset definition with schema information and query capabilities. Datasets define the… |
| `IQueryExpression` | interface | Represents a structured query expression that can be analyzed and translated to different backend query… |

## Base types (5)

| Type | Kind | Purpose |
|---|---|---|
| `DataSetCategories` | class | Registry of DataSet category type options (Model C — Hybrid). |
| `DataSetCategoryBase` | class | Abstract base class for DataSet category type options. Derive from this class to declare compile-time… |
| `DataSetSourceMapperTypeBase` | class | Base class for data set source mapper type definitions. Mappers extract raw records from structured… |
| `DataSetTypeBase` | class | Abstract base class for dataset type definitions following the Fdw.Collections pattern. Provides common… |
| `DataSetTypes` | class | Registry of dataset types. Pure type collection with no DI orchestration. DI registration is handled by… |

## Models and supporting types (21)

| Type | Kind | Purpose |
|---|---|---|
| `CachingConfiguration` | class | Caching configuration for dataset operations. |
| `CalculatedDataField` | class | Represents a calculated field that computes its value from other fields in a DataRow. Calculated fields… |
| `DataField` | class | Represents metadata for a field in a data set. Defines the structure and characteristics of data that… |
| `DataFieldConfiguration` | class | Configuration class for dataset field definitions. |
| `DataFieldConfigurationValidator` | class | Validator for DataFieldConfiguration instances. |
| `DataFieldToFieldDefinitionAdapter` | class | Adapter that converts a to . |
| `DataQueryBuilder<TSource>` | class | Concrete implementation of IDataQuery that builds LINQ expression trees. This class captures method… |
| `DataSetAggregateDefinition` | class | Defines a single aggregate measure within a DataSet, including the group-by keys and the aggregate… |
| `DataSetCategoryConfiguration` | class | Configuration record for a DataSet category, backed by data.DataSetCategory. Runtime-defined categories… |
| `DataSetFilterConditionConfiguration` | class | Configuration for a filter condition stored with a DataSet definition. These filters are applied… |
| `DataSetKeyFieldConfiguration` | class | Configuration for a key field within a DataSet. Loaded from data.DataSetKeyField as a child of… |
| `DataSetSourceMapperContext` | class | Context bag passed to source mappers during record extraction. Contains the raw payload, record selector… |

## Installation

```bash
dotnet add package Fdw.Data.DataSets.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Commands.Abstractions` · `Fdw.Commands.Data.Abstractions` · `Fdw.Configuration.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.Data.DataContainers.Abstractions` · `Fdw.Results` · `Fdw.Types.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
