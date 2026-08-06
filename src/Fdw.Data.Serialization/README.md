# Fdw.Data.Serialization

Serialization for data nodes and store trees.

This package declares 1 interface(s).

## Contracts (1)

| Type | Kind | Purpose |
|---|---|---|
| `IContainerProperties` | interface | Marker interface for container-specific properties used in serialization. |

## Types (9)

| Type | Kind | Purpose |
|---|---|---|
| `ContainerConverter` | class | JSON converter for IStorageContainer. Serializes containers with their key properties and schema.… |
| `ContainerSchemaConverter` | class | JSON converter for IContainerSchema. |
| `EmptyContainerProperties` | class | Empty container properties for containers with no additional properties. |
| `EndpointContainerProperties` | class | Properties specific to HTTP endpoint containers. |
| `FdwJsonOptions` | class | Provides pre-configured JsonSerializerOptions for Fdw types. |
| `FieldConverter` | class | JSON converter for IField. |
| `FieldTypeConverter` | class | JSON converter for IFieldType. Serializes as { "TypeName": "String", "ClrType": "System.String" } |
| `SerializedContainer` | class | Non-generic serialized container for containers without additional properties. |
| `SerializedContainer<TProperties>` | class | A simplified container implementation used for JSON serialization/deserialization. This class captures… |

## Installation

```bash
dotnet add package Fdw.Data.Serialization --prerelease
```

## Dependencies

`Fdw.Data.Abstractions` · `Fdw.Data.Builders` · `Fdw.Data.Files`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
