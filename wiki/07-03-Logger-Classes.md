# Logger Classes

Logger classes are static partial classes that group related log messages.

## Structure

From Reference Solution [`OrderLog.cs:1-48`](../samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging/Logging/OrderLog.cs#L1-L48):

```csharp
using System;
using Microsoft.Extensions.Logging;
using FractalDataWorks.Messages;
using FractalDataWorks.MessageLogging;

namespace Reference.MessageLogging.Logging;

/// <summary>
/// MessageLogging class for order processing operations.
/// Demonstrates the FDW pattern: every log message returns IGenericMessage.
/// </summary>
/// <remarks>
/// <para>
/// Key Principles:
/// </para>
/// <list type="bullet">
/// <item>Every failure mode has an explicit method</item>
/// <item>Every log message is returned in the result</item>
/// <item>NO generic error methods - all messages are specific</item>
/// </list>
/// </remarks>
public static partial class OrderLog
{
    // =========================================================================
    // DEBUG: Detailed internal operations
    // =========================================================================

    /// <summary>
    /// Logs when order processing begins.
    /// </summary>
    [MessageLogging(
        EventId = 8001,
        Level = LogLevel.Debug,
        Message = "Starting order processing for order '{orderId}'")]
    public static partial IGenericMessage ProcessingStarted(
        ILogger logger,
        string orderId);

    /// <summary>
    /// Logs when order validation completes.
    /// </summary>
    [MessageLogging(
        EventId = 8002,
        Level = LogLevel.Debug,
        Message = "Order '{orderId}' validation complete")]
    public static partial IGenericMessage ValidationComplete(
        ILogger logger,
        string orderId);

    // ... additional methods organized by log level
}
```

## Key Requirements

1. **Static partial class** - Required for source generation
2. **ILogger parameter** - First parameter must be `ILogger`
3. **Return IGenericMessage** - Return type must be `IGenericMessage`
4. **Partial method** - Method must be declared partial

## Naming Conventions

| Pattern | Example |
|---------|---------|
| Class | `{Domain}Log` or `{Domain}Logger` |
| Method (action) | `{Action}{Entity}` (e.g., `ProcessingStarted`) |
| Method (result) | `{Entity}{State}` (e.g., `OrderNotFound`) |
| Method (event) | `{Entity}{Event}` (e.g., `OrderCreated`) |

Note: The codebase uses both `*Log` and `*Logger` suffixes. The Reference Solution uses `OrderLog`.

## File Organization

From Reference Solution structure:

```
Reference.MessageLogging/
|-- Logging/
    |-- OrderLog.cs
```

From framework source (`FractalDataWorks.Services.Connections.Abstractions`):

```
Logging/
|-- ConnectionLogger.cs
```

## Next Steps

- [IGenericMessage](07-04-IGenericMessage.md) - Understanding the message interface
- [Result Integration](07-05-Result-Integration.md) - Using messages with GenericResult
