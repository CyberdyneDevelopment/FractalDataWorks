# ResultCodes

Structured, type-safe error codes that replace string-based `Failure("message")` calls with domain-specific, queryable result codes.

## Overview

ResultCodes are TypeCollections that define error conditions with:
- **Unique EventId** - Correlates with logging infrastructure
- **Severity** - Error, Warning, Critical, etc.
- **Domain** - Which subsystem the error belongs to
- **Message Template** - Human-readable message with placeholders
- **Retryable Flag** - Whether the operation can be retried

## Why ResultCodes?

### The Problem

String-based failures are:
- Not queryable or analyzable
- Inconsistent across the codebase
- Missing metadata (severity, retryability)
- Hard to localize or customize

```csharp
// BAD: String literal - no metadata, not queryable
return GenericResult.Failure("Connection failed: timeout");

// BAD: Even with MessageLogging, no structured error code
return GenericResult.Failure(ConnectionLog.Failed(_logger, "timeout"));
```

### The Solution

ResultCodes provide structured error handling:

```csharp
// GOOD: Structured result code with full metadata
return GenericResult.Failure(
    MsSqlResultCodes.ByName("ConnectionTimeout"),
    ResultDetails.Create("server", serverName, "timeout", timeoutMs));
```

## Core Types

### IResultCode

```csharp
public interface IResultCode : ITypeOption<int, ResultCodeBase>
{
    string Code { get; }              // e.g., "MSSQL_CONN_TIMEOUT"
    int EventId { get; }              // e.g., 5201
    IResultSeverity Severity { get; } // Error, Warning, etc.
    string Domain { get; }            // e.g., "MsSql"
    string MessageTemplate { get; }   // e.g., "Connection to {server} timed out"
    bool IsRetryable { get; }         // true for transient failures

    string FormatMessage(IResultDetails? details = null);
    void Log(ILogger logger, IResultDetails? details = null);
    IResultCode LogAndReturn(ILogger logger, IResultDetails? details = null);
}
```

### IResultSeverity

Pre-defined severity levels that map to `Microsoft.Extensions.Logging.LogLevel`:

| Severity | LogLevel | IsSuccess | Description |
|----------|----------|-----------|-------------|
| Trace | Trace (0) | true | Detailed diagnostic |
| Debug | Debug (1) | true | Debugging info |
| Information | Information (2) | true | Operational info |
| Warning | Warning (3) | true | Potential issues |
| Error | Error (4) | false | Operation failed |
| Critical | Critical (5) | false | System-level failure |

```csharp
// Access via TypeCollection
var errorSeverity = ResultSeverities.ByName("Error");
var isFailure = errorSeverity.IsFailure;  // true
```

### ResultDetails

Pooled key-value container for contextual information:

```csharp
// Create with fluent API
var details = ResultDetails.Create()
    .With("server", serverName)
    .With("timeout", timeoutMs)
    .With("retryCount", 3);

// Or with factory overloads
var details = ResultDetails.Create("server", serverName);
var details = ResultDetails.Create("key1", value1, "key2", value2);

// Access values
var server = details.GetValue<string>("server");

// Pool returns automatically (IDisposable)
using var details = ResultDetails.Create("error", ex.Message);
```

## Creating Domain ResultCodes

### File Structure

```
Services.{Domain}/
└── Results/
    └── {Domain}ResultCodes.cs
```

### Four Required Components

From [`DataServiceResultCodes.cs`](../src/FractalDataWorks.Services.Data/Results/DataServiceResultCodes.cs):

```csharp
// 1. Interface (optional but recommended)
public interface IDataServiceResultCode : IResultCode
{
}

// 2. Base Class
[ExcludeFromCodeCoverage]
public abstract class DataServiceResultCodeBase : ResultCodeBase
{
    protected DataServiceResultCodeBase() { }  // For Empty sentinel

    protected DataServiceResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "Data", messageTemplate, isRetryable)
    {
    }
}

// 3. TypeCollection
/// <summary>
/// TypeCollection for Data Service result codes.
/// EventId range: 5650-5699
/// </summary>
[TypeCollection(typeof(DataServiceResultCodeBase), typeof(IResultCode), typeof(DataServiceResultCodes))]
public abstract partial class DataServiceResultCodes
    : TypeCollectionBase<DataServiceResultCodeBase, IResultCode>
{
}

// 4. TypeOptions (individual result codes)
[TypeOption(typeof(DataServiceResultCodes), "DataStoreNameRequired")]
[ExcludeFromCodeCoverage]
public sealed class DataStoreNameRequiredCode : DataServiceResultCodeBase
{
    public DataStoreNameRequiredCode()
        : base(
            id: 1,
            name: "DataStoreNameRequired",
            code: "DATA_STORE_NAME_REQUIRED",
            eventId: 5650,
            severity: ResultSeverities.ByName("Error"),
            messageTemplate: "DataStore name cannot be null or empty",
            isRetryable: false)
    {
    }
}
```

### Naming Conventions

| Property | Convention | Example |
|----------|------------|---------|
| Name | PascalCase, descriptive | `ConnectionTimeout` |
| Code | SCREAMING_SNAKE_CASE | `MSSQL_CONN_TIMEOUT` |
| Class | `{Name}Code` suffix | `ConnectionTimeoutCode` |

### EventId Allocation

Claim a range in your domain and document it in the class XML docs:

