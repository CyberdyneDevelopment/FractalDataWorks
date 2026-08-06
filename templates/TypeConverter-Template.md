# Template: Creating a New Type Converter

**Purpose**: Converts between a data source type and CLR type (e.g., SQL `int` ↔ CLR `int`, JSON `integer` ↔ CLR `long`)

---

## File Location

```
src/Fdw.Data.{TypeSystem}/
└── Converters/
    ├── {TypeSystem}Converters.cs          ← TypeCollection (create once)
    └── {TypeSystem}{Type}Converter.cs     ← Individual converters (one per type)
```

**Examples:**
- `src/Fdw.Data.MsSql/Converters/MsSqlInt32Converter.cs`
- `src/Fdw.Data.JsonSchema/Converters/JsonSchemaIntegerInt64Converter.cs`

---

## Template Code

```csharp
using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.{TypeSystem};

/// <summary>
/// Converts {SourceTypeName} to CLR {TargetType}.
/// </summary>
[TypeOption(typeof({TypeSystem}Converters), "{Name}")]
[ExcludeFromCodeCoverage]
public sealed class {TypeSystem}{Name}Converter()
    : DataTypeConverterBase(
        id: {UniqueId},                    // Unique within this type system
        name: "{Name}",                    // Converter name (e.g., "Int32", "String")
        sourceType: "{sourceTypeName}",    // Source type name (e.g., "int", "integer+int64")
        targetClrType: typeof({ClrType}),  // CLR type
        dbType: DbType.{DbType})           // ADO.NET DbType
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        // Convert database value to CLR type
        return Convert.To{ClrType}(dbValue, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        // Most converters just pass through
        return clrValue;
    }
}
```

---

## Requirements

### 1. Naming Convention

**Class name**: `{TypeSystem}{ClrTypeName}Converter`
- ✅ `MsSqlInt32Converter` (SQL int → CLR Int32)
- ✅ `JsonSchemaIntegerInt64Converter` (JSON integer+int64 → CLR Int64)
- ❌ `SqlConverter` (too generic)
- ❌ `Int32Converter` (missing type system prefix)

### 2. Constructor Parameters (ALL REQUIRED)

```csharp
DataTypeConverterBase(
    id: int,              // Unique ID within type system (1, 2, 3, ...)
    name: string,         // Converter name ("Int32", "String", "IntegerInt64")
    sourceType: string,   // Source type name from data source
    targetClrType: Type,  // CLR type (typeof(int), typeof(string))
    dbType: DbType)       // ADO.NET DbType for parameters
```

**CRITICAL**: Pass ALL values through constructor - NO abstract properties!

### 3. Source Type Naming

**For single type systems (MsSql)**:
- Use database type name: `"int"`, `"nvarchar"`, `"bigint"`, `"bit"`
- Lowercase, matches INFORMATION_SCHEMA.DATA_TYPE

**For type+format systems (JSON Schema)**:
- Use composite key: `"{type}+{format}"`
- Examples: `"integer+int32"`, `"integer+int64"`, `"string+date-time"`, `"number+decimal"`
- Allows differentiation: JSON integer can map to int OR long depending on format

### 4. Attributes (BOTH REQUIRED)

```csharp
[TypeOption(typeof({TypeSystem}Converters), "{Name}")]  // Registers with collection
[ExcludeFromCodeCoverage]                                // No logic to test
```

### 5. ToClr() Implementation

**Pattern**:
1. Check for null/DBNull → return null
2. Convert using type-specific Convert.ToXxx() method
3. Use CultureInfo.InvariantCulture for consistency

**Examples**:
```csharp
// Int32
return Convert.ToInt32(dbValue, CultureInfo.InvariantCulture);

// String
return dbValue.ToString();

// Boolean
return Convert.ToBoolean(dbValue, CultureInfo.InvariantCulture);

// Guid (handles multiple formats)
if (dbValue is Guid guid) return guid;
if (dbValue is string str) return Guid.Parse(str);
if (dbValue is byte[] bytes) return new Guid(bytes);
throw new InvalidCastException($"Cannot convert {dbValue.GetType().Name} to Guid");
```

### 6. ToDb() Implementation

**Most converters**: Just pass through
```csharp
public override object? ToDb(object? clrValue) => clrValue;
```

**Special cases** (rare): Transform for database
```csharp
// Example: DateOnly → DateTime for SQL Server
public override object? ToDb(object? clrValue)
{
    if (clrValue is DateOnly dateOnly)
        return dateOnly.ToDateTime(TimeOnly.MinValue);
    return clrValue;
}
```

