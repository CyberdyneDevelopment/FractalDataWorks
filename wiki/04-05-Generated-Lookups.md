# Generated Lookups

The source generator creates O(1) lookup methods for accessing type options.

## Usage Examples

From Reference Solution [`Program.cs:15-28`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Program.cs#L15-L28):

### Access by Property

```csharp
// Access via static property (generated)
var cash = PaymentMethods.Cash;
Console.WriteLine($"   PaymentMethods.Cash: {cash.Name}, Fee: {cash.FeePercentage}%");
```

### Access by Name (O(1))

```csharp
// Access via lookup
var creditCard = PaymentMethods.ByName("CreditCard");
Console.WriteLine($"   PaymentMethods.ByName(\"CreditCard\"): {creditCard.Name}");
```

### Access by ID (O(1))

From Framework [`SucceededStatus.cs`](../src/Fdw.Orchestration/TypeCollections/ExecutionStatuses/SucceededStatus.cs):

```csharp
// ExecutionStatuses uses integer IDs - Succeeded has id: 4
var succeeded = ExecutionStatuses.ById(4);
if (succeeded is not null)
{
    Console.WriteLine(succeeded.IsSuccess);  // true
}
```

### Enumerate All Options

From Reference Solution [`Program.cs:24`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Program.cs#L24):

```csharp
// Iterate all options
Console.WriteLine($"   All payment methods: {string.Join(", ", PaymentMethods.All().Select(p => p.Name))}");
```

## Polymorphic Usage

Use properties instead of switch statements. From [`IExecutionStatus.cs:12-48`](../src/Fdw.Orchestration.Abstractions/TypeCollections/ExecutionStatuses/IExecutionStatus.cs#L12-L48):

```csharp
// CORRECT: Use polymorphism
// ExecutionStatuses have: IsTerminal, IsSuccess, IsFailure, AllowsRetry, AllowsResume, IsInProgress, HasWarnings
if (status.IsTerminal)
{
    if (status.IsSuccess)
        await HandleSuccessAsync(execution);
    else if (status.AllowsRetry)
        await ScheduleRetryAsync(execution);
    else
        await HandleFailureAsync(execution);
}
```

```csharp
// WRONG: Switch statements
switch (status.Name)
{
    case "Succeeded":  // NEVER
        break;
    case "Failed":  // NEVER
        break;
}
```

## Performance

All lookups are O(1) using source-generated FrozenDictionary:

| Operation | Complexity |
|-----------|------------|
| `PaymentMethods.Cash` | O(1) - direct field access |
| `PaymentMethods.ByName("Cash")` | O(1) - FrozenDictionary lookup |
| `PaymentMethods.ById(1)` | O(1) - FrozenDictionary lookup |
| `PaymentMethods.All()` | O(1) - returns cached collection |

## Next Steps

Return to [Overview](04-01-Overview.md) or continue to [Message Logging](07-01-Overview.md).
