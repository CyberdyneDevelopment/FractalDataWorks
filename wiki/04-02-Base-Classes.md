# TypeCollection Base Classes

Base classes define the shared properties and behavior for all type options. **All properties are set via constructor parameters - never abstract properties.**

## Interface Definition

From [`IPaymentMethod.cs:1-30`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/IPaymentMethod.cs):

```csharp
using FractalDataWorks.Collections;

namespace Reference.TypeCollections.Basic;

/// <summary>
/// Interface for payment method type options.
/// Extends ITypeOption to enable TypeCollection discovery.
/// </summary>
/// <remarks>
/// The interface defines the contract that all payment methods must implement.
/// Each payment method knows its own processing logic - no switch statements needed!
/// </remarks>
public interface IPaymentMethod : ITypeOption<int, PaymentMethodBase>
{
    /// <summary>
    /// Gets the processing fee percentage for this payment method.
    /// </summary>
    decimal FeePercentage { get; }

    /// <summary>
    /// Gets a value indicating whether this method requires external authorization.
    /// </summary>
    bool RequiresAuthorization { get; }

    /// <summary>
    /// Calculates the total amount including fees.
    /// Each payment method implements its own calculation logic.
    /// </summary>
    decimal CalculateTotal(decimal amount);
}
```

## Base Class Definition

From [`PaymentMethodBase.cs:1-45`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/PaymentMethodBase.cs):

```csharp
using FractalDataWorks.Collections;

namespace Reference.TypeCollections.Basic;

/// <summary>
/// Abstract base class for payment methods.
/// Inherits from TypeOptionBase for TypeCollection integration.
/// </summary>
/// <remarks>
/// The base class provides common implementation and ensures all
/// payment methods have the required TypeOption infrastructure.
/// </remarks>
public abstract class PaymentMethodBase : TypeOptionBase<int, PaymentMethodBase>, IPaymentMethod
{
    /// <summary>
    /// Initializes a new instance of PaymentMethodBase.
    /// </summary>
    /// <param name="id">Unique identifier for this payment method.</param>
    /// <param name="name">Name of the payment method (must match TypeOption attribute).</param>
    /// <param name="feePercentage">Processing fee percentage.</param>
    /// <param name="requiresAuthorization">Whether external authorization is required.</param>
    protected PaymentMethodBase(
        int id,
        string name,
        decimal feePercentage,
        bool requiresAuthorization)
        : base(id, name)
    {
        FeePercentage = feePercentage;
        RequiresAuthorization = requiresAuthorization;
    }

    /// <inheritdoc />
    public decimal FeePercentage { get; }

    /// <inheritdoc />
    public bool RequiresAuthorization { get; }

    /// <inheritdoc />
    public virtual decimal CalculateTotal(decimal amount)
    {
        // Default implementation adds fee percentage
        return amount * (1 + FeePercentage / 100);
    }
}
```

## Key Requirements

1. **Inherit from `TypeOptionBase<TKey, TBase>`** - provides Id, Name, and equality
2. **Implement your interface** - the interface extends `ITypeOption<TKey, TBase>`
3. **Constructor parameters for ALL properties** - never use abstract/virtual properties
4. **Get-only properties (no setters)** - immutable after construction

## Framework Base Class

From [`TypeOptionBase.cs:12-88`](../src/FractalDataWorks.Collections/TypeOptionBase.cs):

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
public virtual bool RequiresAuthorization => false;  // NEVER - set via constructor
```

## Next Steps

- [Concrete Options](04-03-Concrete-Options.md) - Creating type options
