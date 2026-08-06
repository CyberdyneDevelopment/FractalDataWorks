# External Exception Handling

TypeCollection-based error dispatch for external systems. Each error code maps to a structured handler with known cause, resolution, and retryability -- no more generic catch blocks.

## Problem Statement

External systems -- databases, APIs, message queues -- throw exceptions with error codes. SQL Server error 229 means permission denied. Error 18456 means login failed. Error 208 means invalid object name. Each code has a known cause and a known resolution.

Yet most catch blocks treat all errors the same way:

```csharp
// WRONG: Every SQL error gets the same treatment
catch (SqlException ex)
{
    _logger.LogError(ex, "SQL error occurred");
    return GenericResult.Failure("Database operation failed");
}
```

This loses information. The caller cannot tell whether the error is retryable (deadlock) or permanent (permission denied). The log entry lacks structure. The UI shows a useless message.

## The Pattern

A TypeCollection of error handlers where each handler maps one or more error codes to a structured failure message. The `ByErrorNumber()` lookup dispatches to the correct handler in O(1). Unrecognized codes fall through to the NotFound sentinel, which captures full context for investigation.

```
SqlException (error 229)
    --> SqlErrorHandlers.ByErrorNumber(229)
    --> PermissionDeniedHandler
    --> MsSqlConnectionLogger.PermissionDenied(logger, ex, commandText)
    --> IGenericMessage (logged + returned in IGenericResult)
```

### Why TypeCollections

- **Zero reflection** -- all handlers registered at compile time via source generator
- **O(1) dispatch** -- lazy dictionary maps error numbers to handlers
- **Open for extension** -- add a handler by creating one file with a `[TypeOption]` attribute
- **Closed for modification** -- existing handlers and the dispatch mechanism never change

## SQL Server Reference Implementation

File structure in `src/Fdw.Services.Connections.MsSql/ErrorHandlers/`:

```
ErrorHandlers/
    ISqlErrorHandler.cs          # Interface
    SqlErrorHandlerBase.cs       # CRTP base class
    SqlErrorHandlers.cs          # TypeCollection with ByErrorNumber()
    PermissionDeniedHandler.cs   # TypeOption (error 229)
```

### Interface

From [`ISqlErrorHandler.cs`](../src/Fdw.Services.Connections.MsSql/ErrorHandlers/ISqlErrorHandler.cs):

```csharp
public interface ISqlErrorHandler : ITypeOption<int, SqlErrorHandlerBase>
{
    IReadOnlyList<int> SqlErrorNumbers { get; }
    bool IsRetryable { get; }
    IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText);
}
```

Key design choices:
- `SqlErrorNumbers` returns a list because multiple error numbers can map to the same handler (e.g., -1, 2, 53 all indicate connection failure)
- `IsRetryable` enables callers to implement retry logic without inspecting the error
- `CreateFailureMessage` logs AND returns -- the Catch/Log/Return pattern

### Base Class (CRTP)

From [`SqlErrorHandlerBase.cs`](../src/Fdw.Services.Connections.MsSql/ErrorHandlers/SqlErrorHandlerBase.cs):

```csharp
[ExcludeFromCodeCoverage]
public abstract class SqlErrorHandlerBase : TypeOptionBase<int, SqlErrorHandlerBase>, ISqlErrorHandler
{
    protected SqlErrorHandlerBase() { }  // For Empty sentinel
    protected SqlErrorHandlerBase(int id, string name) : base(id, name) { }

    public abstract IReadOnlyList<int> SqlErrorNumbers { get; }
    public abstract bool IsRetryable { get; }
    public abstract IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText);
}
```

The parameterless constructor exists for the source-generated Empty/NotFound sentinel.

### TypeCollection with Lookup

From [`SqlErrorHandlers.cs`](../src/Fdw.Services.Connections.MsSql/ErrorHandlers/SqlErrorHandlers.cs):

```csharp
[TypeCollection(typeof(SqlErrorHandlerBase), typeof(ISqlErrorHandler), typeof(SqlErrorHandlers))]
public abstract partial class SqlErrorHandlers : TypeCollectionBase<SqlErrorHandlerBase, ISqlErrorHandler>
{
    private static Dictionary<int, ISqlErrorHandler>? _errorNumberMap;

    public static ISqlErrorHandler ByErrorNumber(int sqlErrorNumber)
    {
        if (_errorNumberMap is null)
        {
            var map = new Dictionary<int, ISqlErrorHandler>();
            foreach (var handler in All())
            {
                foreach (var errorNumber in handler.SqlErrorNumbers)
                {
                    map.TryAdd(errorNumber, handler);
                }
            }

            _errorNumberMap = map;
        }

        return _errorNumberMap.GetValueOrDefault(sqlErrorNumber) ?? NotFound;
    }
}
```

