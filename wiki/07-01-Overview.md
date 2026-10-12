# MessageLogging Overview

FractalDataWorks uses structured logging with the `[MessageLogging]` attribute. Every log message is defined as a partial method that:

1. **Logs via ILogger** - Standard Microsoft.Extensions.Logging
2. **Returns IGenericMessage** - For result integration

## Key Benefits

- **Zero string literals** - All messages defined via attributes
- **Source-generated** - No reflection at runtime
- **Type-safe parameters** - Compile-time validation
- **Result integration** - Messages can be returned in `IGenericResult`
- **Structured message codes** - Generated `Code` in `"{TypeCode}-{EventId}"` format (e.g., `"FDW-8033"`)

## Quick Example

From [`OrderLog.cs:127-134`](../samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging/Logging/OrderLog.cs#L127-L134):

```csharp
[MessageLogging(
    EventId = 8033,
    Level = LogLevel.Error,
    Message = "Order '{orderId}' not found")]
public static partial IGenericMessage OrderNotFound(
    ILogger logger,
    string orderId);
```

## Usage

From [`OrderService.cs`](../samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging/Services/OrderService.cs):

```csharp
public IGenericResult<Order> GetOrder(string orderId)
{
    // Simulate lookup
    if (orderId == "NOT_FOUND")
    {
        return GenericResult<Order>.Failure(
            OrderLog.OrderNotFound(_logger, orderId));
    }

    var order = new Order(orderId, "customer-1", "product-1", 1);
    return GenericResult<Order>.Success(order);
}
```

## Next Steps

- [MessageLogging Attribute](07-02-MessageLogging-Attribute.md) - Attribute parameters
- [Logger Classes](07-03-Logger-Classes.md) - Organizing loggers
- [IGenericMessage](07-04-IGenericMessage.md) - Message interface
- [Result Integration](07-05-Result-Integration.md) - Using with GenericResult
- [External Exception Handling](07-07-External-Exception-Handling.md) - TypeCollection-based error dispatch
