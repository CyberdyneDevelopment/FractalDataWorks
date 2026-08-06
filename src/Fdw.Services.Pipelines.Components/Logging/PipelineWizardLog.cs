using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.UI.Components.Blazor.Logging;

/// <summary>
/// MessageLogging methods for PipelineWizard component operations.
/// EventId range: 11018-11020, 11023-11028 (Trace/Info), 71007, 71009-71011 (Warning),
/// 91007, 91011-91022 (Error).
/// </summary>
[MessageLoggingTypeCode("COMPONENTS12")]
public static partial class PipelineWizardLog
{
    /// <summary>Logs when the wizard advances to the next step.</summary>
    [MessageLogging(EventId = 11018, Level = LogLevel.Trace,
        Message = "PipelineWizard: Advanced to step '{stepName}'")]
    public static partial IGenericMessage StepAdvanced(
        ILogger logger,
        string stepName);

    /// <summary>Logs when the user selects a Pipeline type.</summary>
    [MessageLogging(EventId = 11019, Level = LogLevel.Trace,
        Message = "PipelineWizard: Pipeline type '{typeName}' selected")]
    public static partial IGenericMessage TypeSelected(
        ILogger logger,
        string typeName);

    /// <summary>Logs when saving a Pipeline in the wizard fails.</summary>
    [MessageLogging(EventId = 71007, Level = LogLevel.Error,
        Message = "PipelineWizard: Failed to save Pipeline")]
    public static partial IGenericMessage SaveFailed(
        ILogger logger);

    /// <summary>Logs when saving a Pipeline in the wizard fails with exception.</summary>
    [MessageLogging(EventId = 91007, Level = LogLevel.Error,
        Message = "PipelineWizard: Failed to save Pipeline")]
    public static partial IGenericMessage SaveException(
        ILogger logger,
        Exception exception);

    /// <summary>Logs when the wizard completes successfully.</summary>
    [MessageLogging(EventId = 11020, Level = LogLevel.Information,
        Message = "PipelineWizard: Pipeline '{pipelineName}' created successfully")]
    public static partial IGenericMessage WizardCompleted(
        ILogger logger,
        string pipelineName);

    // ── Engine type loading (GetPipelineTypes — EtlPipelineTypes ServiceTypeCollection) ─────────

    /// <summary>Logs that the wizard has started loading pipeline engine types from the API.</summary>
    [MessageLogging(EventId = 11023, Level = LogLevel.Trace,
        Message = "PipelineWizard: Loading pipeline engine types from API")]
    public static partial IGenericMessage LoadingEngineTypes(ILogger logger);

    /// <summary>Logs that the wizard loaded the given number of pipeline engine types.</summary>
    [MessageLogging(EventId = 11024, Level = LogLevel.Information,
        Message = "PipelineWizard: Loaded {count} pipeline engine type(s)")]
    public static partial IGenericMessage EngineTypesLoaded(ILogger logger, int count);

    /// <summary>Logs that loading pipeline engine types failed (non-exception).</summary>
    [MessageLogging(EventId = 71009, Level = LogLevel.Warning,
        Message = "PipelineWizard: Failed to load pipeline engine types from API")]
    public static partial IGenericMessage LoadEngineTypesFailed(ILogger logger);

    /// <summary>Logs that loading pipeline engine types raised an exception.</summary>
    [MessageLogging(EventId = 91011, Level = LogLevel.Error,
        Message = "PipelineWizard: Exception loading pipeline engine types")]
    public static partial IGenericMessage LoadEngineTypesException(ILogger logger, Exception exception);

    // ── Connections loading (Source/Destination Connection-kind picker) ─────────────────────────

    /// <summary>Logs that the wizard has started loading connections from the API.</summary>
    [MessageLogging(EventId = 11025, Level = LogLevel.Trace,
        Message = "PipelineWizard: Loading connections from API")]
    public static partial IGenericMessage LoadingConnections(ILogger logger);

    /// <summary>Logs that the wizard loaded the given number of connections.</summary>
    [MessageLogging(EventId = 11026, Level = LogLevel.Information,
        Message = "PipelineWizard: Loaded {count} connection(s)")]
    public static partial IGenericMessage ConnectionsLoaded(ILogger logger, int count);

    /// <summary>Logs that loading connections failed (non-exception).</summary>
    [MessageLogging(EventId = 71010, Level = LogLevel.Warning,
        Message = "PipelineWizard: Failed to load connections from API")]
    public static partial IGenericMessage LoadConnectionsFailed(ILogger logger);

    /// <summary>Logs that loading connections raised an exception.</summary>
    [MessageLogging(EventId = 91021, Level = LogLevel.Error,
        Message = "PipelineWizard: Exception loading connections")]
    public static partial IGenericMessage LoadConnectionsException(ILogger logger, Exception exception);

