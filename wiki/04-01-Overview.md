# TypeCollections Overview

TypeCollections are enhanced enums with O(1) lookups and rich behavior. They replace traditional enums with strongly-typed classes.

## Key Benefits

- **O(1) lookups** - Source-generated dictionaries by ID and name
- **No reflection** - All lookup code generated at compile time
- **Rich behavior** - Add properties and methods to each option
- **Type-safe** - Compile-time validation
- **Polymorphism** - Use properties instead of switch statements

## Quick Example

From Reference Solution [`Program.cs:15-28`](../../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Program.cs#L15-L28):

```csharp
// Access via static property (generated)
var cash = PaymentMethods.Cash;
Console.WriteLine($"   PaymentMethods.Cash: {cash.Name}, Fee: {cash.FeePercentage}%");

// Access via lookup
var creditCard = PaymentMethods.ByName("CreditCard");
Console.WriteLine($"   PaymentMethods.ByName(\"CreditCard\"): {creditCard.Name}");

// Iterate all options
Console.WriteLine($"   All payment methods: {string.Join(", ", PaymentMethods.All().Select(p => p.Name))}");

// Use behavior - no switch statements needed!
var amount = 100m;
Console.WriteLine($"   ${amount} via {creditCard.Name} = ${creditCard.CalculateTotal(amount):F2}");
```

For TypeCollections with terminal state handling, see [`ExecutionStatuses`](../../src/FractalDataWorks.Orchestration.Abstractions/TypeCollections/ExecutionStatuses/):

From [`IExecutionStatus.cs:12-27`](../../src/FractalDataWorks.Orchestration.Abstractions/TypeCollections/ExecutionStatuses/IExecutionStatus.cs#L12-L27):

```csharp
public interface IExecutionStatus : ITypeOption<int, ExecutionStatusBase>
{
    bool IsTerminal { get; }
    bool IsSuccess { get; }
    bool IsFailure { get; }
    bool AllowsRetry { get; }
    bool AllowsResume { get; }
    bool IsInProgress { get; }
    bool HasWarnings { get; }
}
```

## Pattern Components

| Component | Purpose |
|-----------|---------|
| Interface (`IPaymentMethod`) | Contract with behavior properties |
| Base class (`PaymentMethodBase`) | Constructor parameters, get-only properties |
| Options (`CashPayment`, etc.) | Concrete implementations |
| Collection (`PaymentMethods`) | Source-generated lookups |

## Next Steps

- [Base Classes](04-02-Base-Classes.md) - Defining base classes
- [Concrete Options](04-03-Concrete-Options.md) - Creating type options
- [Collection Declaration](04-04-Collection-Declaration.md) - The [TypeCollection] attribute
- [Generated Lookups](04-05-Generated-Lookups.md) - Using the generated code
