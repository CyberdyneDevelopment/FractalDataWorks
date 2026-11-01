using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for a Compute stage whose Built transformation reads and writes tables (FDW-1231). Each refusal names the
/// stage and what is wrong with its bindings; none is repaired or assumed.
/// EventIds 20620-20639.
/// </summary>
[MessageLoggingTypeCode("COMPUTE")]
public static partial class ComputeSetLog
{
    /// <summary>Logs a table input of the transformation that the stage binds to no rows.</summary>
    [MessageLogging(EventId = 20620, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' binds no rows to table '{tableName}' of Transformation '{transformationName}'")]
    public static partial IGenericMessage TableNotBound(ILogger logger, string stageName, string tableName, string transformationName);

    /// <summary>Logs a table binding for a table the transformation does not declare.</summary>
    [MessageLogging(EventId = 20621, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' binds rows to '{tableName}', which Transformation '{transformationName}' does not declare as a table input")]
    public static partial IGenericMessage TableNotDeclared(ILogger logger, string stageName, string tableName, string transformationName);

    /// <summary>Logs a table output of the transformation that the stage routes nowhere.</summary>
    [MessageLogging(EventId = 20622, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' routes no destination for table output '{outputName}' of Transformation '{transformationName}'")]
    public static partial IGenericMessage OutputNotRouted(ILogger logger, string stageName, string outputName, string transformationName);

    /// <summary>Logs a route for an output the transformation does not declare.</summary>
    [MessageLogging(EventId = 20623, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' routes '{outputName}', which Transformation '{transformationName}' does not declare as a table output")]
    public static partial IGenericMessage OutputNotDeclared(ILogger logger, string stageName, string outputName, string transformationName);

    /// <summary>Logs a route that is neither Replace nor a complete Named route.</summary>
    [MessageLogging(EventId = 20624, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' gives table output '{outputName}' an invalid route: {reason}")]
    public static partial IGenericMessage RouteInvalid(ILogger logger, string stageName, string outputName, string reason);

    /// <summary>Logs more than one output that would replace the stage's rows.</summary>
    [MessageLogging(EventId = 20625, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' routes {count} outputs to replace its rows; at most one can")]
    public static partial IGenericMessage ManyReplaceRoutes(ILogger logger, string stageName, int count);

    /// <summary>Logs a member of the one-record-at-a-time mode stated for a transformation that reads and writes tables.</summary>
    [MessageLogging(EventId = 20626, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' states {member}, but Transformation '{transformationName}' reads and writes tables; state table bindings, output routes and single values instead")]
    public static partial IGenericMessage RecordMemberInSetMode(ILogger logger, string stageName, string member, string transformationName);

    /// <summary>Logs a member of the tables mode stated for a transformation that reads one record at a time.</summary>
    [MessageLogging(EventId = 20627, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' states {member}, but Transformation '{transformationName}' reads one record at a time; state the fields that feed its inputs instead")]
    public static partial IGenericMessage SetMemberInRecordMode(ILogger logger, string stageName, string member, string transformationName);

    /// <summary>Logs a tables Compute stage run one record at a time.</summary>
    [MessageLogging(EventId = 20628, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' reads and writes tables and needs a run that handles the rows as a batch")]
    public static partial IGenericMessage SetNeedsABatchRun(ILogger logger, string stageName);

    /// <summary>Logs a table bound to a data set that cannot be read.</summary>
    [MessageLogging(EventId = 20629, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' could not read data set '{dataSet}' for table '{tableName}': {reason}")]
    public static partial IGenericMessage DataSetUnreadable(ILogger logger, string stageName, string dataSet, string tableName, string reason);

    /// <summary>Logs a tables Compute stage in a run with no data gateway.</summary>
    [MessageLogging(EventId = 20630, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' reads data set '{dataSet}' and the run has no data gateway")]
    public static partial IGenericMessage NoGateway(ILogger logger, string stageName, string dataSet);

    /// <summary>Logs a table bound to a named output no earlier stage wrote.</summary>
    [MessageLogging(EventId = 20631, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' reads named output '{outputName}' for table '{tableName}', but no earlier stage wrote it")]
    public static partial IGenericMessage NamedOutputMissing(ILogger logger, string stageName, string outputName, string tableName);

    /// <summary>Logs a single value that does not match its stated type.</summary>
    [MessageLogging(EventId = 20632, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' gives input '{inputName}' the value '{valueText}', which is not a {valueType}")]
    public static partial IGenericMessage ScalarInvalid(ILogger logger, string stageName, string inputName, string valueText, string valueType);

    /// <summary>Logs a table binding with an unknown kind or a missing name.</summary>
    [MessageLogging(EventId = 20633, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' gives table '{tableName}' an invalid source: {reason}")]
    public static partial IGenericMessage TableSourceInvalid(ILogger logger, string stageName, string tableName, string reason);

    /// <summary>Logs a declared input of a tables transformation that the stage gives no value.</summary>
    [MessageLogging(EventId = 20636, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' gives no value to input '{inputName}' of Transformation '{transformationName}'")]
    public static partial IGenericMessage ScalarNotGiven(ILogger logger, string stageName, string inputName, string transformationName);

    /// <summary>Logs a value given to an input the transformation does not declare.</summary>
    [MessageLogging(EventId = 20637, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' gives a value to '{inputName}', which Transformation '{transformationName}' does not declare as an input")]
    public static partial IGenericMessage ScalarNotDeclared(ILogger logger, string stageName, string inputName, string transformationName);

    /// <summary>Logs a transformation that has both value outputs and table outputs.</summary>
    [MessageLogging(EventId = 20638, Level = LogLevel.Error,
        Message = "Transformation '{transformationName}' has both value outputs and table outputs; a Compute stage applies one kind or the other")]
    public static partial IGenericMessage MixedOutputs(ILogger logger, string transformationName);

    /// <summary>Logs a tables Compute stage that does not say what a failing row does.</summary>
    [MessageLogging(EventId = 20639, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' reads and writes tables but does not say what a failing row does; state onRowFailure as Fail, Skip or Reject")]
    public static partial IGenericMessage RowFailureNotStated(ILogger logger, string stageName);

    /// <summary>Logs a stage that states an action that is not Fail, Skip or Reject.</summary>
    [MessageLogging(EventId = 20640, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' states onRowFailure '{action}', which is not Fail, Skip or Reject")]
    public static partial IGenericMessage RowFailureUnknown(ILogger logger, string stageName, string action);

    /// <summary>Records that a failing row was dropped and reported.</summary>
    [MessageLogging(EventId = 20641, Level = LogLevel.Warning,
        Message = "Compute stage '{stageName}' dropped a row of output '{outputName}' ({action}): {reason}")]
    public static partial IGenericMessage RowDropped(ILogger logger, string stageName, string outputName, string action, string reason);

    /// <summary>Records that a tables transformation compiled for a stage.</summary>
    [MessageLogging(EventId = 20634, Level = LogLevel.Debug,
        Message = "Compute stage '{stageName}': Transformation '{transformationName}' compiled with {outputCount} table output(s)")]
    public static partial IGenericMessage SetCompiled(ILogger logger, string stageName, string transformationName, int outputCount);

    /// <summary>Records that a tables Compute stage ran.</summary>
    [MessageLogging(EventId = 20635, Level = LogLevel.Information,
        Message = "Compute stage '{stageName}' read {rowsIn} row(s) and wrote {outputCount} table output(s) of {rowsOut} row(s)")]
    public static partial IGenericMessage SetApplied(ILogger logger, string stageName, int rowsIn, int outputCount, int rowsOut);
}
