# TypeCollection Components Guide

This guide shows the different components required for each variety of TypeCollection in FractalDataWorks.

## Overview

There are 6 TypeCollection variants organized in two families:

| Family | Variant | Key Type | Mutability | Description |
|--------|---------|----------|------------|-------------|
| **TypeCollection** | TypeCollection | `int` (FNV-1a hash) | Immutable (FrozenDictionary) | Enum-like extensible type system |
| | MutableTypeCollection | `int` | Mutable (Dictionary + RebuildLookups) | Runtime registration support |
| | TypeInstanceCollection | `int` | Immutable | Returns new instances each access |
| **ServiceTypeCollection** | ServiceTypeCollection | `Guid` (UUID v5) | Immutable | DI integration with `RegisterAll()` |
| | MutableServiceTypeCollection | `Guid` | Mutable | Runtime registration + DI |
| | ServiceTypeInstanceCollection | `Guid` | Immutable | Returns new instances each access + DI |

---

## Headless Logic Providers (Protocols)

While the components above handle type-safe discovery and DI, **Logic Providers** (Protocols) manage the runtime state and API orchestration for the UI.

### The Headless Pattern

Logic Providers are non-rendering Blazor components that serve as the "brain" for a page or feature. They wrap visual components (the "Skin") and provide them with data, loading states, and action methods.

| Feature | Provider (Protocol) | Visual Skin (Reference) |
|---------|---------------------|--------------------------|
| **Responsibility** | API calls, state, validation | Rendering, CSS, Layout |
| **Logic Location** | Core Framework | Reference Project |
| **Reuse** | 100% Shared | Project Specific |

### Example: ConnectionProvider

```razor
<ConnectionProvider @ref="_provider">
    <ChildContent Context="logic">
        @if (logic.IsLoading) { <MudProgressLinear Indeterminate="true" /> }
        else {
            <MudTable Items="logic.Connections">
                <!-- Rendering details here -->
            </MudTable>
        }
    </ChildContent>
</ConnectionProvider>
```

---

## 1. TypeCollection (Immutable)

### Required Components

```
+---------------------------------------------------------------------+
|  1. INTERFACE (IMyOption)                                           |
|     - Extends ITypeOption<TKey, TBase>                              |
|     - Defines the contract for all options                          |
+---------------------------------------------------------------------+
|  2. BASE CLASS (MyOptionBase)                                       |
|     - Inherits TypeOptionBase<TKey, TSelf>                          |
|     - Implements the interface                                      |
|     - Contains [TypeLookup] properties                              |
+---------------------------------------------------------------------+
|  3. COLLECTION (MyOptions)                                          |
|     - Marked partial                                                |
|     - [TypeCollection] attribute                                    |
|     - Inherits TypeCollectionBase<TBase, TInterface>                |
+---------------------------------------------------------------------+
|  4. TYPE OPTIONS (ConcreteOption1, ConcreteOption2, ...)            |
|     - [TypeOption] attribute referencing collection                 |
|     - Inherits from base class                                      |
|     - Parameterless constructor calling base(id, name, ...)         |
+---------------------------------------------------------------------+
```

### Example Code

The following example shows a complete TypeCollection from the Reference Solution - a payment methods collection.

From [`IPaymentMethod.cs`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/IPaymentMethod.cs):
```csharp
// ═══════════════════════════════════════════════════════════════════
// 1. INTERFACE - Defines the contract
// ═══════════════════════════════════════════════════════════════════
using Fdw.Collections;

namespace Reference.TypeCollections.Basic;

/// <summary>
/// Interface for payment method type options.
/// Extends ITypeOption to enable TypeCollection discovery.
/// </summary>
public interface IPaymentMethod : ITypeOption<int, PaymentMethodBase>
{
    /// <summary>
    /// Gets the processing fee percentage for this payment method.
    /// </summary>
    decimal FeePercentage { get; }

    /// <summary>
    /// Gets a value indicating whether this method requires verification.
    /// </summary>
    bool RequiresVerification { get; }

    /// <summary>
    /// Calculates the total amount including fees.
    /// Each payment method implements its own calculation logic.
    /// </summary>
    decimal CalculateTotal(decimal amount);
}
```

From [`PaymentMethodBase.cs`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/PaymentMethodBase.cs):
```csharp
// ═══════════════════════════════════════════════════════════════════
// 2. BASE CLASS - Common implementation
// ═══════════════════════════════════════════════════════════════════
using Fdw.Collections;

namespace Reference.TypeCollections.Basic;

/// <summary>
/// Abstract base class for payment methods.
/// Inherits from TypeOptionBase for TypeCollection integration.
/// </summary>
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

    /// <inheritdoc />
    public decimal FeePercentage { get; }

    /// <inheritdoc />
    public bool RequiresVerification { get; }

    /// <inheritdoc />
    public decimal CalculateTotal(decimal amount)
    {
        return amount * (1 + FeePercentage / 100m);
    }
}
```

