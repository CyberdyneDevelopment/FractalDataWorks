using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// MessageLogging for the row-flow engine (FDW-1386): graph refusals, component failures and the end of a run. Every refusal is
/// logged and returned in the failure result.
/// EventId range: 20901-20949. The Set variable calculation and script token messages (FDW-1957, FDW-1970) take the catalog's category numbers (RESULTCODE-CATALOG.md): 20000 and 20001 for a value that is missing or invalid, 30000 for a name that is not found, 90001 for an operation that failed, and 21000 and 21001 for this package's own missing and invalid values.
/// </summary>
[MessageLoggingTypeCode("FLOW")]
public static partial class FlowLog
{
    /// <summary>Logs a graph that cannot run.</summary>
    [MessageLogging(EventId = 20901, Level = LogLevel.Error,
        Message = "The flow graph cannot run: {reason}")]
    public static partial IGenericMessage GraphInvalid(ILogger logger, string reason);

    /// <summary>Logs a run asked for with a link capacity below one.</summary>
    [MessageLogging(EventId = 20902, Level = LogLevel.Error,
        Message = "A flow run buffers at least one batch per link; {capacity} was asked for")]
    public static partial IGenericMessage CapacityInvalid(ILogger logger, int capacity);

    /// <summary>Logs a component that ended the run with a failure.</summary>
    [MessageLogging(EventId = 20903, Level = LogLevel.Error,
        Message = "Flow component '{component}' failed: {reason}")]
    public static partial IGenericMessage ComponentFailed(ILogger logger, string component, string? reason);

    /// <summary>Logs a component that threw instead of returning a failure.</summary>
    [MessageLogging(EventId = 20904, Level = LogLevel.Error,
        Message = "Flow component '{component}' threw {exceptionType}: {reason}")]
    public static partial IGenericMessage ComponentThrew(ILogger logger, string component, string exceptionType, string reason);

    /// <summary>Logs a run that was cancelled.</summary>
    [MessageLogging(EventId = 20905, Level = LogLevel.Warning,
        Message = "The flow run was cancelled")]
    public static partial IGenericMessage RunCancelled(ILogger logger);

    /// <summary>Logs rows a component left unread when it finished, which were read and discarded so no writer waits forever.</summary>
    [MessageLogging(EventId = 20906, Level = LogLevel.Warning,
        Message = "Flow component '{component}' finished without reading input '{input}'; {rows} rows were discarded")]
    public static partial IGenericMessage InputNotConsumed(ILogger logger, string component, string input, long rows);

    /// <summary>Logs a mapped field that the incoming row does not have.</summary>
    [MessageLogging(EventId = 20907, Level = LogLevel.Error,
        Message = "Flow component '{component}' maps field '{field}' on its input '{input}' but the incoming row has no such field")]
    public static partial IGenericMessage FieldMissing(ILogger logger, string component, string input, string field);

    /// <summary>Logs a row that matched no output of a split that states it fails on an unmatched row.</summary>
    [MessageLogging(EventId = 20908, Level = LogLevel.Error,
        Message = "Flow component '{component}' received a row that matches none of its outputs and it is set to fail on an unmatched row")]
    public static partial IGenericMessage RowMatchedNoOutput(ILogger logger, string component);

    /// <summary>Logs the end of a successful run.</summary>
    [MessageLogging(EventId = 20909, Level = LogLevel.Information,
        Message = "Flow run finished: {links} links moved {rows} rows in {elapsedMs} ms")]
    public static partial IGenericMessage RunFinished(ILogger logger, int links, long rows, long elapsedMs);

    /// <summary>Logs a component that was built with a setting it cannot run with.</summary>
    [MessageLogging(EventId = 20910, Level = LogLevel.Error,
        Message = "Flow component '{component}' is not usable: {reason}")]
    public static partial IGenericMessage ComponentInvalid(ILogger logger, string component, string reason);

    /// <summary>Logs a filter expression that cannot be read.</summary>
    [MessageLogging(EventId = 20911, Level = LogLevel.Error,
        Message = "Flow filter '{expression}' cannot be read: {reason}")]
    public static partial IGenericMessage FilterInvalid(ILogger logger, string expression, string? reason);

    /// <summary>Logs a component that was cancelled because another component failed or the caller cancelled the run.</summary>
    [MessageLogging(EventId = 20912, Level = LogLevel.Information,
        Message = "Flow component '{component}' was cancelled")]
    public static partial IGenericMessage ComponentCancelled(ILogger logger, Exception exception, string component);

    /// <summary>Logs the release of a finished component's inputs being cancelled with the run.</summary>
    [MessageLogging(EventId = 20913, Level = LogLevel.Trace,
        Message = "Flow component '{component}' stopped releasing its inputs because the run was cancelled")]
    public static partial IGenericMessage ReleaseCancelled(ILogger logger, Exception exception, string component);

