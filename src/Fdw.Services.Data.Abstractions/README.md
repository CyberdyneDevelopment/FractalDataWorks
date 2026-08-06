# Fdw.Services.Data.Abstractions

The data contracts: `IDataGateway`, `IConfigurationGateway`, `DataStoreTarget`, `IDataStoreType` and the node interfaces a store tree is built from.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (23)

| Type | Kind | Purpose |
|---|---|---|
| `ICacheInvalidator` | interface | Invalidates cached DataGateway command results by tag. Writers call this after persisting changes to… |
| `ICalculationPipeline` | interface | Fluent builder for computed columns that executes a chain of calculations against tabular data. |
| `IConfigurationContainerLookup` | interface | Resolves nodes from the IDataStore tree by configuration type name or by section path (category). Used… |
| `IConfigurationGateway` | interface | Gateway variant that targets configuration data. Provides a property that exposes the IDataStore tree… |
| `IDataGateway` | interface | Service that routes data commands to the appropriate connection. Addressing (DataStore, Path, Container)… |
| `IDataGatewayTransaction` | interface | A transaction scope opened by . |
| `IDataSetProvider` | interface | Provides live runtime instances resolved by name or ID. |
| `IDataSetSchemaService` | interface | Service for managing DataSet field schemas. |
| `IDataStoreBuilder` | interface | Per-transport builder that assembles one tree (the uniform model: store → paths → containers → fields,… |
| `IDataStoreConfiguration` | interface | Marker interface for typed data store body configurations (MsSqlDataStoreConfiguration,… |
| `IDataStoreProvider` | interface | Resolves DataStores as the uniform tree. returns a fully-built, navigable (Paths → Containers, assembled… |
| `IDataStoreType` | interface | Interface for data store type definitions. DataStore types define how to configure, create, and register… |
| `IDataStoreType<TConfiguration>` | interface | Generic interface for data store types with typed configuration. |
| `IDiscoveredContainer` | interface | Connection-type-agnostic shape of a container as returned by an . Translates to a IGenericConfiguration… |
| `IDiscoveredField` | interface | A field/column on an . |
| `ISchemaDiscoverer` | interface | Connection-type-agnostic schema discovery — given a connected , list the tables/views (and optionally… |

## Base types (7)

| Type | Kind | Purpose |
|---|---|---|
| `DataGatewayMessage` | class | Base class for DataGateway-related messages. |
| `DataGatewayMessageCollectionBase` | class | Collection base for data gateway messages. Generates static factory methods in DataGatewayMessages class. |
| `DataStoreTypeBase<TConfiguration>` | class | Base class for data store type definitions. Provides configuration binding, builder supply, and provider… |
| `DataStoreTypes` | class | Registry of data store types. Pure type collection with no DI orchestration. DI registration is handled… |
| `SchemaDiscoveryTypeBase` | class | Abstract base class for schema discovery type definitions. Uses CRTP pattern consistent with other… |
| `SchemaDiscoveryTypes` | class | Registry of schema discovery types. Each type represents a store-specific schema discoverer (MsSql,… |
| `VisualizationTypeBase` | class | Base class for visualization type TypeOptions using CRTP pattern. |

## Models and supporting types (31)

| Type | Kind | Purpose |
|---|---|---|
| `BarChartVisualizationType` | class | Bar chart visualization type - displays data as vertical or horizontal bars. |
| `ColumnCalculation` | class | Defines a calculation to apply to a column in the pipeline. |
| `ColumnStatSet` | class | Statistical summary for a single column. |
| `ConnectionNotFoundMessage` | class | Message indicating that a requested connection was not found. |
| `ConnectionRetrievalFailedMessage` | class | Message indicating that connection retrieval failed. |
| `ContainerNotFoundMessage` | class | Message indicating that a requested container was not found in configuration. |
| `DataGatewayCallExtensions` | class | Extension methods that route a to or , keeping call sites concise. |
| `DataRegistrationOptions` | class | Concrete implementation of registration options for data services. |
| `DataSetFieldDefinition` | record | Describes a single field (column) within a DataSet schema. |
| `DataSetTarget` | record | Identifies a DataSet by name. Used with the target-typed overloads of to route a command through the… |
| `DataStoreDiscoveryOptions` | class | Options for DataStore schema discovery. |
| `DataStoreRequest` | record | Typed lookup request for a data store — identifies the store being requested by logical Id and/or Name.… |

## Installation

```bash
dotnet add package Fdw.Services.Data.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Data.Abstractions` · `Fdw.Data.DataContainers.Abstractions` · `Fdw.Data.DataSets.Abstractions` · `Fdw.Data.RowSources.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Services.Abstractions` · `Fdw.Services.Connections.Abstractions` · `Fdw.Services.SecretManagers.Abstractions` · `Fdw.Types.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
