# Template: Creating a New Data Mapper

**Purpose**: Maps values between two type systems (e.g., SQL `int` ↔ JSON `integer`, used for federated queries)

**Status**: Not yet implemented - this template describes future Phase 6 work

---

## File Location

```
src/Fdw.Data.{SourceTypeSystem}/
└── Mappers/
    └── {Source}To{Target}Mapper.cs
```

**OR** (for generic/cross-cutting mappers):
```
src/Fdw.Data.Abstractions/
└── Mappers/
    ├── IDataMapper.cs
    ├── DataMapperBase.cs
    ├── DefaultDataMapper.cs
    └── DataMappers.cs
```

**Examples:**
- `src/Fdw.Data.JsonSchema/Mappers/JsonIntegerToMsSqlBigIntMapper.cs`
- `src/Fdw.Data.Abstractions/Mappers/DefaultDataMapper.cs`

---

## Template Code - Explicit Mapper

```csharp
using System;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Data.JsonSchema;

namespace Fdw.Data.JsonSchema.Mappers;

/// <summary>
/// Explicit mapper from {SourceTypeSystem} {SourceType} to {TargetTypeSystem} {TargetType}.
/// Provides optimized direct mapping without CLR bridge.
/// </summary>
[TypeOption(typeof(DataMappers), "{MapperName}")]
public sealed class {Source}To{Target}Mapper()
    : DataMapperBase<{SourceConverter}, {TargetConverter}>(
        id: "{mapperId}",
        name: "{Source} → {Target}",
        sourceConverter: new {SourceConverter}(),
        targetConverter: new {TargetConverter}())
{
    /// <summary>
    /// Direct optimized mapping.
    /// </summary>
    public override object? Map(object? sourceValue)
    {
        // Optimized path for common cases
        if (sourceValue is {SourceClrType} sourceTyped)
            return ({TargetClrType})sourceTyped;

        if (sourceValue is null)
            return null;

        // Fallback to CLR bridge for edge cases
        return MapViaClr(sourceValue);
    }
}
```

**Example** (JSON integer → SQL bigint):
```csharp
[TypeOption(typeof(DataMappers), "JsonIntegerInt64_MsSqlInt64")]
public sealed class JsonIntegerToMsSqlBigIntMapper()
    : DataMapperBase<JsonSchemaIntegerInt64Converter, MsSqlInt64Converter>(
        id: "JsonIntegerInt64_MsSqlInt64",
        name: "JSON Integer (int64) → MS SQL bigint",
        sourceConverter: new JsonSchemaIntegerInt64Converter(),
        targetConverter: new MsSqlInt64Converter())
{
    public override object? Map(object? sourceValue)
    {
        // Both are long, direct cast works
        if (sourceValue is long l) return l;
        if (sourceValue is int i) return (long)i;  // Widen int to long
        if (sourceValue is null) return null;

        // Fallback
        return MapViaClr(sourceValue);
    }
}
```

---

## Template Code - Base Mapper (CLR Bridge)

```csharp
using System;
using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base class for data mappers with default CLR bridge implementation.
/// </summary>
public abstract class DataMapperBase<TSource, TTarget>(
    string id,
    string name,
    TSource sourceConverter,
    TTarget targetConverter)
    : TypeOptionBase<string, DataMapperBase<TSource, TTarget>>(id, name),
      IDataMapper<TSource, TTarget>
    where TSource : IDataTypeConverter
    where TTarget : IDataTypeConverter
{
    public TSource SourceConverter { get; } = sourceConverter;
    public TTarget TargetConverter { get; } = targetConverter;

    public virtual bool CanMap => true;

    /// <summary>
    /// Default CLR bridge: Source → CLR → Target (two-step conversion).
    /// </summary>
    public virtual object? MapViaClr(object? sourceValue)
    {
        // Step 1: Source type → CLR
        var clrValue = SourceConverter.ToClr(sourceValue);

        // Step 2: CLR → Target type
        var targetValue = TargetConverter.ToDb(clrValue);

        return targetValue;
    }

    /// <summary>
    /// Override for explicit optimized mapping.
    /// Default: delegates to MapViaClr.
    /// </summary>
    public abstract object? Map(object? sourceValue);
}
```

---

## Template Code - Default Mapper (Auto-Generated)

```csharp
using System;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Default mapper that uses CLR bridge (no explicit mapping).
/// Created automatically when no explicit mapper is registered.
/// </summary>
public sealed class DefaultDataMapper<TSource, TTarget>(
    TSource sourceConverter,
    TTarget targetConverter)
    : DataMapperBase<TSource, TTarget>(
        id: $"Default_{sourceConverter.Name}_to_{targetConverter.Name}",
        name: $"{sourceConverter.Name} → {targetConverter.Name}",
        sourceConverter: sourceConverter,
        targetConverter: targetConverter)
    where TSource : IDataTypeConverter
    where TTarget : IDataTypeConverter
{
    /// <summary>
    /// Uses default CLR bridge (calls MapViaClr).
    /// </summary>
    public override object? Map(object? sourceValue) => MapViaClr(sourceValue);
}
```