The lazy dictionary is built once on first use from the source-generated `All()` method. Every handler's `SqlErrorNumbers` are flattened into a single lookup. Unrecognized error numbers return the `NotFound` sentinel, which captures full context for diagnosis. This is the same lazy-initialization pattern used by other TypeCollection lookups (see [Generated Lookups](04-05-Generated-Lookups.md)).

### Example Handler: PermissionDenied

From [`PermissionDeniedHandler.cs`](../src/Fdw.Services.Connections.MsSql/ErrorHandlers/PermissionDeniedHandler.cs):

```csharp
[TypeOption(typeof(SqlErrorHandlers), "PermissionDenied")]
[ExcludeFromCodeCoverage]
public sealed class PermissionDeniedHandler : SqlErrorHandlerBase
{
    public PermissionDeniedHandler() : base(1, "PermissionDenied") { }

    public override IReadOnlyList<int> SqlErrorNumbers => [229];
    public override bool IsRetryable => false;

    public override IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText)
        => MsSqlConnectionLogger.PermissionDenied(logger, ex, commandText);
}
```

Four lines of meaningful code. The handler delegates to a MessageLogging method which both logs the error with full context and returns a structured `IGenericMessage` for the result pipeline.

## Source Generator Support: Partial NotFound Sentinel

The TypeCollection source generator creates a NotFound sentinel as a `partial` class. This allows you to declare your own partial class that overrides abstract members with custom behavior -- for example, capturing full context when an unrecognized error code is encountered.

For `SqlErrorHandlers`, the generated NotFound sentinel provides default empty implementations. You can extend it:

```csharp
// In your project -- partial matches the generated NotFound class
public partial class SqlErrorHandlersNotFound
{
    public override IReadOnlyList<int> SqlErrorNumbers => [];
    public override bool IsRetryable => false;

    public override IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText)
        => MsSqlConnectionLogger.UnknownSqlError(logger, ex, commandText);
}
```

This captures full diagnostic context (command text, exception details, stack trace) for unknown error codes, making it straightforward to identify new error conditions and create handlers for them.

## Adding New Error Handlers

Create one `.cs` file in the `ErrorHandlers/` directory:

```csharp
[TypeOption(typeof(SqlErrorHandlers), "DeadlockVictim")]
[ExcludeFromCodeCoverage]
public sealed class DeadlockVictimHandler : SqlErrorHandlerBase
{
    public DeadlockVictimHandler() : base(2, "DeadlockVictim") { }

    public override IReadOnlyList<int> SqlErrorNumbers => [1205];
    public override bool IsRetryable => true;

    public override IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText)
        => MsSqlConnectionLogger.DeadlockDetected(logger, ex, commandText);
}
```

That is all. The `[TypeOption]` attribute registers the handler with the source generator. On the next build, `SqlErrorHandlers.All()` includes it and `ByErrorNumber(1205)` returns it. No factory changes, no switch statements, no DI registration.

## Error Response Pipeline

Errors flow through a structured pipeline from external exception to UI display:

```
SqlException (error 229)
  --> SqlErrorHandlers.ByErrorNumber(229)           // TypeCollection dispatch
  --> PermissionDeniedHandler.CreateFailureMessage() // Log + structured message
  --> IGenericMessage                                // Returned to caller
  --> IGenericResult (Failure)                       // Wrapped in result
  --> SendErrorResponse()                            // Endpoint maps to HTTP
  --> ErrorResponse DTO                              // Safe for client
  --> UI display                                     // User sees actionable message
```

### Dispatch in the Connection Layer

When a SQL command fails, the connection implementation catches the `SqlException` and dispatches on `ex.Number` via the TypeCollection. Because `ByErrorNumber()` returns the `NotFound` sentinel for unrecognized error numbers, no null check is needed:

```csharp
catch (SqlException ex)
{
    MsSqlConnectionLogger.SqlExecutionError(Logger, command.CommandText, ex.Message, ex.Number);

    var handler = SqlErrorHandlers.ByErrorNumber(ex.Number);
    return GenericResult<T>.Failure(handler.CreateFailureMessage(_logger, ex, command.CommandText));
}
```

### Endpoint Error Response

The CRUD endpoint base classes in `src/Fdw.Web.RestEndpoints/Crud/` handle failed results by mapping them to HTTP responses via `ResultHttpStatusMapper`.

From [`CrudGetEndpoint.cs`](../src/Fdw.Web.RestEndpoints/Crud/CrudGetEndpoint.cs):

