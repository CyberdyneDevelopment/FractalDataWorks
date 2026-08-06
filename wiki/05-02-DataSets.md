# DataSets

DataSets provide a logical abstraction over physical data sources, supporting:
- **Field mappings** (logical to physical names)
- **Calculated fields** (post-query computations)
- **Multi-source routing** (priority/cost-based selection)
- **Predicate pushdown** (filter optimization)

## Configuration Methods

FractalDataWorks supports three methods for configuring DataSets:

### 1. TypeCollection (Compile-Time)

Best for: Strongly-typed datasets with calculated fields.

The `DataSetTypes` collection uses `[MutableTypeCollection]` to allow runtime registration.

From [`DataSetTypes.cs:12-15`](../../src/FractalDataWorks.Data.DataSets.Abstractions/DataSetTypes.cs#L12-L15):

```csharp
[MutableTypeCollection(typeof(DataSetTypeBase), typeof(IDataSetType), typeof(DataSetTypes))]
public abstract partial class DataSetTypes : TypeCollectionBase<DataSetTypeBase, IDataSetType>
{
}
```

Dataset implementations extend `DataSetTypeBase` and use the `[TypeOption]` attribute:

From [`DataSetTypeBase.cs:29-41`](../../src/FractalDataWorks.Data.DataSets.Abstractions/DataSetTypeBase.cs#L29-L41) (constructor signature):

```csharp
protected DataSetTypeBase(
    int id,
    string name,
    string description,
    Type recordType,
    IReadOnlyCollection<IDataField> fields,
    string? category = null)
    : base(id, name, $"DataSets:{name}", $"{name} Data Set", description, category ?? "Dataset")
{
    RecordType = recordType;
    _fields = fields ?? Array.Empty<IDataField>();
    _keyFields = _fields.Where(f => f.IsKey).Select(f => f.Name).ToList().AsReadOnly();
}
```

Example dataset implementation pattern:

```csharp
// Domain/DataSets/CustomerMetricsDataSet.cs
[TypeOption(typeof(DataSetTypes), "CustomerMetrics")]
public sealed class CustomerMetricsDataSet : DataSetTypeBase
{
    public CustomerMetricsDataSet()
        : base(
            id: 1,
            name: "CustomerMetrics",
            description: "Customer data with calculated metrics",
            recordType: typeof(Customer),
            fields: CreateFields(),
            category: "CRM")
    {
    }

    public override IDataQuery CreateQuery() => new DataQueryBuilder<Customer>(Name);

    private static IReadOnlyCollection<IDataField> CreateFields()
    {
        return
        [
            new DataField("Id", typeof(int), isKey: true),
            new DataField("Name", typeof(string)),
            new DataField("CreatedAt", typeof(DateTime)),
            // Calculated field - executes in-memory after query
            new CalculatedDataField(
                "AccountAgeDays",
                typeof(int),
                row => (int)(DateTime.UtcNow - row.GetValue<DateTime>("CreatedAt")).TotalDays,
                "Days since account was created")
        ];
    }
}
```

### 2. SQL Seed (Database)

Best for: Ops-managed configuration, CI/CD pipelines.

The configuration database uses the `cfg` schema with these entity tables:

From [`DataSetEntity.cs:9-55`](../../src/FractalDataWorks.Configuration.MsSql/Models/DataSetEntity.cs#L9-L55):

```csharp
public sealed class DataSetEntity
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DataSetName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Version { get; init; } = "1.0";
    public string Category { get; init; } = string.Empty;
    public string? RecordTypeName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}
```

Example SQL seed script:

```sql
-- docker/mssql/init.sql
DECLARE @DataSetId UNIQUEIDENTIFIER = NEWID();

INSERT INTO cfg.DataSet (Id, Name, DataSetName, Description, Version, Category, RecordTypeName, CreatedAt)
VALUES (@DataSetId, 'CustomerMetrics_Seed', 'CustomerMetrics_Seed',
        'Customer data configured via SQL', '1.0', 'CRM',
        'ReferenceSolution.Domain.Models.Customer, ReferenceSolution.Domain',
        GETUTCDATE());

-- Field definitions (IsKey is determined by cfg.DataSetKeyField table)
INSERT INTO cfg.DataSetField (Id, DataSetId, Name, TypeName, IsRequired, Ordinal)
VALUES
    (NEWID(), @DataSetId, 'Id', 'System.Int32', 1, 0),
    (NEWID(), @DataSetId, 'Name', 'System.String', 1, 1);

-- Key field definitions (surrogate key)
INSERT INTO cfg.DataSetSurrogateKeyField (Id, DataSetId, FieldName, Ordinal)
VALUES (NEWID(), @DataSetId, 'Id', 0);

-- Natural key definition (if needed)
-- INSERT INTO cfg.DataSetNaturalKeyField (Id, DataSetId, FieldName, Ordinal)
-- VALUES (NEWID(), @DataSetId, 'Email', 0);

-- Source mapping
DECLARE @SourceId UNIQUEIDENTIFIER = NEWID();
INSERT INTO cfg.DataSetSource (Id, DataSetId, SourceName, DataStoreName, ConnectionName, ConnectionType,
                               Priority, EstimatedCost, SqlSchemaName, SqlTableName, SupportsPredicatePushdown)
VALUES (@SourceId, @DataSetId, 'Primary', 'OrdersStore', 'OrdersDb', 'MsSql',
        1, 50, 'dbo', 'Customers', 1);
```

### 3. Programmatic (Runtime)

Best for: Dynamic/tenant-specific configuration.

#### IDataSetConfiguration Interface

The `IDataSetConfiguration` interface is defined in `DataSets.Abstractions` (netstandard2.0) to allow services in `Services.Data.Abstractions` to reference DataSet configurations without depending on the full `DataSets` package (net10.0).

From [`IDataSetConfiguration.cs`](../../src/FractalDataWorks.Data.DataSets.Abstractions/IDataSetConfiguration.cs):

```csharp
public interface IDataSetConfiguration
{
    string DataSetName { get; }
    string Description { get; }
    string Version { get; }
    string Category { get; }
    string RecordTypeName { get; }
    IList<DataFieldConfiguration> Fields { get; }
    IList<string> SurrogateKeyFields { get; }
    IList<string> NaturalKeyFields { get; }
    IList<Guid> SourceIds { get; }
    IList<JoinConfiguration> Joins { get; }
    CachingConfiguration? Caching { get; }
}
```

#### DataSetConfiguration Class

The concrete implementation lives in the `DataSets` package (net10.0) and implements both `IDataSetConfiguration` and the ManagedConfiguration pattern.

From [`DataSetConfiguration.cs`](../../src/FractalDataWorks.Data.DataSets/DataSetConfiguration.cs):

```csharp
public sealed partial class DataSetConfiguration
    : ConfigurationBase<DataSetConfiguration>, IDataSetConfiguration
{
    public string DataSetName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public string Category { get; set; } = "Dataset";
    public string RecordTypeName { get; set; } = string.Empty;
    public IList<DataFieldConfiguration> Fields { get; set; } = new List<DataFieldConfiguration>();
    public IList<string> SurrogateKeyFields { get; set; } = new List<string>();
    public IList<string> NaturalKeyFields { get; set; } = new List<string>();
    public IList<Guid> SourceIds { get; set; } = new List<Guid>();
    public IList<JoinConfiguration> Joins { get; set; } = new List<JoinConfiguration>();
    public CachingConfiguration? Caching { get; set; }
}
```

#### IDataSetProvider

From [`IDataSetProvider.cs`](../../src/FractalDataWorks.Services.Data.Abstractions/IDataSetProvider.cs):

```csharp
/// <summary>
/// Registers a DataSet with the provider.
/// </summary>
/// <param name="name">The unique identifier for the DataSet.</param>
/// <param name="configuration">The DataSet configuration to register.</param>
void RegisterDataSet(string name, IDataSetConfiguration configuration);
```

**Note:** The provider uses `IDataSetConfiguration` to allow netstandard2.0 projects to interact with DataSets without a hard dependency on the net10.0 concrete implementation.

Example registration:

```csharp
// Infrastructure/Bootstrap/DataSetBootstrap.cs
public IGenericResult RegisterProgrammaticDataSets()
{
    var config = new DataSetConfiguration
    {
        DataSetName = "CustomerMetrics_Bootstrap",
        RecordTypeName = "ReferenceSolution.Domain.Models.Customer, ReferenceSolution.Domain",
        Fields =
        [
            new DataFieldConfiguration { Name = "Id", TypeName = "System.Int32", IsKey = true },
            new DataFieldConfiguration { Name = "Name", TypeName = "System.String" }
        ],
        SurrogateKeyFields = ["Id"],  // Auto-generated primary key
        NaturalKeyFields = [],        // Business identifier (if any)
        Sources = new Dictionary<string, SourceMappingConfiguration>
        {
            ["Primary"] = new SourceMappingConfiguration
            {
                ConnectionName = "OrdersDb",
                ConnectionType = "MsSql",
                DataStoreName = "OrdersStore",
                Sql = new SqlMappingConfiguration { TableName = "Customers" }
            }
        }
    };

    _dataSetProvider.RegisterDataSet("CustomerMetrics_Bootstrap", config);
    return GenericResult.Success();
}
```

### 4. TUI/API (Manual)

Use the Management TUI (`fdw tui`) or POST to `/api/admin/datasets`.

## Querying DataSets

All configuration methods use identical query code. See [05-01-DataGateway-Pattern.md](05-01-DataGateway-Pattern.md) for detailed examples.

From [`QueryCommand.cs:59-72`](../../src/FractalDataWorks.Commands.Data/Commands/QueryCommand.cs#L59-L72):

```csharp
public sealed class QueryCommand<T> : DataCommandBase<IEnumerable<T>>, IQueryCommand
{
    public QueryCommand(string containerName)
        : base("Query", containerName)
    {
    }

    public IFilterExpression? Filter { get; init; }
    public IProjectionExpression? Projection { get; init; }
    public IOrderingExpression? Ordering { get; init; }
    public IPagingExpression? Paging { get; init; }
}
```

Example query execution:

```csharp
var query = new QueryCommand<Customer>("CustomerMetrics")
{
    ConnectionName = "OrdersDb",
    Filter = new FilterExpression
    {
        Root = new FilterCondition
        {
            PropertyName = "IsActive",
            Operator = FilterOperators.Equal,
            Value = true
        }
    }
};

var result = await _dataGateway.Execute<IEnumerable<Customer>>(query, ct);
```

## Calculated Fields

Calculated fields execute after the database query, in-memory.

From [`CalculatedDataField.cs:10-60`](../../src/FractalDataWorks.Data.DataSets.Abstractions/CalculatedDataField.cs#L10-L60):

```csharp
public sealed class CalculatedDataField : IDataField
{
    public CalculatedDataField(
        string name,
        Type type,
        Func<IDataRow, object> calculator,
        string? description = null)
    {
        Name = name;
        FieldType = type;
        Calculator = calculator;
        Description = description;
    }

    public string Name { get; }
    public Type FieldType { get; }
    public bool IsKey => false;
    public bool IsRequired => false;
    public string? Description { get; }
    public bool IsCalculated => true;
    public Func<IDataRow, object>? Calculator { get; }
}
```

Example usage:

```csharp
new CalculatedDataField(
    "RiskLevel",
    typeof(string),
    row =>
    {
        var creditLimit = row.GetValue<decimal>("CreditLimit");
        var balance = row.GetValue<decimal>("Balance");
        var utilization = creditLimit > 0 ? balance / creditLimit : 0;
        return utilization > 0.8m ? "High" : utilization > 0.5m ? "Medium" : "Low";
    },
    "Credit utilization risk level")
```

**Important:** Calculated fields are only supported with TypeCollection configuration because they require C# lambda functions that cannot be serialized to SQL or JSON.

## DataSet Tables (cfg schema)

The configuration database includes these tables for SQL-configured datasets:

| Table | Purpose |
|-------|---------|
| `cfg.DataSet` | DataSet header (name, description, record type) |
| `cfg.DataSetField` | Field definitions with PropertyRoles |
| `cfg.DataSetSurrogateKeyField` | Surrogate key field names (auto-generated keys) |
| `cfg.DataSetNaturalKeyField` | Natural key field names (business identifiers) |
| `cfg.DataSetSource` | Source mappings (connection, priority, cost) |
| `cfg.DataSetFieldMapping` | Logical ↔ physical field name mappings |

## See Also

- [DataGateway Pattern](05-01-DataGateway-Pattern.md) - Data command execution and routing
- [TypeCollections Overview](04-01-Overview.md) - Compile-time type collections
- [Reference Solution](../../samples/ReferenceSolution/README.md) - Working implementation examples
