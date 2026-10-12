using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for the structural validation of a pipeline's load configuration (FDW-1108): load mode, load
/// steps, columns, value sources and run parameters. Every refusal is logged and returned in the failure result.
/// EventId range: 20001-20099.
/// </summary>
[MessageLoggingTypeCode("PIPELINELOAD")]
public static partial class PipelineLoadLog
{

    /// <summary>Logs a refused load configuration: A pipeline without load steps must state a load mode.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Warning,
        Message = "A pipeline without load steps must state a load mode")]
    public static partial IGenericMessage LoadModeMissing(ILogger logger);

    /// <summary>Logs a refused load configuration: Load mode 'loadMode' is not one of the pipeline load modes.</summary>
    [MessageLogging(EventId = 20002, Level = LogLevel.Warning,
        Message = "Load mode '{loadMode}' is not one of the pipeline load modes")]
    public static partial IGenericMessage PipelineLoadModeUnknown(ILogger logger, string loadMode);

    /// <summary>Logs a refused load configuration: Load mode 'loadMode' is stated together with load steps; a pipeline states a load mode or load steps, never both.</summary>
    [MessageLogging(EventId = 20003, Level = LogLevel.Warning,
        Message = "Load mode '{loadMode}' is stated together with load steps; a pipeline states a load mode or load steps, never both")]
    public static partial IGenericMessage LoadModeWithSteps(ILogger logger, string loadMode);

    /// <summary>Logs a refused load configuration: The pipeline states load options or value sources for its own load together with load steps; each step carries its own.</summary>
    [MessageLogging(EventId = 20004, Level = LogLevel.Warning,
        Message = "The pipeline states load options or value sources for its own load together with load steps; each step carries its own")]
    public static partial IGenericMessage OwnLoadWithSteps(ILogger logger);

    /// <summary>Logs a refused load configuration: The pipeline states its own load more than once.</summary>
    [MessageLogging(EventId = 20005, Level = LogLevel.Warning,
        Message = "The pipeline states its own load more than once")]
    public static partial IGenericMessage OwnLoadRepeated(ILogger logger);

    /// <summary>Logs a refused load configuration: Load mode 'loadMode' needs load options (key columns and write columns) and none are stated.</summary>
    [MessageLogging(EventId = 20006, Level = LogLevel.Warning,
        Message = "Load mode '{loadMode}' needs load options (key columns and write columns) and none are stated")]
    public static partial IGenericMessage LoadModeNeedsOptions(ILogger logger, string loadMode);

    /// <summary>Logs a refused load configuration: The pipeline's own load states option, which only a load step has.</summary>
    [MessageLogging(EventId = 20007, Level = LogLevel.Warning,
        Message = "The pipeline's own load states {option}, which only a load step has")]
    public static partial IGenericMessage OwnLoadStatesStepOption(ILogger logger, string option);

    /// <summary>Logs a refused load configuration: Load step 'stepName' has step order stepOrder; step orders run from 1 to the number of steps (stepCount), each used once.</summary>
    [MessageLogging(EventId = 20008, Level = LogLevel.Warning,
        Message = "Load step '{stepName}' has step order {stepOrder}; step orders run from 1 to the number of steps ({stepCount}), each used once")]
    public static partial IGenericMessage LoadStepOrderInvalid(ILogger logger, string stepName, int stepOrder, int stepCount);

    /// <summary>Logs a refused load configuration: More than one load step has step order stepOrder; each step order is used once.</summary>
    [MessageLogging(EventId = 20009, Level = LogLevel.Warning,
        Message = "More than one load step has step order {stepOrder}; each step order is used once")]
    public static partial IGenericMessage LoadStepOrderDuplicated(ILogger logger, int stepOrder);

    /// <summary>Logs a refused load configuration: The load step with step order stepOrder has no name.</summary>
    [MessageLogging(EventId = 20010, Level = LogLevel.Warning,
        Message = "The load step with step order {stepOrder} has no name")]
    public static partial IGenericMessage LoadStepNameBlank(ILogger logger, int stepOrder);

    /// <summary>Logs a refused load configuration: More than one load step is named 'stepName'; step names are unique within the pipeline.</summary>
    [MessageLogging(EventId = 20011, Level = LogLevel.Warning,
        Message = "More than one load step is named '{stepName}'; step names are unique within the pipeline")]
    public static partial IGenericMessage LoadStepNameDuplicated(ILogger logger, string stepName);

    /// <summary>Logs a refused load configuration: In owner: the kind of step is not stated.</summary>
    [MessageLogging(EventId = 20012, Level = LogLevel.Warning,
        Message = "In {owner}: the kind of step is not stated")]
    public static partial IGenericMessage LoadStepKindMissing(ILogger logger, string owner);

    /// <summary>Logs a refused load configuration: In owner: kind 'kind' is not a load step kind.</summary>
    [MessageLogging(EventId = 20013, Level = LogLevel.Warning,
        Message = "In {owner}: kind '{kind}' is not a load step kind")]
    public static partial IGenericMessage LoadStepKindUnknown(ILogger logger, string owner, string kind);

    /// <summary>Logs a refused load configuration: In owner: the destination data set is not stated.</summary>
    [MessageLogging(EventId = 20014, Level = LogLevel.Warning,
        Message = "In {owner}: the destination data set is not stated")]
    public static partial IGenericMessage LoadStepDestinationMissing(ILogger logger, string owner);

    /// <summary>Logs a refused load configuration: In owner: option is needed and not stated.</summary>
    [MessageLogging(EventId = 20015, Level = LogLevel.Warning,
        Message = "In {owner}: {option} is needed and not stated")]
    public static partial IGenericMessage LoadStepOptionMissing(ILogger logger, string owner, string option);

    /// <summary>Logs a refused load configuration: In owner: option is stated, which kind does not use.</summary>
    [MessageLogging(EventId = 20016, Level = LogLevel.Warning,
        Message = "In {owner}: {option} is stated, which {kind} does not use")]
    public static partial IGenericMessage LoadStepOptionNotApplicable(ILogger logger, string owner, string option, string kind);

    /// <summary>Logs a refused load configuration: In owner: a column has role 'role', which is not a load column role.</summary>
    [MessageLogging(EventId = 20017, Level = LogLevel.Warning,
        Message = "In {owner}: a column has role '{role}', which is not a load column role")]
    public static partial IGenericMessage LoadColumnRoleUnknown(ILogger logger, string owner, string role);

    /// <summary>Logs a refused load configuration: In owner: a column has role 'role', which is not valid there: expected.</summary>
    [MessageLogging(EventId = 20018, Level = LogLevel.Warning,
        Message = "In {owner}: a column has role '{role}', which is not valid there: {expected}")]
    public static partial IGenericMessage LoadColumnRoleNotForOwner(ILogger logger, string owner, string role, string expected);

    /// <summary>Logs a refused load configuration: In owner: two role columns are at ordinal ordinal.</summary>
    [MessageLogging(EventId = 20019, Level = LogLevel.Warning,
        Message = "In {owner}: two {role} columns are at ordinal {ordinal}")]
    public static partial IGenericMessage LoadColumnOrdinalDuplicated(ILogger logger, string owner, string role, int ordinal);

    /// <summary>Logs a refused load configuration: In owner: a role column has no column name.</summary>
    [MessageLogging(EventId = 20020, Level = LogLevel.Warning,
        Message = "In {owner}: a {role} column has no column name")]
    public static partial IGenericMessage LoadColumnNameBlank(ILogger logger, string owner, string role);

    /// <summary>Logs a refused load configuration: In owner: option is stated but not missing; both are needed.</summary>
    [MessageLogging(EventId = 20021, Level = LogLevel.Warning,
        Message = "In {owner}: {option} is stated but not {missing}; both are needed")]
    public static partial IGenericMessage LoadOptionPairIncomplete(ILogger logger, string owner, string option, string missing);

    /// <summary>Logs a refused load configuration: In owner: soft delete mode 'mode' is not a soft delete mode.</summary>
    [MessageLogging(EventId = 20022, Level = LogLevel.Warning,
        Message = "In {owner}: soft delete mode '{mode}' is not a soft delete mode")]
    public static partial IGenericMessage SoftDeleteModeUnknown(ILogger logger, string owner, string mode);

    /// <summary>Logs a refused load configuration: In owner: the soft delete options do not fit together: reason.</summary>
    [MessageLogging(EventId = 20023, Level = LogLevel.Warning,
        Message = "In {owner}: the soft delete options do not fit together: {reason}")]
    public static partial IGenericMessage SoftDeleteOptionsInconsistent(ILogger logger, string owner, string reason);

    /// <summary>Logs a refused load configuration: In owner: value source 'valueSource' has kind 'kind', which is not a value source kind.</summary>
    [MessageLogging(EventId = 20024, Level = LogLevel.Warning,
        Message = "In {owner}: value source '{valueSource}' has kind '{kind}', which is not a value source kind")]
    public static partial IGenericMessage ValueSourceKindUnknown(ILogger logger, string owner, string valueSource, string kind);

    /// <summary>Logs a refused load configuration: In owner: value source 'valueSource' needs option.</summary>
    [MessageLogging(EventId = 20025, Level = LogLevel.Warning,
        Message = "In {owner}: value source '{valueSource}' needs {option}")]
    public static partial IGenericMessage ValueSourceOptionMissing(ILogger logger, string owner, string valueSource, string option);

    /// <summary>Logs a refused load configuration: In owner: value source 'valueSource' states option, which a kind value source does not use.</summary>
    [MessageLogging(EventId = 20026, Level = LogLevel.Warning,
        Message = "In {owner}: value source '{valueSource}' states {option}, which a {kind} value source does not use")]
    public static partial IGenericMessage ValueSourceOptionNotApplicable(ILogger logger, string owner, string valueSource, string option, string kind);

    /// <summary>Logs a refused load configuration: In owner: value source 'valueSource' is a kind value source, which maps a source column and only a load that takes a source data set has one.</summary>
    [MessageLogging(EventId = 20027, Level = LogLevel.Warning,
        Message = "In {owner}: value source '{valueSource}' is a {kind} value source, which maps a source column and only a load that takes a source data set has one")]
    public static partial IGenericMessage ValueSourceNeedsSourceDataSet(ILogger logger, string owner, string valueSource, string kind);

    /// <summary>Logs a refused load configuration: In owner: value source 'valueSource' is a kind value source, which cannot supply the parameter of an executable container.</summary>
    [MessageLogging(EventId = 20028, Level = LogLevel.Warning,
        Message = "In {owner}: value source '{valueSource}' is a {kind} value source, which cannot supply the parameter of an executable container")]
    public static partial IGenericMessage ValueSourceNotForContainer(ILogger logger, string owner, string valueSource, string kind);

    /// <summary>Logs a refused load configuration: In owner: the data store of the executable container is not stated.</summary>
    [MessageLogging(EventId = 20029, Level = LogLevel.Warning,
        Message = "In {owner}: the data store of the executable container is not stated")]
    public static partial IGenericMessage LoadStepDataStoreMissing(ILogger logger, string owner);

    /// <summary>Logs a refused load configuration: In owner: value source 'valueSource' reads its constant as type 'parameterType', which is not a data type.</summary>
    [MessageLogging(EventId = 20030, Level = LogLevel.Warning,
        Message = "In {owner}: value source '{valueSource}' reads its constant as type '{parameterType}', which is not a data type")]
    public static partial IGenericMessage ValueSourceConstantTypeUnknown(ILogger logger, string owner, string valueSource, string parameterType);

    /// <summary>Logs a refused load configuration: In owner: a value is supplied for 'columnName' by two value sources.</summary>
    [MessageLogging(EventId = 20031, Level = LogLevel.Warning,
        Message = "In {owner}: a value is supplied for '{columnName}' by two value sources")]
    public static partial IGenericMessage ValueSourceColumnDuplicated(ILogger logger, string owner, string columnName);

    /// <summary>Logs a refused load configuration: A run parameter has no name.</summary>
    [MessageLogging(EventId = 20032, Level = LogLevel.Warning,
        Message = "A run parameter has no name")]
    public static partial IGenericMessage RunParameterNameBlank(ILogger logger);

    /// <summary>Logs a refused load configuration: Run parameter 'parameterName' is declared twice.</summary>
    [MessageLogging(EventId = 20033, Level = LogLevel.Warning,
        Message = "Run parameter '{parameterName}' is declared twice")]
    public static partial IGenericMessage RunParameterNameDuplicated(ILogger logger, string parameterName);

    /// <summary>Logs a refused load configuration: Run parameter 'parameterName' has type 'parameterType', which is not a data type a run parameter can have.</summary>
    [MessageLogging(EventId = 20034, Level = LogLevel.Warning,
        Message = "Run parameter '{parameterName}' has type '{parameterType}', which is not a data type a run parameter can have")]
    public static partial IGenericMessage RunParameterTypeUnknown(ILogger logger, string parameterName, string parameterType);

    /// <summary>Logs a refused load configuration: In owner: the load phase is not stated.</summary>
    [MessageLogging(EventId = 20035, Level = LogLevel.Warning,
        Message = "In {owner}: the load phase is not stated")]
    public static partial IGenericMessage LoadPhaseMissing(ILogger logger, string owner);

    /// <summary>Logs a refused load configuration: In owner: 'phase' is not one of the load phases.</summary>
    [MessageLogging(EventId = 20036, Level = LogLevel.Warning,
        Message = "In {owner}: '{phase}' is not one of the load phases")]
    public static partial IGenericMessage LoadPhaseUnknown(ILogger logger, string owner, string phase);

    /// <summary>Logs a refused load configuration: The pipeline's own load cannot be a reject step.</summary>
    [MessageLogging(EventId = 20037, Level = LogLevel.Warning,
        Message = "A reject step has a step order and a name; the pipeline's own load cannot be one")]
    public static partial IGenericMessage RejectStepIsOwnLoad(ILogger logger);

    /// <summary>Logs a refused load configuration: In owner: a 'kind' step cannot write rejected records.</summary>
    [MessageLogging(EventId = 20038, Level = LogLevel.Warning,
        Message = "In {owner}: a '{kind}' step cannot write rejected records; the kinds that can are {accepted}")]
    public static partial IGenericMessage RejectStepKindRefused(ILogger logger, string owner, string kind, string accepted);

    /// <summary>Logs a refused load configuration: In owner: a reject step names the reserved column among its write columns.</summary>
    [MessageLogging(EventId = 20039, Level = LogLevel.Warning,
        Message = "In {owner}: a reject step writes the reserved column '{column}', so it names it among its write columns")]
    public static partial IGenericMessage RejectStepColumnMissing(ILogger logger, string owner, string column);

    /// <summary>Logs a refused load configuration: In owner: the reject destination is a destination the pipeline's load writes to.</summary>
    [MessageLogging(EventId = 20040, Level = LogLevel.Warning,
        Message = "In {owner}: the reject destination '{dataSet}' is a destination the pipeline's load writes to; rejected records go to a data set of their own")]
    public static partial IGenericMessage RejectDestinationIsLoadDestination(ILogger logger, string owner, string dataSet);
}
