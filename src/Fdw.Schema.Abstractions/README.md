# Fdw.Schema.Abstractions

Schema contracts — the discovered shape of a backend, independent of which backend it is.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (8)

| Type | Kind | Purpose |
|---|---|---|
| `IColumnDefinition` | interface | Represents a physical database column with SQL type information. Use for database schema operations, DDL… |
| `IDataLayout` | interface | Interface for data layout types. |
| `IFieldDefinition` | interface | Represents a logical field with .NET type information and data mapping. Use for data transformation, ETL… |
| `IIndexDefinition<TProperty>` | interface | Defines an index on a schema. |
| `IKeyDefinition<TProperty>` | interface | Defines a key (primary or unique) on a schema. |
| `IPropertyDefinition` | interface | Base interface for all property/field/column definitions in a schema. |
| `IPropertyRole` | interface | Interface for property roles. |
| `ISchemaDefinition<TProperty>` | interface | Generic schema definition interface for describing data structures. |

## Base types (5)

| Type | Kind | Purpose |
|---|---|---|
| `DataLayoutBase` | class | Base class for data layouts using CRTP pattern. |
| `DataLayouts` | class | MutableTypeCollection for data layouts. Source generator will create static properties for each layout… |
| `PropertyRoleBase` | class | Base class for property roles using CRTP pattern. |
| `PropertyRoles` | class | MutableTypeCollection for property roles. Source generator will create static properties for each role… |
| `SchemaDefinitionBase<TProperty>` | class | Abstract base class for schema definitions. |

## Models and supporting types (16)

| Type | Kind | Purpose |
|---|---|---|
| `AttributeRole` | class | Attribute role - descriptive data that is not indexed. |
| `ColumnDefinition` | class | Default implementation of . |
| `DocumentLayout` | class | Document layout - single complex object. |
| `FieldDefinition` | class | Default implementation of . |
| `GraphLayout` | class | Graph layout - nodes and edges with relationships. |
| `HierarchicalLayout` | class | Hierarchical layout - nested parent-child structure. |
| `IndexDefinition<TProperty>` | class | Concrete implementation of . |
| `KeyDefinition<TProperty>` | class | Concrete implementation of . |
| `KeyValueLayout` | class | Key-value layout - simple key-value pairs. |
| `LookupRole` | class | Lookup role - indexed for search but not part of key. |
| `MeasureRole` | class | Measure role - aggregatable numeric data. |
| `NaturalKeyRole` | class | Natural key role - business identifier that is human-meaningful. |

## Installation

```bash
dotnet add package Fdw.Schema.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Types.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
