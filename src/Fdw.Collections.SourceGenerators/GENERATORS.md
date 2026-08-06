# TypeCollection Source Generators

## Overview

This project contains 6 source generators for TypeCollections that enable compile-time plugin discovery with cross-assembly support.

## Generator Inventory

| Generator | Attribute | ID Type | Storage | Use Case |
|-----------|-----------|---------|---------|----------|
| `TypeCollectionGenerator` | `[TypeCollection]` | `int` (FNV-1a) | FrozenDictionary | Static, compile-time known options |
| `TypeInstanceCollectionGenerator` | `[TypeInstanceCollection]` | `int` (FNV-1a) | Dictionary + Func | Factory pattern (new instances) |
| `MutableTypeCollectionGenerator` | `[MutableTypeCollection]` | `int` (FNV-1a) | ConcurrentDictionary | Runtime registration/plugin systems |
| `ServiceTypeCollectionGenerator` | `[ServiceTypeCollection]` | `Guid` (UUID v5) | FrozenDictionary | DI-integrated services |
| `ServiceTypeInstanceCollectionGenerator` | `[ServiceTypeInstanceCollection]` | `Guid` (UUID v5) | Dictionary + Func | DI services with factory pattern |
| `MutableServiceTypeCollectionGenerator` | `[MutableServiceTypeCollection]` | `Guid` (UUID v5) | ConcurrentDictionary | Runtime DI service registration |

## Architecture

### Shared Utilities (`Shared/` folder)

| File | Purpose |
|------|---------|
| `Models.cs` | Value-equatable `record struct` models for caching |
| `TypeOptionDiscovery.cs` | Cross-assembly TypeOption discovery via SymbolVisitor |
| `ServiceTypeOptionDiscovery.cs` | Cross-assembly ServiceTypeOption discovery |
| `CodeGeneration.cs` | Shared code generation helpers |
| `Diagnostics.cs` | Diagnostic descriptors (TC001-TC007, ST001-ST004) |

### Discovery Pattern

All generators follow this pattern:

```csharp
public void Initialize(IncrementalGeneratorInitializationContext context)
{
    // 1. Discover collections via ForAttributeWithMetadataName (current compilation only)
    var collectionsProvider = context.SyntaxProvider
        .ForAttributeWithMetadataName(AttributeName, predicate, transform);

    // 2. Discover ALL TypeOptions via Compilation traversal (cross-assembly)
    var optionsProvider = context.CompilationProvider
        .Select((compilation, _) => TypeOptionDiscovery.DiscoverAll(compilation, restrict));

    // 3. Combine and generate
    context.RegisterSourceOutput(collectionsProvider.Combine(optionsProvider), Execute);
}
```

## Feature Comparison

### Collection Types

| Feature | Immutable | Instance/Factory | Mutable |
|---------|-----------|------------------|---------|
| Storage | FrozenDictionary | Dictionary + Func | ConcurrentDictionary |
| Thread-Safe | Yes (immutable) | No | Yes |
| Runtime Registration | No | Yes | Yes |
| Returns Singleton | Yes | No (new instance) | Yes |
| O(1) Lookup | Yes | Yes | Yes |

### TypeCollection vs ServiceType

| Feature | TypeCollection | ServiceType |
|---------|---------------|-------------|
| ID Type | `int` (FNV-1a hash) | `Guid` (UUID v5 SHA1) |
| ID Collision Risk | Possible | Extremely rare |
| DI Integration | No | Yes (`Register(IServiceCollection)`) |
| Provider Generation | No | Optional (`GenerateProvider=true`) |

## Attribute Reference

### Constructor Arguments (All Attributes)

```csharp
[TypeCollection(
    typeof(BaseClass),        // Position 0: Base class for TypeOptions
    typeof(IInterface),       // Position 1: Interface TypeOptions implement
    typeof(CollectionClass),  // Position 2: The collection class itself (for matching)
    typeof(ParentCollection), // Position 3: Optional parent collection
    "ChildName"               // Position 4: Optional child accessor name
)]
```

### Named Arguments

| Argument | Type | Default | Description |
|----------|------|---------|-------------|
| `RestrictToCurrentCompilation` | `bool` | `false` | Skip cross-assembly discovery |
| `GenerateProvider` | `bool` | `false` | Generate factory provider (ServiceType only) |
| `ServiceInterface` | `Type` | `null` | For provider generation |
| `ConfigurationInterface` | `Type` | `null` | For provider generation |

### TypeOption Attributes

```csharp
[TypeOption(typeof(CollectionClass), "OptionName")]
public class MyOption : BaseClass { }

[ServiceTypeOption(typeof(ServiceTypeCollection), "ServiceName")]
public class MyServiceType : ServiceTypeBase { }
```

### TypeLookup Attribute

```csharp
// On base class property
[TypeLookup("BySqlOperator")]
public string SqlOperator { get; init; }

// Generates: FilterOperators.BySqlOperator("=")
```

## Generated API

### Immutable Collections

```csharp
// Static singleton accessors
public static MyType OptionName => ...;
public static MyType OptionName(params) => new(...);  // Constructor overloads

// Lookup methods
public static IInterface ById(int id) => ...;
public static IInterface ByName(string name) => ...;
public static IInterface CustomLookup(TKey value) => ...;  // From [TypeLookup]

// All items
public static IReadOnlyCollection<IInterface> All() => ...;

// NotFound sentinel
public static IInterface NotFound => ...;

// Child collections (if configured)
public static Type ChildName => typeof(ChildCollection);
public static IReadOnlyCollection<Type> ChildCollectionTypes { get; }
```

### Additional for Mutable

```csharp
public static bool Register(IInterface option);
public static bool Unregister(IInterface option);
```

### Additional for Factory/Instance

```csharp
public static void Register<T>(int id, string name) where T : IInterface, new();
public static IInterface CreateById(int id);
public static IInterface CreateByName(string name);
public static IEnumerable<IInterface> CreateAll();
```

### Additional for ServiceType

```csharp
public static void Register(IServiceCollection services);
```

## Diagnostics

| ID | Severity | Description |
|----|----------|-------------|
| TC001 | Error | ID hash collision between TypeOptions |
| TC002 | Error | TypeOption doesn't implement interface |
| TC003 | Warning | TypeCollection has no TypeOptions |
| TC004 | Error | TypeOption references non-existent collection |
| TC005 | Info | No [TypeLookup] properties on base class |
| TC007 | Error | Duplicate TypeOption name |
| ST001 | Error | ServiceType ID collision |
| ST002 | Error | ServiceType interface not implemented |
| ST003 | Warning | ServiceTypeCollection has no options |
| ST004 | Error | Duplicate ServiceType name |
