# Fdw.ServiceTypes.Analyzers

Analyzers for ServiceTypeCollection declarations and their options.

Roslyn analyzers that enforce FDW conventions at compile time.

> **These rules do not ship.** This project is not packable and is not packed to `analyzers/dotnet/cs` by any package here, so the diagnostics below bind this repository only. Downstream consumers get the generated code, not the rules.

## Diagnostics

`ENHENUM001` · `FDW025` · `FDW026` · `FDW027` · `FDW028` · `FDW029` · `FDW030` · `FDW031` · `FDW032` · `FDW033` · `FDW034` · `FDW044` · `FDW045` · `SVCTYPE001` · `TC001` · `TC002` · `TC003`

## Analyzers

| Type | Kind | Purpose |
|---|---|---|
| `AbstractMemberAnalyzer` | class | Analyzer that warns about abstract properties and fields in enhanced enum base classes. Suggests using… |
| `DuplicateLookupValueAnalyzer` | class | Analyzer that detects duplicate lookup values in enhanced enum ServiceTypes when AllowMultiple is not… |
| `DuplicateTypeOptionAnalyzer` | class | Analyzer that detects duplicate enum option names within the same collection. |
| `FactoryProviderInjectionAnalyzer` | class | Analyzer that keeps service factories PURE: a class implementing Fdw.Abstractions.IServiceFactory must… |
| `InstancePropertyAnalyzer` | class | Analyzer that detects and forbids the static Instance property pattern on TypeOption/ServiceType… |
| `MissingTypeOptionAnalyzer` | class | Analyzer that warns when a type inherits from a base type specified in a [TypeCollection] attribute but… |
| `ServiceProviderInjectionAnalyzer` | class | Analyzer that ensures a service-type-option service (a class implementing an IServiceOption-derived… |
| `ServiceServiceTypeCollectionAttributeAnalyzer` | class | Analyzer that ensures EnumCollection attribute has CollectionName specified and validates inheritance. |
| `TypeLookupNamingConflictAnalyzer` | class | Analyzer that detects when a [TypeLookup] attribute on a base type property will generate a method that… |
| `TypeOptionBaseAnalyzer` | class | Analyzer that enforces ITypeOption implementation on type option base classes. |
| `TypeOptionBaseConstructorAnalyzer` | class | Analyzer that enforces constructor-based patterns for enhanced enum base classes. |
| `TypeOptionConstructorAnalyzer` | class | Analyzer that ensures every cross-assembly [TypeOption]-tagged class has a public parameterless… |

## Installation

```bash
dotnet add package Fdw.ServiceTypes.Analyzers --prerelease
```

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
