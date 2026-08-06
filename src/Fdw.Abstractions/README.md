# Fdw.Abstractions

The root contracts every other FDW package builds on — `IGenericResult`, `IGenericMessage`, `IGenericCommand`, `IGenericService` and the service/configuration marker interfaces.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (15)

| Type | Kind | Purpose |
|---|---|---|
| `IGenericCommand` | interface | Base interface for all commands in the Fdw framework. |
| `IGenericConfiguration` | interface | Base interface for all configuration objects in the Fdw framework. Provides common properties for all… |
| `IGenericConfiguration<T>` | interface | Generic configuration interface for type-safe configuration. |
| `IGenericMessage` | interface | Interface for framework messages that provide structured information about operations. |
| `IGenericMessage<TSeverity>` | interface | Generic interface for framework messages with strongly typed severity. |
| `IGenericResult` | interface | Represents a result that can be either success or failure. |
| `IGenericResult<out T>` | interface | Represents a result that can be either success or failure with a value. |
| `IGenericService` | interface | Base interface for all services in the Fdw framework. |
| `IMemoryStore<TKey, TValue>` | interface | Defines a basic generic memory store for managing state in-memory. |
| `IPagedRequest` | interface | Standard pagination request parameters. |
| `IPagedResponse<out T>` | interface | Standard paginated response with items and pagination metadata. |
| `IServiceDispatchHost` | interface | Implemented by a root-header configuration whose nested typed body carries the discriminator that… |
| `IServiceFactory` | interface | Generic factory interface for creating Service instances |
| `IServiceFactory<out TService>` | interface | Generic factory interface for creating Service instances of a specific type. Covariant in TService to… |
| `IServiceFactory<out TService, in TConfiguration>` | interface | Generic factory interface for creating Service instances with specific service and configuration types.… |

## Models and supporting types (2)

| Type | Kind | Purpose |
|---|---|---|
| `ConventionOverrideAttribute` | class | Override convention analyzer thresholds for a specific method or class. Values of -1 indicate "use the… |
| `PagedResponse<T>` | class | Default implementation of . |

## Installation

```bash
dotnet add package Fdw.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Results.Abstractions`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
