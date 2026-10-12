# Result Integration

MessageLogging integrates with the Result pattern. Log methods return `IGenericMessage` which can be passed directly to `GenericResult.Failure()`.

## Pattern

From [`OrderService.cs`](../samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging/Services/OrderService.cs):

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

## Benefits

1. **Single source of truth** - Message defined once in attribute
2. **Always logged** - Can't forget to log
3. **Consistent messages** - Same text in log and result
4. **Type-safe** - Parameters validated at compile time

## Wrong vs Correct Patterns

Avoid string literals and separate logging:

```csharp
// WRONG: String literal - no structured logging
return GenericResult.Failure("Order not found");

// WRONG: Separate logging and result - message duplication
_logger.LogWarning("Order not found: {Id}", orderId);
return GenericResult.Failure(new GenericMessage("Order not found"));

// CORRECT: MessageLogging method - logs AND returns in one call
return GenericResult<Order>.Failure(
    OrderLog.OrderNotFound(_logger, orderId));
```

## Exception Handling

For exceptions, use methods that accept `Exception`. Note that the exception parameter comes after `ILogger`:

From [`OrderLog.cs:165-172`](../samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging/Logging/OrderLog.cs#L165-L172):

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

From [`OrderService.cs:82-88`](../samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging/Services/OrderService.cs#L82-L88):

```csharp
catch (Exception ex)
{
    // CRITICAL PATTERN: Catch → Log → Return (never rethrow!)
    // The message is logged AND returned
    return GenericResult<Order>.Failure(
        OrderLog.ProcessingException(_logger, ex, orderId));
}
```

## Next Steps

- [ResultCodes](07-06-ResultCodes.md) - Structured, type-safe error codes (alternative to MessageLogging for error handling)
- [Overview](07-01-Overview.md) - Return to MessageLogging overview
- [TypeCollections](04-01-Overview.md) - Understanding the TypeCollection pattern
