### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
COLL0001 | Critical | Error | InstancePropertyAnalyzer - Static Instance property pattern is forbidden on TypeOption classes
TC001 | Usage | Warning | MissingTypeOptionAnalyzer - Type option missing required [TypeOption] attribute
TC002 | Usage | Error | MissingTypeOptionAnalyzer - TGeneric in base class doesn't match defaultReturnType in TypeCollection attribute
TC003 | Usage | Error | MissingTypeOptionAnalyzer - TBase in base class doesn't match baseType in TypeCollection attribute
TC004 | Usage | Error | GenericTypeArgumentMismatchAnalyzer - Generic type argument mismatch between TypeOption attribute and base class
TYPECOLL001 | Collections | Warning | TypeLookupNamingConflictAnalyzer - TypeLookup generates method that conflicts with collection member

### Removed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
FDW042 | Usage | Warning | EnhancedEnumBaseAnalyzer - keyed on attribute names no framework type has (FDW-3511)
FDW043 | Design | Warning | EnhancedEnumConstructorAnalyzer - keyed on attribute names no framework type has (FDW-3511)
