# Schema Abstractions

The `Fdw.Schema.Abstractions` package provides a unified schema system for describing data structures across different storage systems (SQL tables, JSON documents, CSV files, REST APIs, etc.).

## Overview

The schema system consists of:

1. **PropertyRoles** - TypeCollection defining semantic meaning of properties (Surrogate, NaturalKey, Lookup, Attribute, Measure)
2. **DataLayouts** - TypeCollection defining structural characteristics (Tabular, Hierarchical, Document, KeyValue, Graph)
3. **IPropertyDefinition** - Base interface for all property-like elements (columns, fields, attributes)
4. **ISchemaDefinition<T>** - Generic interface for describing data structures
5. **IKeyDefinition<T>** and **IIndexDefinition<T>** - Key and index metadata

## PropertyRoles TypeCollection

PropertyRoles define the **semantic meaning** of properties in a schema. This eliminates the need for switch statements based on property names or types.

### Available Roles

| Role | IsKeyRole | IsIndexable | IsAggregatable | Description |
|------|-----------|-------------|----------------|-------------|
| **Surrogate** | ✓ | ✓ | ✗ | Auto-generated key with no business meaning (e.g., Id, RowId) |
| **NaturalKey** | ✓ | ✓ | ✗ | Business identifier, human-meaningful (e.g., Email, SKU) |
| **Lookup** | ✗ | ✓ | ✗ | Indexed for search, not part of key (e.g., LastName, City) |
| **Attribute** | ✗ | ✗ | ✗ | Descriptive, non-indexed (e.g., Description, Comments) |
| **Measure** | ✗ | ✗ | ✓ | Aggregatable numeric (e.g., TotalSales, Quantity) |

### Example: Using PropertyRoles

```csharp
using Fdw.Schema;

var idProperty = new PropertyDefinition
{
    Name = "Id",
    Role = PropertyRoles.Surrogate,  // Generated static property
    DataType = DataTypes.Integer,
    IsRequired = true
};

var emailProperty = new PropertyDefinition
{
    Name = "Email",
    Role = PropertyRoles.NaturalKey,
    DataType = DataTypes.String,
    IsRequired = true
};

var salesProperty = new PropertyDefinition
{
    Name = "TotalSales",
    Role = PropertyRoles.Measure,
    DataType = DataTypes.Decimal,
    IsRequired = false
};

// No switch statements - roles know their properties!
if (idProperty.Role.IsKeyRole)
{
    GeneratePrimaryKeyConstraint(idProperty);
}

if (emailProperty.Role.IsIndexable)
{
    GenerateIndex(emailProperty);
}

if (salesProperty.Role.IsAggregatable)
{
    // SUM(TotalSales), AVG(TotalSales), etc.
    GenerateAggregateQuery(salesProperty);
}
```

### Querying by Role

```csharp
// Get all key fields from a schema
var keyFields = schema.Properties
    .Where(p => p.Role.IsKeyRole)
    .ToList();

// Get all indexable fields
var indexableFields = schema.Properties
    .Where(p => p.Role.IsIndexable)
    .ToList();

// Get all measures for aggregation
var measures = schema.Properties
    .Where(p => p.Role.IsAggregatable)
    .ToList();
```

## DataLayouts TypeCollection

DataLayouts define the **structural characteristics** of data, enabling layout-specific processing without switch statements.

### Available Layouts

| Layout | SupportsNesting | SupportsFlattening | IsTabular | Description |
|--------|----------------|-------------------|-----------|-------------|
| **Tabular** | ✗ | ✗ | ✓ | Flat rows and columns (SQL table, CSV, Excel) |
| **Hierarchical** | ✓ | ✓ | ✗ | Nested parent-child structure (JSON, XML) |
| **Document** | ✓ | ✓ | ✗ | Single complex object (MongoDB document, config file) |
| **KeyValue** | ✗ | ✓ | ✗ | Key-value pairs (Redis, config sections) |
| **Graph** | ✓ | ✗ | ✗ | Nodes and edges (Neo4j, relationships) |

### Example: Using DataLayouts

```csharp
using Fdw.Schema;

// SQL table schema
var sqlSchema = new SchemaDefinition
{
    Name = "Customers",
    Layout = DataLayouts.Tabular,  // Generated static property
    Properties = customerProperties
};

// JSON API response schema
var jsonSchema = new SchemaDefinition
{
    Name = "OrderWithLineItems",
    Layout = DataLayouts.Hierarchical,
    Properties = orderProperties,
    Children = new[]
    {
        new SchemaDefinition
        {
            Name = "LineItems",
            Layout = DataLayouts.Hierarchical,
            PathExpression = "$.lineItems[*]"
        }
    }
};

// No switch statements - layouts know their properties!
if (sqlSchema.Layout.IsTabular)
{
    GenerateSqlDdl(sqlSchema);
}

if (jsonSchema.Layout.SupportsNesting)
{
    ProcessChildSchemas(jsonSchema.Children);
}

if (jsonSchema.Layout.SupportsFlattening)
{
    var flattenedSchema = FlattenToTabular(jsonSchema);
}
```

