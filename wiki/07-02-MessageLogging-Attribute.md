# MessageLogging Attribute

The `[MessageLogging]` attribute defines structured log messages that return `IGenericMessage`.

## Attribute Parameters

From [`MessageLoggingAttribute.cs:82-116`](../src/FractalDataWorks.MessageLogging.Abstractions/MessageLoggingAttribute.cs#L82-L116):

| Parameter | Type | Description |
|-----------|------|-------------|
| `EventId` | int | Unique event identifier (default: -1) |
| `EventName` | string? | Optional event name |
| `Level` | LogLevel | Log level (Debug, Information, Warning, Error, Critical) |
| `Message` | string | Message template with placeholders |
| `SkipEnabledCheck` | bool | Skip log level enabled check |
| `Severity` | MessageSeverity | Severity for IGenericMessage |
| `AutoMapSeverity` | bool | Auto-map severity from LogLevel (default: true) |

## Examples

### Basic Usage (Debug Level)

From Reference Solution [`OrderLog.cs:31-37`](../samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging/Logging/OrderLog.cs#L31-L37):

```csharp
[MessageLogging(
    EventId = 8001,
    Level = LogLevel.Debug,
    Message = "Starting order processing for order '{orderId}'")]
public static partial IGenericMessage ProcessingStarted(
    ILogger logger,
    string orderId);
```

### With Multiple Parameters

From Reference Solution [`OrderLog.cs:57-64`](../samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging/Logging/OrderLog.cs#L57-L64):

```csharp
[MessageLogging(
    EventId = 8010,
    Level = LogLevel.Information,
    Message = "Order '{orderId}' created successfully for customer '{customerId}'")]
public static partial IGenericMessage OrderCreated(
    ILogger logger,
    string orderId,
    string customerId);
```

### With Exception

From Reference Solution [`OrderLog.cs:165-172`](../samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging/Logging/OrderLog.cs#L165-L172):

```csharp
[MessageLogging(
    EventId = 8034,
    Level = LogLevel.Error,
    Message = "Order '{orderId}': Unexpected error during processing")]
public static partial IGenericMessage ProcessingException(
    ILogger logger,
    Exception exception,
    string orderId);
```

Note: The `Exception` parameter is placed immediately after `ILogger` for proper exception logging by the source generator.

### Error Level (Specific Failure Modes)

From Reference Solution [`OrderLog.cs:137-145`](../samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging/Logging/OrderLog.cs#L137-L145):

```csharp
[MessageLogging(
    EventId = 8032,
    Level = LogLevel.Error,
    Message = "Order '{orderId}': Payment declined - {declineReason}")]
public static partial IGenericMessage PaymentDeclined(
    ILogger logger,
    string orderId,
    string declineReason);
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
