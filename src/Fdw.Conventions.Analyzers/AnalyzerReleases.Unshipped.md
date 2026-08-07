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
FDW046 | Design | Warning | InlineStyleAttributeAnalyzer - Inline style attribute in Razor markup
FDW047 | Design | Warning | RawSvgMarkupAnalyzer - Raw svg element in Razor markup
