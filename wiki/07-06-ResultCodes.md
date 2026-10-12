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
    LogLevel LogLevel { get; }        // Microsoft.Extensions.Logging.LogLevel derived from Severity
    string Domain { get; }            // e.g., "MsSql"
    string MessageTemplate { get; }   // e.g., "Connection to {server} timed out"
    bool IsRetryable { get; }         // true for transient failures

    string FormatMessage(IResultDetails? details = null);
    void Log(ILogger logger, IResultDetails? details = null);
    IResultCode LogAndReturn(ILogger logger, IResultDetails? details = null);
}
```

(See [`IResultCode.cs`](../src/Fdw.Results.Abstractions/IResultCode.cs) for the full interface including XML docs.)

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

From [`DataServiceResultCodes.cs`](../src/Fdw.Services.Data/Results/DataServiceResultCodes.cs):

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
| Code | `{PREFIX}-{number}` | `MSSQL-21000` |
| Class | `{Name}Code` suffix | `ConnectionTimeoutCode` |

### Number Allocation

**The number IS the identity.** `Id == EventId ==` the numeric part of `Code` (`"{PREFIX}-{number}"`).
The prefix says which package the code belongs to; the leading digit says how the condition is handled.

`Category = Id / 10000`. The closed `ResultCategories` TypeCollection carries the behaviour
(`RangeBase` / `IsFailure` / `IsRetryable` / `HttpStatus`) on each option — dispatch off the option,
never off a magic-number range check at a call site.

| Category | Band | Meaning | Handling |
|----------|------|---------|----------|
| 1 | 10000–19999 | Non-error outcomes — Success, Informational, Cancelled | continue |
| 2 | 20000–29999 | Validation / BadInput | 400, do not retry |
| 3 | 30000–39999 | NotFound / Missing | 404 |
| 4 | 40000–49999 | Conflict / State | 409 |
| 5 | 50000–59999 | Auth (N & Z) | 401 / 403 |
| 6 | 60000–69999 | Configuration / Setup | fail fast at boot |
| 7 | 70000–79999 | Dependency / Connection / IO | 502 / 503, often retry |
| 8 | 80000–89999 | Timeout / Transient | retry with backoff |
| 9 | 90000–99999 | Internal / Unexpected | 500, alert |

`IsFailure => Id >= 20000` — category 1 is the non-failure band, so Success **and** Cancelled correctly
read as non-failures.

**The NUMBER is not unique — `{PREFIX}-{number}` is.** The same number appears under many prefixes:
`FDW-10001`, `CON-10001` and `SQLCON-10001` are all valid and all distinct codes. What must hold is that
the number carries the **same general meaning under every prefix** — if `10001` means "informational" for
one package it means the same kind of thing for all of them. The prefix says *whose* it is; the number
says *what kind* it is.

Numbers are **progressive within a category**: allocate the next one in the band as the meaning gets more
specific, so related conditions stay adjacent and a reader can tell roughly what a code is from its number
alone.

**Therefore: allocate by meaning, not by availability.** The order is:

1. **Search for a related or similar existing code.** If one already represents this meaning — a canonical,
   or the number another package uses for the same kind of condition — **reuse that number** under your
   own prefix.
2. **If nothing existing represents it** — the meaning is genuinely new, or the closest numbers mean
   something else — **take a free number** in the correct category band, and pick it progressively
   (adjacent to the related codes it belongs with).

The mistake to avoid is starting at step 2: asking "what number is free?" without first asking "what does
this mean, and does that meaning already have a number?" That yields a code whose number contradicts its
meaning, and breaks the property that a number means the same kind of thing under every prefix.

Reserved canonicals (`C0000–C0999`) — use one of these before inventing anything:

| Number | Name | Use |
|--------|------|-----|
| 10000 | Succeeded | the `Success()` default |
| 10010 | Cancelled | caller/token-initiated; Warning severity, non-failure |
| 20000 | RequiredValueMissing | a required value was not provided |
| 20001 | InvalidInput | a value is present but invalid |
| 20002 | ValidationFailed | multi-field validation failure |
| 30000 | NotFound | every not-found condition, all domains |
| 40000 | InvalidState | operation invalid in the current state |
| 40001 | AlreadyExists | duplicate natural key |
| 40002 | VersionConflict | stale version on a version-on-write save |
| 50000 | AuthenticationFailed | |
| 50001 | PermissionDenied | |

A code with no canonical goes in the open band (`C1000–C9999`) of the category it belongs to — e.g. a
setup failure specific to one package lands at `61000+`.

> Authoritative source: **`RESULTCODE-CATALOG.md`** at the repository root. The older per-domain EventId
> bands (`5000-5099 Core Connections`, `5200-5299 MsSql Connections`, …) and the underscore `Code` format
> are **RETIRED** — `EVENTID-ALLOCATION.md` and the dev-guide allocation table were archived under
> `docs/_archive/` by FDW-608. Do not allocate from them.

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
  <ProjectReference Include="..\Fdw.Results\Fdw.Results.csproj" />
  <ProjectReference Include="..\Fdw.Collections\Fdw.Collections.csproj" />
</ItemGroup>
```

## Next Steps

- [Result Integration](07-05-Result-Integration.md) - MessageLogging with Results
- [TypeCollections Overview](04-01-Overview.md) - Understanding the TypeCollection pattern
- [MessageLogging Overview](07-01-Overview.md) - Structured logging
