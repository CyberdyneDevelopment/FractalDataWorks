# TypeCollections with Behavior: Dispatcher Pattern

## Overview

TypeCollections can contain **behavior** (methods), not just data. When TypeOptions implement methods with **identical signatures**, you can dispatch to any option polymorphically and let **C# overload resolution** pick the correct implementation based on input type.

This eliminates reflection, provides compile-time type safety, and enables extensible plugin architectures.

## TypeOptions Contain Behavior

**Traditional TypeCollections** (data only):
```csharp
[TypeOption(typeof(OrderStatuses), "Pending")]
public sealed class PendingStatus : OrderStatusBase
{
    public PendingStatus() : base(1, "Pending", isTerminal: false) { }
    // Just data properties - no methods
}
```

**Behavioral TypeCollections** (data + methods):
```csharp
[TypeOption(typeof(MsSqlDataCommandTranslators), "Query")]
public sealed class MsSqlQueryTranslator : MsSqlDataCommandTranslatorBase
{
    // Common method signature (same across ALL translators)
    public override Task<IGenericResult<SqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken ct) { ... }

    // Overload for specific input type
    public Task<IGenericResult<SqlCommand>> Translate(
        IQueryCommand command,
        IStorageContainer container,
        CancellationToken ct) { ... }
}
```

## The Key Principle

**All TypeOptions share identical method signatures (same name, same parameters, same return type):**

```
TypeCollection: ConnectionTypes
├─ MsSql
│  └─ Execute(IDataCommand, CancellationToken) → IGenericResult
├─ PostgreSql
│  └─ Execute(IDataCommand, CancellationToken) → IGenericResult
└─ Oracle
   └─ Execute(IDataCommand, CancellationToken) → IGenericResult

TypeCollection: MsSqlDataCommandTranslators
├─ Query
│  └─ Translate(IDataCommand, IStorageContainer, CancellationToken) → IGenericResult<SqlCommand>
├─ Insert
│  └─ Translate(IDataCommand, IStorageContainer, CancellationToken) → IGenericResult<SqlCommand>
└─ Update
   └─ Translate(IDataCommand, IStorageContainer, CancellationToken) → IGenericResult<SqlCommand>
```

**CRITICAL**: Same method NAME, same parameters, same return type across ALL options.

**Why this matters**: Dispatcher can call `translator.Translate(...)` on ANY option without knowing which specific option it is. The method signature is guaranteed to exist.

## Pattern Components

1. **TypeCollection** - Discover all options at compile time
2. **TypeOptions** - Contain behavior with identical signatures
3. **Base method** - Accepts general interface (IDataCommand)
4. **Overload methods** - Accept specific interfaces (IQueryCommand, IFilterableCommand)
5. **C# overload resolution** - Compile-time dispatch to correct overload

## Pattern Visualization

```
TypeCollection: MsSqlDataCommandTranslators
├─ Query (TypeOption)
│  ├─ Translate(IDataCommand) → error
│  └─ Translate(IQueryCommand) → SELECT statement
├─ Update (TypeOption)
│  ├─ Translate(IDataCommand) → error
│  └─ Translate(IFilterableCommand) → UPDATE statement
└─ Delete (TypeOption)
   ├─ Translate(IDataCommand) → error
   └─ Translate(IFilterableCommand) → DELETE statement

All TypeOptions have SAME method signature:
  Task<IGenericResult<SqlCommand>> Translate(IDataCommand, IStorageContainer, CancellationToken)

Dispatcher calls ANY option → Overload resolution picks correct method!
```

## Example: MsSql Command Translation

### Problem: Generic Commands with Reflection

```csharp
// Reflection-based property access (SLOW, UNSAFE)
var commandType = command.GetType();
var filter = commandType.GetProperty("Filter")?.GetValue(command) as IFilterExpression;
```

### Solution: Strongly-Typed Interfaces

**Step 1: Create Interface**