---

## TypeCollection Setup (One-Time Per Type System)

Before creating converters, create the TypeCollection:

```csharp
// src/Fdw.Data.{TypeSystem}/Converters/{TypeSystem}Converters.cs

using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.{TypeSystem};

[TypeCollection(typeof(DataTypeConverterBase),
                typeof(IDataTypeConverter),
                typeof({TypeSystem}Converters))]
public abstract partial class {TypeSystem}Converters
    : TypeCollectionBase<DataTypeConverterBase, IDataTypeConverter>
{
    // Source generator creates: All(), ById(), ByName(), NotFound()

    // Add custom lookup methods as needed:
    public static IDataTypeConverter BySourceType(string sourceType)
    {
        if (string.IsNullOrWhiteSpace(sourceType))
            return NotFound();

        var converter = All().FirstOrDefault(c =>
            c.SourceType.Equals(sourceType, System.StringComparison.Ordinal));

        return converter ?? NotFound();
    }
}
```

---

## Integration Checklist

After creating converters:

- [ ] Add [TypeOption] attribute pointing to {TypeSystem}Converters
- [ ] Pass all 5 constructor parameters
- [ ] Implement ToClr() and ToDb()
- [ ] Add [ExcludeFromCodeCoverage] attribute
- [ ] Build project - verify source generator discovers it
- [ ] Test collection: `{TypeSystem}Converters.BySourceType("{type}")` works
- [ ] Verify appears in `{TypeSystem}Converters.All()`

---

## Common Mistakes

❌ **Creating Empty class manually**
```csharp
// DON'T DO THIS - generator creates it!
public class Empty{TypeSystem}Converter : DataTypeConverterBase { }
```

❌ **Using abstract properties**
```csharp
// WRONG
public override string SourceType => "int";
public override Type TargetClrType => typeof(int);
```

❌ **Not using CultureInfo.InvariantCulture**
```csharp
// WRONG
return Convert.ToInt32(dbValue);  // Uses current culture!

// RIGHT
return Convert.ToInt32(dbValue, CultureInfo.InvariantCulture);
```

❌ **Testing individual converters**
```csharp
// DON'T - converters are data, no logic to test
public class MsSqlInt32ConverterTests { }  // Delete this!
```

✅ **Test the collection instead**
```csharp
// DO - test collection behavior
public class MsSqlConvertersTests
{
    [Fact]
    public void BySourceTypeShouldFindIntConverter()
    {
        var converter = MsSqlConverters.BySourceType("int");
        converter.ShouldNotBe(MsSqlConverters.NotFound());
    }
}
```

---

## Example: MsSql Type System

| Converter | ID | SourceType | CLR Type | DbType |
|-----------|----|-----------| ---------|--------|
| MsSqlInt32Converter | 1 | int | int | Int32 |
| MsSqlInt64Converter | 2 | bigint | long | Int64 |
| MsSqlStringConverter | 3 | nvarchar | string | String |
| MsSqlBooleanConverter | 4 | bit | bool | Boolean |
| MsSqlDateTimeConverter | 5 | datetime | DateTime | DateTime |
| MsSqlDateTimeOffsetConverter | 6 | datetimeoffset | DateTimeOffset | DateTimeOffset |
| MsSqlDecimalConverter | 7 | decimal | decimal | Decimal |
| MsSqlFloatConverter | 8 | float | double | Double |
| MsSqlGuidConverter | 9 | uniqueidentifier | Guid | Guid |
| MsSqlByteArrayConverter | 10 | varbinary | byte[] | Binary |

---

## Example: JSON Schema Type System (Future)

| Converter | ID | SourceType | CLR Type | DbType |
|-----------|----|-----------| ---------|--------|
| JsonSchemaIntegerInt32Converter | 1 | integer+int32 | int | Int32 |
| JsonSchemaIntegerInt64Converter | 2 | integer+int64 | long | Int64 |
| JsonSchemaNumberDecimalConverter | 5 | number+decimal | decimal | Decimal |
| JsonSchemaStringDateTimeConverter | 6 | string+date-time | DateTime | DateTime |
| JsonSchemaStringConverter | 10 | string | string | String |
| JsonSchemaBooleanConverter | 11 | boolean | bool | Boolean |

Note the composite keys for types with formats.

---

**See**: `src/Fdw.Data.MsSql/Converters/` for complete working examples
