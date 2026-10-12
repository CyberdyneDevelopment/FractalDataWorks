# Fdw.Services.EtlMappers.Dynamic

ETL row mapper using compiled expression trees for efficient field access.

## Overview

This package provides an ETL row mapper implementation that uses compiled expression trees to generate optimized field accessors at initialization time. The `DynamicStructMapper` compiles `IDataReader.IsDBNull`/`GetValue` calls into delegates via `System.Linq.Expressions`, reducing per-row overhead. Unlike the pooled mapper, it allocates one dictionary per row (estimated allocations per row = 1) but avoids virtual dispatch costs on field access.

## Installation

```xml
<PackageReference Include="Fdw.Services.EtlMappers.Dynamic" Version="x.y.z" />
```

## Key Types

- `DynamicStructMapper` - IEtlRowMapper implementation using compiled expression trees
- `DynamicStructMapperType` - TypeOption registered as "Dynamic" in EtlRowMapperTypes
- `DynamicStructMapperFactory` - Factory for creating DynamicStructMapper instances
- `DynamicStructMapperConfiguration` - Configuration for the dynamic mapper
- `CompiledFieldAccessor` - Compiled expression tree accessor for efficient field reads

## Usage

```csharp
// Automatic registration via EtlRowMapperTypes
EtlRowMapperTypes.Configure(services, configuration, loggerFactory);
EtlRowMapperTypes.Register(services, loggerFactory);

// Use "Dynamic" mapper type in configuration
// "EtlMappers:Dynamic": { ... }
```

## Related Packages

- [Fdw.Services.EtlMappers.Abstractions](../Fdw.Services.EtlMappers.Abstractions/) - Interfaces and base classes
- [Fdw.Services.EtlMappers](../Fdw.Services.EtlMappers/) - Provider and registration infrastructure
- [Fdw.Services.EtlMappers.Pooled](../Fdw.Services.EtlMappers.Pooled/) - Alternative mapper using dictionary pooling
