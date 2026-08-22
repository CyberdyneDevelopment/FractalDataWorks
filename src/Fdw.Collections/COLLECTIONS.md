# Type Collections — the standard

Authoritative for `Fdw.Collections`, `Fdw.Collections.SourceGenerators`, and every collection
declared with these attributes. Every claim here was read from source; where behaviour and a comment
disagreed, the source won.

---

## 1. The one fact

**A collection gathers options that attached themselves. It never sweeps, scans, or discovers.**

Membership is decided at compile time by the generators and fixed at first access. There is no
runtime reflection, no assembly scanning, no discovery. When an option is missing from a collection
the question is always *which registration path did it take, and which precondition failed* — never
*why did the collection not find it*.

Everything below follows from that.

---

## 2. The four kinds

| kind | generator | membership | when to use |
|---|---|---|---|
| **Type collection** | `TypeCollectionGenerator` | fixed at freeze, singletons | an enhanced enum: a closed set of named, data-bearing values |
| **Service type collection** | `ServiceTypeCollectionGenerator` | fixed at freeze, singletons | a service domain the framework resolves and dispatches |
| **Mutable collection** | `MutableTypeCollectionGenerator` | `ConcurrentDictionary`, `Register()` at runtime | members arrive after startup and are not knowable at compile time |
| **Instance collection** | `TypeInstanceCollectionGenerator` | factory-based, new instance per request | members carry per-use state and cannot be shared |

`MutableServiceTypeCollectionGenerator` and `ServiceTypeInstanceCollectionGenerator` are the
service-type forms of the last two.

**Default to a plain type collection.** Reach for mutable only when membership genuinely is not
knowable at compile time, and for instance only when a member cannot be a singleton. Both give up the
freeze, and with it the guarantee that the member set is the same in every process.

---

## 2a. Choosing a kind — decision matrix

Answer in order. The first row that is true decides it.

| if | then | because |
|---|---|---|
| Members are not knowable until runtime — plugins, tenant-supplied, discovered from data | **Mutable** | the freeze cannot hold a set that has not arrived yet |
| A member carries per-use state — an open handle, a cursor, a buffer | **Instance** | a singleton shared across callers would leak state between them |
| The framework resolves and dispatches it, and it has a factory and configuration | **Service type** | it needs the three-phase lifecycle and a provider |
| Anything else — a closed named set of values | **Type collection** | the default; cheapest and the only one whose membership is provably identical in every process |

### What each kind costs

| | Type | Service type | Mutable | Instance |
|---|---|---|---|---|
| membership fixed at first access | yes | yes | **no** | yes |
| same member set in every process | yes | yes | **no** | yes |
| members are singletons | yes | yes | yes | **no** |
| self-registers from a library | **no** | **yes** | no | no |
| three-phase Configure/Register/Initialize | no | **yes** | no | no |
| duplicate key caught at build | yes (`TC008`) | yes (`ST005`) | **runtime only** | yes |
| `[Replaces]` honoured | yes | yes | yes | yes |

**Mutable and Instance give up the guarantee that makes a collection worth having.** A mutable
collection cannot be checked at build time and cannot be reasoned about from source alone — two
processes with the same binaries can disagree about its contents. Take that only when membership
genuinely is not knowable at compile time, and record why in the declaration.

### Compatibility — what may reference what

| from | may reference | never |
|---|---|---|
| `{Domain}.Abstractions` | nothing in the domain | the domain, any implementation |
| `{Domain}` | `{Domain}.Abstractions` | any implementation |
| `{Domain}.Clients` | `{Domain}.Abstractions` | **the domain**, any implementation |
| `{Domain}.Components` / `.Endpoints` | `{Domain}.Abstractions` | any implementation |
| `{Impl}` | `{Domain}`, its own abstractions | another implementation |
| `{Impl}.ServiceType` | `{Impl}` | anything else new |
| entry point | `{Impl}.Registration` | — |

The row that carries the isolation is `.Clients`: because every lookup returns an interface, a client
never needs the domain package. Twenty-seven of twenty-nine already satisfy this by accident.

### Changing a declaration — what breaks

| change | breaks | safe? |
|---|---|---|
| rename a **service type** option's `Name` | its Guid Id; persisted configuration rows | **no** |
| rename a **plain** option's type or namespace | its int Id, which nothing reads from storage | yes |
| move any option to a different **project**, same namespace | nothing — FQN and Id unchanged | yes |
| move a **plain** option to a different assembly | its registration path, not its Id | only via an entry point that scans it |
| move a **service type** option to a different assembly | nothing, if the package references the registration generator | yes |
| set `RestrictToCurrentCompilation = true` | reachability from every other assembly | only within one assembly |
| add an option to a frozen collection at runtime | throws | no |
| flip a lookup to `isUnique: false` | every consumer's call site — the return type changes | compile-time break, so visible |