From [`PaymentMethods.cs`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/PaymentMethods.cs):
```csharp
// ═══════════════════════════════════════════════════════════════════
// 3. COLLECTION - The static accessor class (partial for generation)
// ═══════════════════════════════════════════════════════════════════
using Fdw.Collections;

namespace Reference.TypeCollections.Basic;

/// <summary>
/// TypeCollection for payment methods.
/// Source generator creates static properties, lookup methods, and FrozenDictionary storage.
/// </summary>
[TypeCollection(typeof(PaymentMethodBase), typeof(IPaymentMethod), typeof(PaymentMethods))]
public abstract partial class PaymentMethods : TypeCollectionBase<PaymentMethodBase, IPaymentMethod>
{
    // Source generator populates this class with:
    // - Static constructor initializing FrozenDictionaries
    // - Static properties for each [TypeOption] payment method
    // - ById(), ByName(), All() methods
    // - Empty sentinel value
}
```

From [`CashPayment.cs`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/Options/CashPayment.cs):
```csharp
// ═══════════════════════════════════════════════════════════════════
// 4. TYPE OPTIONS - Concrete implementations
// ═══════════════════════════════════════════════════════════════════
using Fdw.Collections;

namespace Reference.TypeCollections.Basic.Options;

/// <summary>
/// Cash payment method - no fees, no verification required.
/// </summary>
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

### Generated Code (Conceptual)

The source generator creates code similar to the following (simplified for clarity):

```csharp
partial class PaymentMethods
{
    private static readonly FrozenDictionary<int, IPaymentMethod> _byId;
    private static readonly FrozenDictionary<string, IPaymentMethod> _byName;
    private static readonly IPaymentMethod[] _all;

    static PaymentMethods()
    {
        _all = new IPaymentMethod[]
        {
            new CashPayment(),
            new CreditCardPayment(),
            new BankTransferPayment(),
        };
        _byId = _all.ToFrozenDictionary(x => x.Id);
        _byName = _all.ToFrozenDictionary(x => x.Name);
    }

    // Static accessors (singletons)
    private static CashPayment? _cash;
    public static CashPayment Cash => _cash ??= (CashPayment)_byId[1];

    // Lookup methods
    public static IPaymentMethod ById(int value) => _byId.GetValueOrDefault(value) ?? Empty;
    public static IPaymentMethod ByName(string value) => _byName.GetValueOrDefault(value) ?? Empty;

    // All items
    public static IReadOnlyCollection<IPaymentMethod> All() => _all;

    // Empty sentinel
    private static readonly IPaymentMethod _empty = new EmptyPaymentMethods();
    public static IPaymentMethod Empty => _empty;
}
```

---

## 2. MutableTypeCollection

Same components as TypeCollection, but uses `[MutableTypeCollection]` attribute.

### Differences from TypeCollection

| Aspect | TypeCollection | MutableTypeCollection |
|--------|---------------|----------------------|
| Attribute | `[TypeCollection]` | `[MutableTypeCollection]` |
| Storage | `FrozenDictionary` | `ConcurrentDictionary` (thread-safe) |
| Registration | Compile-time only | Runtime via `Register()` |
| Thread Safety | Immutable = safe | ConcurrentDictionary = thread-safe |

### Example

From [`Plugins.cs`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Mutable/Plugins.cs):
```csharp
using Fdw.Collections;

namespace Reference.TypeCollections.Mutable;

/// <summary>
/// MutableTypeCollection for plugins.
/// Supports runtime registration of new plugins via Register() method.
/// </summary>
/// <remarks>
/// <para>
/// Unlike immutable TypeCollection, MutableTypeCollection uses ConcurrentDictionary
/// and provides a Register() method for adding new options at runtime.
/// </para>
/// <para>
/// Use cases:
/// <list type="bullet">
/// <item>Plugin systems where third-party plugins can be loaded dynamically</item>
/// <item>Feature flags that can be toggled at runtime</item>
/// <item>Extension points for downstream assemblies</item>
/// </list>
/// </para>
/// </remarks>
[MutableTypeCollection(typeof(PluginBase), typeof(IPlugin), typeof(Plugins))]
public abstract partial class Plugins : TypeCollectionBase<PluginBase, IPlugin>
{
    // Source generator creates:
    // - ConcurrentDictionary storage (thread-safe)
    // - Register(IPlugin plugin) method
    // - ById(), ByName(), All() methods
    // - Static properties for compile-time discovered plugins
}
```

### Generated Code (Conceptual)

```csharp
partial class Plugins
{
    // Thread-safe mutable storage
    private static readonly ConcurrentDictionary<int, IPlugin> _byId = new();
    private static readonly ConcurrentDictionary<string, IPlugin> _byName = new();

