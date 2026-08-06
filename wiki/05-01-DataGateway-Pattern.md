# DataGateway Pattern

## Overview

The IDataGateway pattern routes data commands to appropriate connections without requiring repository or service layer wrappers. API endpoints inject IDataGateway directly and execute data commands (Query, Insert, Update, Delete) in-place.

## Key Principles

1. **Direct Injection** - API endpoints inject `IDataGateway` directly
2. **No Service Wrappers** - Do NOT create domain services that wrap IDataGateway
3. **Command-Based** - Use concrete data commands (`QueryCommand<T>`, `InsertCommand<T>`, etc.)
4. **DataStore/Path/Container Routing** - Commands specify DataStoreName, PathName, and ContainerName
5. **Railway-Oriented** - All operations return `IGenericResult<T>` for consistent error handling

## DataStore/Path/Container Hierarchy

Every data command requires three location parameters:

| Parameter | Description | Example |
|-----------|-------------|---------|
| **DataStoreName** | The DataStore (connection/database) | `"ConfigurationDb"`, `"NflStats"` |
| **PathName** | The DataPath within the DataStore (schema) | `"conn"`, `"dbo"`, `"auth"` |
| **ContainerName** | The DataContainer (table/view) | `"Customers"`, `"Users"` |

```csharp
// Fluent API (preferred)
var command = Query.From<Customer>("OrdersDb", "dbo", "Customers")
    .Where(c => c.Name).Equal("Acme")
    .Build();

// Or for Insert/Update/Delete
var command = Insert.Into<Customer>("Customers")
    .DataStore("OrdersDb")
    .Path("dbo")
    .Value(customer);
```

## The IDataGateway Interface