---

## 3. Declaring a collection

```csharp
[TypeCollection(
    typeof(ConnectionStateBase),      // BaseType        — the option base class
    typeof(IConnectionState),         // DefaultReturnType — what lookups return
    typeof(ConnectionStates))]        // CollectionType  — this class
public partial class ConnectionStates : TypeCollectionBase<IConnectionState> { }
```

`partial` is required — the generator writes the other half.

### Options on `[TypeCollection]`

| property | default | effect |
|---|---|---|
| `UseMethods` | `false` | emit members as methods rather than properties |
| `RestrictToCurrentCompilation` | `false` | suppress cross-assembly registration for this collection |
| `GenerateUIComponent` | `false` | emit a picker component for the collection |
| `UIComponent` | `null` | use this component instead of a generated one |
| `TypeOption` + `TypeOptionName` | `null` | **child collection** — this collection is itself an option of a parent collection. Both must be set together; setting them makes `IsChildCollection` true |

---

## 4. Declaring an option

```csharp
[TypeOption(typeof(ConnectionStates), "Open")]
public sealed class OpenConnectionState : ConnectionStateBase
{
    public OpenConnectionState() : base("Open") { }   // REQUIRED: public parameterless ctor
}
```

### Options on `[TypeOption]`

| property | default | effect |
|---|---|---|
| `CollectionType` | *required* | the collection this option joins |
| `Name` | *required* | the option's key — **and, for service types, its identity** |
| `RestrictToCurrentCompilation` | `false` | this option is not registered via module initializer in consuming assemblies |
| `Category` | `null` | groups the option under a heading for `ByCategory` and UI pickers |

### `[Replaces]`

```csharp
[TypeOption(typeof(ConnectionStates), "Open")]
[Replaces(typeof(OpenConnectionState))]
public sealed class MyOpenConnectionState : ConnectionStateBase { }
```

Takes a `Type`, not a name — so the replacing assembly must reference the assembly it replaces from.
The entry-point generator builds a replacement map across all referenced assemblies and emits
registrations for the replacements only. Two options replacing the same target is `TC010`; replacing
a type nothing declares is `TC011`.

---

## 5. Identity — two schemes, and they differ

This is the most consequential difference between the two families and the easiest to get wrong.

| | `[TypeOption]` | `[ServiceTypeOption]` |
|---|---|---|
| base | `TypeOptionBase<int, T>` | `TypeOptionBase<Guid, IServiceType<Guid>>` |
| key type | `int` | `Guid` |
| derived from | **FNV-1a of `GetType().FullName`** | **MD5 of the bare `Name`** (`OptionId.Derive`) |
| computed | lazily, in the `Id` property | at construction, passed to `base(...)` |
| namespace-sensitive | **yes** | no |

```csharp
// TypeOptionBase — the int-keyed form
public override int Id => _id ?? GenerateIdFromName(GetType().FullName ?? GetType().Name);

// ServiceTypeBase — identity fixed at construction, from the NAME
protected static Guid DeriveId(string name) => OptionId.Derive(name);
```

**Why the plain form hashes the FQN and not the name:** names repeat. `"Query"` is the name of
`MsSqlQueryTranslator`, `PostgreSqlQueryTranslator`, `SqliteQueryTranslator` and `QueryCommand`.
Eight names in this codebase are shared that way.

**Requirements that follow:**

- **Renaming a plain option's type or namespace changes its Id.** Moving the *project* never does.
- **That costs nothing.** Option Ids are not persisted: a configuration row stores a NAME —
  `ConnectionConfiguration.ServiceType` and `.ServiceOptionType` are both `string` — and the provider
  dispatches on that string. An Id is an in-memory lookup key, recomputed every run.
  Even when Ids do get persisted, the databases are rebuilt from an idempotent seed, so a changed Id
  costs a reseed rather than a migration. **Namespace moves and type renames are ordinary work.** Use
  `MoveNamespace` and `MoveTypesToNamespace` where they are the right tool; the Id column in their
  reports is information, not a blocker.
- **Renaming a service type option's `Name` changes its Id.** Moving it between assemblies or
  namespaces does not. A rename orphans persisted configuration rows.
- **An explicitly supplied Id always wins** (`_id ??`). Nullable rather than a zero test, because
  **zero is a real Id** — the `NotFound` sentinel holds it.

---

## 6. Registration — the two paths

**Only the entry point assembles the full set.** A library cannot do cross-assembly registration for
plain options and has no reason to: it references a collection and consumes what arrived. When
diagnosing a missing option, the host's own generated initializer is the ground truth.

### Path A — same assembly

`TypeCollectionGenerator` emits a **static constructor** on the collection calling
`RegisterMember(new X())` for every option in the compilation. Runs on first touch. Needs nothing
from the host.