    static Plugins()
    {
        // Register compile-time discovered options
        var logging = new LoggingPlugin();
        _byId.TryAdd(logging.Id, logging);
        _byName.TryAdd(logging.Name, logging);
    }

    // Runtime registration
    public static void Register(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));
        _byId.TryAdd(plugin.Id, plugin);
        _byName.TryAdd(plugin.Name, plugin);
    }

    // Lookup methods
    public static IPlugin ById(int value) => _byId.GetValueOrDefault(value) ?? Empty;
    public static IPlugin ByName(string value) => _byName.GetValueOrDefault(value) ?? Empty;
    public static IReadOnlyCollection<IPlugin> All() => _byId.Values.ToList();
}
```

---

## 3. TypeInstanceCollection

Same components as TypeCollection, but uses `[TypeInstanceCollection]` attribute.

### Differences from TypeCollection

| Aspect | TypeCollection | TypeInstanceCollection |
|--------|---------------|----------------------|
| Attribute | `[TypeCollection]` | `[TypeInstanceCollection]` |
| Instance Behavior | Singleton (same instance) | Factory (new instance each time) |
| Static Accessors | Return cached instance | Return new instance |
| Use Case | Enum-like constants | Stateful/disposable options |

### Example

From [`Validators.cs`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Instance/Validators.cs):
```csharp
using Fdw.Collections;

namespace Reference.TypeCollections.Instance;

/// <summary>
/// TypeInstanceCollection for validators.
/// Creates new validator instances on each access - useful for stateful or disposable options.
/// </summary>
/// <remarks>
/// <para>
/// Unlike TypeCollection (which returns singleton instances), TypeInstanceCollection
/// creates a new instance every time you access an option. This is ideal for:
/// </para>
/// <list type="bullet">
/// <item>Validators with internal state (validation context, accumulated errors)</item>
/// <item>Disposable resources that need fresh instances</item>
/// <item>Options that should not share state across uses</item>
/// </list>
/// </remarks>
[TypeInstanceCollection(typeof(ValidatorBase), typeof(IValidator), typeof(Validators))]
public abstract partial class Validators : TypeCollectionBase<ValidatorBase, IValidator>
{
    // Source generator creates:
    // - Factory dictionary (stores Func<T> factories, not instances)
    // - CreateById(), CreateByName() methods (return new instances)
    // - Register() method for runtime factory registration
}
```

### Generated Code (Conceptual)

```csharp
partial class Validators
{
    // Stores factories, not instances
    private static readonly Dictionary<string, Func<IValidator>> _factories;

    static Validators()
    {
        _factories = new Dictionary<string, Func<IValidator>>
        {
            ["Required"] = () => new RequiredValidator(),
            ["Email"] = () => new EmailValidator(),
        };
    }

    // Static accessor returns NEW instance each time
    public static RequiredValidator Required => new RequiredValidator();
    public static EmailValidator Email => new EmailValidator();

    // CreateByName returns new instance
    public static IValidator CreateByName(string name) =>
        _factories.TryGetValue(name, out var factory)
            ? factory()
            : throw new KeyNotFoundException($"Validator '{name}' not found");
}
```

---

## 4. ServiceTypeCollection

ServiceTypeCollections extend TypeCollections with dependency injection support and a three-phase registration pattern.

### Additional Components

```
+---------------------------------------------------------------------+
|  Same as TypeCollection, PLUS:                                      |
+---------------------------------------------------------------------+
|  - Uses Guid keys (UUID v5 derived from the type)                   |
|  - Base class has RegisterServices() and RegisterFactory() methods  |
|  - Collection has RegisterAll() and InitializeFactories() methods   |
|  - Uses [TypeOption] attribute (NOT [ServiceTypeOption])            |
|  - Three-phase registration pattern for proper DI lifecycle         |
+---------------------------------------------------------------------+
```

### Example

The following example shows a ServiceTypeCollection from the Reference Solution - a notification types collection implementing the three-phase registration pattern.

From [`NotificationTypeBase.cs`](../samples/ReferenceSolution/concepts/02-service-types/src/Reference.ServiceTypes/ServiceTypes/NotificationTypeBase.cs):
```csharp
// ═══════════════════════════════════════════════════════════════════
// 1. INTERFACE AND BASE CLASS
// ═══════════════════════════════════════════════════════════════════
using Fdw.Collections;
using Microsoft.Extensions.DependencyInjection;

