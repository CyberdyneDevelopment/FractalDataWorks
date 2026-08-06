# Fdw.Collections.Analyzers

Analyzers for TypeCollection declarations and their options.

Roslyn analyzers that enforce FDW conventions at compile time.

> **These rules do not ship.** This project is not packable and is not packed to `analyzers/dotnet/cs` by any package here, so the diagnostics below bind this repository only. Downstream consumers get the generated code, not the rules.

## Diagnostics

`COLL0001` · `ENHENUM001` · `FDW035` · `FDW036` · `FDW037` · `FDW038` · `FDW039` · `FDW040` · `FDW041` · `FDW042` · `FDW043` · `TC001` · `TC002` · `TC003` · `TC004` · `TYPECOLL001`

## Analyzers

| Type | Kind | Purpose |
|---|---|---|
| `AbstractMemberAnalyzer` | class | Analyzer that warns about abstract properties and fields in enhanced enum base classes. Suggests using… |
| `DuplicateEnumOptionAnalyzer` | class | Analyzer that detects duplicate enum option names within the same collection. |
| `DuplicateLookupValueAnalyzer` | class | Analyzer that detects duplicate lookup values in enhanced enum collections when AllowMultiple is not… |
| `EnhancedEnumBaseAnalyzer` | class | Analyzer that enforces ITypeOption implementation on enhanced enum base classes. |
| `EnhancedEnumConstructorAnalyzer` | class | Analyzer that enforces constructor-based patterns for enhanced enum base classes. |
| `EnumCollectionAttributeAnalyzer` | class | Analyzer that ensures EnumCollection attribute has CollectionName specified and validates inheritance. |
| `EnumOptionConstructorAnalyzer` | class | Analyzer that ensures enum options have a public parameterless constructor when factory methods are not… |
| `GenericTypeArgumentMismatchAnalyzer` | class | Analyzer that detects when a [TypeOption] attribute references a closed generic collection type but the… |
| `InstancePropertyAnalyzer` | class | Analyzer that detects and forbids the static Instance property pattern on TypeOption classes. This… |
| `MissingTypeOptionAnalyzer` | class | Analyzer that warns when a type inherits from a base type specified in a [TypeCollection] attribute but… |
| `TypeLookupNamingConflictAnalyzer` | class | Analyzer that detects when a [TypeLookup] attribute on a base type property will generate a method that… |

## Installation

```bash
dotnet add package Fdw.Collections.Analyzers --prerelease
```

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