```csharp
protected virtual async Task SendErrorResponse(IGenericResult result, CancellationToken ct)
{
    var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
    HttpContext.Response.StatusCode = statusCode;
    await HttpContext.Response.WriteAsJsonAsync(errorResponse, ct).ConfigureAwait(false);
}
```

`ResultHttpStatusMapper` maps result codes to HTTP status codes (403 for permission denied, 503 for login/connection failed, 409 for deadlock, 504 for timeout, 500 default) and constructs user-safe `ErrorResponse` DTOs with the `ReferenceId` set to the request's `TraceIdentifier`.

## Security Model

The error handling pipeline enforces strict information separation between server-side logging and client-side responses.

### Server-Side: Log Everything

The MessageLogging method in `CreateFailureMessage()` logs with full diagnostic context:

- SQL command text that failed
- SQL Server error number and message
- Connection details (server, database, login)
- Full exception with stack trace
- Correlation ID from the request

This goes to structured logging (Seq/Application Insights) and is queryable by EventId.

### Client-Side: Return Only Safe Messages

The `ErrorResponse` DTO in `src/Fdw.Web.RestEndpoints/Models/ErrorResponse.cs` contains only information safe for the client:

```csharp
public sealed class ErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ReferenceId { get; set; }
    public bool IsRetryable { get; set; }
    public string? Action { get; set; }
    public string? ActionUrl { get; set; }
}
```

### What Never Leaves the Server

| Data | Server Log | Client Response |
|------|-----------|-----------------|
| SQL command text | Yes | Never |
| Error number | Yes | Never |
| Stack trace | Yes | Never |
| Server address | Yes | Never |
| Database name | Yes | Never |
| Login/username | Yes | Never |
| Safe message | Yes | Yes |
| Error code | Detailed | Category only (e.g., MSSQL_PERMISSION_DENIED) |
| Reference ID | Yes | Yes (for support correlation) |
| IsRetryable | Yes | Yes |
| Suggested action | N/A | Yes (e.g., "Contact your administrator") |

The `ReferenceId` bridges the gap -- support staff can correlate the client's reference ID to the full server-side log entry.

## API Endpoint Usage

The CRUD endpoint bases in `src/Fdw.Web.RestEndpoints/Crud/` call `SendErrorResponse()` when an operation returns a failure result. The flow in `HandleAsync`:

```csharp
var result = await FindByIdentifier(req, ct).ConfigureAwait(false);

if (!result.IsSuccess)
{
    await SendErrorResponse(result, ct).ConfigureAwait(false);
    return;
}
```

The base `SendErrorResponse` handles most cases via `ResultHttpStatusMapper`. Override only when you need domain-specific behavior beyond the default mappings:

```csharp
protected override async Task SendErrorResponse(IGenericResult result, CancellationToken ct)
{
    var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);

    // Add domain-specific action URL for permission denied
    if (statusCode == 403)
    {
        errorResponse.ActionUrl = $"/access-requests/new?resource={ResourceName}";
    }

    HttpContext.Response.StatusCode = statusCode;
    await HttpContext.Response.WriteAsJsonAsync(errorResponse, ct).ConfigureAwait(false);
}
```

## UI Error Display

UI components receive the `ErrorResponse` DTO from API calls and render appropriate feedback. The pattern:

1. API client catches non-success HTTP status codes
2. Deserializes the response body as `ErrorResponse`
3. Passes to a display component that renders severity-based styling

```csharp
// In a Blazor provider/service
var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
if (!response.IsSuccessStatusCode)
{
    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(ct).ConfigureAwait(false);
    // Surface to UI via state management
}
```

The UI renders different visual treatments based on the status code:
- **403** -- warning severity, "Contact your administrator" action
- **503** -- info severity, "Retry in a moment" action with automatic retry
- **500** -- error severity, "Contact support" with `ReferenceId` for support correlation

## Extending to Other Systems

The same pattern applies to any external system with error codes. To add PostgreSQL error handling:

1. Create `ISqlErrorHandler` equivalent with `SqlState` instead of error numbers
2. Create the CRTP base class
3. Create the TypeCollection with `BySqlState()` lookup
4. Add handlers for known SQLSTATE codes (e.g., `42501` for insufficient privilege)

The TypeCollection infrastructure, source generation, and result pipeline are reused unchanged. Only the handler interface and lookup method change to match the external system's error identification mechanism.

## Next Steps

- [ResultCodes](07-06-ResultCodes.md) - Structured error codes (complementary pattern)
- [Result Integration](07-05-Result-Integration.md) - MessageLogging with Results
- [TypeCollections Overview](04-01-Overview.md) - Understanding TypeCollections
- [Dispatcher Pattern](04-06-Dispatcher-Pattern.md) - Type-safe dispatch without reflection
- [API Endpoints](12-07-API-Endpoints.md) - Endpoint architecture