namespace Reference.ServiceTypes.ServiceTypes;

/// <summary>
/// Interface for notification service types.
/// </summary>
public interface INotificationType : ITypeOption<int, NotificationTypeBase>
{
    string ChannelType { get; }
    string DisplayName { get; }
    void RegisterServices(IServiceCollection services);
    void RegisterFactory(NotificationProvider provider, IServiceProvider services);
}

/// <summary>
/// Base class for notification service types.
/// Implements the three-phase registration pattern.
/// </summary>
public abstract class NotificationTypeBase : TypeOptionBase<int, NotificationTypeBase>, INotificationType
{
    protected NotificationTypeBase(int id, string name, string channelType, string displayName)
        : base(id, name)
    {
        ChannelType = channelType;
        DisplayName = displayName;
    }

    public string ChannelType { get; }
    public new string DisplayName { get; }

    // Phase 1: Register services with DI container
    public abstract void RegisterServices(IServiceCollection services);

    // Phase 2: Register factory with provider
    public abstract void RegisterFactory(NotificationProvider provider, IServiceProvider services);
}
```

From [`NotificationTypes.cs`](../samples/ReferenceSolution/concepts/02-service-types/src/Reference.ServiceTypes/ServiceTypes/NotificationTypes.cs):
```csharp
// ═══════════════════════════════════════════════════════════════════
// 2. COLLECTION - With three-phase registration methods
// ═══════════════════════════════════════════════════════════════════
using Fdw.Collections;
using Microsoft.Extensions.DependencyInjection;

namespace Reference.ServiceTypes.ServiceTypes;

[TypeCollection(typeof(NotificationTypeBase), typeof(INotificationType), typeof(NotificationTypes))]
public abstract partial class NotificationTypes : TypeCollectionBase<NotificationTypeBase, INotificationType>
{
    /// <summary>
    /// Phase 1: Register all notification type services with DI.
    /// Call this during ConfigureServices.
    /// </summary>
    public static void RegisterAll(IServiceCollection services)
    {
        // Register the provider as Scoped (default ProviderLifetime for [ServiceTypeCollection]).
        services.AddScoped<NotificationProvider>();

        // Register each notification type's services
        foreach (var type in All())
        {
            type.RegisterServices(services);
        }
    }

    /// <summary>
    /// Phase 2: Initialize factories by resolving from DI and registering with provider.
    /// Call this after Build(), before Run().
    /// </summary>
    public static void InitializeFactories(IServiceProvider serviceProvider)
    {
        // Get the provider (registered in Phase 1)
        var provider = serviceProvider.GetRequiredService<NotificationProvider>();

        // Register each notification type's factory with the provider
        foreach (var type in All())
        {
            type.RegisterFactory(provider, serviceProvider);
        }

        // IMPORTANT: serviceProvider is NOT stored anywhere!
    }
}
```

From [`EmailNotificationType.cs`](../samples/ReferenceSolution/concepts/02-service-types/src/Reference.ServiceTypes/Channels/Email/EmailNotificationType.cs):
```csharp
// ═══════════════════════════════════════════════════════════════════
// 3. SERVICE TYPE OPTIONS
// ═══════════════════════════════════════════════════════════════════
using Fdw.Collections;
using Microsoft.Extensions.DependencyInjection;

namespace Reference.ServiceTypes.Channels.Email;

[TypeOption(typeof(NotificationTypes), "Email")]
public sealed class EmailNotificationType : NotificationTypeBase
{
    public EmailNotificationType()
        : base(id: 1, name: "Email", channelType: "Email", displayName: "Email Notification")
    {
    }

    /// <summary>
    /// Phase 1: Register email-specific services with DI.
    /// </summary>
    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IEmailNotificationFactory, EmailNotificationFactory>();
    }

    /// <summary>
    /// Phase 2: Resolve factory from DI and register with provider.
    /// </summary>
    public override void RegisterFactory(NotificationProvider provider, IServiceProvider services)
    {
        var factory = services.GetRequiredService<IEmailNotificationFactory>();
        provider.RegisterFactory(ChannelType, factory);
    }
}
```

### Usage Pattern

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Phase 1: Register services with DI
NotificationTypes.RegisterAll(builder.Services);

var app = builder.Build();

// Phase 2: Initialize factories (after Build(), before Run())
NotificationTypes.InitializeFactories(app.Services);

// Phase 3: Runtime - use provider to create instances
// (No IServiceProvider stored or used at runtime!)

app.Run();
```