### Path B — across assemblies

| generator | emits for | scope |
|---|---|---|
| `TypeOptionModuleInitializerGenerator` | **executables only** — returns early for `DynamicallyLinkedLibrary` | scans every referenced assembly |
| `ServiceTypeOptionModuleInitializerGenerator` | executables **and libraries** | DLL mode scans the DLL's own types for options targeting a collection in a *different* assembly |

**A plain `[TypeOption]` in a library has no self-registration path.** A `[ServiceTypeOption]` does.
That asymmetry is what makes an option package viable for service types and not for plain options.

**Requirement:** an option package that must register itself references
`Fdw.Registration.SourceGenerators`. Without it no DLL-mode initializer is emitted and the option
registers only if the entry point happens to scan it.

**Overlap is expected and handled.** The entry-point scan does not exclude options whose collection
is in the same assembly, so a same-assembly option that leaves `RestrictToCurrentCompilation` off is
offered twice. `RegisterMember` absorbs it; see §7.

---

## 7. Lifecycle — pending, freeze, frozen

```
RegisterMember(option)  →  _pendingRegistrations  →  EnsureFrozen()  →  _all + lookups
```

`RegisterMember` is idempotent and dedupes on the **runtime type**:

```csharp
if (!_registeredTypes.Add(type.GetType()))
    return;                       // already registered — no-op at any point in the lifecycle
if (_frozen) { _registeredTypes.Remove(type.GetType()); throw ... }
_pendingRegistrations.Add(type);
```

**Membership is asked before frozen, deliberately.** Re-offering a member already present is harmless
even after the freeze, because the collection already holds it. Only a genuinely *new* member
arriving after the close is an error. Asking the frozen question first turned the harmless case into
a throw.

**The removal before throwing is deliberate too.** Without it a second attempt reads as a duplicate
and returns quietly — a loud failure turned silent.

**Requirement:** every assembly declaring options must be loaded before first access to the
collection. After freeze, a new member throws.

---

## 8. The generated surface

**Every lookup returns the interface. Only extension methods return the concrete option.**

```csharp
IConnectionState                       ById(int id)
IConnectionState                       ByName(string name)
IReadOnlyCollection<IConnectionState>  All()
IReadOnlyList<IConnectionState>        ByCategory(string category)
IReadOnlyList<string>                  Categories
TypeCollectionMetadata                 GetMetadata()
void                                   RegisterMember(IConnectionState)

// generated extension — the only place a concrete option is the return type
public static NoneSessionContext None() =>
    (NoneSessionContext)NoSessionContextTypes.ById(659313259);
```

**This is what makes package isolation work.** A consumer that calls `ByName("MsSql")` gets an
interface and never names the concrete type, so it needs the abstractions package and nothing else.
The option's package has to be present exactly once — at the entry point, so registration happens.

**Requirement:** consumers bind to `.Abstractions`. Only entry points reference option packages.

Any generated member can be overridden by declaring a static method with the exact signature in the
partial class — the generator detects it and skips generation.

### `[TypeLookup]` — declaring a lookup

```csharp
[TypeLookup("ByName")]                       // unique (default) → returns the option
public string Name { get; }

[TypeLookup("ByKind", isUnique: false)]      // non-unique → returns IReadOnlyList<T>
public string Kind { get; }
```

| parameter | default | effect |
|---|---|---|
| `MethodName` | *required* | the generated method's name |
| `ReturnType` | `null` | override the collection's return type for this lookup |
| `IsUnique` | **`true`** | one option per value → returns the option and `NotFound` on a miss. `false` → returns every match and an empty list on a miss |

**`IsUnique` is a promise the collection enforces.** A unique lookup builds a plain dictionary, so two
options carrying the same value throw at freeze, naming the collection, the lookup, both options, the
property and the value. A non-unique lookup groups instead, and `TC008` stops flagging its duplicates
because the duplicate is the feature.

**Default is true because it is the loud option**, not merely the compatible one: the same collision
under a non-unique lookup is a two-element list nobody inspects and every caller reads the first of.

---

## 9. Hard requirements

1. **Public parameterless constructor on every option.** The generator emits `new X()`. An option
   without one is silently skipped — `FDW027` catches it.
2. **`partial` on every collection class.**
3. **Never name an option a reserved member:** `All`, `ByCategory`, `ById`, `ByName`, `Categories`,
   `GetMetadata`, `NotFound`, `RegisterMember`. `TC012` / `ST006`.
4. **A package reference is registration intent.** Referencing an assembly that declares options is
   how a host asks for them. There is no separate opt-in list.
5. **Moving an option between assemblies is never a mechanical refactor.** It changes which generator
   sees it, and therefore whether it registers at all.