```csharp
/// <summary>
/// TypeCollection for MsSql result codes.
/// EventId range: 5200-5299
/// </summary>
```

| Range | Domain |
|-------|--------|
| 5000-5099 | Core Connections |
| 5100-5199 | HTTP Connections |
| 5200-5299 | MsSql Connections |
| 5300-5399 | Authentication |
| 5400-5499 | Secret Managers |
| 5500-5599 | Pipelines |
| 5600-5649 | REST DataStores |
| 5650-5699 | Data Services |
| 5700-5799 | Workflows |
| 6000-6099 | CodeBuilder |
| 6100-6199 | Schema Importers |
| 6200-6299 | SqlServer DataStores |
| 7000-7099 | Calculations |
| 8000-8099 | ServiceTypes |
| 8100-8199 | ETL |
| 8200-8299 | Scheduling |
| 8300-8399 | UI Rendering |
| 9000-9099 | Roslyn Workspace |
| 9100-9199 | Roslyn Commands |

## Usage Patterns

### Basic Usage

```csharp
public IGenericResult<DataStore> GetDataStore(string name)
{
    if (string.IsNullOrEmpty(name))
    {
        return GenericResult<DataStore>.Failure(
            DataServiceResultCodes.ByName("DataStoreNameRequired"));
    }

    // ... implementation
}
```

### With ResultDetails

```csharp
public IGenericResult<Connection> Connect(string server, int timeout)
{
    try
    {
        // ... connection attempt
    }
    catch (TimeoutException ex)
    {
        return GenericResult<Connection>.Failure(
            MsSqlResultCodes.ByName("ConnectionTimeout"),
            ResultDetails.Create("server", server, "timeout", timeout));
    }
}
```

### Log AND Return Pattern

```csharp
public IGenericResult<Order> ProcessOrder(string orderId)
{
    if (!IsValid(orderId))
    {
        // Log immediately AND return the result code
        return GenericResult<Order>.Failure(
            OrderResultCodes.ByName("InvalidOrderId")
                .LogAndReturn(_logger, ResultDetails.Create("orderId", orderId)));
    }

    // ... implementation
}
```

### Checking Retryability

```csharp
var result = await connection.Execute(command);
if (!result.IsSuccess)
{
    var resultCode = result.ResultCode;
    if (resultCode?.IsRetryable == true && retryCount < maxRetries)
    {
        await Task.Delay(backoffMs);
        return await Execute(command, retryCount + 1);
    }
}
```

## ResultCodes vs MessageLogging

Both patterns integrate with `GenericResult.Failure()` but serve different purposes:

| Aspect | MessageLogging | ResultCodes |
|--------|---------------|-------------|
| **Primary Purpose** | Structured logging with compile-time validation | Structured error classification |
| **When to Use** | Operational events, diagnostics | Domain-specific error conditions |
| **Queryability** | By EventId in logs | By Code in application logic |
| **Retryability** | Not tracked | `IsRetryable` property |
| **Severity** | Via LogLevel | Via `IResultSeverity` |
| **Details** | Method parameters | `ResultDetails` dictionary |

### Choose MessageLogging When

- Logging operational events (start, complete, metrics)
- Need compile-time parameter validation
- Focus is on observability/diagnostics

### Choose ResultCodes When

- Defining domain error conditions
- Need to check retryability
- Building error catalogs for documentation
- API error responses need stable codes

### Combined Usage

For critical failures, you may want both:

```csharp
// Log with full context via MessageLogging
var message = ConnectionLog.Failed(_logger, ex, serverName, timeout);

// Return with structured code for programmatic handling
return GenericResult<Connection>.Failure(
    MsSqlResultCodes.ByName("ConnectionFailed"),
    ResultDetails.Create("server", serverName));
```

## Testing ResultCodes

ResultCodes are TypeOptions, so they don't need individual unit tests. Test the TypeCollection behavior:

```csharp
[Fact]
public void ByNameReturnsExpectedResultCode()
{
    var code = DataServiceResultCodes.ByName("DataStoreNameRequired");

    code.ShouldNotBeNull();
    code.EventId.ShouldBe(5650);
    code.Severity.Name.ShouldBe("Error");
    code.IsRetryable.ShouldBeFalse();
}

[Fact]
public void NotFoundReturnsEmptySentinel()
{
    var code = DataServiceResultCodes.ByName("NonExistent");

    code.ShouldNotBeNull();
    code.Name.ShouldBe("NotFound");
}

[Fact]
public void FormatMessageReplacesPlaceholders()
{
    var code = MsSqlResultCodes.ByName("ConnectionTimeout");
    var details = ResultDetails.Create("server", "db.example.com", "timeout", 30000);

    var message = code.FormatMessage(details);

    message.ShouldContain("db.example.com");
    message.ShouldContain("30000");
}
```

## Project References

Add these references to use ResultCodes:

```xml
<ItemGroup>
  <ProjectReference Include="..\FractalDataWorks.Results\FractalDataWorks.Results.csproj" />
  <ProjectReference Include="..\FractalDataWorks.Collections\FractalDataWorks.Collections.csproj" />
</ItemGroup>
```

## Next Steps

- [Result Integration](07-05-Result-Integration.md) - MessageLogging with Results
- [TypeCollections Overview](04-01-Overview.md) - Understanding the TypeCollection pattern
- [MessageLogging Overview](07-01-Overview.md) - Structured logging