From [`IQueryCommand.cs:14-45`](../src/Fdw.Commands.Data.Abstractions/Commands/IQueryCommand.cs#L14-L45):

```csharp
public interface IQueryCommand : IDataCommand
{
    /// <summary>
    /// Gets the filter expression (WHERE clause).
    /// </summary>
    IFilterExpression? Filter { get; }

    /// <summary>
    /// Gets the projection expression (SELECT clause).
    /// </summary>
    IProjectionExpression? Projection { get; }

    /// <summary>
    /// Gets the ordering expression (ORDER BY clause).
    /// </summary>
    IOrderingExpression? Ordering { get; }

    /// <summary>
    /// Gets the paging expression (SKIP/TAKE).
    /// </summary>
    IPagingExpression? Paging { get; }

    /// <summary>
    /// Gets the aggregation expression (GROUP BY).
    /// </summary>
    IAggregationExpression? Aggregation { get; }

    /// <summary>
    /// Gets the join expressions (JOIN clauses).
    /// </summary>
    IReadOnlyList<IJoinExpression> Joins { get; }
}
```

**Step 2: Implement Interface**

From [`QueryCommand.cs:59-106`](../src/Fdw.Commands.Data/Commands/QueryCommand.cs#L59-L106):

```csharp
[TypeOption(typeof(DataCommands), "Query")]
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
    public IAggregationExpression? Aggregation { get; init; }
    public IReadOnlyList<IJoinExpression> Joins { get; init; } = [];
}
```

**Step 3: Translator with Overloads**

From [`MsSqlQueryTranslator.cs:38-108`](../src/Fdw.Data.MsSql/Translators/MsSqlQueryTranslator.cs#L38-L108):

```csharp
[TypeOption(typeof(MsSqlDataCommandTranslators), "Query")]
public sealed class MsSqlQueryTranslator : MsSqlDataCommandTranslatorBase
{
    public MsSqlQueryTranslator()
        : base("Query")
    {
    }

    /// <summary>
    /// Base Translate - returns error via MessageLogging for invalid command types.
    /// Overload resolution dispatches to Translate(IQueryCommand) for valid commands.
    /// </summary>
    public override Task<IGenericResult<SqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            GenericResult<SqlCommand>.Failure(
                MsSqlTranslatorLog.InvalidCommandType(
                    NullLogger<MsSqlQueryTranslator>.Instance,
                    "MsSqlQueryTranslator",
                    "IQueryCommand",
                    command.GetType().Name)));
    }

    /// <summary>
    /// Translates an IQueryCommand to a T-SQL SELECT statement.
    /// Overload resolution ensures this method is called for IQueryCommand instances.
    /// </summary>
    public Task<IGenericResult<SqlCommand>> Translate(
        IQueryCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        // Build SELECT statement with strongly-typed properties (no reflection!)
        var sqlCommand = BuildSelectStatement(
            container,
            dbPath,
            command.Filter,
            command.Projection,
            command.Ordering,
            command.Paging);

        return Task.FromResult(GenericResult<SqlCommand>.Success(sqlCommand));
    }
}
```

**Step 4: Dispatcher Routes via TypeCollection**

The connection uses TypeCollection.ByName() for O(1) lookup - NO manual switch statement:

```csharp
// In ConnectionBase - abstract method for derived classes
protected abstract IDataCommandTranslator<TCommand> GetTranslator(string commandType);

// In MsSqlConnection - uses TypeCollection for dispatch
protected override IDataCommandTranslator<SqlCommand> GetTranslator(string commandType)
    => MsSqlDataCommandTranslators.ByName(commandType);

// Usage in Execute():
var translator = GetTranslator(command.CommandType);
var result = await translator.Translate(command, container, cancellationToken);
```

## How It Works

### Compile-Time Dispatch

When you call `_queryTranslator.Translate(command, container, ct)`:

1. **C# overload resolution** checks if `command` implements `IQueryCommand`
2. **If YES**: Calls `Translate(IQueryCommand, ...)` - the fast path
3. **If NO**: Calls `Translate(IDataCommand, ...)` - returns error via MessageLogging

**Zero runtime type inspection! All resolved at compile time.**

### CommandType Routing

Commands expose their type via property (set in constructor).

From [`DataCommandBase.cs:21-52`](../src/Fdw.Commands.Data.Abstractions/Commands/DataCommandBase.cs#L21-L52):

```csharp
public abstract class DataCommandBase : IDataCommand
{
    protected DataCommandBase(string commandType, string containerName, string category = "Data")
    {
        CommandId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        CommandType = commandType ?? throw new ArgumentNullException(nameof(commandType));
        ContainerName = containerName ?? throw new ArgumentNullException(nameof(containerName));
        Category = category ?? "Data";
        Metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    public Guid CommandId { get; }
    public DateTime CreatedAt { get; }
    public string CommandType { get; }
    public string Category { get; }
    public string ContainerName { get; }
    public string ConnectionName { get; init; } = "Default";
    public IReadOnlyDictionary<string, object> Metadata { get; init; }
}
```

**Routing uses string comparison** (fast) instead of `GetType().Name.StartsWith()` (slow reflection).

## ID Generation Pattern

Translators calculate deterministic IDs from names using **FNV-1a hash**.

From [`DataCommandTranslatorBase.cs:33-61`](../src/Fdw.Commands.Data.Abstractions/Translators/DataCommandTranslatorBase.cs#L33-L61):

```csharp
protected DataCommandTranslatorBase(string name, string domainName)
{
    Id = GenerateIdFromName(name);
    Name = name;
    DomainName = domainName;
}

/// <summary>
/// Generates a deterministic ID from a translator name using FNV-1a hash.
/// </summary>
private static int GenerateIdFromName(string name)
{
    if (string.IsNullOrEmpty(name))
        throw new ArgumentNullException(nameof(name));

    unchecked
    {
        const int FnvPrime = 0x01000193;
        const int FnvOffsetBasis = (int)0x811C9DC5;

        int hash = FnvOffsetBasis;
        foreach (char c in name)
        {
            hash ^= c;
            hash *= FnvPrime;
        }
        return hash & 0x7FFFFFFF;
    }
}
```

**No manual ID assignment** - prevents collisions, ensures consistency.

## TypeCollection Integration

Translators are discovered via TypeCollection.

From [`MsSqlDataCommandTranslators.cs:37-49`](../src/Fdw.Data.MsSql/Translators/MsSqlDataCommandTranslators.cs#L37-L49):

```csharp
[TypeCollection(typeof(MsSqlDataCommandTranslatorBase), typeof(IDataCommandTranslator<SqlCommand>), typeof(MsSqlDataCommandTranslators))]
[ExcludeFromCodeCoverage]
public abstract partial class MsSqlDataCommandTranslators :
    TypeCollectionBase<MsSqlDataCommandTranslatorBase, IDataCommandTranslator<SqlCommand>>
{
    // Source generator creates:
    // - Static constructor
    // - Static properties: Query, Insert, Update, Delete, BulkInsert, BatchInsert, CompoundQuery
    // - public static IReadOnlyList<IDataCommandTranslator> All()
    // - public static IDataCommandTranslator ByName(string name)
    // - public static IDataCommandTranslator ById(int id)
}
```

**Usage in composite translator:**
```csharp
var translator = MsSqlDataCommandTranslators.Query;
return translator.Translate(command, container, cancellationToken); // Overload resolution!
```

## Benefits

### Performance
- **Eliminates reflection overhead** - no GetType(), GetProperty(), GetValue()
- **Compile-time dispatch** - overload resolution at compile time
- **Zero boxing** - strongly-typed interfaces
- **Hot path optimization** - command execution is performance-critical

### Type Safety
- **Compile-time verification** - can't access properties that don't exist
- **No null reference exceptions** - interfaces are explicit contracts
- **IntelliSense support** - IDE knows available properties

### Maintainability
- **Self-documenting** - interfaces show command capabilities
- **Clear separation** - error path vs success path
- **Easy debugging** - no reflection magic to trace
- **Testable** - can mock interfaces

## Pattern Variations

### Error Handling with MessageLogging

Use MessageLogging (not string literals) for errors.

From [`MsSqlTranslatorLog.cs:10-24`](../src/Fdw.Data.MsSql/Logging/MsSqlTranslatorLog.cs#L10-L24):

```csharp
public static partial class MsSqlTranslatorLog
{
    /// <summary>
    /// Logs when a translator receives an invalid command type.
    /// </summary>
    [MessageLogging(
        EventId = 2001,
        Level = LogLevel.Error,
        Message = "Translator '{translatorName}' expected {expectedType} but received {actualType}")]
    public static partial IGenericMessage InvalidCommandType(
        ILogger logger,
        string translatorName,
        string expectedType,
        string actualType);
}
```

**Call with NullLogger** (no DI needed for error cases):
```csharp
MsSqlTranslatorLog.InvalidCommandType(
    NullLogger<MsSqlQueryTranslator>.Instance,
    "MsSqlQueryTranslator",
    "IQueryCommand",
    command.GetType().Name)
```

## When to Use This Pattern

✅ **Use dispatcher pattern when:**
- You have multiple implementations of a common interface (translators, handlers, services)
- You need to route based on a type/category property
- Performance matters (hot path, high throughput)
- You want compile-time type safety

❌ **Don't use when:**
- You only have 2-3 implementations (simple if/switch is fine)
- Performance isn't critical
- The overhead of interfaces outweighs benefits

## Comparison with Other Patterns

| Pattern | Dispatch Speed | Type Safety | Complexity |
|---------|---------------|-------------|------------|
| **Reflection** | Slow (runtime) | ❌ None | Low |
| **Switch on GetType()** | Medium | ❌ None | Low |
| **Switch on property** | Fast (string compare) | ⚠️ Partial | Medium |
| **Method overload** | Fastest (compile-time) | ✅ Full | Medium |
| **Visitor pattern** | Fast | ✅ Full | High |

**Method overload dispatcher** provides the best balance of performance, type safety, and simplicity.

## Implementation Files

**Command Interfaces:**
- [`IQueryCommand.cs`](../src/Fdw.Commands.Data.Abstractions/Commands/IQueryCommand.cs)
- [`IFilterableCommand.cs`](../src/Fdw.Commands.Data.Abstractions/Commands/IFilterableCommand.cs)

**Command Implementations:**
- [`QueryCommand.cs`](../src/Fdw.Commands.Data/Commands/QueryCommand.cs)
- [`UpdateCommand.cs`](../src/Fdw.Commands.Data/Commands/UpdateCommand.cs)
- [`DeleteCommand.cs`](../src/Fdw.Commands.Data/Commands/DeleteCommand.cs)

**Translators:**
- [`MsSqlQueryTranslator.cs`](../src/Fdw.Data.MsSql/Translators/MsSqlQueryTranslator.cs)
- [`MsSqlUpdateTranslator.cs`](../src/Fdw.Data.MsSql/Translators/MsSqlUpdateTranslator.cs)
- [`MsSqlDeleteTranslator.cs`](../src/Fdw.Data.MsSql/Translators/MsSqlDeleteTranslator.cs)

**Dispatcher (TypeCollection):**
- [`MsSqlDataCommandTranslators.cs`](../src/Fdw.Data.MsSql/Translators/MsSqlDataCommandTranslators.cs) - source-generated

**MessageLogging:**
- [`MsSqlTranslatorLog.cs`](../src/Fdw.Data.MsSql/Logging/MsSqlTranslatorLog.cs)

## Related Documentation

- [TypeCollections Overview](04-01-Overview.md) - Compile-time type discovery
- [MessageLogging Overview](07-01-Overview.md) - Structured logging with IGenericMessage
