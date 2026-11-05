### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
FDW005 | Naming | Warning | FileNameMustMatchTypeNameAnalyzer - File name must match type name
FDW006 | Maintainability | Warning | MethodTooLongAnalyzer - Method too long
FDW007 | Maintainability | Warning | MethodTooComplexAnalyzer - Method too complex
FDW008 | Naming | Warning | MethodNameUnderscoreAnalyzer - Method name contains underscore
FDW009 | Naming | Disabled | DuplicateTypeNameAnalyzer - Duplicate type name in compilation
FDW010 | Design | Info | MisplacedImplementationTypeAnalyzer - Implementation-specific type in base assembly
FDW011 | Design | Warning | MisplacedImplementationTypeAnalyzer - Service/Config/TypeOption type with implementation prefix in base assembly
FDW017 | Design | Warning | TypeCollectionOpportunityAnalyzer - Enum declaration should be replaced with TypeCollection
FDW018 | Design | Warning | TypeCollectionOpportunityAnalyzer - Switch on enum type suggests TypeCollection ByName lookup
FDW019 | Design | Warning | TypeCollectionOpportunityAnalyzer - If/else chain comparing enum values suggests TypeCollection ByName dispatch
FDW020 | Design | Info | UnimplementedAbstractTypeAnalyzer - Abstract type has no implementation in the compilation
FDW021 | Design | Info | UnusedTypeAnalyzer - Type is not referenced anywhere in the compilation
FDW048 | Design | Warning | LocalStatusClassMapperAnalyzer - Component-local value-to-CSS-class mapper
FDW052 | Design | Error | DataGatewayOnlyAnalyzer - ADO.NET connection/command created or executed outside an assembly that declares [assembly: DataAccessAdapter]
FDW053 | Design | Error | ConnectionTypeVisibilityAnalyzer - is/as/typeof/cast/type pattern on a connection implementation type from another assembly, outside a DataAccessAdapter assembly
FDW054 | Design | Error | TopLevelConfigurationWriteAnalyzer - ConfigurationWriteCommand<T> for a top-level configuration constructed outside a configuration provider
FDW055 | Design | Error | NoFallbackAnalyzer - ?? or ??= falling back to a value that could be a real one or to a default message, and a valid-value initializer on a connection, schedule, scheduler or notification configuration; allowed: ?? throw, a null logger, string.Empty, [], ImmutableDictionary.Empty, DBNull.Value, a static NotFound, the same field of a parent or stored record, ??= on a field or local, a result/message failure chain, a [PlaceholderText] constant, a [PlatformDefault] member, an [ApprovedFallback] member
FDW056 | Design | Error | ApplicationServiceRegistrationAnalyzer - services.Add*/TryAdd* call or Add* extension on IServiceCollection/IHostApplicationBuilder in an application (an executable that is not a test project)