## IPropertyDefinition Interface

Base interface for all property-like elements across different storage systems.

```csharp
public interface IPropertyDefinition
{
    string Name { get; }
    IPropertyRole Role { get; }
    bool IsRequired { get; }
    string? Description { get; }
    IReadOnlyDictionary<string, object>? Metadata { get; }
}
```

### Specializations

**IColumnDefinition** - SQL column metadata (extends IPropertyDefinition):
```csharp
public interface IColumnDefinition : IPropertyDefinition
{
    string DataTypeName { get; }    // SQL type: "varchar(100)", "int", "decimal(18,2)"
    int? MaxLength { get; }
    int? Precision { get; }
    int? Scale { get; }
    object? DefaultValue { get; }
}
```

**IFieldDefinition** - Document/JSON field metadata (extends IPropertyDefinition):
```csharp
public interface IFieldDefinition : IPropertyDefinition
{
    Type FieldType { get; }         // .NET type
    object? DefaultValue { get; }
    bool IsCalculated { get; }
    Func<IDataRow, object>? Calculator { get; }
}
```

## ISchemaDefinition<T> Interface

Generic interface for describing data structures, supporting both flat (tabular) and nested (hierarchical) schemas.

```csharp
public interface ISchemaDefinition<TProperty> where TProperty : IPropertyDefinition
{
    string Name { get; }
    string? Description { get; }
    IReadOnlyList<TProperty> Properties { get; }

    // Keys
    IKeyDefinition<TProperty>? SurrogateKey { get; }  // Auto-generated (Id, RowId)
    IKeyDefinition<TProperty>? NaturalKey { get; }    // Business identifier (Email, SKU)

    // Indexes
    IReadOnlyList<IIndexDefinition<TProperty>> Indexes { get; }

    // Layout characteristics
    IDataLayout Layout { get; }

    // Hierarchical support
    IReadOnlyList<ISchemaDefinition<TProperty>>? Children { get; }
    string? PathExpression { get; }

    // Query methods
    TProperty? GetProperty(string name);
    IReadOnlyList<TProperty> Get(IPropertyRole role);
}
```

### Example: IContainerSchema Implementation

The existing `IContainerSchema` interface now implements `ISchemaDefinition<IField>`:

```csharp
public interface IContainerSchema : ISchemaDefinition<IField>
{
    // ISchemaDefinition members inherited
    // Additional container-specific members...
}
```

## IKeyDefinition<T> Interface

Represents a key (surrogate or natural) on a schema.

```csharp
public interface IKeyDefinition<TProperty> where TProperty : IPropertyDefinition
{
    string Name { get; }
    IReadOnlyList<TProperty> Properties { get; }  // Multi-column keys supported
    bool IsSurrogate { get; }                     // true = auto-generated, false = natural
    bool IsClustered { get; }                     // SQL-specific
}
```

### Example: Defining Keys

```csharp
// Surrogate key (single column)
var surrogateKey = new KeyDefinition<IColumnDefinition>
{
    Name = "PK_Customers_Id",
    Properties = new[] { idColumn },
    IsSurrogate = true,
    IsClustered = true
};

// Natural key (composite)
var naturalKey = new KeyDefinition<IColumnDefinition>
{
    Name = "UK_Customers_Email",
    Properties = new[] { emailColumn },
    IsSurrogate = false,
    IsClustered = false
};

var schema = new SchemaDefinition
{
    Name = "Customers",
    SurrogateKey = surrogateKey,
    NaturalKey = naturalKey
};
```

## IIndexDefinition<T> Interface

Represents an index on a schema.

```csharp
public interface IIndexDefinition<TProperty> where TProperty : IPropertyDefinition
{
    string Name { get; }
    IReadOnlyList<TProperty> Properties { get; }
    bool IsUnique { get; }
    bool IsClustered { get; }
    string? FilterExpression { get; }  // Filtered index (SQL Server)
}
```

### Example: Defining Indexes

```csharp
// Simple index
var lastNameIndex = new IndexDefinition<IColumnDefinition>
{
    Name = "IX_Customers_LastName",
    Properties = new[] { lastNameColumn },
    IsUnique = false
};

// Composite unique index
var uniqueEmailIndex = new IndexDefinition<IColumnDefinition>
{
    Name = "IX_Customers_Email_Unique",
    Properties = new[] { emailColumn },
    IsUnique = true
};

// Filtered index (SQL Server)
var activeCustomersIndex = new IndexDefinition<IColumnDefinition>
{
    Name = "IX_Customers_Active",
    Properties = new[] { lastNameColumn, emailColumn },
    FilterExpression = "IsActive = 1"
};
```