    // ── DataSets loading (Source/Destination DataSet-kind picker) ────────────────────────────────

    /// <summary>Logs that the wizard has started loading DataSets from the API.</summary>
    [MessageLogging(EventId = 11027, Level = LogLevel.Trace,
        Message = "PipelineWizard: Loading DataSets from API")]
    public static partial IGenericMessage LoadingWizardDataSets(ILogger logger);

    /// <summary>Logs that the wizard loaded the given number of DataSets.</summary>
    [MessageLogging(EventId = 11028, Level = LogLevel.Information,
        Message = "PipelineWizard: Loaded {count} DataSet(s)")]
    public static partial IGenericMessage WizardDataSetsLoaded(ILogger logger, int count);

    /// <summary>Logs that loading DataSets failed (non-exception).</summary>
    [MessageLogging(EventId = 71011, Level = LogLevel.Warning,
        Message = "PipelineWizard: Failed to load DataSets from API")]
    public static partial IGenericMessage LoadWizardDataSetsFailed(ILogger logger);

    /// <summary>Logs that loading DataSets raised an exception.</summary>
    [MessageLogging(EventId = 91022, Level = LogLevel.Error,
        Message = "PipelineWizard: Exception loading DataSets")]
    public static partial IGenericMessage LoadWizardDataSetsException(ILogger logger, Exception exception);

    // ── Fail-loud validation (required step fields) ──────────────────────────────────────────────

    /// <summary>Logs that pipeline creation was blocked because no engine type is selected.</summary>
    [MessageLogging(EventId = 91012, Level = LogLevel.Error,
        Message = "PipelineWizard: Cannot proceed — no pipeline engine type selected")]
    public static partial IGenericMessage NoEngineTypeSelected(ILogger logger);

    /// <summary>Logs that pipeline creation was blocked because no pipeline name is set.</summary>
    [MessageLogging(EventId = 91013, Level = LogLevel.Error,
        Message = "PipelineWizard: Cannot proceed — pipeline name is required")]
    public static partial IGenericMessage PipelineNameRequired(ILogger logger);

    /// <summary>Logs that pipeline creation was blocked because no source is selected.</summary>
    [MessageLogging(EventId = 91014, Level = LogLevel.Error,
        Message = "PipelineWizard: Cannot proceed — source connection/DataSet is required")]
    public static partial IGenericMessage SourceRequired(ILogger logger);

    /// <summary>Logs that pipeline creation was blocked because no destination is selected.</summary>
    [MessageLogging(EventId = 91015, Level = LogLevel.Error,
        Message = "PipelineWizard: Cannot proceed — destination connection/DataSet is required")]
    public static partial IGenericMessage DestinationRequired(ILogger logger);

    /// <summary>Logs that the source DataSet could not be loaded while resolving its connection.</summary>
    [MessageLogging(EventId = 91016, Level = LogLevel.Error,
        Message = "PipelineWizard: Failed to load source DataSet '{dataSetName}' while resolving its connection")]
    public static partial IGenericMessage SourceDataSetLoadFailed(ILogger logger, string dataSetName);

    /// <summary>
    /// Logs that the source DataSet's underlying connection could not be resolved (no primary
    /// source, or the primary source has no ConnectionName). No fallback connection is substituted.
    /// </summary>
    [MessageLogging(EventId = 91017, Level = LogLevel.Error,
        Message = "PipelineWizard: Source DataSet '{dataSetName}' has no resolvable connection (no single primary source with a ConnectionName)")]
    public static partial IGenericMessage SourceDataSetConnectionUnresolved(ILogger logger, string dataSetName);

    /// <summary>Logs that the destination DataSet could not be loaded while resolving its connection.</summary>
    [MessageLogging(EventId = 91018, Level = LogLevel.Error,
        Message = "PipelineWizard: Failed to load destination DataSet '{dataSetName}' while resolving its connection")]
    public static partial IGenericMessage DestinationDataSetLoadFailed(ILogger logger, string dataSetName);

    /// <summary>
    /// Logs that the destination DataSet's underlying connection could not be resolved (no primary
    /// source, or the primary source has no ConnectionName). No fallback connection is substituted.
    /// </summary>
    [MessageLogging(EventId = 91019, Level = LogLevel.Error,
        Message = "PipelineWizard: Destination DataSet '{dataSetName}' has no resolvable connection (no single primary source with a ConnectionName)")]
    public static partial IGenericMessage DestinationDataSetConnectionUnresolved(ILogger logger, string dataSetName);

    /// <summary>Logs that creating the pipeline shell raised an exception.</summary>
    [MessageLogging(EventId = 91020, Level = LogLevel.Error,
        Message = "PipelineWizard: Exception creating pipeline '{pipelineName}'")]
    public static partial IGenericMessage CreatePipelineException(ILogger logger, Exception exception, string pipelineName);
}
