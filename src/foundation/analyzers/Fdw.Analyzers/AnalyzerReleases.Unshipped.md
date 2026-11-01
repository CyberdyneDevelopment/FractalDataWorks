### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
FDW001 | Naming | Warning | AsyncSuffixAnalyzer - Method name should not end with 'Async'
FDW002 | Usage | Warning | PlainStringFailureAnalyzer - Use MessageLogging or ResultCode instead of plain string in GenericResult.Failure()
FDW003 | Usage | Warning | DirectLoggerCallAnalyzer - Use MessageLogging method instead of direct ILogger call
FDW004 | Usage | Warning | ManualGenericMessageAnalyzer - Use MessageLogging method instead of new GenericMessage()
FDW012 | Usage | Warning | UncheckedGenericResultAnalyzer - GenericResult value is not checked
FDW013 | Usage | Warning | UnhandledFailurePathAnalyzer - GenericResult failure path is not handled
FDW014 | Usage | Warning | ExceptionNotPropagatedAnalyzer - Exception not propagated in GenericResult
FDW015 | Usage | Warning | BrokenResultChainAnalyzer - Result chain broken, use Chain() to preserve context
FDW016 | Usage | Warning | UncheckedResultValueAccessAnalyzer - IGenericResult<T>.Value accessed without success check
FDW022 | Usage | Warning | SwallowedExceptionAnalyzer - Caught exception is neither observed nor rethrown
FDW023 | Usage | Info | SwallowedExceptionAnalyzer - Broad System.Exception catch with no specific catch clauses
