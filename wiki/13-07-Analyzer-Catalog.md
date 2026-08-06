# Analyzer Catalog

The complete catalogue of FractalDataWorks Roslyn analyzers, derived from the `DiagnosticDescriptor` definitions in the four analyzer projects under `public/src/Fdw.*.Analyzers/`. Each row lists the diagnostic id, what it enforces, and its **declared** severity (the descriptor's `DiagnosticSeverity`).

> **Declared severity vs Release enforcement.** The severity column is the analyzer's own default. Whether a warning *fails a Release build* is a separate decision configured in `public/Directory.Build.props` and `.editorconfig` (warnings-as-errors, `WarningsNotAsErrors`, `NoWarn`). For the Release-gate view and the `[ConventionOverride]` thresholds, see [Code Conventions](13-03-Code-Conventions.md).

## Analyzer Projects

| Project | Owns | Keyed on |
|---------|------|----------|
| `Fdw.Analyzers` | FDW001–004, FDW012–016, FDW022–023 | Result/logging/exception patterns |
| `Fdw.Conventions.Analyzers` | FDW005–011, FDW017–021 | File/method/type conventions |
| `Fdw.ServiceTypes.Analyzers` | FDW024–034 | `[ServiceTypeCollection]` / `[ServiceTypeOption]` |
| `Fdw.Collections.Analyzers` | FDW035–043, TC001–004 | `[TypeCollection]` / `[TypeOption]` / enum collections |

Legacy `ENH`-prefixed analyzers were renumbered into the `FDW024`–`FDW043` band; `PLATSVC001` was deleted; the `Fdw.ServiceTypes.Analyzers` and `Fdw.Collections.Analyzers` projects are now wired globally in `public/Directory.Build.props`. `FDW044` is the newest rule.

## Fdw.Analyzers — Results, Logging & Exceptions

| ID | Enforces | Severity |
|----|----------|----------|
| FDW001 | Method name should not end with `Async` | Warning |
| FDW002 | Use MessageLogging or ResultCode instead of a plain-string `GenericResult.Failure` | Warning |
| FDW003 | Use a MessageLogging method instead of a direct `ILogger` call | Warning |
| FDW004 | Use a MessageLogging method instead of `new GenericMessage()` | Warning |
| FDW012 | `GenericResult` value is not checked | Warning |
| FDW013 | `GenericResult` failure path is not handled | Warning |
| FDW014 | Exception not propagated in `GenericResult` | Warning |
| FDW015 | Result chain broken — use `ToNewResult()` or `Chain()` to preserve context | Warning |
| FDW016 | `IGenericResult<T>.Value` accessed without a success check | Warning |
| FDW022 | Swallowed exception — caught but neither observed nor rethrown | Warning |
| FDW023 | Broad `catch (Exception)` / bare `catch` with no specific clause and no `when` filter (survey) | Info |

## Fdw.Conventions.Analyzers — File/Method/Type Conventions

| ID | Enforces | Severity |
|----|----------|----------|
| FDW005 | File name must match type name (generic-arity aware) | Warning |
| FDW006 | Method is too long | Warning |
| FDW007 | Method is too complex (cyclomatic) | Warning |
| FDW008 | Method name should not contain underscores | Warning |
| FDW009 | Duplicate type name in compilation (disabled by default) | Warning |
| FDW010 | Implementation-specific type in a base/Abstractions assembly | Info |
| FDW011 | Service/Config/TypeOption with an implementation prefix in a base assembly | Warning |
| FDW017 | Enum declaration should be replaced with a TypeCollection | Warning |
| FDW018 | Switch on an enum type suggests a TypeCollection `ByName` lookup | Warning |
| FDW019 | If/else chain comparing enum values suggests TypeCollection `ByName` dispatch | Warning |
| FDW020 | Abstract type has no implementation | Info |
| FDW021 | Type is not referenced in the compilation | Info |

Details and `[ConventionOverride]` thresholds for FDW005–011: [Code Conventions](13-03-Code-Conventions.md).

## Fdw.ServiceTypes.Analyzers — ServiceTypeCollection / ServiceTypeOption

| ID | Enforces | Severity |
|----|----------|----------|
| FDW024 | Every `[ServiceTypeCollection]` must declare the three static PlatformServices phase methods (`Configure`/`Register`/`Initialize`) so PlatformServices discovers it (has a code fix) | Error |
| FDW025 | Singleton/instance property pattern is forbidden on a service type | Error |
| FDW026 | Duplicate `[ServiceTypeOption]` name | Error |
| FDW027 | `[ServiceTypeOption]` missing a public parameterless constructor | Error |
| FDW028 | Abstract property in a service-type enhanced enum | Warning |
| FDW029 | Abstract field in a service-type enhanced enum | Error |
| FDW030 | Collection attribute must specify a collection name | Error |
| FDW031 | Collection class must inherit the required base | Error |
| FDW032 | Generic collection must specify a non-generic interface constraint for `T` | Error |
| FDW033 | Type-option base class should implement `ITypeOption` | Warning |
| FDW034 | Enhanced-enum base class should use the constructor-based pattern | Warning |
| FDW044 | A service-option (`IServiceOption`) must inject another service-option through its `IFdwServiceProvider<TService, TConfiguration>`, never the service interface directly | Error |

`FDW044` (`ServiceProviderInjectionAnalyzer`) detection is semantic and transitive (`AllInterfaces`); `IFdwServiceProvider<...>` parameters are the correct shape and excluded. A parameter may opt out with `[ServiceOptionDependency]` when the owning provider supplies it already-resolved. See [TypeCollection Patterns](10-TypeCollection-Patterns.md#service-options-and-the-provider-injection-contract).

## Fdw.Collections.Analyzers — TypeCollection / TypeOption

| ID | Enforces | Severity |
|----|----------|----------|
| FDW035 | Duplicate `[TypeOption]` name | Error |
| FDW036 | `[TypeOption]` missing a public parameterless constructor | Error |
| FDW037 | Abstract property in an enhanced enum | Warning |
| FDW038 | Abstract field in an enhanced enum | Error |
| FDW039 | Collection attribute must specify a collection name | Error |
| FDW040 | Collection class must inherit the required base | Error |
| FDW041 | Generic collection must specify a non-generic interface constraint for `T` | Error |
| FDW042 | Enhanced-enum base class should implement `ITypeOption` | Warning |
| FDW043 | Enhanced-enum base class should use the constructor-based pattern | Warning |
| TC001 | Type option missing the required `[TypeOption]` attribute | Warning |
| TC002 | `TGeneric` in the base class doesn't match `defaultReturnType` in the `[TypeCollection]` attribute | Error |
| TC003 | `TBase` in the base class doesn't match `baseType` in the `[TypeCollection]` attribute | Error |
| TC004 | Generic type-argument mismatch between the `[TypeOption]` attribute and the base class | Error |

> The FDW026–FDW034 (ServiceTypes) and FDW035–FDW043 (Collections) families overlap in intent because the same structural checks exist for both the `[ServiceTypeCollection]`/`[ServiceTypeOption]` world and the `[TypeCollection]`/`[TypeOption]` world. `TC###` and `FDW####` are distinct diagnostic families; generator diagnostics (`ST###`, `SYSLIB100x`) are separate again.

## Related Documentation

- [Code Conventions](13-03-Code-Conventions.md) — FDW005–011 details, `[ConventionOverride]`, code fixes, `fdw-split`
- [TypeCollection Patterns](10-TypeCollection-Patterns.md) — `IServiceOption`, FDW044, FDW024, `[ServiceOptionDependency]`
- [Result Integration](07-05-Result-Integration.md) — the `GenericResult` patterns FDW012–016 enforce
