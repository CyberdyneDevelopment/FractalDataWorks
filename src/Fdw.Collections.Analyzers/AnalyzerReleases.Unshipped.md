### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
COLL0001 | Critical | Error | InstancePropertyAnalyzer - Static Instance property pattern is forbidden on TypeOption classes
FDW035 | Usage | Error | DuplicateEnumOptionAnalyzer - Duplicate enhanced enum option ID detected
FDW036 | Usage | Error | EnumOptionConstructorAnalyzer - Enhanced enum option constructor issues
FDW037 | Design | Warning | AbstractMemberAnalyzer - Abstract property in enhanced enum
FDW038 | Design | Error | AbstractMemberAnalyzer - Abstract field in enhanced enum  
FDW039 | Usage | Error | EnumCollectionAttributeAnalyzer - EnumCollection attribute must specify CollectionName
FDW040 | Usage | Error | EnumCollectionAttributeAnalyzer - EnumCollection classes must inherit from EnumOptionBase<T>
FDW041 | Usage | Error | EnumCollectionAttributeAnalyzer - Generic EnumCollection must specify a non-generic interface constraint for T
ENHENUM001 | Collections | Warning | DuplicateLookupValueAnalyzer - Duplicate lookup values detected without AllowMultiple
TC001 | Usage | Warning | MissingTypeOptionAnalyzer - Type option missing required [TypeOption] attribute
TC002 | Usage | Error | MissingTypeOptionAnalyzer - TGeneric in base class doesn't match defaultReturnType in TypeCollection attribute
TC003 | Usage | Error | MissingTypeOptionAnalyzer - TBase in base class doesn't match baseType in TypeCollection attribute
TC004 | Usage | Error | GenericTypeArgumentMismatchAnalyzer - Generic type argument mismatch between TypeOption attribute and base class
TYPECOLL001 | Collections | Warning | TypeLookupNamingConflictAnalyzer - TypeLookup generates method that conflicts with collection member
