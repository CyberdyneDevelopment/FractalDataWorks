# Complete TypeCollection Architecture

## Core Concept: Two Separate Key Types

**Key Insight**: TypeCollections have TWO different key types that serve different purposes:

1. **Collection's Own Id** (always `int`) - Used when the collection itself is treated as a TypeOption
2. **TypeOptions' Keys** (`TKey`) - Used for looking up TypeOptions within the collection

## Architecture

### 1. TypeCollectionBase Signature (NO TKey parameter)

```csharp
public abstract class TypeCollectionBase<TBase, TGeneric> :
    TypeOptionBase<int, TypeCollectionBase<TBase, TGeneric>>
    where TBase : class, TGeneric
    where TGeneric : class
{
    // Inherits from TypeOptionBase:
    // - public int Id { get; } (hash of collection type name)
    // - public string Name { get; } (auto-generated from type name)
    // - public string Category { get; } (defaults to "TypeCollections")
}
```

**Why no TKey parameter?**
- The TypeCollection's own identity uses `int` (hash)
- The `TKey` for TypeOptions is extracted from `TBase` at generation time

### 2. TypeOption Base Classes

```csharp
// Base type that TypeOptions inherit from
public abstract class CommandTypeBase :
    TypeOptionBase<int, CommandTypeBase>,  // <-- Declares TKey = int
    IGenericCommandType
{
    protected CommandTypeBase(int id, string name) : base(id, name) { }
}

// Concrete TypeOption
[TypeOption(typeof(CommandTypes), "Add")]
public sealed class AddCommand : CommandTypeBase
{
    public AddCommand() : base(1, "Add") { }
}
```

### 3. TypeCollection Declaration

```csharp
[TypeCollection(typeof(CommandTypeBase), typeof(IGenericCommandType), typeof(CommandTypes))]
public abstract partial class CommandTypes :
    TypeCollectionBase<CommandTypeBase, IGenericCommandType>  // NO TKey parameter
{
    // Nothing needed - inheritance from TypeOptionBase provides Id/Name/Category
}
```

### 4. How TKey is Discovered

**Generator algorithm:**

1. User declares: `TypeCollection(typeof(CommandTypeBase), ...)`
2. Generator looks at `CommandTypeBase`
3. Finds: `CommandTypeBase : TypeOptionBase<int, CommandTypeBase>`
                              ^^^
4. Extracts `TKey = int` from the `ITypeOption<int, ...>` implementation
5. Generates: `FrozenDictionary<int, CommandTypeBase> _all = ...`

**Code in generator** (TypeCollectionGenerator.cs:829):

```csharp
private static ITypeSymbol? ExtractTKeyFromBaseType(INamedTypeSymbol baseType)
{
    // Look at what the BASE TYPE implements (not the collection)
    foreach (var iface in baseType.AllInterfaces)
    {
        if (iface.Name == "ITypeOption" && iface.TypeArguments.Length == 2)
        {
            return iface.TypeArguments[0];  // This is TKey
        }
    }
    return null;
}
```

### 5. Generated Code Example

```csharp
public abstract partial class CommandTypes
{
    // TKey extracted from CommandTypeBase's ITypeOption<int, CommandTypeBase>
    private static readonly FrozenDictionary<int, CommandTypeBase> _all =
        new Dictionary<int, CommandTypeBase>
        {
            [1] = new AddCommand(),
            [2] = new DeleteCommand()
        }.ToFrozenDictionary();

    public static IGenericCommandType Id(int id) =>
        _all.TryGetValue(id, out var result) ? result : _empty;

    public static IGenericCommandType Add => _all[1];
    public static IGenericCommandType Delete => _all[2];
}
```

## Parent-Child Collections (MemberOf)

### Parent Collection

```csharp
// Parent collects ITypeOption<string> (child collections with string keys)
[TypeCollection(typeof(ITypeOption<string>), typeof(ITypeOption<string>), typeof(AllTests))]
public abstract partial class AllTests :
    TypeCollectionBase<ITypeOption<string>, ITypeOption<string>>  // Collects children
{
    // Generator extracts TKey from ITypeOption<string> → TKey = string
    // Generated:
    // private static readonly FrozenDictionary<string, ITypeOption<string>> _all = ...
    // public static ITypeOption<string> Get(string key) => ...
}
```

