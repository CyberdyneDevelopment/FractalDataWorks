# What You Built

The Reference Solution demonstrates FractalDataWorks patterns through modular, self-contained tutorials.

## Architecture Overview

```
ReferenceSolution/
|-- concepts/                           <-- Individual pattern tutorials
|   |-- 01-type-collections/            <-- TypeCollection patterns
|   |-- 02-service-types/               <-- ServiceType with DI
|   |-- 03-message-logging/             <-- MessageLogging pattern
|   |-- 05-configuration/               <-- Configuration binding
|   |-- 06-data-layer/                  <-- Data access patterns
|
|-- Directory.Build.props               <-- Shared MSBuild settings
|-- Directory.Packages.props            <-- Central package versions
|-- README.md                           <-- Setup instructions
```

## Key Patterns

### 1. TypeCollections

TypeCollections replace enums with extensible, type-safe alternatives.

From [`concepts/01-type-collections/src/Reference.TypeCollections/Basic/PaymentMethods.cs:17-19`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/PaymentMethods.cs#L17-L19):

```csharp
[TypeCollection(typeof(PaymentMethodBase), typeof(IPaymentMethod), typeof(PaymentMethods))]
public abstract partial class PaymentMethods : TypeCollectionBase<PaymentMethodBase, IPaymentMethod>
{
```

From [`concepts/01-type-collections/src/Reference.TypeCollections/Program.cs:21-29`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Program.cs#L21-L29):

```csharp
// Access via static property (generated)
var cash = PaymentMethods.Cash;
Console.WriteLine($"   PaymentMethods.Cash: {cash.Name}, Fee: {cash.FeePercentage}%");

// Access via lookup
var creditCard = PaymentMethods.ByName("CreditCard");
Console.WriteLine($"   PaymentMethods.ByName(\"CreditCard\"): {creditCard.Name}");

// Iterate all options
Console.WriteLine($"   All payment methods: {string.Join(", ", PaymentMethods.All().Select(p => p.Name))}");
```

### 2. MessageLogging Pattern

MessageLogging provides structured logging that returns messages for result integration.

From [`concepts/03-message-logging/src/Reference.MessageLogging/Program.cs:24-30`](../samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging/Program.cs#L24-L30):

```csharp
var orderServiceLogger = loggerFactory.CreateLogger<OrderService>();
var orderService = new OrderService(orderServiceLogger);

// 1. Successful order creation
Console.WriteLine("1. Creating a valid order...");
var result = orderService.CreateOrder("customer-123", "product-456", 2);
PrintResult(result);
```

Where `PrintResult` walks `IGenericResult<T>.IsSuccess`, `.Value`, and `.Messages` (lines 80-89).

### 3. Configuration Pattern

Configuration demonstrates binding to strongly-typed classes.

From [`concepts/05-configuration/src/Reference.Configuration/appsettings.json:8-17`](../samples/ReferenceSolution/concepts/05-configuration/src/Reference.Configuration/appsettings.json#L8-L17):

```json
"Connections": {
  "MsSql": [
    {
      "Name": "OrdersDb",
      "Server": "localhost",
      "Database": "Orders",
      "Port": 1433,
      "Encrypt": true,
      "TrustServerCertificate": true
    },
```

## Running the Examples

Each concept tutorial is a standalone console application:

```bash
# Run TypeCollections demo
cd public/samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections
dotnet run

# Run MessageLogging demo
cd public/samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging
dotnet run

# Run Configuration demo
cd public/samples/ReferenceSolution/concepts/05-configuration/src/Reference.Configuration
dotnet run
```

## Next Steps

- [Project Layout](02-01-Project-Layout.md) - Detailed file organization
- [Directory.Build.props](02-02-Directory-Build-Props.md) - Build configuration