6. **A collection with no build-time member-count gate has no protection.** Nothing fails at build
   time when a collection empties; capture the counts from the host's generated initializer and fail
   the build on a decrease.
7. **Do not set `RestrictToCurrentCompilation` on an option that must be reachable from another
   assembly.** It is not merely unregistered there — it is unregisterable until the flag is cleared.

---

## 10. Diagnostics

| id | severity | meaning |
|---|---|---|
| `TC001` | Error | Id hash collision between two options |
| `TC002` | Error | option does not implement the collection's interface |
| `TC003` | Warning | collection declares no options |
| `TC004` | Error | referenced collection not found |
| `TC005` | Info | option declares no lookup properties |
| `TC007` | Error | two options share a name |
| `TC008` | Error | two options share a value for a **unique** lookup |
| `TC009` | Error | constructor parameter type cannot be safely defaulted |
| `TC010` | Error | two options declare `[Replaces]` on the same target |
| `TC011` | Warning | `[Replaces]` names a type nothing declares |
| `TC012` | Error | option name collides with a generated member |
| `ST001`–`ST006` | as above | the service-type equivalents |

Related analyzers: `FDW025` (static `Instance` property forbidden), `FDW026` (duplicate option Id),
`FDW027` (missing parameterless constructor), `FDW028`/`FDW029` (abstract members on an option),
`STC001`/`STC002` (phase-func ownership on service types).

---

## 11. Canonical shapes

### Plain type collection

```csharp
// Fdw.{Area}.Abstractions
public interface IThingKind : ITypeOption<int, IThingKind> { }

public abstract class ThingKindBase : TypeOptionBase<IThingKind>, IThingKind
{
    protected ThingKindBase(string name) : base(name) { }
}

[TypeCollection(typeof(ThingKindBase), typeof(IThingKind), typeof(ThingKinds))]
public partial class ThingKinds : TypeCollectionBase<IThingKind> { }

[TypeOption(typeof(ThingKinds), "Fast")]
public sealed class FastThingKind : ThingKindBase
{
    public FastThingKind() : base("Fast") { }
}
```

### Service type collection

```csharp
[ServiceTypeCollection(typeof(ThingTypeBase<,,>), typeof(IThingType), typeof(ThingTypes),
                       ServiceCategory = "Thing")]
public partial class ThingTypes : ServiceTypeCollectionBase<ThingTypeBase<...>, IThingType>
{
    static ThingTypes()
    {
        var collectOptions = RegisterFunc;          // capture, then call explicitly
        Registration((builder, loggerFactory) =>
        {
            // domain-wide wiring that every option needs goes HERE, not on the option base
            return collectOptions(builder, loggerFactory);
        });
    }
}

[ServiceTypeOption(typeof(ThingTypes), "MsSql")]
public sealed class MsSqlThingType : ThingTypeBase<...>
{
    public MsSqlThingType() : base(name: "MsSql", ...)
    {
        Registration((builder, loggerFactory) => { /* this option's own wiring */ });
    }
}
```

**A phase holds one body and the class declaring it owns that body.** `Append*`/`Prepend*` exist so a
consumer can customise a service type it did not author; using them inside the declaring class is
`STC001`, and a base class between `ServiceTypeBase` and the declared option setting a phase func at
all is `STC002`. Wiring every option needs belongs in the collection's `Register`, where the option
set is already in hand.

### Child collection

```csharp
[TypeCollection(typeof(ConverterBase), typeof(IConverter), typeof(MsSqlConverters),
                TypeOption = typeof(Converters), TypeOptionName = "MsSql")]
public partial class MsSqlConverters : TypeCollectionBase<IConverter> { }
```

The child is simultaneously a collection and an option of its parent. Both `TypeOption` and
`TypeOptionName` must be set.

---

## 12. Sources

Read for this document: `Fdw.Collections/Attributes/{TypeOption,TypeCollection,TypeLookup,Replaces}Attribute.cs`,
`Fdw.Collections/{TypeOptionBase,TypeCollectionBase,OptionId}.cs`,
`Fdw.Collections.SourceGenerators/{TypeCollectionGenerator,ServiceTypeCollectionGenerator,TypeOptionExtensionGenerator}.cs`,
`Fdw.Collections.SourceGenerators/Shared/{LookupPropertyModel,TypeOptionDiscovery,ServiceTypeOptionDiscovery,TypeCollectionGeneratorDiagnostics,ReservedMemberNames}.cs`,
`Fdw.Registration.SourceGenerators/{TypeOptionModuleInitializerGenerator,ServiceTypeOptionModuleInitializerGenerator}.cs`,
`Fdw.Services.Abstractions/{ServiceTypeBase,IServiceType}.cs`, and the generated
`ConnectionStates.TypeCollection.g.cs` plus ReferenceApi's `TypeOptionModuleInitializer.g.cs`.