---

## 5. Generics Support

TypeCollections can be generic. The generic type parameter becomes part of the matching key. This enables type-safe collections for different entity types while reusing the same infrastructure.

### Conceptual Example: Generic TypeCollection

The following example demonstrates how generic TypeCollections work conceptually:

```csharp
// Generic interface
public interface IDataReader<TEntity> : ITypeOption<int, DataReaderBase<TEntity>>
    where TEntity : class
{
    Task<TEntity?> ReadAsync(string id);
}

// Generic base class
public abstract class DataReaderBase<TEntity> : TypeOptionBase<int, DataReaderBase<TEntity>>, IDataReader<TEntity>
    where TEntity : class
{
    protected DataReaderBase(int id, string name) : base(id, name) { }

    public abstract Task<TEntity?> ReadAsync(string id);
}

// Generic collection - uses unbound generics in attribute
[TypeCollection(
    typeof(DataReaderBase<>),           // Unbound generic
    typeof(IDataReader<>),              // Unbound generic
    typeof(DataReaders<>))]             // Unbound generic
public abstract partial class DataReaders<TEntity> : TypeCollectionBase<DataReaderBase<TEntity>, IDataReader<TEntity>>
    where TEntity : class
{
}

// Concrete implementations specify the closed generic argument
[TypeOption(typeof(DataReaders<Customer>), "SqlCustomerReader")]
public sealed class SqlCustomerReader : DataReaderBase<Customer>
{
    public SqlCustomerReader() : base(1, "SqlCustomerReader") { }
    public override Task<Customer?> ReadAsync(string id) => /* implementation */;
}

[TypeOption(typeof(DataReaders<Order>), "SqlOrderReader")]
public sealed class SqlOrderReader : DataReaderBase<Order>
{
    public SqlOrderReader() : base(1, "SqlOrderReader") { }
    public override Task<Order?> ReadAsync(string id) => /* implementation */;
}
```

### Usage

```csharp
// Each closed generic gets its own collection
var customerReader = DataReaders<Customer>.ByName("SqlCustomerReader");
var orderReader = DataReaders<Order>.ByName("SqlOrderReader");

// Type safety - compiler enforces correct entity types
Customer? customer = await customerReader.ReadAsync("123");
Order? order = await orderReader.ReadAsync("456");
```

---

## Key Rules Summary

### TypeLookup Properties

| Property | Attribute | Must Be Unique |
|----------|-----------|----------------|
| `Id` | `[TypeLookup("ById")]` | ✅ Yes (auto-generated hash) |
| `Name` | `[TypeLookup("ByName")]` | ✅ Yes |
| Custom | `[TypeLookup("ByXxx")]` | ✅ Yes - analyzer error if duplicates |

### Cross-Project Discovery

- **Same project**: TypeOptions discovered at compile time, included in static accessors
- **Referenced projects**: TypeOptions discovered, included in collection
- **Downstream projects**: NOT visible to parent project's collection
  - Use **MutableTypeCollection** with runtime registration
  - OR define collection in outermost consuming project

### Analyzer Diagnostics

| Code | Severity | Description |
|------|----------|-------------|
| TC001 | Warning | Type option missing required `[TypeOption]` attribute (won't be discovered) |
| TC002 | Error | Generic return type in base class doesn't match the `[TypeCollection]` attribute |
| TC003 | Error | Base type in base class doesn't match the `[TypeCollection]` attribute |
| TC004 | Error | Generic type argument mismatch between `[TypeOption]` and the base class |
| TYPECOLL001 | Warning | A `[TypeLookup]` generates a method that conflicts with a collection member |
| ENHENUM001 | Warning | Duplicate lookup values without `AllowMultiple` |

(ServiceType collections share the same `TC*` analyzers; there is no separate `ST*` rule family.)

---

## Next Steps

- [TypeCollection Overview](04-01-Overview.md) - Conceptual introduction to TypeCollections
- [Base Classes](04-02-Base-Classes.md) - Implementing TypeOptionBase and interfaces
- [Concrete Options](04-03-Concrete-Options.md) - Creating TypeOption implementations
- [Collection Declaration](04-04-Collection-Declaration.md) - Declaring TypeCollection classes
- [Generated Lookups](04-05-Generated-Lookups.md) - Understanding generated lookup methods
- [Dispatcher Pattern](04-06-Dispatcher-Pattern.md) - Replacing switch statements with TypeCollections
- [Reference Solution](../samples/ReferenceSolution/concepts/01-type-collections/README.md) - Complete working examples