    /// <summary>Logs a joined or brought-across field whose name is already taken in the output row.</summary>
    [MessageLogging(EventId = 20914, Level = LogLevel.Error,
        Message = "Flow component '{component}' would write field '{field}' twice in one output row; give the right-hand fields a prefix or the brought columns another name")]
    public static partial IGenericMessage FieldCollision(ILogger logger, string component, string field);

    /// <summary>Logs a lookup whose reference input has two rows for one key.</summary>
    [MessageLogging(EventId = 20915, Level = LogLevel.Error,
        Message = "Flow component '{component}' found more than one reference row for the key {key}; a lookup needs one row per key")]
    public static partial IGenericMessage DuplicateReferenceKey(ILogger logger, string component, string key);

    /// <summary>Logs a row that has no match in a lookup's reference and the lookup is set to fail on it.</summary>
    [MessageLogging(EventId = 20916, Level = LogLevel.Error,
        Message = "Flow component '{component}' found no reference row for key {key} and it is set to fail on an unmatched row")]
    public static partial IGenericMessage LookupNoMatch(ILogger logger, string component, string key);

    /// <summary>Logs a cross apply over a text value that no delimiter was stated for.</summary>
    [MessageLogging(EventId = 20917, Level = LogLevel.Error,
        Message = "Flow component '{component}' found text in field '{field}' and no delimiter was stated to split it; state one or give it a list")]
    public static partial IGenericMessage TextNeedsDelimiter(ILogger logger, string component, string field);

    /// <summary>Logs a step that sets or reads a variable nobody declared.</summary>
    [MessageLogging(EventId = 20919, Level = LogLevel.Error,
        Message = "Flow step '{step}' uses variable '{variable}' which is not declared in scope")]
    public static partial IGenericMessage VariableNotDeclared(ILogger logger, string step, string variable);

    /// <summary>Logs a step that sets a parameter the pipeline declared read-only.</summary>
    [MessageLogging(EventId = 20935, Level = LogLevel.Error,
        Message = "Flow step '{step}' sets '{variable}', which is a read-only parameter; no step may change it")]
    public static partial IGenericMessage VariableReadOnly(ILogger logger, string step, string variable);

    /// <summary>Logs a loop that ran past the iteration limit it was given.</summary>
    [MessageLogging(EventId = 20920, Level = LogLevel.Error,
        Message = "Flow step '{step}' did not finish within its limit of {maximum} iterations")]
    public static partial IGenericMessage LoopLimitExceeded(ILogger logger, string step, int maximum);

    /// <summary>Logs a loop whose list variable does not hold a list.</summary>
    [MessageLogging(EventId = 20921, Level = LogLevel.Error,
        Message = "Flow step '{step}' loops over variable '{variable}' which does not hold a list")]
    public static partial IGenericMessage LoopNeedsList(ILogger logger, string step, string variable);

    /// <summary>Logs a loop that works through its list in batches (FDW-3086).</summary>
    [MessageLogging(EventId = 20940, Level = LogLevel.Information,
        Message = "Flow step '{step}' works through the {items} items of variable '{variable}' in batches of {batchSize}")]
    public static partial IGenericMessage LoopBatched(ILogger logger, string step, string variable, int items, int batchSize);

    /// <summary>Logs an Execute Pipeline step in a run that cannot start pipelines.</summary>
    [MessageLogging(EventId = 20922, Level = LogLevel.Error,
        Message = "Flow step '{step}' starts pipeline '{pipeline}' but the run has no {missing}, so there is nothing to start it under")]
    public static partial IGenericMessage ExecuteUnavailable(ILogger logger, string step, string pipeline, string missing);

    /// <summary>Logs a started pipeline that failed and the step is set to fail on it.</summary>
    [MessageLogging(EventId = 20923, Level = LogLevel.Error,
        Message = "Flow step '{step}' started pipeline '{pipeline}' and it did not succeed")]
    public static partial IGenericMessage ChildPipelineFailed(ILogger logger, string step, string pipeline);

    /// <summary>Logs a step that ran.</summary>
    [MessageLogging(EventId = 20924, Level = LogLevel.Information,
        Message = "Flow step '{step}' finished")]
    public static partial IGenericMessage StepFinished(ILogger logger, string step);

    /// <summary>Logs a source whose read of a page of rows failed.</summary>
    [MessageLogging(EventId = 20925, Level = LogLevel.Error,
        Message = "Flow source '{component}' could not read rows (skipping {skipped}): {reason}")]
    public static partial IGenericMessage SourceReadFailed(ILogger logger, string component, int skipped, string? reason);

    /// <summary>Logs a source whose read succeeded and returned no row set.</summary>
    [MessageLogging(EventId = 20926, Level = LogLevel.Error,
        Message = "Flow source '{component}' read succeeded but returned no row set")]
    public static partial IGenericMessage SourceReturnedNothing(ILogger logger, string component);

