# Template: Creating a New Schema Importer

**Purpose**: Discovers schema from a data source (database, API, file) and returns IDataStore with typed containers

**KEY CHANGE**: Schema importers now live in **type-system projects** (Data.MsSql, Data.JsonSchema), NOT in DataStores.* projects!

---

## File Location

```
src/Fdw.Data.{TypeSystem}/
└── Importers/
    └── {Name}SchemaImporter.cs
```

**Examples:**
- `src/Fdw.Data.MsSql/Importers/MsSqlSchemaImporter.cs` (was SqlServerSchemaImporter in DataStores.SqlServer)
- `src/Fdw.Data.JsonSchema/Importers/RestOpenApiSchemaImporter.cs` (move from DataStores.Rest)
- `src/Fdw.Data.JsonSchema/Importers/ODataSchemaImporter.cs` (move from DataStores.Rest)

**Why the move?** Importers need converters to map types. Converters live in type-system projects. Importers belong with their converters.

---

## Template Code

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Data.Builders;
using Fdw.Data.DataStores.Abstractions;
using Fdw.Data.Importers.Abstractions;
using Fdw.Data.Importers.Abstractions.Configuration;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.{TypeSystem}.Importers;

/// <summary>
/// Imports schema from {DataSourceType} ({description}).
/// Returns IDataStore with {PathType}Paths containing {ContainerType}Containers.
/// </summary>
[TypeOption(typeof(SchemaImporters), "{Name}")]
public sealed partial class {Name}SchemaImporter
    : SchemaImporterBase<{Configuration}>,
      ISchemaImporter<{Configuration}>
{
    private readonly ILogger<{Name}SchemaImporter> _logger;

    public {Name}SchemaImporter(ILogger<{Name}SchemaImporter> logger)
        : base(
            id: {UniqueId},
            name: "{Name}",
            description: "{Description}",
            dataStoreType: "{DataStoreType}")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<IGenericResult<IDataStore<{Configuration}>>> Import(
        string source,
        SchemaImporterOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Connect to source
            var connection = await OpenConnection(source, cancellationToken);

            // 2. Discover objects (tables, endpoints, files, etc.)
            var objectsResult = await DiscoverObjects(connection, options, cancellationToken);
            if (!objectsResult.IsSuccess)
                return GenericResult<IDataStore<{Configuration}>>.Failure(objectsResult.CurrentMessage);

            // 3. Build DataStore
            var builder = new DataStoreBuilder<{Configuration}>()
                .WithId(ExtractId(source))
                .WithName(ExtractName(source))
                .WithStoreType("{DataStoreType}")
                .WithTranslatorType("{TranslatorType}")
                .WithLocation(source)
                .WithConfiguration(new {Configuration} { /* ... */ });

            // 4. For each discovered object, create path + container
            foreach (var obj in objectsResult.Value)
            {
                var pathResult = await CreatePath(connection, obj, options, cancellationToken);
                if (pathResult.IsSuccess && pathResult.Value != null)
                {
                    builder.AddPath(pathResult.Value);
                }
            }

            // 5. Build and return
            return builder.Build();
        }
        catch (Exception ex)
        {
            return GenericResult<IDataStore<{Configuration}>>.Failure(
                $"Import failed: {ex.Message}");
        }
    }

    private async Task<IGenericResult<IDataPath>> CreatePath(
        Connection connection,
        ObjectInfo obj,
        SchemaImporterOptions? options,
        CancellationToken cancellationToken)
    {
        // 1. Get columns/fields for this object
        var fieldsResult = await GetFields(connection, obj, cancellationToken);
        if (!fieldsResult.IsSuccess)
            return GenericResult<IDataPath>.Failure(fieldsResult.CurrentMessage);

        // 2. Build schema from fields using CONVERTERS
        var schema = BuildSchema(fieldsResult.Value);

        // 3. Create path
        var path = new {PathType}(/* ... */);

        // 4. Create container
        var container = new {ContainerType}(
            name: obj.Name,
            path: path,
            schema: schema,
            format: FormatTypes.{Format});

        // 5. Build IDataPath with embedded container
        var pathBuilder = new DataPathBuilder<IStorageContainer>()
            .WithId(obj.Id)
            .WithName(obj.Name)
            .WithPathType("{PathTypeName}")
            .AddContainer(container);

        return pathBuilder.Build();
    }

    private static ContainerSchema BuildSchema(List<FieldInfo> fields)
    {
        var schemaFields = fields.Select(MapToField).ToList();

        return new ContainerSchema
        {
            Fields = schemaFields
        };
    }

    private static IField MapToField(FieldInfo fieldInfo)
    {
        // ⭐ CRITICAL: Use type-system-specific converters!
        var converter = {TypeSystem}Converters.BySourceType(fieldInfo.TypeName);

        var clrType = converter.TargetClrType;
        if (fieldInfo.IsNullable && clrType.IsValueType)
        {
            clrType = typeof(Nullable<>).MakeGenericType(clrType);
        }

        return new Field
        {
            Name = fieldInfo.Name,
            FieldType = new SimpleFieldType
            {
                TypeName = clrType.Name,
                ClrType = clrType
            },
            Role = fieldInfo.IsPrimaryKey ? FieldRole.Identity : FieldRole.Attribute,
            IsNullable = fieldInfo.IsNullable,
            IsPrimaryKey = fieldInfo.IsPrimaryKey,
            TypeSystemId = "{TypeSystemId}",      // ⭐ Set type system!
            ConverterTypeId = converter.Id,        // ⭐ Type-safe ID!
            Description = fieldInfo.Description
        };
    }
}
```

---

## Requirements

### 1. Use Correct Converter Collection

**✅ DO** - Use type-system-specific converters:
```csharp
// In MsSqlSchemaImporter
var converter = MsSqlConverters.BySourceType("int");

