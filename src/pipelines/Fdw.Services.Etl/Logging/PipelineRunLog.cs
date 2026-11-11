using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for what a run does with its run parameters (FDW-1109): reading the
/// parameters a trigger or schedule supplies, resolving the <c>{{name}}</c> references in expressions
/// and keeping the last-successful watermark. Every refusal is logged and returned in the failure result.
/// EventId range: 20201-20299.
/// </summary>
[MessageLoggingTypeCode("PIPELINERUN")]
public static partial class PipelineRunLog
{
    /// <summary>Logs a refused run parameter: not declared by the pipeline.</summary>
    [MessageLogging(EventId = 20201, Level = LogLevel.Warning,
        Message = "Run parameter '{parameterName}' is not declared by the pipeline")]
    public static partial IGenericMessage RunParameterUndeclared(ILogger logger, string parameterName);

    /// <summary>Logs a refused run parameter: supplied more than once.</summary>
    [MessageLogging(EventId = 20202, Level = LogLevel.Warning,
        Message = "Run parameter '{parameterName}' is supplied more than once")]
    public static partial IGenericMessage RunParameterRepeated(ILogger logger, string parameterName);

    /// <summary>Logs a refused run: a required parameter has no value.</summary>
    [MessageLogging(EventId = 20203, Level = LogLevel.Warning,
        Message = "Run parameter '{parameterName}' is required and has no value")]
    public static partial IGenericMessage RunParameterRequiredMissing(ILogger logger, string parameterName);