    /// <summary>Logs a destination whose load of a batch failed.</summary>
    [MessageLogging(EventId = 20927, Level = LogLevel.Error,
        Message = "Flow destination '{component}' could not load a batch of {rows} rows: {reason}")]
    public static partial IGenericMessage DestinationLoadFailed(ILogger logger, string component, int rows, string? reason);

    /// <summary>Logs a destination whose truncate before the first batch failed.</summary>
    [MessageLogging(EventId = 20928, Level = LogLevel.Error,
        Message = "Flow destination '{component}' could not truncate its target before the first batch: {reason}")]
    public static partial IGenericMessage DestinationTruncateFailed(ILogger logger, string component, string? reason);

    /// <summary>Logs a destination that received rows in a test run and wrote none (FDW-1665).</summary>
    [MessageLogging(EventId = 20937, Level = LogLevel.Information,
        Message = "Flow destination '{component}' is in a test run: {rows} row(s) received, none written")]
    public static partial IGenericMessage DestinationSkippedWrites(ILogger logger, string component, int rows);

    /// <summary>Logs a step with an outside effect that a test run did not run (FDW-1665).</summary>
    [MessageLogging(EventId = 20938, Level = LogLevel.Information,
        Message = "Step '{step}' ({kind}) has an outside effect and was skipped in a test run")]
    public static partial IGenericMessage StepSkippedInTestRun(ILogger logger, string step, string kind);

    /// <summary>Logs a file of a list of files whose fields are not those of the first file (FDW-3089).</summary>
    [MessageLogging(EventId = 20939, Level = LogLevel.Error,
        Message = "Flow source '{component}' reads a list of files as one stream, and '{file}' has different fields from the first file '{firstFile}': {difference}")]
    public static partial IGenericMessage SourceFilesDiffer(ILogger logger, string component, string file, string firstFile, string difference);

    /// <summary>Logs a destination that finished loading.</summary>
    [MessageLogging(EventId = 20929, Level = LogLevel.Information,
        Message = "Flow destination '{component}' loaded {rows} rows")]
    public static partial IGenericMessage DestinationLoaded(ILogger logger, string component, int rows);

    /// <summary>Logs a stage that rejected rows in a component with no error output to take them.</summary>
    [MessageLogging(EventId = 20936, Level = LogLevel.Error,
        Message = "Flow component '{component}' rejected {rows} rows and has no error output to take them; the first was rejected because: {reason}")]
    public static partial IGenericMessage StageRejectedRows(ILogger logger, string component, int rows, string reason);

    /// <summary>Logs a Set variable step with a calculation whose input text names something the run does not have.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Error,
        Message = "Flow step '{step}' cannot read its input: {reason}")]
    public static partial IGenericMessage CalculateInputUnreadable(ILogger logger, string step, string? reason);

    /// <summary>Logs a Set variable step that names a transformation the platform does not have.</summary>
    [MessageLogging(EventId = 30000, Level = LogLevel.Error,
        Message = "Flow step '{step}' applies the transformation '{transformation}', which is not a registered field transformation")]
    public static partial IGenericMessage CalculateTransformationUnknown(ILogger logger, string step, string transformation);

    /// <summary>Logs a Set variable step with a calculation that leaves out parameters its transformation requires.</summary>
    [MessageLogging(EventId = 20000, Level = LogLevel.Error,
        Message = "Flow step '{step}' applies the transformation '{transformation}' without its required parameters [{missing}]")]
    public static partial IGenericMessage CalculateParametersMissing(ILogger logger, string step, string transformation, string missing);

    /// <summary>Logs a transformation of a Set variable step that failed.</summary>
    [MessageLogging(EventId = 90001, Level = LogLevel.Error,
        Message = "Flow step '{step}' failed applying the transformation '{transformation}': {reason}")]
    public static partial IGenericMessage CalculateFailed(ILogger logger, string step, string transformation, string? reason);

    /// <summary>Logs a SQL script that names a token the run has no value for.</summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error,
        Message = "'{owner}' names $({name}) in its script, which this run has no value for; the script did not run")]
    public static partial IGenericMessage ScriptTokenNotGiven(ILogger logger, string owner, string name);

    /// <summary>Logs a row a component refused because a mapping could not be applied to it: a required value missing, a transformation or a conversion that failed.</summary>
    [MessageLogging(EventId = 21002, Level = LogLevel.Error,
        Message = "Flow component '{component}' refused a row on its input '{input}': {reason}")]
    public static partial IGenericMessage MappingRefused(ILogger logger, string component, string input, string reason);

    /// <summary>Logs a SQL script whose token has a value that is not a name; the value is not written, it may be SQL.</summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Error,
        Message = "'{owner}' names $({name}) in its script, whose value is not a name; a token takes letters, digits, underscore and dot only (a value belongs in a named parameter such as @Year), and the script did not run")]
    public static partial IGenericMessage ScriptTokenNotAName(ILogger logger, string owner, string name);
}
