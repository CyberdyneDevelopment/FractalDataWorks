# TypeCollections Overview

TypeCollections are enhanced enums with O(1) lookups and rich behavior. They replace traditional enums with strongly-typed classes.

## Key Benefits

- **O(1) lookups** - Source-generated dictionaries by ID and name
- **No reflection** - All lookup code generated at compile time
- **Rich behavior** - Add properties and methods to each option
- **Type-safe** - Compile-time validation
- **Polymorphism** - Use properties instead of switch statements

## Quick Example

From [`FilterOperators.cs`](../src/Fdw.Data/Operators/FilterOperators.cs):

```csharp
// Access via static property (generated)
var equalOp = FilterOperators.Equal;
Console.WriteLine($"   FilterOperators.Equal: {equalOp.Name}");

// Access via lookup
var greaterThan = FilterOperators.ByName("GreaterThan");
Console.WriteLine($"   FilterOperators.ByName(\"GreaterThan\"): {greaterThan.Name}");

// Iterate all options
Console.WriteLine($"   All operators: {string.Join(", ", FilterOperators.All().Select(p => p.Name))}");

// Use behavior - no switch statements needed!
// Each operator knows how to generate SQL predicates, etc.
```

For TypeCollections with terminal state handling, see [`ExecutionStatuses`](../src/Fdw.Orchestration.Abstractions/TypeCollections/ExecutionStatuses/):

From [`IExecutionStatus.cs:12-27`](../src/Fdw.Orchestration.Abstractions/TypeCollections/ExecutionStatuses/IExecutionStatus.cs#L12-L27):

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