---

## Requirements

### 1. When to Create Explicit Mappers

**Create explicit mapper when:**
- Types map directly without CLR intermediary (optimization)
- Special conversion logic needed (e.g., timezone handling)
- Performance critical path

**Use default mapper when:**
- Types both target same CLR type (SQL int and JSON integer both → long)
- No special logic needed
- CLR bridge works fine

### 2. Mapper Factory Method

```csharp
// In DataMappers TypeCollection
public static IDataMapper<TSource, TTarget> GetMapper<TSource, TTarget>(
    TSource sourceConverter,
    TTarget targetConverter)
    where TSource : IDataTypeConverter
    where TTarget : IDataTypeConverter
{
    // Try to find explicit mapper
    var explicitMapper = All().FirstOrDefault(m =>
        m.SourceConverter.Id == sourceConverter.Id &&
        m.TargetConverter.Id == targetConverter.Id);

    if (explicitMapper != null)
        return (IDataMapper<TSource, TTarget>)explicitMapper;

    // Create default mapper (CLR bridge)
    return new DefaultDataMapper<TSource, TTarget>(sourceConverter, targetConverter);
}
```

### 3. Usage in Federated Queries (Future)

```csharp
// Joining SQL customer data with REST order data
var sqlField = new Field { TypeSystemId = "MsSql", ConverterTypeId = 1 };  // int
var restField = new Field { TypeSystemId = "JsonSchema", ConverterTypeId = 2 };  // integer+int64

// Get converters
var sqlConverter = MsSqlConverters.ById(sqlField.ConverterTypeId.Value);
var jsonConverter = JsonSchemaConverters.ById(restField.ConverterTypeId.Value);

// Get mapper (explicit or default)
var mapper = DataMappers.GetMapper(sqlConverter, jsonConverter);

// Map value for join
var sqlCustomerId = 123;  // From SQL query
var mappedValue = mapper.Map(sqlCustomerId);  // SQL int → JSON integer
var matchingOrders = restOrders.Where(o => o.customer_id == mappedValue);
```

---

## Integration Checklist

- [ ] Create IDataMapper<TSource, TTarget> interface (Phase 6)
- [ ] Create DataMapperBase with MapViaClr() (Phase 6)
- [ ] Create DefaultDataMapper (Phase 6)
- [ ] Create DataMappers TypeCollection (Phase 6)
- [ ] Create explicit mappers for optimization (as needed)
- [ ] Test GetMapper() factory method
- [ ] Test MapViaClr() default implementation
- [ ] Test explicit mapper optimizations

---

## Performance Considerations

### CLR Bridge (Default)
```
Source Type → ToClr() → CLR Type → ToDb() → Target Type
```
**Performance**: Two conversions, some boxing
**When to use**: Most cases (it's fast enough)

### Explicit Mapper (Optimized)
```
Source Type → Direct cast/conversion → Target Type
```
**Performance**: One conversion, no boxing
**When to use**: Hot paths, large datasets, profiling shows bottleneck

### Example Performance Comparison

```csharp
// CLR Bridge (Default)
SQL int (42) → ToClr() → CLR int (42) → ToDb() → JSON integer (42)
// ~20ns per conversion

// Explicit Mapper
SQL int (42) → Direct cast → JSON integer (42)
// ~5ns per conversion

// For 1 million records: 15ms saved
```

**Recommendation**: Start with CLR bridge (default), add explicit mappers if profiling shows need.

---

## Common Mistakes

❌ **Creating mappers before they're needed**
```csharp
// Don't create mappers in Phase 4-5
// Wait until Phase 6 (federated query implementation)
```

❌ **Not handling null**
```csharp
public override object? Map(object? sourceValue)
{
    return (int)sourceValue;  // Will throw on null!
}
```

✅ **Always check null first**
```csharp
public override object? Map(object? sourceValue)
{
    if (sourceValue is null) return null;
    if (sourceValue is int i) return i;
    return MapViaClr(sourceValue);  // Fallback
}
```

---

## Future Use Cases

### Simple Mapping (Same CLR Type)
```
SQL int (42) → CLR int (42) → JSON integer (42)
```
**Use**: Default mapper (CLR bridge works perfectly)

### Type Widening
```
JSON int32 (42) → CLR int (42) → SQL bigint (42)
```
**Use**: Default mapper or explicit (explicit avoids boxing)

### Type Narrowing (Lossy)
```
SQL bigint (9223372036854775807) → CLR long → JSON int32 (overflow!)
```
**Use**: Explicit mapper with overflow checks

### Complex Conversions
```
SQL datetime (local time) → CLR DateTime → JSON date-time (UTC ISO 8601)
```
**Use**: Explicit mapper with timezone conversion

---

**Note**: Mappers are for Phase 6 (federated queries). Not needed for Phases 4-5 (JsonSchemaConverters + REST importers).
