# Fdw.Services.EtlMappers.Pooled

Zero-allocation ETL row mapper using dictionary pooling.

## Overview

This package provides a high-performance ETL row mapper implementation that eliminates per-row dictionary allocations by using an object pool. The `PooledDictionaryMapper` pre-computes field ordinals during initialization and reuses dictionaries via a thread-safe `DictionaryPool`, achieving zero estimated allocations per row after warmup. This is the default mapper type registered with `EtlRowMapperProvider`.

## Installation

```xml
<PackageReference Include="Fdw.Services.EtlMappers.Pooled" Version="x.y.z" />
```

## Key Types

- `PooledDictionaryMapper` - IEtlRowMapper implementation with zero allocations per row
- `PooledDictionaryMapperType` - TypeOption registered as "Pooled" in EtlRowMapperTypes
- `PooledDictionaryMapperFactory` - Factory for creating PooledDictionaryMapper instances
- `PooledDictionaryMapperConfiguration` - Configuration with pool size and dictionary size limits
- `DictionaryPool` - Thread-safe ConcurrentBag-based dictionary pool
- `RowMappingContext` - Pre-computed field ordinals and names cached per read operation

## Usage

```csharp
// Automatic registration via EtlRowMapperTypes (default mapper type)
EtlRowMapperTypes.Configure(services, configuration, loggerFactory);
EtlRowMapperTypes.Register(services, loggerFactory);

// Configuration in appsettings.json
// "EtlMappers:Pooled": { "MaxPoolSize": 1000, "MaxDictionarySize": 100 }
```

## Related Packages

- [Fdw.Services.EtlMappers.Abstractions](../Fdw.Services.EtlMappers.Abstractions/) - Interfaces and base classes
- [Fdw.Services.EtlMappers](../Fdw.Services.EtlMappers/) - Provider and registration infrastructure
- [Fdw.Services.EtlMappers.Dynamic](../Fdw.Services.EtlMappers.Dynamic/) - Alternative mapper using compiled expressions
