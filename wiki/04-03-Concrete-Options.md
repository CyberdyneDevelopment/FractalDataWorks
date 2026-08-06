# Concrete Options

Concrete options are sealed classes that represent individual values in the TypeCollection.

## Pattern

Each option:
1. Inherits from the base class
2. Has `[TypeOption]` attribute
3. Calls base constructor with all values
4. Is sealed

## Example Options

From [`CashPayment.cs:1-29`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/Options/CashPayment.cs):

```csharp
using FractalDataWorks.Collections.Attributes;

namespace Reference.TypeCollections.Basic.Options;

/// <summary>
/// Cash payment method - no fees, no authorization required.
/// </summary>
[TypeOption(typeof(PaymentMethods), "Cash")]
public sealed class CashPayment : PaymentMethodBase
{
    /// <summary>
    /// Initializes a new instance of CashPayment.
    /// </summary>
    public CashPayment()
        : base(
            id: 1,
            name: "Cash",
            feePercentage: 0m,
            requiresAuthorization: false)
    {
    }

    /// <inheritdoc />
    public override decimal CalculateTotal(decimal amount)
    {
        // Cash has no fees
        return amount;
    }
}
```

From [`CreditCardPayment.cs:1-22`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/Options/CreditCardPayment.cs):

```csharp
using FractalDataWorks.Collections.Attributes;

namespace Reference.TypeCollections.Basic.Options;

/// <summary>
/// Credit card payment method - 2.5% fee, requires authorization.
/// </summary>
[TypeOption(typeof(PaymentMethods), "CreditCard")]
public sealed class CreditCardPayment : PaymentMethodBase
{
    /// <summary>
    /// Initializes a new instance of CreditCardPayment.
    /// </summary>
    public CreditCardPayment()
        : base(
            id: 2,
            name: "CreditCard",
            feePercentage: 2.5m,
            requiresAuthorization: true)
    {
    }
}
```

From [`BankTransferPayment.cs:1-22`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/Options/BankTransferPayment.cs):

```csharp
using FractalDataWorks.Collections.Attributes;

namespace Reference.TypeCollections.Basic.Options;

/// <summary>
/// Bank transfer payment method - 0.5% fee, requires authorization.
/// </summary>
[TypeOption(typeof(PaymentMethods), "BankTransfer")]
public sealed class BankTransferPayment : PaymentMethodBase
{
    /// <summary>
    /// Initializes a new instance of BankTransferPayment.
    /// </summary>
    public BankTransferPayment()
        : base(
            id: 3,
            name: "BankTransfer",
            feePercentage: 0.5m,
            requiresAuthorization: true)
    {
    }
}
```

## TypeOption Attribute

From [`TypeOptionAttribute.cs:14-20`](../src/FractalDataWorks.Collections/Attributes/TypeOptionAttribute.cs#L14-L20):

```csharp
public TypeOptionAttribute(Type collectionType, string name)
{
    CollectionType = collectionType ?? throw new ArgumentNullException(nameof(collectionType));
    Name = name ?? throw new ArgumentNullException(nameof(name));
}
```

| Parameter | Description |
|-----------|-------------|
| `collectionType` | The TypeCollection class this option belongs to |
| `name` | The name for the generated accessor (e.g., `PaymentMethods.Cash`) |

## ID Conventions

Assign unique IDs to each option:

| Payment Method | ID |
|----------------|-----|
| Cash | 1 |
| CreditCard | 2 |
| BankTransfer | 3 |

## Next Steps

- [Collection Declaration](04-04-Collection-Declaration.md) - The [TypeCollection] attribute