    /// <summary>Logs a refused run: the default a pipeline declares for a parameter the run did not give is not a value of the parameter's type.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Warning,
        Message = "The default declared for run parameter '{parameterName}' is not a valid {parameterType}, and the run did not give the parameter")]
    public static partial IGenericMessage RunParameterDefaultUnreadable(ILogger logger, string parameterName, string parameterType);

    /// <summary>Logs a refused run parameter: its text is not a value of the declared type.</summary>
    [MessageLogging(EventId = 20204, Level = LogLevel.Warning,
        Message = "Run parameter '{parameterName}' is not a valid {parameterType}")]
    public static partial IGenericMessage RunParameterUnreadable(ILogger logger, string parameterName, string parameterType);

    /// <summary>Logs a refused run parameter: it is declared as a list and its text is not JSON, with the parser's exception (FDW-5189).</summary>
    [MessageLogging(EventId = 20253, Level = LogLevel.Warning,
        Message = "Run parameter '{parameterName}' is a list, and its text is not a JSON array of items")]
    public static partial IGenericMessage RunParameterListNotJson(ILogger logger, Exception ex, string parameterName);

    /// <summary>Logs a refused run: the default declared for a list parameter the run did not give is not JSON, with the parser's exception (FDW-5189).</summary>
    [MessageLogging(EventId = 20002, Level = LogLevel.Warning,
        Message = "The default declared for list run parameter '{parameterName}' is not a JSON array of items, and the run did not give the parameter")]
    public static partial IGenericMessage RunParameterDefaultListNotJson(ILogger logger, Exception ex, string parameterName);

    /// <summary>Logs a refused expression: it references a parameter that has no value for this run.</summary>
    [MessageLogging(EventId = 20205, Level = LogLevel.Warning,
        Message = "In {owner}: the expression references run parameter '{parameterName}', which has no value for this run")]
    public static partial IGenericMessage RunParameterReferenceUnresolved(ILogger logger, string owner, string parameterName);

    /// <summary>Logs a refused expression: it references the watermark and the pipeline has none and no parameter overrides it.</summary>
    [MessageLogging(EventId = 20206, Level = LogLevel.Warning,
        Message = "In {owner}: the expression references LastSuccessfulRun, but this pipeline has no successful run on record and no run parameter overrides it")]
    public static partial IGenericMessage LastSuccessfulRunMissing(ILogger logger, string owner);

    /// <summary>Logs a refused expression: a parameter value cannot be placed inside it.</summary>
    [MessageLogging(EventId = 20207, Level = LogLevel.Warning,
        Message = "In {owner}: the value of run parameter '{parameterName}' cannot be placed in an expression: {reason}")]
    public static partial IGenericMessage RunParameterValueUnusable(ILogger logger, string owner, string parameterName, string reason);

    /// <summary>Logs that run parameters were bound for a run.</summary>
    [MessageLogging(EventId = 20208, Level = LogLevel.Debug,
        Message = "Run of pipeline '{pipelineName}' has {parameterCount} run parameters, watermark present: {hasWatermark}")]
    public static partial IGenericMessage RunParametersBound(ILogger logger, string pipelineName, int parameterCount, bool hasWatermark);

    /// <summary>Logs a refused value source: the constant text is not a value of its type.</summary>
    [MessageLogging(EventId = 20211, Level = LogLevel.Warning,
        Message = "In {owner}: the constant of the value source for column '{columnName}' is not a valid {constantType}")]
    public static partial IGenericMessage ConstantUnreadable(ILogger logger, string owner, string columnName, string constantType);

    /// <summary>Logs a refused value source: its kind has no factory.</summary>
    [MessageLogging(EventId = 20212, Level = LogLevel.Warning,
        Message = "In {owner}: the value source for column '{columnName}' has kind '{kind}', which this engine cannot build")]
    public static partial IGenericMessage ValueSourceKindUnbuildable(ILogger logger, string owner, string columnName, string kind);

    /// <summary>Logs a refused value source: the load takes rows and cannot supply this kind.</summary>
    [MessageLogging(EventId = 20213, Level = LogLevel.Warning,
        Message = "In {owner}: the value source for column '{columnName}' is a {kind} value source, which a {loadKind} load cannot supply; use an upsert or an insert-from-query for it")]
    public static partial IGenericMessage ValueSourceNotForLoad(ILogger logger, string owner, string columnName, string kind, string loadKind);

    /// <summary>Logs a refused load: a row lacks a column the load names.</summary>
    [MessageLogging(EventId = 20214, Level = LogLevel.Warning,
        Message = "In {owner}: a row does not carry the column '{columnName}' the load names")]
    public static partial IGenericMessage RowColumnMissing(ILogger logger, string owner, string columnName);

    /// <summary>Logs a refused load: a column is supplied by the rows and by a value source.</summary>
    [MessageLogging(EventId = 20215, Level = LogLevel.Warning,
        Message = "In {owner}: column '{columnName}' is supplied by the rows and by a value source; name it once")]
    public static partial IGenericMessage ColumnSuppliedTwice(ILogger logger, string owner, string columnName);

    /// <summary>Logs a refused filter expression.</summary>
    [MessageLogging(EventId = 20216, Level = LogLevel.Warning,
        Message = "In {owner}: the filter expression cannot be used: {reason}")]
    public static partial IGenericMessage FilterExpressionInvalid(ILogger logger, string owner, string reason);

    /// <summary>Logs a load destination or source that cannot be resolved.</summary>
    [MessageLogging(EventId = 20217, Level = LogLevel.Warning,
        Message = "In {owner}: '{target}' cannot be used by a load: {reason}")]
    public static partial IGenericMessage LoadTargetUnusable(ILogger logger, string owner, string target, string reason);

    /// <summary>Logs a destination written as files whose declared container is not a file.</summary>
    [MessageLogging(EventId = 20260, Level = LogLevel.Warning,
        Message = "In {owner}: '{target}' is not a file, so it cannot be written as one file or as files split by a field")]
    public static partial IGenericMessage TargetNotAFile(ILogger logger, string owner, string target);

    /// <summary>Logs a Files destination whose split field is not a field of the rows.</summary>
    [MessageLogging(EventId = 20261, Level = LogLevel.Warning,
        Message = "In {owner}: the split field '{field}' is not a field of the rows, which have {available}; nothing was written")]
    public static partial IGenericMessage SplitFieldNotInRows(ILogger logger, string owner, string field, string available);

    /// <summary>Logs a Files destination with a row whose split value is null or blank.</summary>
    [MessageLogging(EventId = 20262, Level = LogLevel.Warning,
        Message = "In {owner}: row {row} has no value in the split field '{field}', so no file can be named for it; nothing was written")]
    public static partial IGenericMessage SplitValueBlank(ILogger logger, string owner, string field, int row);

    /// <summary>Logs a Files destination with a split value that cannot name a file.</summary>
    [MessageLogging(EventId = 20263, Level = LogLevel.Warning,
        Message = "In {owner}: the split value '{value}' cannot name a file: {reason}; nothing was written")]
    public static partial IGenericMessage SplitValueInvalid(ILogger logger, string owner, string value, string reason);

    /// <summary>Logs a Files destination with two split values that differ only in case.</summary>
    [MessageLogging(EventId = 20264, Level = LogLevel.Warning,
        Message = "In {owner}: the split values '{first}' and '{second}' differ only in case, so a store that ignores case would write one file; nothing was written")]
    public static partial IGenericMessage SplitValuesCollide(ILogger logger, string owner, string first, string second);

    /// <summary>Logs a load that needs a service the pipeline was built without.</summary>
    [MessageLogging(EventId = 20218, Level = LogLevel.Error,
        Message = "In {owner}: the pipeline was built without the {service} that loads need")]
    public static partial IGenericMessage LoadServiceMissing(ILogger logger, string owner, string service);

    /// <summary>Logs a load whose connection cannot run a unit of work.</summary>
    [MessageLogging(EventId = 20219, Level = LogLevel.Warning,
        Message = "In {owner}: the connection of '{target}' does not support transactions, so the load steps cannot run in one unit of work")]
    public static partial IGenericMessage ConnectionNotTransactional(ILogger logger, string owner, string target);

    /// <summary>Logs load steps that return to a connection after leaving it.</summary>
    [MessageLogging(EventId = 20220, Level = LogLevel.Warning,
        Message = "In {owner}: the steps on one connection must follow each other; this step returns to a connection an earlier step left")]
    public static partial IGenericMessage StepsInterleaved(ILogger logger, string owner);

    /// <summary>Logs an insert-from-query whose source is on another connection.</summary>
    [MessageLogging(EventId = 20221, Level = LogLevel.Warning,
        Message = "In {owner}: the source data set '{sourceDataSet}' is not on the same connection as the destination '{destination}'")]
    public static partial IGenericMessage SourceNotOnSameConnection(ILogger logger, string owner, string sourceDataSet, string destination);

    /// <summary>Logs a source that is not table-like.</summary>
    [MessageLogging(EventId = 20222, Level = LogLevel.Warning,
        Message = "In {owner}: the source data set '{sourceDataSet}' is not stored in a table, so a set-based load cannot read it")]
    public static partial IGenericMessage SourceNotTableLike(ILogger logger, string owner, string sourceDataSet);

    /// <summary>Logs a soft delete mode the load kind cannot take.</summary>
    [MessageLogging(EventId = 20223, Level = LogLevel.Warning,
        Message = "In {owner}: soft delete mode {mode} is not allowed on a {loadKind} load: {reason}")]
    public static partial IGenericMessage SoftDeleteModeNotForLoad(ILogger logger, string owner, string mode, string loadKind, string reason);

    /// <summary>Logs a load command whose options the command layer refuses.</summary>
    [MessageLogging(EventId = 20224, Level = LogLevel.Warning,
        Message = "In {owner}: the {loadKind} load is not valid: {reason}")]
    public static partial IGenericMessage LoadOptionsRefused(ILogger logger, string owner, string loadKind, string reason);

    /// <summary>Logs a load kind with no command builder.</summary>
    [MessageLogging(EventId = 20225, Level = LogLevel.Error,
        Message = "In {owner}: load kind '{loadKind}' has no command builder")]
    public static partial IGenericMessage LoadKindUnbuildable(ILogger logger, string owner, string loadKind);

    /// <summary>Logs a pipeline whose destination cannot take the load mode it states.</summary>
    [MessageLogging(EventId = 20226, Level = LogLevel.Warning,
        Message = "Pipeline '{pipelineName}' states load mode {loadMode}, which its destination cannot take: {reason}")]
    public static partial IGenericMessage LoadModeNotForDestination(ILogger logger, string pipelineName, string loadMode, string reason);

    /// <summary>Logs a row with no value for a key column.</summary>
    [MessageLogging(EventId = 20227, Level = LogLevel.Warning,
        Message = "In {owner}: a row has no value for the key column '{columnName}', so it cannot be matched")]
    public static partial IGenericMessage KeyValueMissing(ILogger logger, string owner, string columnName);

    /// <summary>Logs an absent-from-source soft delete with no source rows.</summary>
    [MessageLogging(EventId = 20228, Level = LogLevel.Warning,
        Message = "In {owner}: the source produced no rows, so a soft delete of keys absent from it would flag every row; refused")]
    public static partial IGenericMessage AbsentSetEmpty(ILogger logger, string owner);

    /// <summary>Logs a load that stages its keys but names no stage container (FDW-4956).</summary>
    [MessageLogging(EventId = 20242, Level = LogLevel.Warning,
        Message = "In {owner}: an absent-from-source soft delete stages its keys, and it names no stage container")]
    public static partial IGenericMessage StageNotNamed(ILogger logger, string owner);

    /// <summary>Logs a stage container that is not on the target's connection (FDW-4956): a batch's keys must commit in the same transaction as its rows.</summary>
    [MessageLogging(EventId = 20243, Level = LogLevel.Warning,
        Message = "In {owner}: the stage container '{stage}' is not on the connection of '{target}', so a batch's keys could not commit with its rows")]
    public static partial IGenericMessage StageOnAnotherConnection(ILogger logger, string owner, string stage, string target);

    /// <summary>Logs a stage container that does not declare a column the stage needs (FDW-4956): an insert would drop the value, and the sweep could not match it.</summary>
    [MessageLogging(EventId = 20244, Level = LogLevel.Warning,
        Message = "In {owner}: the stage container '{stage}' does not declare the column '{column}' (it declares the key columns and the stage key column)")]
    public static partial IGenericMessage StageColumnNotDeclared(ILogger logger, string owner, string stage, string column);

    /// <summary>Logs an absent-key sweep refused because rows were rejected upstream and the destination's rule is Refuse (FDW-4956).</summary>
    [MessageLogging(EventId = 20245, Level = LogLevel.Warning,
        Message = "In {owner}: {rejected} row(s) were rejected upstream, so their keys never reached the stage and would be flagged as gone; the rejected-rows rule is Refuse, and nothing was flagged")]
    public static partial IGenericMessage SweepRefusedRejectedRows(ILogger logger, string owner, long rejected);

    /// <summary>Logs an absent-key sweep that ran (FDW-4956).</summary>
    [MessageLogging(EventId = 20246, Level = LogLevel.Information,
        Message = "In {owner}: the sweep checked {staged} staged key row(s) and flagged {flagged} target row(s) as gone from the source")]
    public static partial IGenericMessage KeysSwept(ILogger logger, string owner, long staged, int flagged);

    /// <summary>Logs an absent-key sweep not run because no batch was staged (FDW-4956): an empty source never flags the whole target.</summary>
    [MessageLogging(EventId = 20247, Level = LogLevel.Information,
        Message = "In {owner}: the source produced no rows, so the absent-key sweep was not run (an empty source never flags every row)")]
    public static partial IGenericMessage SweepSkippedNothingStaged(ILogger logger, string owner);

    /// <summary>Logs an absent-key sweep not run because the run was stopped before every row arrived (FDW-4956).</summary>
    [MessageLogging(EventId = 20248, Level = LogLevel.Information,
        Message = "In {owner}: the run was stopped before every row arrived, so the absent-key sweep was not run")]
    public static partial IGenericMessage SweepSkippedRunStopped(ILogger logger, string owner);

    /// <summary>Logs a stage whose rows for this load could not be removed (FDW-4956); they carry the run's execution id, so they can be found and removed later.</summary>
    [MessageLogging(EventId = 20249, Level = LogLevel.Warning,
        Message = "In {owner}: the stage rows under '{stageKey}' could not be removed: {reason}")]
    public static partial IGenericMessage StageNotCleared(ILogger logger, string owner, string stageKey, string? reason);

    /// <summary>Logs an absent-key sweep with no reader of the rows rejected upstream (FDW-4956): the sweep is not judged blind.</summary>
    [MessageLogging(EventId = 20250, Level = LogLevel.Error,
        Message = "In {owner}: the destination was not told how many rows were rejected upstream, so the absent-key sweep cannot be judged; refused")]
    public static partial IGenericMessage SweepUnjudged(ILogger logger, string owner);

    /// <summary>Logs a batch whose keys matched no row under the Fail rule (FDW-4988); its unit of work rolled back.</summary>
    [MessageLogging(EventId = 20251, Level = LogLevel.Error,
        Message = "In {owner}: {unmatched} key(s) of the batch matched no row in the target and the unmatched keys rule is Fail, so the batch was rolled back: {sample}")]
    public static partial IGenericMessage UnmatchedKeysFailed(ILogger logger, string owner, long unmatched, string sample);

    /// <summary>Logs a batch whose keys matched no row under the Warn rule (FDW-4988); the batch committed.</summary>
    [MessageLogging(EventId = 20252, Level = LogLevel.Warning,
        Message = "In {owner}: {unmatched} key(s) of the batch matched no row in the target ({total} so far in this run); the unmatched keys rule is Warn, so the batch committed: {sample}")]
    public static partial IGenericMessage UnmatchedKeysWarned(ILogger logger, string owner, long unmatched, long total, string sample);

    /// <summary>Logs a pipeline that states a legacy truncate flag its load definition contradicts.</summary>
    [MessageLogging(EventId = 20229, Level = LogLevel.Warning,
        Message = "Pipeline '{pipelineName}' sets TruncateBeforeLoad, which contradicts its load definition ({detail}); state the truncation as load mode Replace or as a Truncate load step")]
    public static partial IGenericMessage TruncateBeforeLoadContradicts(ILogger logger, string pipelineName, string detail);

    /// <summary>Logs load steps skipped because the output has no rows.</summary>
    [MessageLogging(EventId = 20230, Level = LogLevel.Information,
        Message = "Pipeline '{pipelineName}' produced no rows, so its load steps were not run (a destination is never emptied for an empty source)")]
    public static partial IGenericMessage LoadStepsSkippedNoRows(ILogger logger, string pipelineName);

    /// <summary>Logs a connection type that does not list a capability the load needs.</summary>
    [MessageLogging(EventId = 20238, Level = LogLevel.Warning,
        Message = "In {owner}: the connection type of '{target}' does not list the {capability} capability, so it cannot run this load")]
    public static partial IGenericMessage ConnectionLacksCapability(ILogger logger, string owner, string target, string capability);

    /// <summary>Logs that the store holding the watermark is not named.</summary>
    [MessageLogging(EventId = 60231, Level = LogLevel.Error,
        Message = "The operational store that holds the pipeline watermark is not named by the host")]
    public static partial IGenericMessage WatermarkStoreNotNamed(ILogger logger);

    /// <summary>Logs a failed read of the watermark.</summary>
    [MessageLogging(EventId = 70232, Level = LogLevel.Error,
        Message = "Reading the watermark of pipeline '{pipelineName}' failed: {reason}")]
    public static partial IGenericMessage WatermarkReadFailed(ILogger logger, string pipelineName, string reason);

    /// <summary>Logs a failed write of the watermark after the load committed.</summary>
    [MessageLogging(EventId = 20233, Level = LogLevel.Error,
        Message = "Pipeline '{pipelineName}' loaded its data but its watermark was not advanced: {reason}")]
    public static partial IGenericMessage WatermarkWriteFailed(ILogger logger, string pipelineName, string reason);

    /// <summary>Logs that the watermark moved forward.</summary>
    [MessageLogging(EventId = 20234, Level = LogLevel.Information,
        Message = "Pipeline '{pipelineName}' watermark advanced to {lastSuccessfulRun}")]
    public static partial IGenericMessage WatermarkAdvanced(ILogger logger, string pipelineName, string lastSuccessfulRun);

    /// <summary>Logs that a later run had already recorded a later watermark, so this run left it where it was.</summary>
    [MessageLogging(EventId = 20239, Level = LogLevel.Information,
        Message = "Pipeline '{pipelineName}' watermark was already ahead of this run's start, so it was left as it is")]
    public static partial IGenericMessage WatermarkAlreadyAhead(ILogger logger, string pipelineName);

    /// <summary>Logs that a run has no tenant, so no watermark is kept for it.</summary>
    [MessageLogging(EventId = 20235, Level = LogLevel.Information,
        Message = "Run of pipeline '{pipelineName}' states no tenant, so its watermark is neither read nor written")]
    public static partial IGenericMessage WatermarkNotTracked(ILogger logger, string pipelineName);

    /// <summary>Logs that the load steps committed.</summary>
    [MessageLogging(EventId = 20236, Level = LogLevel.Information,
        Message = "Pipeline '{pipelineName}' load steps committed in {groupCount} unit(s) of work, {affected} rows affected")]
    public static partial IGenericMessage LoadStepsCommitted(ILogger logger, string pipelineName, int groupCount, int affected);

    /// <summary>Logs that the watermark was left where it was because the run recorded errors.</summary>
    [MessageLogging(EventId = 20237, Level = LogLevel.Warning,
        Message = "Pipeline '{pipelineName}' recorded errors during the run, so its watermark was not advanced")]
    public static partial IGenericMessage WatermarkHeldForErrors(ILogger logger, string pipelineName);

    /// <summary>Logs that the reject steps wrote the rejected records.</summary>
    [MessageLogging(EventId = 20240, Level = LogLevel.Information,
        Message = "Pipeline '{pipelineName}' wrote {rejected} rejected record(s) through its reject steps")]
    public static partial IGenericMessage RejectedRecordsWritten(ILogger logger, string pipelineName, int rejected);

    /// <summary>Logs reject steps skipped because no record was rejected.</summary>
    [MessageLogging(EventId = 20241, Level = LogLevel.Information,
        Message = "Pipeline '{pipelineName}' rejected no records, so its reject steps were not run")]
    public static partial IGenericMessage RejectStepsSkippedNoRejections(ILogger logger, string pipelineName);
}
