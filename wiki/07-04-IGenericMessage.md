# IGenericMessage

`IGenericMessage` is the interface returned by all `[MessageLogging]` methods.

## Interface Definition

From [`IGenericMessage.cs:8-27`](../src/Fdw.Abstractions/IGenericMessage.cs#L8-L27):

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

From [`IGenericMessage.cs:29-40`](../src/Fdw.Abstractions/IGenericMessage.cs#L29-L40):

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
| `Code` | TypeCode-EventId string (e.g., `"FDW-8001"`) |
| `Source` | Logger class name |

The `Code` property uses the format `"{TypeCode}-{EventId}"` where TypeCode defaults to `"FDW"` and can be customized via the `TypeCode` property on `[MessageLogging]`. See [MessageLogging Attribute](07-02-MessageLogging-Attribute.md#typecode) for details.

## Usage

### Defining MessageLogging Methods

From Reference Solution [`OrderLog.cs:127-134`](../samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging/Logging/OrderLog.cs#L127-L134):

```csharp
[MessageLogging(
    EventId = 8033,
    Level = LogLevel.Error,
    Message = "Order '{orderId}' not found")]
public static partial IGenericMessage OrderNotFound(
    ILogger logger,
    string orderId);
```

### Logging and Returning in Result

From Reference Solution [`OrderService.cs`](../samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging/Services/OrderService.cs):

```csharp
public IGenericResult<Order> GetOrder(string orderId)
{
    if (orderId == "NOT_FOUND")
    {
        return GenericResult<Order>.Failure(
            OrderLog.OrderNotFound(_logger, orderId));
    }

    var order = new Order(orderId, "customer-1", "product-1", 1);
    return GenericResult<Order>.Success(order);
}
```

### Accessing Message Properties

```csharp
var result = orderService.GetOrder("NOT_FOUND");
Console.WriteLine($"   Success: {result.IsSuccess}");
Console.WriteLine($"   Error: {result.Messages[0].Message}");
```

## Next Steps

- [Result Integration](07-05-Result-Integration.md) - Using messages with GenericResult
