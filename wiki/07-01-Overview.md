# MessageLogging Overview

FractalDataWorks uses structured logging with the `[MessageLogging]` attribute. Every log message is defined as a partial method that:

1. **Logs via ILogger** - Standard Microsoft.Extensions.Logging
2. **Returns IGenericMessage** - For result integration

## Key Benefits

- **Zero string literals** - All messages defined via attributes
- **Source-generated** - No reflection at runtime
- **Type-safe parameters** - Compile-time validation
- **Result integration** - Messages can be returned in `IGenericResult`

## Quick Example

From [`OrderLog.cs:114-120`](../../samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging/Logging/OrderLog.cs#L114-L120):

```csharp
[MessageLogging(
    EventId = 8030,
    Level = LogLevel.Error,
    Message = "Order '{orderId}' not found")]
public static partial IGenericMessage OrderNotFound(
    ILogger logger,
    string orderId);
```

## Usage

From [`OrderService.cs:115-126`](../../samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging/Services/OrderService.cs#L115-L126):

```csharp
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

## Next Steps

- [MessageLogging Attribute](02-MessageLogging-Attribute.md) - Attribute parameters
- [Logger Classes](03-Logger-Classes.md) - Organizing loggers
- [IGenericMessage](04-IGenericMessage.md) - Message interface
- [Result Integration](05-Result-Integration.md) - Using with GenericResult