From [`IDataGateway.cs:9-24`](../src/Fdw.Services.Data.Abstractions/IDataGateway.cs#L9-L24):

```csharp
/// <summary>
/// Service that routes data commands to the appropriate connection.
/// The DataGateway selects the correct DataStore based on the command's DataStoreName
/// and delegates execution to that connection.
/// </summary>
public interface IDataGateway
{
    /// <summary>
    /// Executes a data command by routing it to the appropriate connection.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="command">The data command containing DataStoreName, PathName, and ContainerName.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result.</returns>
    Task<IGenericResult<T>> Execute<T>(IDataCommand command, CancellationToken cancellationToken = default);
}
```

## Service Registration

DataGateway is registered automatically by `DefaultDataGatewayServiceType` (a `ServiceTypeOption`) through the framework's ServiceTypeCollection three-phase registration — there is **no** standalone `services.AddDataGateway()` call. See [Creating a Server](12-01-Creating-A-Server.md).

```csharp
// DefaultDataGatewayServiceType.RegisterRequiredServices(...) wires this up:
//
//   Scoped (per request):
//     services.AddScoped<IDataGateway, LimitEnforcementDataGateway>();
//       → decorates DataGatewayService  (caching is built-in, no separate decorator)
//
//   Singleton cache store (cross-request cache state):
//     services.AddSingleton<DataGatewayResultCache>();   // owns IMemoryCache + tag sidecar
//
// Caching is built directly into DataGatewayService, not layered as a separate decorator.
// Cache keys are tenant/org-discriminated. DataGatewayService holds a reference to the
// singleton DataGatewayResultCache; cross-request state never leaks between scopes.
// API endpoints inject IDataGateway directly — no service wrappers.
```

## Data Commands

FractalDataWorks provides four core data commands that work across all connection types:

### QueryCommand<T>

Retrieves data (SELECT operation). Returns `IEnumerable<T>`.

From [`QueryCommand.cs:60-106`](../src/Fdw.Commands.Data/Commands/QueryCommand.cs#L60-L106):

```csharp
[TypeOption(typeof(DataCommands), "Query")]
public sealed class QueryCommand<T> : DataCommandBase<IEnumerable<T>>, IQueryCommand
{
    public QueryCommand(string containerName)
        : base("Query", containerName)
    {
    }

    /// <summary>
    /// Gets or sets the filter expression (WHERE clause).
    /// </summary>
    public IFilterExpression? Filter { get; init; }

    /// <summary>
    /// Gets or sets the projection expression (SELECT clause).
    /// </summary>
    public IProjectionExpression? Projection { get; init; }

    /// <summary>
    /// Gets or sets the ordering expression (ORDER BY clause).
    /// </summary>
    public IOrderingExpression? Ordering { get; init; }

    /// <summary>
    /// Gets or sets the paging expression (SKIP/TAKE).
    /// </summary>
    public IPagingExpression? Paging { get; init; }

    /// <summary>
    /// Gets or sets the aggregation expression (GROUP BY).
    /// </summary>
    public IAggregationExpression? Aggregation { get; init; }

    /// <summary>
    /// Gets or sets the join expressions (JOIN clauses).
    /// </summary>
    public IReadOnlyList<IJoinExpression> Joins { get; init; } = [];
}
```

### InsertCommand<T>

Inserts new records (INSERT operation). Returns the inserted entity or affected row count.

From [`InsertCommand.cs:35-47`](../src/Fdw.Commands.Data/Commands/InsertCommand.cs#L35-L47):

```csharp
[TypeOption(typeof(DataCommands), "Insert")]
public sealed class InsertCommand<T> : DataCommandBase<int, T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InsertCommand{T}"/> class.
    /// </summary>
    /// <param name="containerName">The name of the container to insert into.</param>
    /// <param name="data">The entity to insert.</param>
    public InsertCommand(string containerName, T data)
        : base("Insert", containerName, data)
    {
    }
}
```

### UpdateCommand<T>

Updates existing records (UPDATE operation). Returns affected row count.

From [`UpdateCommand.cs:47-65`](../src/Fdw.Commands.Data/Commands/UpdateCommand.cs#L47-L65):

```csharp
[TypeOption(typeof(DataCommands), "Update")]
public sealed class UpdateCommand<T> : DataCommandBase<int, T>, IFilterableCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCommand{T}"/> class.
    /// </summary>
    /// <param name="containerName">The name of the container to update.</param>
    /// <param name="data">The updated entity data.</param>
    public UpdateCommand(string containerName, T data)
        : base("Update", containerName, data)
    {
    }

    /// <summary>
    /// Gets or sets the filter expression (WHERE clause for update).
    /// Determines which records to update.
    /// </summary>
    public IFilterExpression? Filter { get; init; }
}
```

### DeleteCommand

Deletes records (DELETE operation). Returns affected row count.

From [`DeleteCommand.cs:45-62`](../src/Fdw.Commands.Data/Commands/DeleteCommand.cs#L45-L62):

```csharp
[TypeOption(typeof(DataCommands), "Delete")]
public sealed class DeleteCommand : DataCommandBase<int>, IFilterableCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteCommand"/> class.
    /// </summary>
    /// <param name="containerName">The name of the container to delete from.</param>
    public DeleteCommand(string containerName)
        : base("Delete", containerName)
    {
    }

    /// <summary>
    /// Gets or sets the filter expression (WHERE clause for delete).
    /// Determines which records to delete.
    /// </summary>
    public IFilterExpression? Filter { get; init; }
}
```

## Pattern: Direct DataGateway Injection in Endpoints

When using IDataGateway in endpoints:

1. Inject `IDataGateway` via constructor
2. Create a data command using fluent builders with `DataStore`, `Path`, and `Container`
3. Execute via `_dataGateway.Execute<T>(command, ct)`
4. Handle `IGenericResult<T>` (check `IsFailure`, access `Value`)

## Example: Query with Filter

From Reference.DataLayer [`Program.cs:141-168`](../samples/ReferenceSolution/concepts/06-data-layer/src/Reference.DataLayer/Program.cs#L141-L168):

```csharp
var directQuery = new QueryCommand<CustomerDto>("dbo.Customers")
{
    Filter = new FilterExpression
    {
        Root = new FilterCondition
        {
            PropertyName = "IsActive",
            Operator = FilterOperators.Equal,
            Value = true
        }
    },
    Ordering = new OrderingExpression
    {
        OrderedFields =
        [
            new OrderedField
            {
                PropertyName = "CustomerName",
                Direction = SortDirections.Ascending
            }
        ]
    },
    Paging = new PagingExpression
    {
        Skip = 0,
        Take = 50
    }
};
```

## Example: Filter Conditions

From Reference.DataLayer [`Program.cs:66-91`](../samples/ReferenceSolution/concepts/06-data-layer/src/Reference.DataLayer/Program.cs#L66-L91):

```csharp
// Simple equality condition
var nameFilter = new FilterCondition
{
    PropertyName = "CustomerName",
    Operator = FilterOperators.Equal,
    Value = "Acme Corp"
};

// Contains (LIKE) condition - operator knows its own SQL!
var searchFilter = new FilterCondition
{
    PropertyName = "Description",
    Operator = FilterOperators.Contains,
    Value = "widget"
};

// Null check (no value needed)
var nullCheck = new FilterCondition
{
    PropertyName = "DeletedAt",
    Operator = FilterOperators.IsNull
    // No Value - IsNull doesn't need one (RequiresValue = false)
};
```

## Example: Insert Operation

From Reference.DataLayer [`Program.cs:190-199`](../samples/ReferenceSolution/concepts/06-data-layer/src/Reference.DataLayer/Program.cs#L190-L199):

```csharp
// Insert - data is passed to constructor
var newCustomer = new CustomerDto
{
    Id = 0,
    CustomerName = "New Customer",
    Email = "new@example.com",
    IsActive = true,
    CreatedDate = DateTime.UtcNow
};
var insertCommand = new InsertCommand<CustomerDto>("dbo.Customers", newCustomer);
```

## Example: Update Operation

From Reference.DataLayer [`Program.cs:201-222`](../samples/ReferenceSolution/concepts/06-data-layer/src/Reference.DataLayer/Program.cs#L201-L222):

```csharp
// Update - data passed to constructor, filter via init
var updatedCustomer = new CustomerDto
{
    Id = 1,
    CustomerName = "Updated Customer",
    Email = "updated@example.com",
    IsActive = true,
    CreatedDate = DateTime.UtcNow
};
var updateCommand = new UpdateCommand<CustomerDto>("dbo.Customers", updatedCustomer)
{
    Filter = new FilterExpression
    {
        Root = new FilterCondition
        {
            PropertyName = "Id",
            Operator = FilterOperators.Equal,
            Value = 1
        }
    }
};
```

## Example: Delete Operation

From Reference.DataLayer [`Program.cs:224-236`](../samples/ReferenceSolution/concepts/06-data-layer/src/Reference.DataLayer/Program.cs#L224-L236):

```csharp
// Delete
var deleteCommand = new DeleteCommand("dbo.Customers")
{
    Filter = new FilterExpression
    {
        Root = new FilterCondition
        {
            PropertyName = "IsActive",
            Operator = FilterOperators.Equal,
            Value = false
        }
    }
};
```

## Example: Using QueryCommandBuilder (Fluent API)

From Reference.DataLayer [`Program.cs:121-126`](../samples/ReferenceSolution/concepts/06-data-layer/src/Reference.DataLayer/Program.cs#L121-L126):

```csharp
var query = new QueryCommandBuilder<CustomerDto>("dbo.Customers")
    .Where("IsActive", FilterOperators.Equal, true)
    .Where("Status", FilterOperators.NotEqual, "Deleted")
    .OrderBy("CustomerName")
    .Paging(skip: 0, take: 100)
    .Build();
```

## Anti-Pattern: Service Layer Wrappers

**DO NOT DO THIS:**

```csharp
// WRONG - Don't create service wrappers for IDataGateway
public interface ICustomerService
{
    Task<IGenericResult<Customer>> GetCustomerAsync(int id);
    Task<IGenericResult<IEnumerable<Customer>>> ListCustomersAsync();
    Task<IGenericResult<Customer>> CreateCustomerAsync(Customer customer);
}

public class CustomerService : ICustomerService
{
    private readonly IDataGateway _dataGateway;

    public CustomerService(IDataGateway dataGateway)
    {
        _dataGateway = dataGateway;
    }

    // This is unnecessary abstraction - inject IDataGateway directly in endpoints!
}
```

**Why This Is Wrong:**

1. Adds unnecessary abstraction layer
2. Hides the actual data operations being performed
3. Creates more code to maintain
4. Reduces visibility into what data is accessed
5. Makes testing more complex (now need to mock the service too)

**Correct Approach:**

Inject `IDataGateway` directly in endpoints and create data commands in-place. This makes data operations explicit and testable.

## Result Handling Pattern

All data commands return `IGenericResult<T>`. Always check for failure before accessing the value.

### Database Failures → 500 Internal Server Error

When `result.IsSuccess` is `false`, this indicates a **database or infrastructure failure** (connection error, timeout, SQL error). Return HTTP 500 with error details:

```csharp
var result = await _dataGateway.Execute<IEnumerable<Team>>(command, ct);

if (!result.IsSuccess)
{
    // The failed result carries a structured message — use it directly, never a `?? "fallback"`.
    NflLog.TeamsFetchFailed(_logger, result.CurrentMessage);  // Log AND return
    HttpContext.Response.StatusCode = 500;
    await HttpContext.Response.WriteAsJsonAsync(
        new ApiErrorResponse { Error = "Failed to fetch teams", Details = result.CurrentMessage }, ct);
    return;
}
```

### Empty Results → 200 OK with Empty Array

When query succeeds but returns no data, return 200 OK with empty collection (for list endpoints):

```csharp
var players = new List<PlayerStat>(result.Value ?? []);
await Send.OkAsync(players, ct);  // Returns [] if no data
```

### Single Item Not Found → 404 Not Found

When querying for a specific item that doesn't exist:

```csharp
var team = result.Value?.FirstOrDefault();
if (team == null)
{
    NflLog.TeamNotFound(_logger, req.TeamId);
    await Send.NotFoundAsync(ct);
    return;
}
await Send.OkAsync(team, ct);
```

### Error Response DTO

Use a consistent error response format:

```csharp
public sealed class ApiErrorResponse
{
    public required string Error { get; set; }
    public string? Details { get; set; }
}
```

### Complete Pattern Summary

| Condition | HTTP Status | Response |
|-----------|-------------|----------|
| `!result.IsSuccess` (DB error) | 500 | `ApiErrorResponse` with error details |
| `result.Value` is empty list | 200 | Empty array `[]` |
| `result.Value?.FirstOrDefault()` is null | 404 | Empty response |
| Success with data | 200 | The data |

## DataStore/Path/Container Resolution

Every data command must specify DataStoreName, PathName, and ContainerName using the fluent builders:

```csharp
// Query - all three in From() call
var command = Query.From<Customer>("OrdersDb", "dbo", "Customers")
    .Where(c => c.IsActive).Equal(true)
    .Build();

// Insert/Update/Delete - fluent methods
var command = Insert.Into<Customer>("Customers")
    .DataStore("OrdersDb")
    .Path("dbo")
    .Value(customer);
```

The DataGateway:
1. Receives the command
2. Reads the `DataStoreName` property
3. Resolves the DataStore from `IDataStoreProvider`
4. Uses `PathName` to locate the schema within the DataStore
5. Uses `ContainerName` to locate the specific table/view
6. Routes the command to the appropriate connection for execution

## Best Practices

### 1. Inject IDataGateway, Not Domain Services

```csharp
// CORRECT
public MyEndpoint(IDataGateway dataGateway)
{
    _dataGateway = dataGateway;
}

// WRONG
public MyEndpoint(ICustomerService customerService)
{
    _customerService = customerService;
}
```

### 2. Create Commands In-Place Using Fluent Builders

```csharp
// CORRECT - command creation is explicit and visible using fluent API
var command = Query.From<Customer>("OrdersDb", "dbo", "Customers")
    .Where(c => c.IsActive).Equal(true)
    .Build();

var result = await _dataGateway.Execute<IEnumerable<Customer>>(command, ct);

// WRONG - hiding the command behind a service method
var result = await _customerService.GetActiveCustomersAsync();
```

### 3. Always Handle Result Failures

```csharp
// CORRECT - check IsFailure before accessing Value
var result = await _dataGateway.Execute<IEnumerable<Customer>>(command, ct);
if (result.IsFailure)
{
    // Handle error
    return;
}

// WRONG - assuming success
var customers = result.Value; // Could throw if result failed
```

### 4. Use Strongly-Typed Commands

```csharp
// CORRECT - type-safe, IntelliSense support
var result = await _dataGateway.Execute<IEnumerable<Customer>>(command, ct);
var customers = result.Value; // IEnumerable<Customer>, no casting

// WRONG - dynamic/weakly typed
var result = await _dataGateway.ExecuteDynamic(command, ct);
var customers = (List<Customer>)result.Value; // Requires casting, runtime errors
```

### 5. Specify DataStore/Path/Container Explicitly

```csharp
// CORRECT - explicit DataStore, Path, and Container
var command = Query.From<Customer>("OrdersDb", "dbo", "Customers").Build();

// For mutations - use fluent methods
var command = Insert.Into<Customer>("Customers")
    .DataStore("OrdersDb")
    .Path("dbo")
    .Value(customer);

// WRONG - missing required parameters
var command = Query.From<Customer>("Customers").Build();  // Compile error - requires 3 parameters
```

## Testing

When testing code that uses IDataGateway, mock the gateway and verify commands:

```csharp
[Fact]
public async Task ExecuteQueryCommandReturnsExpectedResult()
{
    // Arrange
    var mockGateway = new Mock<IDataGateway>();
    var expectedCustomer = new CustomerDto { Id = 1, CustomerName = "Test" };

    mockGateway
        .Setup(g => g.Execute<IEnumerable<CustomerDto>>(
            It.Is<QueryCommand<CustomerDto>>(cmd =>
                cmd.DataStoreName == "OrdersDb" &&
                cmd.PathName == "dbo" &&
                cmd.ContainerName == "Customers"),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(GenericResult<IEnumerable<CustomerDto>>.Success([expectedCustomer]));

    // Act - use fluent builder
    var command = Query.From<CustomerDto>("OrdersDb", "dbo", "Customers")
        .Where(c => c.Id).Equal(1)
        .Build();
    var result = await mockGateway.Object.Execute<IEnumerable<CustomerDto>>(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldNotBeNull();
    result.Value.First().CustomerName.ShouldBe("Test");
}
```

## When to Use Domain Services

Use domain services for:

- **Business logic** - Calculations, validations, domain rules
- **Orchestration** - Coordinating multiple operations
- **Domain events** - Publishing events based on domain state changes
- **Complex workflows** - Multi-step processes with business rules

Do NOT use domain services for:

- **Simple data access** - Use IDataGateway directly
- **CRUD operations** - Commands handle this
- **Wrapping IDataGateway** - This adds no value

## Summary

The DataGateway pattern in FractalDataWorks provides:

1. **Direct data access** - No unnecessary abstraction layers
2. **Command-based operations** - Explicit, type-safe data commands
3. **DataStore routing** - Automatic routing by DataStoreName/PathName/ContainerName
4. **Railway-Oriented** - Consistent error handling with IGenericResult
5. **Testability** - Mock IDataGateway, verify commands sent

Follow the Reference Solution examples for correct implementation patterns.

## Next Steps

- [DataSets](05-02-DataSets.md) - Logical data abstraction layer
- [TypeCollections Overview](04-01-Overview.md) - Type-safe lookup collections
- [Reference Solution Data Layer](../samples/ReferenceSolution/concepts/06-data-layer/) - Working examples
