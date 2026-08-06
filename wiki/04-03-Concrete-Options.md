# Concrete Options

Concrete options are sealed classes that represent individual values in the TypeCollection.

## Pattern

Each option:
1. Inherits from the base class
2. Has `[TypeOption]` attribute
3. Calls base constructor with all values
4. Is sealed

## Example Options

From [`CashPayment.cs`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/Options/CashPayment.cs):

```csharp
using Fdw.Collections;

namespace Reference.TypeCollections.Basic.Options;

[TypeOption(typeof(PaymentMethods), "Cash")]
public sealed class CashPayment : PaymentMethodBase
{
    public CashPayment()
        : base(
            id: 1,
            name: "Cash",
            feePercentage: 0m,
            requiresVerification: false)
    {
    }
}
```

From [`CreditCardPayment.cs`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/Options/CreditCardPayment.cs):

```csharp
using Fdw.Collections;

namespace Reference.TypeCollections.Basic.Options;

[TypeOption(typeof(PaymentMethods), "CreditCard")]
public sealed class CreditCardPayment : PaymentMethodBase
{
    public CreditCardPayment()
        : base(
            id: 2,
            name: "CreditCard",
            feePercentage: 2.5m,
            requiresVerification: true)
    {
    }
}
```

From [`BankTransferPayment.cs`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/Options/BankTransferPayment.cs):

```csharp
using Fdw.Collections;

namespace Reference.TypeCollections.Basic.Options;

[TypeOption(typeof(PaymentMethods), "BankTransfer")]
public sealed class BankTransferPayment : PaymentMethodBase
{
    public BankTransferPayment()
        : base(
            id: 3,
            name: "BankTransfer",
            feePercentage: 0.5m,
            requiresVerification: false)
    {
    }
}
```

## TypeOption Attribute

From [`TypeOptionAttribute.cs:14-20`](../src/Fdw.Collections/Attributes/TypeOptionAttribute.cs#L14-L20):

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