## DDL Generation

The `Fdw.Schema.Ddl` package provides DDL generation from schema definitions.

### IDdlGenerator Interface

```csharp
public interface IDdlGenerator
{
    string GenerateCreateTableDdl(ISchemaDefinition<IColumnDefinition> schema);
    string GenerateAlterTableDdl(ISchemaDefinition<IColumnDefinition> existing, ISchemaDefinition<IColumnDefinition> target);
    string GenerateDropTableDdl(string tableName);
}
```

### Example: MsSqlDdlGenerator

```csharp
using Fdw.Schema.Ddl;
using Fdw.Schema.Ddl.MsSql;

var generator = new MsSqlDdlGenerator();

var schema = new SchemaDefinition
{
    Name = "Customers",
    Layout = DataLayouts.Tabular,
    Properties = new[]
    {
        new ColumnDefinition
        {
            Name = "Id",
            Role = PropertyRoles.Surrogate,
            DataTypeName = "int",
            IsRequired = true
        },
        new ColumnDefinition
        {
            Name = "Email",
            Role = PropertyRoles.NaturalKey,
            DataTypeName = "nvarchar(255)",
            IsRequired = true
        },
        new ColumnDefinition
        {
            Name = "TotalSales",
            Role = PropertyRoles.Measure,
            DataTypeName = "decimal(18,2)",
            IsRequired = false
        }
    },
    SurrogateKey = new KeyDefinition<IColumnDefinition>
    {
        Name = "PK_Customers",
        Properties = new[] { idColumn },
        IsSurrogate = true
    },
    Indexes = new[]
    {
        new IndexDefinition<IColumnDefinition>
        {
            Name = "IX_Customers_Email",
            Properties = new[] { emailColumn },
            IsUnique = true
        }
    }
};

// Generate DDL
var ddl = generator.GenerateCreateTableDdl(schema);

/*
CREATE TABLE [dbo].[Customers] (
    [Id] int NOT NULL,
    [Email] nvarchar(255) NOT NULL,
    [TotalSales] decimal(18,2) NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY CLUSTERED ([Id])
);

CREATE UNIQUE NONCLUSTERED INDEX [IX_Customers_Email]
    ON [dbo].[Customers] ([Email]);
*/
```

## Migration from Legacy Patterns

### Old: FieldRole Enum

```csharp
// OLD - enum-based
public enum FieldRole
{
    Key,
    Lookup,
    Attribute,
    Measure
}

// Switch statements everywhere
switch (field.Role)
{
    case FieldRole.Key:
        GeneratePrimaryKey(field);
        break;
    case FieldRole.Lookup:
        GenerateIndex(field);
        break;
    // ...
}
```

### New: PropertyRoles TypeCollection

```csharp
// NEW - TypeCollection-based
var field = new FieldDefinition
{
    Name = "Id",
    Role = PropertyRoles.Surrogate  // Type-safe!
};

// No switch - just property access
if (field.Role.IsKeyRole)
{
    GeneratePrimaryKey(field);
}
if (field.Role.IsIndexable)
{
    GenerateIndex(field);
}
```

### Old: KeyFields List

```csharp
// OLD - string-based
public IList<string> KeyFields { get; set; } = new List<string>();

// Manual checking
if (KeyFields.Contains(fieldName))
{
    // This is a key field
}
```

### New: Typed Keys with Roles

```csharp
// NEW - strongly-typed with semantic distinction
public IList<string> SurrogateKeyFields { get; set; } = new List<string>();  // Auto-generated
public IList<string> NaturalKeyFields { get; set; } = new List<string>();    // Business identifier

// Or use IKeyDefinition
public IKeyDefinition<IField>? SurrogateKey { get; set; }
public IKeyDefinition<IField>? NaturalKey { get; set; }

// Query by role
var keyFields = schema.Properties.Where(p => p.Role.IsKeyRole).ToList();
```

## Benefits of Unified Schema System

1. **Type-Safe** - No magic strings or enums
2. **Extensible** - Add new roles/layouts without breaking existing code
3. **Cross-Platform** - Works across SQL, JSON, CSV, REST
4. **No Switch Statements** - Roles/layouts know their own properties
5. **Metadata-Driven** - DDL generation, validation, documentation
6. **Semantic Clarity** - Surrogate vs. Natural keys are explicit

## See Also

- [TypeCollection Patterns](10-TypeCollection-Patterns.md) - PropertyRoles and DataLayouts examples
- [DataSets](05-02-DataSets.md) - Using schema system with DataSets
- [ManagedConfiguration](03-01-ManagedConfiguration.md) - DDL generation for configuration tables
