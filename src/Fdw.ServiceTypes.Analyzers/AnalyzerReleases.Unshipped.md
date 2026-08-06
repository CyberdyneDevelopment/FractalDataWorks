### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
FDW024 | Usage | Error | ServiceTypeCollectionPhaseMethodsAnalyzer - [ServiceTypeCollection]/[PlatformServiceProvider] class does not declare the static Configure/Register/Initialize phase methods the registration generator emits a method group for
FDW025 | Critical | Error | InstancePropertyAnalyzer - Static Instance property pattern is forbidden on TypeOption/ServiceType classes
FDW026 | Usage | Error | DuplicateTypeOptionAnalyzer - Duplicate enhanced enum option ID detected
FDW027 | Usage | Error | TypeOptionConstructorAnalyzer - [TypeOption] class missing a public parameterless constructor
FDW028 | Design | Warning | AbstractMemberAnalyzer - Abstract property in enhanced enum
FDW029 | Design | Error | AbstractMemberAnalyzer - Abstract field in enhanced enum  
FDW030 | Usage | Error | ServiceServiceTypeCollectionAttributeAnalyzer - EnumCollection attribute must specify CollectionName
FDW031 | Usage | Error | ServiceServiceTypeCollectionAttributeAnalyzer - EnumCollection classes must inherit from EnumOptionBase<T>
FDW032 | Usage | Error | ServiceServiceTypeCollectionAttributeAnalyzer - Generic EnumCollection must specify a non-generic interface constraint for T
ENHENUM001 | ServiceTypes | Warning | DuplicateLookupValueAnalyzer - Duplicate lookup values detected without AllowMultiple
TC001 | Usage | Warning | MissingTypeOptionAnalyzer - Type option missing required [TypeOption] attribute
TC002 | Usage | Error | MissingTypeOptionAnalyzer - TGeneric in base class doesn't match defaultReturnType in TypeCollection attribute
TC003 | Usage | Error | MissingTypeOptionAnalyzer - TBase in base class doesn't match baseType in TypeCollection attribute
SVCTYPE001 | ServiceTypes | Warning | TypeLookupNamingConflictAnalyzer - TypeLookup generates method that conflicts with collection member
FDW044 | Usage | Error | ServiceProviderInjectionAnalyzer - service-option service injects another service-option service directly instead of its IFdwServiceProvider<TService, TConfiguration>
FDW045 | Usage | Error | FactoryProviderInjectionAnalyzer - service factory injects an IFdwServiceProvider or IServiceScopeFactory through its constructor instead of receiving resolved values from its owning provider
