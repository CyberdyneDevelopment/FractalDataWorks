# IGenericMessage

`IGenericMessage` is the interface returned by all `[MessageLogging]` methods.

## Interface Definition

From [`IGenericMessage.cs:8-27`](../src/FractalDataWorks.Abstractions/IGenericMessage.cs#L8-L27):

```csharp
public interface IGenericMessage
{
    /// <summary>
    /// Gets the message text.
    /// </summary>
    /// <value>The human-readable message text describing the condition or status.</value>
    string Message { get; }

    /// <summary>
    /// Gets the message code or identifier.
    /// </summary>
    /// <value>A unique identifier for this type of message, useful for programmatic handling.</value>
    string? Code { get; }

    /// <summary>
    /// Gets the source component or operation that generated the message.
    /// </summary>
    /// <value>The name or identifier of the source that generated this message.</value>
    string? Source { get; }
}
```

A generic variant with strongly typed severity is also available:

From [`IGenericMessage.cs:29-40`](../src/FractalDataWorks.Abstractions/IGenericMessage.cs#L29-L40):

```csharp
public interface IGenericMessage<TSeverity> : IGenericMessage where TSeverity : Enum
{
    /// <summary>
    /// Gets the severity level of the message.
    /// </summary>
    /// <value>The severity level indicating the importance and impact of the message.</value>
    TSeverity Severity { get; }
}
```

## Properties

| Property | Description |
|----------|-------------|
| `Message` | The formatted message text |
| `Code` | Event ID as string (e.g., "1002") |
| `Source` | Logger class name |

## Usage

### Defining MessageLogging Methods

From Reference Solution [`OrderLog.cs:114-120`](../samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging/Logging/OrderLog.cs#L114-L120):

```csharp
/// <summary>
/// Logs when an order is not found.
/// </summary>
[MessageLogging(
    EventId = 8030,
    Level = LogLevel.Error,
    Message = "Order '{orderId}' not found")]
public static partial IGenericMessage OrderNotFound(
    ILogger logger,
    string orderId);
```

### Logging and Returning in Result

From Reference Solution [`OrderService.cs:115-126`](../samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging/Services/OrderService.cs#L115-L126):

```csharp
/// <summary>
/// Gets an order by ID, demonstrating "not found" error pattern.
/// </summary>
public IGenericResult<Order> GetOrder(string orderId)
{
    // Simulate order not found
    if (orderId.StartsWith("NOTFOUND"))
    {
        // Log AND return failure - the message is both logged and returned
        return GenericResult<Order>.Failure(
            OrderLog.OrderNotFound(_logger, orderId));
    }

    return GenericResult<Order>.Success(new Order { OrderId = orderId, Status = "Found" });
}
```

### Accessing Message Properties

From Reference Solution [`Program.cs:64-68`](../samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging/Program.cs#L64-L68):

```csharp
var result4 = orderService.GetOrder("NOTFOUND123");
Console.WriteLine($"   Success: {result4.IsSuccess}");
Console.WriteLine($"   Error: {result4.Messages[0].Message}");
```

## Next Steps

- [Result Integration](07-05-Result-Integration.md) - Using messages with GenericResult
