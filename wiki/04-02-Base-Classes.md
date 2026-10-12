# TypeCollection Base Classes

Base classes define the shared properties and behavior for all type options. **All properties are set via constructor parameters - never abstract properties.**

## Interface Definition

From [`IPaymentMethod.cs`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/IPaymentMethod.cs):

```csharp
using Fdw.Collections;

namespace Reference.TypeCollections.Basic;

/// <summary>
/// Interface for payment method type options.
/// Defines the contract for all payment methods in the TypeCollection.
/// </summary>
public interface IPaymentMethod : ITypeOption<int, PaymentMethodBase>
{
    decimal FeePercentage { get; }
    bool RequiresVerification { get; }
    decimal CalculateTotal(decimal amount);
}
```

## Base Class Definition

From [`PaymentMethodBase.cs`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/PaymentMethodBase.cs):

```csharp
using Fdw.Collections;

namespace Reference.TypeCollections.Basic;

public abstract class PaymentMethodBase : TypeOptionBase<int, PaymentMethodBase>, IPaymentMethod
{
    protected PaymentMethodBase(
        int id,
        string name,
        decimal feePercentage,
        bool requiresVerification)
        : base(id, name)
    {
        FeePercentage = feePercentage;
        RequiresVerification = requiresVerification;
    }

    public decimal FeePercentage { get; }
    public bool RequiresVerification { get; }

    public decimal CalculateTotal(decimal amount)
    {
        return amount * (1 + FeePercentage / 100m);
    }
}
```

## Key Requirements

1. **Inherit from `TypeOptionBase<TKey, TBase>`** - provides Id, Name, and equality
2. **Implement your interface** - the interface extends `ITypeOption<TKey, TBase>`
3. **Constructor parameters for ALL properties** - never use abstract/virtual properties
4. **Get-only properties (no setters)** - immutable after construction

## Framework Base Class

From [`TypeOptionBase.cs:12-88`](../src/Fdw.Collections/TypeOptionBase.cs):

```csharp
public abstract class TypeOptionBase<TKey, T> : ITypeOption<TKey, T>
    where TKey : IEquatable<TKey>
    where T : ITypeOption<TKey, T>
{
    [TypeLookup("ById")]
    public virtual TKey Id { get; }

    [TypeLookup("ByName")]
    public string Name { get; }

    public string Category => string.IsNullOrEmpty(_category) ? "NotCategorized" : _category;

    protected TypeOptionBase(TKey id, string name) : this(id, name, string.Empty)
    {
    }

    protected TypeOptionBase(TKey id, string name, string? category)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));

        Id = id;
        Name = name;
        _category = category ?? string.Empty;
        ConfigurationKey = $"TypeOptions:{name}";
        DisplayName = name;
        Description = $"Type option: {name}";
    }

    // ... equality and hash code implementations
}
```

## WRONG - Never Do This

```csharp
// WRONG: Abstract properties force each derived class to repeat the value
public abstract class PaymentMethodBase
{
    public abstract decimal FeePercentage { get; }  // NEVER - use constructor parameter
}

// WRONG: Virtual properties with default values encourage override chains
public virtual bool RequiresVerification => false;  // NEVER - set via constructor
```

## Next Steps

- [Concrete Options](04-03-Concrete-Options.md) - Creating type options
