using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints.Logging;

/// <summary>
/// MessageLogging for Pipeline endpoint base operations.
/// EventId range: 7250
/// </summary>
/// <remarks>
/// Why: Relocated from 7139 to avoid collision with DataStoreEndpointLog and ConnectionProviderLogger.
/// The 7248-7260 range is reserved for configuration endpoint logs.
/// </remarks>
[MessageLoggingTypeCode("ENDPOINTS9")]
public static partial class PipelineEndpointLog
{
    /// <summary>Logs when a modification is rejected because the pipeline is a system configuration.</summary>
    [MessageLogging(EventId = 41000, Level = LogLevel.Warning, Message = "Rejected modification of system pipeline '{pipelineName}' — system configurations are read-only")]
    public static partial IGenericMessage SystemPipelineReadOnly(ILogger logger, string pipelineName);

    /// <summary>Logs that a persisted pipeline is missing its required kind (Implementation).</summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error, Message = "Pipeline '{pipelineName}' has no kind (Implementation)")]
    public static partial IGenericMessage PipelineMissingKind(ILogger logger, string pipelineName);

    /// <summary>Logs that a pipeline draft is being checked without being saved.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Information, Message = "Checking draft pipeline '{pipelineName}' without saving it")]
    public static partial IGenericMessage DraftCheckStarted(ILogger logger, string pipelineName);

    /// <summary>Logs the outcome of checking a pipeline draft.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information, Message = "Checked draft pipeline '{pipelineName}': {problemCount} problem(s)")]
    public static partial IGenericMessage DraftCheckFinished(ILogger logger, string pipelineName, int problemCount);

    /// <summary>Logs that an edit of a saved pipeline is being checked without being saved.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Information, Message = "Checking an edit of saved pipeline '{pipelineName}' without saving it")]
    public static partial IGenericMessage EditCheckStarted(ILogger logger, string pipelineName);

    /// <summary>Logs the outcome of checking an edit of a saved pipeline.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Information, Message = "Checked an edit of saved pipeline '{pipelineName}': {problemCount} problem(s)")]
    public static partial IGenericMessage EditCheckFinished(ILogger logger, string pipelineName, int problemCount);

    /// <summary>Logs that the pipeline an edit check names does not exist.</summary>
    [MessageLogging(EventId = 30000, Level = LogLevel.Warning, Message = "Pipeline '{pipelineName}' was not found")]
    public static partial IGenericMessage PipelineNotFound(ILogger logger, string pipelineName);

    /// <summary>Logs a run-role expansion that failed without saying why, so no reason can be given to the caller.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "Expanding the run roles {roles} of pipeline '{pipelineName}' failed and gave no reason")]
    public static partial IGenericMessage RunRolesExpansionUnexplained(ILogger logger, string pipelineName, string roles);

    /// <summary>Logs a run-role expansion that reported success but returned no roles.</summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "Expanding the run roles {roles} of pipeline '{pipelineName}' succeeded but returned nothing")]
    public static partial IGenericMessage RunRolesExpansionEmpty(ILogger logger, string pipelineName, string roles);

    /// <summary>Logs a load step whose stated order is not its position in the list.</summary>
    [MessageLogging(EventId = 22001, Level = LogLevel.Warning,
        Message = "Load step '{stepName}' states order {stepOrder} but sits at position {position}; a step's order equals its position in the list, from 1")]
    public static partial IGenericMessage LoadStepOrderNotPosition(ILogger logger, string stepName, int stepOrder, int position);

    /// <summary>Logs load options stated for a load mode that takes none.</summary>
    [MessageLogging(EventId = 22002, Level = LogLevel.Warning,
        Message = "Load mode '{loadMode}' takes no load options, and some are stated")]
    public static partial IGenericMessage LoadOptionsNotForMode(ILogger logger, string loadMode);

    /// <summary>Logs a column a load definition names that is not a field of the data set it loads.</summary>
    [MessageLogging(EventId = 22003, Level = LogLevel.Warning,
        Message = "In {owner}: '{column}' is not a field of data set '{dataSet}'")]
    public static partial IGenericMessage LoadColumnNotOnDataSet(ILogger logger, string owner, string column, string dataSet);

    /// <summary>Logs a load that names columns but no destination data set to check them against.</summary>
    [MessageLogging(EventId = 22004, Level = LogLevel.Warning,
        Message = "In {owner}: columns are named, and they are checked against the destination data set, but none is named for it")]
    public static partial IGenericMessage LoadNeedsDestinationDataSet(ILogger logger, string owner);

    /// <summary>Logs a data set a load names that the caller cannot see.</summary>
    [MessageLogging(EventId = 22005, Level = LogLevel.Warning,
        Message = "In {owner}: data set '{dataSet}' is not visible to the caller")]
    public static partial IGenericMessage LoadDataSetNotVisible(ILogger logger, string owner, string dataSet);

    /// <summary>Logs a set-based copy whose source shares no connection with its destination.</summary>
    [MessageLogging(EventId = 22006, Level = LogLevel.Warning,
        Message = "In {owner}: source data set '{sourceDataSet}' shares no connection with destination data set '{destinationDataSet}'; a set-based copy runs on one connection")]
    public static partial IGenericMessage LoadSourceNotOnDestinationConnection(ILogger logger, string owner, string sourceDataSet, string destinationDataSet);

    /// <summary>Logs an expression that references a run parameter the pipeline does not declare.</summary>
    [MessageLogging(EventId = 22013, Level = LogLevel.Warning,
        Message = "In {owner}: the expression references run parameter '{parameter}', which the pipeline does not declare")]
    public static partial IGenericMessage ParameterReferenceUndeclared(ILogger logger, string owner, string parameter);

    /// <summary>Logs a data set a load reads or writes that the engine could not resolve to one container.</summary>
    [MessageLogging(EventId = 22019, Level = LogLevel.Warning,
        Message = "In {owner}: data set '{dataSet}' cannot be loaded or read by a load: {reason}")]
    public static partial IGenericMessage LoadDataSetUnusable(ILogger logger, string owner, string dataSet, string reason);
}
