# MessageLogging Attribute

The `[MessageLogging]` attribute defines structured log messages that return `IGenericMessage`.

## Attribute Parameters

From [`MessageLoggingAttribute.cs:82-116`](../src/Fdw.MessageLogging.Abstractions/MessageLoggingAttribute.cs#L82-L116):

| Parameter | Type | Description |
|-----------|------|-------------|
| `EventId` | int | Unique event identifier (default: -1) |
| `EventName` | string? | Optional event name |
| `Level` | LogLevel | Log level (Debug, Information, Warning, Error, Critical) |
| `Message` | string | Message template with placeholders |
| `SkipEnabledCheck` | bool | Skip log level enabled check |
| `Severity` | MessageSeverity | Severity for IGenericMessage |
| `AutoMapSeverity` | bool | Auto-map severity from LogLevel (default: true) |
| `TypeCode` | char[]? | Type code prefix for message code (default: `{ 'F', 'D', 'W' }`) |

### TypeCode

The `TypeCode` property controls the prefix in the generated message code. The source generator produces a `Code` value in the format `"{TypeCode}-{EventId}"` (e.g., `"FDW-8101"`).

- Defaults to `FDW` when not specified
- Must be 2-6 uppercase alphanumeric characters
- Invalid values emit diagnostic `SYSLIB1028`

```csharp
// Default TypeCode (FDW):
[MessageLogging(EventId = 8001, Level = LogLevel.Error, Message = "Operation failed")]
public static partial IGenericMessage OperationFailed(ILogger logger);
// Generated Code: "FDW-8001"

// Custom TypeCode:
[MessageLogging(EventId = 1001, Level = LogLevel.Error, Message = "API error",
    TypeCode = new[] { 'A', 'P', 'I' })]
public static partial IGenericMessage ApiError(ILogger logger);
// Generated Code: "API-1001"
```

## Examples

### Basic Usage (Debug Level)

From [`ConnectionProviderLogger.cs`](../src/Fdw.Services.Connections/Logging/ConnectionProviderLogger.cs):

```csharp
[MessageLogging(
    EventId = 7101,
    Level = LogLevel.Debug,
    Message = "Looking up connection '{name}'")]
public static partial IGenericMessage LookingUpConnection(
    ILogger logger,
    string name);
```

### With Multiple Parameters

Example (pattern; full file lives in the separate `reference-api` repository):

```csharp
[MessageLogging(
    EventId = 10001,
    Level = LogLevel.Information,
    Message = "Starting {serviceName} version {version}")]
public static partial IGenericMessage ServiceStarting(
    ILogger logger,
    string serviceName,
    string version);
```

### With Exception

Example (pattern; full file lives in the separate `reference-api` repository):

```csharp
[MessageLogging(
    EventId = 7010,
    Level = LogLevel.Error,
    Message = "Authentication failed for user '{username}'")]
public static partial IGenericMessage AuthenticationFailed(
    ILogger logger,
    Exception ex,
    string username);
```

Note: The `Exception` parameter is placed immediately after `ILogger` for proper exception logging by the source generator.

### Error Level (Specific Failure Modes)

From [`ConnectionProviderLogger.cs`](../src/Fdw.Services.Connections/Logging/ConnectionProviderLogger.cs):

```csharp
[MessageLogging(
    EventId = 7103,
    Level = LogLevel.Error,
    Message = "Connection '{name}' not found")]
public static partial IGenericMessage ConnectionNotFound(
    ILogger logger,
    string name);
```

## Event ID Conventions

Organize event IDs by domain. Example from the Reference Solution:

| Range | Domain |
|-------|--------|
| 8000-8099 | Order Processing |
| 5000-5999 | Connections |
| 6000-6999 | Secret Managers |
| 7000-7999 | Authentication |

## Next Steps

- [Logger Classes](07-03-Logger-Classes.md) - Organizing logger classes
- [IGenericMessage](07-04-IGenericMessage.md) - Understanding the message interface
- [Result Integration](07-05-Result-Integration.md) - Using messages with Results
