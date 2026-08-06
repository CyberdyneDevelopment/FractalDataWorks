# What You Built

The Reference Solution demonstrates FractalDataWorks patterns through modular, self-contained tutorials.

## Architecture Overview

```
ReferenceSolution/
|-- concepts/                           <-- Individual pattern tutorials
|   |-- 01-type-collections/            <-- TypeCollection patterns
|   |-- 02-service-types/               <-- ServiceType with DI
|   |-- 03-message-logging/             <-- MessageLogging pattern
|   |-- 04-results/                     <-- IGenericResult patterns
|   |-- 05-configuration/               <-- Configuration binding
|   |-- 06-data-layer/                  <-- Data access patterns
|   |-- 07-translators/                 <-- Data translation
|
|-- integration/                        <-- Combined pattern examples
|   |-- 01-pipeline/                    <-- Multi-step pipelines
|   |-- 02-custom-connection/           <-- Custom connection types
|   |-- 03-full-translator/             <-- Complete translator example
|   |-- 04-three-phase-di/              <-- Service registration pattern
|
|-- appsettings.json                    <-- Shared configuration
|-- README.md                           <-- Setup instructions
```

## Key Patterns

### 1. TypeCollections

TypeCollections replace enums with extensible, type-safe alternatives.

From [`concepts/01-type-collections/src/Reference.TypeCollections/Basic/PaymentMethods.cs:26-28`](../../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/PaymentMethods.cs#L26-L28):

```csharp
[TypeCollection(typeof(PaymentMethodBase), typeof(IPaymentMethod), typeof(PaymentMethods))]
public abstract partial class PaymentMethods : TypeCollectionBase<PaymentMethodBase, IPaymentMethod>
{
```

From [`concepts/01-type-collections/src/Reference.TypeCollections/Program.cs:16-24`](../../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Program.cs#L16-L24):

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

From [`concepts/03-message-logging/src/Reference.MessageLogging/Program.cs:25-34`](../../samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging/Program.cs#L25-L34):

```csharp
var result1 = orderService.CreateOrder("CUST001", "ITEM001", 5);
if (result1.IsSuccess)
{
    Console.WriteLine($"   Order created: {result1.Value!.OrderId}");
    Console.WriteLine($"   Messages returned: {result1.Messages.Count}");
    foreach (var msg in result1.Messages)
    {
        Console.WriteLine($"   - {msg.Message}");
    }
}
```

### 3. Configuration Pattern

Configuration demonstrates binding to strongly-typed classes.

From [`concepts/05-configuration/src/Reference.Configuration/appsettings.json:8-18`](../../samples/ReferenceSolution/concepts/05-configuration/src/Reference.Configuration/appsettings.json#L8-L18):

```json
"Connections": {
  "OrdersDb": {
    "Name": "OrdersDb",
    "Type": "Sql",
    "IsEnabled": true,
    "TimeoutSeconds": 30,
    "Server": "localhost",
    "Database": "Orders",
    "Port": 1433,
    "IntegratedSecurity": true
  }
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