### Child Collection

```csharp
[TypeCollection(typeof(TestType), typeof(ITestType), typeof(TestCollection), MemberOf = typeof(AllTests))]
public abstract partial class TestCollection :
    TypeCollectionBase<TestType, ITestType>,  // Collects TestTypes (with int keys)
    ITypeOption<string, TestCollection>       // IS a TypeOption with string key for parent
{
    public static string Id => "Test";  // Key for registration with AllTests
    public static string Name => "Test Collection";

    // Generator creates:
    // static TestCollection() { AllTests.Register("Test", Empty()); }
}
```

**Key Points:**

1. `TestCollection` has TWO roles:
   - **As a TypeCollection**: Contains `TestTypes` with `int` keys (from `TestType`'s `ITypeOption`)
   - **As a TypeOption**: Has `string` key for parent registration (via `ITypeOption<string, TestCollection>`)

2. TKey extraction happens differently:
   - **For TestCollection's dictionary**: Extract from `TestType : ITypeOption<int, ...>` → `TKey = int`
   - **For parent registration**: `TestCollection` implements `ITypeOption<string, ...>` → `ParentKey = string`

## Benefits

1. **No TKey parameter pollution** - TypeCollectionBase stays simple
2. **Type safety** - TKey is derived from actual TypeOption declarations
3. **Flexibility** - Different collections can use different TKey types (int, string, Guid, etc.)
4. **Automatic** - No manual TKey specification needed
5. **Consistent** - All TypeCollections implement ITypeOption via TypeOptionBase inheritance

## Migration Path

**Old (broken):**
```csharp
public abstract partial class CommandTypes :
    TypeCollectionBase<int, CommandTypeBase, IGenericCommandType>
```

**New (correct):**
```csharp
public abstract partial class CommandTypes :
    TypeCollectionBase<CommandTypeBase, IGenericCommandType>
```

The `TKey` is discovered automatically from `CommandTypeBase : ITypeOption<int, CommandTypeBase>`.

## Generator Changes Summary

### Key Methods Added/Modified

1. **ExtractTKeyFromBaseType()** (Line 829)
   - Extracts `TKey` from base type's `ITypeOption<TKey, TSelf>` implementation
   - Looks at **base type**, not collection class
   - Returns `ITypeSymbol` for the key type

2. **BuildTypeCollectionDefinition()** (Line 760-770)
   - Calls `ExtractTKeyFromBaseType(baseType)` to get TKey
   - Stores `KeyType` in model for code generation
   - Reports diagnostic if base type doesn't implement ITypeOption

3. **DetectReturnType()** (Line 849)
   - Updated to handle 1-parameter and 2-parameter TypeCollectionBase
   - `TypeCollectionBase<TBase>` → returns `TBase`
   - `TypeCollectionBase<TBase, TGeneric>` → returns `TGeneric`

### Code Generation Flow

```
User Code:
[TypeCollection(typeof(CommandTypeBase), typeof(IGenericCommandType), typeof(CommandTypes))]
public abstract partial class CommandTypes : TypeCollectionBase<CommandTypeBase, IGenericCommandType>

↓

Generator Analysis:
1. Finds CommandTypeBase in attribute
2. Analyzes CommandTypeBase : ITypeOption<int, CommandTypeBase>
                                           ^^^
3. Extracts TKey = int
4. Detects return type = IGenericCommandType (from 2nd generic param)

↓

Generated Code:
private static readonly FrozenDictionary<int, CommandTypeBase> _all = ...
public static IGenericCommandType Id(int id) => ...
```

## Design Principles

1. **Type Information Flows From Base Types**
   - TKey comes from base type's ITypeOption implementation
   - Return type comes from TypeCollectionBase generic parameters
   - No redundant type specifications needed

2. **Collections Are Also TypeOptions**
   - Every TypeCollection inherits from TypeOptionBase
   - Enables hierarchical collection structures
   - Consistent Id/Name/Category properties

3. **Compile-Time Type Safety**
   - Dictionary key types match TypeOption Id types
   - Incorrect types caught at compile time
   - No runtime type conversions needed

4. **Generator Validates Structure**
   - Ensures base types implement ITypeOption
   - Reports diagnostics for missing interfaces
   - Prevents generation of invalid code