// In RestOpenApiSchemaImporter (future)
var converter = JsonSchemaConverters.BySourceType("integer+int64");
```

**❌ DON'T** - Use parent collection or wrong type system:
```csharp
var converter = DataTypeConverters.All().FirstOrDefault(...);  // Wrong!
var converter = MsSqlConverters.BySourceType("integer");  // Wrong type system!
```

### 2. Set TypeSystemId and ConverterTypeId

**Required on every field:**
```csharp
new Field {
    Name = "CustomerId",
    TypeSystemId = "MsSql",           // ✅ Identifies type system
    ConverterTypeId = converter.Id,   // ✅ Type-safe converter reference
    // ConverterTypeName - DELETED, don't use!
};
```

### 3. Single Lookup Per Field

**✅ DO** - Lookup converter ONCE:
```csharp
var converter = MsSqlConverters.BySourceType(column.DataType);
var clrType = converter.TargetClrType;  // Use it
var converterId = converter.Id;         // Use it
```

**❌ DON'T** - Double lookup:
```csharp
// WRONG - looking up twice!
var clrType = MsSqlConverters.BySourceType("int").TargetClrType;
var converterId = MsSqlConverters.BySourceType("int").Id;  // Same converter!
```

### 4. Handle Unknown Types

```csharp
var converter = MsSqlConverters.BySourceType(sqlType);

if (converter == MsSqlConverters.NotFound())
{
    // Log warning, use object fallback
    _logger.LogWarning("Unknown SQL type: {SqlType}", sqlType);
    converter = MsSqlConverters.NotFound();  // Still use NotFound (not null!)
}
```

---

## Type System Mapping Examples

### MsSql (SQL Server)
```csharp
var converter = MsSqlConverters.BySourceType(column.DataType.ToLowerInvariant());
// "int" → MsSqlInt32Converter
// "bigint" → MsSqlInt64Converter
// "nvarchar" → MsSqlStringConverter
```

### JsonSchema (REST APIs, OpenAPI)
```csharp
// Build composite key from type + format
var compositeKey = string.IsNullOrEmpty(format)
    ? jsonType
    : $"{jsonType}+{format}";

var converter = JsonSchemaConverters.BySourceType(compositeKey);
// "integer+int32" → JsonSchemaIntegerInt32Converter
// "integer+int64" → JsonSchemaIntegerInt64Converter
// "string+date-time" → JsonSchemaStringDateTimeConverter

// Fallback without format if not found
if (converter == JsonSchemaConverters.NotFound())
{
    converter = JsonSchemaConverters.BySourceType(jsonType);  // Try without format
}
```

### OData (Edm Types)
```csharp
var converter = ODataConverters.BySourceType(edmType);
// OR reuse JsonSchemaConverters if Edm types map 1:1
```

---

## Integration with DataGateway

**Flow:**
```
1. Importer discovers schema
   ↓
2. Creates IDataStore with paths/containers
   ↓
3. User registers containers:
   services.AddDataGateway(containers => {
       var importer = new MsSqlSchemaImporter(logger);
       var dataStore = await importer.Import(connectionString);

       foreach (var path in dataStore.AvailablePaths)
       {
           var container = path.GetContainer();
           containers[container.Name] = container;  // Register!
       }
   });
   ↓
4. DataGateway uses containers for queries
   connection.Execute(command, container)  ← Container has schema with TypeSystemId!
```

---

## Common Mistakes

❌ **Hardcoding SQL type names in non-SQL importers**
```csharp
// WRONG - REST importer using SQL types!
var sqlType = jsonType switch {
    "integer" => "int",  // Bad!
    "string" => "nvarchar"  // Bad!
};
```

❌ **Not setting TypeSystemId**
```csharp
new Field {
    Name = "id",
    ConverterTypeId = 1  // Which type system? Unknown!
};
```

❌ **Using switch statements for type mapping**
```csharp
// WRONG - use TypeCollection lookup instead!
var clrType = sqlType switch {
    "int" => typeof(int),
    "nvarchar" => typeof(string),
    ...
};
```

---

**See**: `src/Fdw.Data.MsSql/Importers/` for working example (was in DataStores.SqlServer, moved to Data.MsSql)
