# DataSets

DataSets provide a logical abstraction over physical data sources, supporting:
- **Field mappings** (logical to physical names)
- **Calculated fields** (post-query computations)
- **Multi-source routing** (priority-based selection)
- **Predicate pushdown** (filter optimization)

## Configuration Methods

FractalDataWorks supports three methods for configuring DataSets:

### 1. TypeCollection (Compile-Time)

Best for: Strongly-typed datasets with calculated fields.

The `DataSetTypes` collection uses `[MutableTypeCollection]` to allow runtime registration.

From [`DataSetTypes.cs:12-15`](../src/Fdw.Data.DataSets.Abstractions/DataSetTypes.cs#L12-L15):

```csharp
[MutableTypeCollection(typeof(DataSetTypeBase), typeof(IDataSetType), typeof(DataSetTypes))]
public abstract partial class DataSetTypes : TypeCollectionBase<DataSetTypeBase, IDataSetType>
{
}
```

Dataset implementations extend `DataSetTypeBase` and use the `[TypeOption]` attribute:

From [`DataSetTypeBase.cs:29-41`](../src/Fdw.Data.DataSets.Abstractions/DataSetTypeBase.cs#L29-L41) (constructor signature):

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

DataSet entities live under the `data` schema in ConfigurationDb (`data.DataSet` parent + child tables `data.DataSetField`, `data.DataSetKeyField`, `data.DataSetSource`, `data.DataSetFieldMapping`, `data.DataSetJoin`, `data.DataSetNote`, `data.DataSetFieldNote`). The parent row carries `Id` (logical identity), `Name`, `ServiceOptionType`, `Version`, `Category`, `RecordTypeName`, version/audit columns, and tenant/visibility scoping.

Example SQL seed (mirror the idempotent pattern from `databases/ConfigurationDb/seed/*`):

```sql
DECLARE @DataSetId UNIQUEIDENTIFIER = NEWID();

INSERT INTO data.DataSet (Id, Name, ServiceOptionType, Description, Version, Category, RecordTypeName)
VALUES (@DataSetId, 'CustomerMetrics_Seed', 'Default',
        'Customer data configured via SQL', '1.0', 'CRM',
        'ReferenceSolution.Domain.Models.Customer, ReferenceSolution.Domain');

-- Field definitions
INSERT INTO data.DataSetField (Id, DataSetId, Name, TypeName, IsRequired, Ordinal)
VALUES
    (NEWID(), @DataSetId, 'Id',   'System.Int32',  1, 0),
    (NEWID(), @DataSetId, 'Name', 'System.String', 1, 1);

-- Key fields (data.DataSetKeyField — IsKey is derived from this child table)
INSERT INTO data.DataSetKeyField (Id, DataSetId, FieldName, Ordinal, KeyType)
VALUES (NEWID(), @DataSetId, 'Id', 0, 'Surrogate');

-- Source binding (data.DataSetSource — variant-specific columns depend on the source type)
INSERT INTO data.DataSetSource (Id, DataSetId, SourceName, DataStoreName, PathName, Priority)
VALUES (NEWID(), @DataSetId, 'Primary', 'OrdersDb', 'dbo', 1);
```

See `databases/ConfigurationDb/data/Tables/data.DataSet*.sql` for the authoritative column lists.

### 3. Programmatic (Runtime)

Best for: Dynamic/tenant-specific configuration.

#### DataSetConfiguration Class

The concrete `DataSetConfiguration` class lives in the `Fdw.Data.DataSets` package (net10.0) and applies the `[ManagedConfiguration]` pattern. The aggregate is exposed via the concrete class — there is no `IDataSetConfiguration` interface in the abstractions package.

From [`DataSetConfiguration.cs`](../src/Fdw.Data.DataSets/DataSetConfiguration.cs):

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

From [`IDataSetProvider.cs`](../src/Fdw.Services.Data.Abstractions/IDataSetProvider.cs):

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
                DataStoreName = "OrdersDb",
                PathName = "dbo",
                ConnectionType = "MsSql",
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

From [`QueryCommand.cs:59-72`](../src/Fdw.Commands.Data/Commands/QueryCommand.cs#L59-L72):

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
// Use fluent API with DataStore, Path, and Container
var query = Query.From<Customer>("OrdersDb", "dbo", "CustomerMetrics")
    .Where(c => c.IsActive).Equal(true)
    .Build();

var result = await _dataGateway.Execute<IEnumerable<Customer>>(query, ct);
```

## Calculated Fields

Calculated fields execute after the database query, in-memory.

From [`CalculatedDataField.cs:10-60`](../src/Fdw.Data.DataSets.Abstractions/CalculatedDataField.cs#L10-L60):

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

## DataSet Tables (data schema)

DataSets live in ConfigurationDb's `data` schema:

| Table | Purpose |
|-------|---------|
| `data.DataSet` | DataSet header (name, description, record type) |
| `data.DataSetField` | Field definitions with PropertyRoles |
| `data.DataSetSurrogateKeyField` | Surrogate key field names (auto-generated keys) |
| `data.DataSetNaturalKeyField` | Natural key field names (business identifiers) |
| `data.DataSetSource` | Source mappings (connection, priority) |
| `data.DataSetFieldMapping` | Logical ↔ physical field name mappings |

DataSet is a top-level named configuration, so writes go through the DataSet domain provider's `Save()`/`Delete()` (version-on-write + tag-based cache invalidation) — there is no `IConfigurationWriter<T>`. The generic admin UI path is `IDynamicConfigurationWriter`; child records use `ConfigurationSaveCommand<T>` via the DataGateway.

## DataSet Source Mapper TypeCollection

DataSet source mappings are resolved via the **DataSet Source Mapper TypeCollection**
(`DataSetSourceMapperTypes`). Each mapper type (e.g., `XPath`, `Direct`, `Calculated`) is a
`TypeOption` registered by its owning package via `Registration.SourceGenerators` module
initialisers. New source mappings reference the mapper type name via
`DataSetSourceMapperTypes.ByName(mapperTypeName)`.

```csharp
// O(1) lookup by mapper type name
var mapper = DataSetSourceMapperTypes.ByName("XPath");
if (mapper == DataSetSourceMapperTypes.NotFound)
{
    // handle unknown mapper type — do not throw
    DataSetLog.UnknownMapperType(_logger, mapperTypeName);
    return GenericResult.Failure(...);
}
```

See the [DataSet Source Mapper client guide](../docs/CLIENT-GUIDE-DATASET-MAPPERS.md) for migration details.

## See Also

- [DataGateway Pattern](05-01-DataGateway-Pattern.md) - Data command execution and routing
- [TypeCollections Overview](04-01-Overview.md) - Compile-time type collections
- [Reference Solution](../samples/ReferenceSolution/README.md) - Working implementation examples
