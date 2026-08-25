using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Components.Logging;

/// <summary>
/// MessageLogging for PipelineBuilderProvider operations.
/// EventId range: 4250-4265
/// </summary>
[MessageLoggingTypeCode("COMPONENTS12")]
public static partial class PipelineBuilderProviderLog
{
    /// <summary>
    /// Logs that task types are being loaded for the pipeline builder.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Trace,
        Message = "Loading task types for pipeline builder")]
    public static partial IGenericMessage LoadingTaskTypes(ILogger logger);

    /// <summary>
    /// Logs that task types were loaded for the pipeline builder.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="count">The number of task types that were loaded.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Information,
        Message = "Loaded {count} task types for pipeline builder")]
    public static partial IGenericMessage LoadedTaskTypes(ILogger logger, int count);

    /// <summary>
    /// Logs that loading task types for the pipeline builder failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 71002,
        Level = LogLevel.Error,
        Message = "Failed to load task types for pipeline builder")]
    public static partial IGenericMessage LoadTaskTypesFailed(ILogger logger);

    /// <summary>
    /// Logs that an exception occurred while loading task types for the pipeline builder.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="exception">The exception that was thrown while loading task types.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 91002,
        Level = LogLevel.Error,
        Message = "Failed to load task types for pipeline builder")]
    public static partial IGenericMessage LoadTaskTypesException(ILogger logger, Exception exception);

    /// <summary>
    /// Logs that an existing pipeline is being loaded.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="id">The identifier of the pipeline being loaded.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Trace,
        Message = "Loading existing pipeline '{id}'")]
    public static partial IGenericMessage LoadingExistingPipeline(ILogger logger, Guid id);

    /// <summary>
    /// Logs that an existing pipeline was loaded.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="id">The identifier of the pipeline that was loaded.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Information,
        Message = "Loaded existing pipeline '{id}'")]
    public static partial IGenericMessage LoadedExistingPipeline(ILogger logger, Guid id);

    /// <summary>
    /// Logs that loading an existing pipeline failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="id">The identifier of the pipeline that failed to load.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 71003,
        Level = LogLevel.Error,
        Message = "Failed to load existing pipeline '{id}'")]
    public static partial IGenericMessage LoadExistingPipelineFailed(ILogger logger, Guid id);

    /// <summary>
    /// Logs that an exception occurred while loading an existing pipeline.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="exception">The exception that was thrown while loading the pipeline.</param>
    /// <param name="id">The identifier of the pipeline that failed to load.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 91003,
        Level = LogLevel.Error,
        Message = "Failed to load existing pipeline '{id}'")]
    public static partial IGenericMessage LoadExistingPipelineException(ILogger logger, Exception exception, Guid id);

    /// <summary>
    /// Logs that a pipeline is being saved.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the pipeline being saved.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Trace,
        Message = "Saving pipeline '{name}'")]
    public static partial IGenericMessage SavingPipeline(ILogger logger, string name);

    /// <summary>
    /// Logs that a pipeline was saved successfully.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the pipeline that was saved.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Information,
        Message = "Pipeline '{name}' saved successfully")]
    public static partial IGenericMessage PipelineSaved(ILogger logger, string name);

    /// <summary>
    /// Logs that saving a pipeline failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the pipeline that failed to save.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 71004,
        Level = LogLevel.Error,
        Message = "Failed to save pipeline '{name}'")]
    public static partial IGenericMessage SavePipelineFailed(ILogger logger, string name);

    /// <summary>
    /// Logs that an exception occurred while saving a pipeline.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="exception">The exception that was thrown while saving the pipeline.</param>
    /// <param name="name">The name of the pipeline that failed to save.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 91004,
        Level = LogLevel.Error,
        Message = "Failed to save pipeline '{name}'")]
    public static partial IGenericMessage SavePipelineException(ILogger logger, Exception exception, string name);

    /// <summary>
    /// Logs that a pipeline is being published.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the pipeline being published.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Trace,
        Message = "Publishing pipeline '{name}'")]
    public static partial IGenericMessage PublishingPipeline(ILogger logger, string name);

    /// <summary>
    /// Logs that a pipeline was published successfully.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the pipeline that was published.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Information,
        Message = "Pipeline '{name}' published successfully")]
    public static partial IGenericMessage PipelinePublished(ILogger logger, string name);

    /// <summary>
    /// Logs that publishing a pipeline failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the pipeline that failed to publish.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 71005,
        Level = LogLevel.Error,
        Message = "Failed to publish pipeline '{name}'")]
    public static partial IGenericMessage PublishPipelineFailed(ILogger logger, string name);

    /// <summary>
    /// Logs that an exception occurred while publishing a pipeline.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="exception">The exception that was thrown while publishing the pipeline.</param>
    /// <param name="name">The name of the pipeline that failed to publish.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 91005,
        Level = LogLevel.Error,
        Message = "Failed to publish pipeline '{name}'")]
    public static partial IGenericMessage PublishPipelineException(ILogger logger, Exception exception, string name);

    // ── Journey Actions ────────────────────────────────────────────────────────

    /// <summary>
    /// Logs that the Pipeline Builder was opened with a pre-selected source DataSet via query parameter.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="dataSetName">The name of the DataSet that was pre-selected as the source.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Information,
        Message = "Pipeline Builder opened with initial source DataSet '{dataSetName}'")]
    public static partial IGenericMessage InitialSourceDataSetApplied(ILogger logger, string dataSetName);

    /// <summary>
    /// Logs that a drag-and-drop from the palette used a node type name that is not registered in
    /// <c>CanvasNodeTypes</c>.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="nodeTypeName">The node type name that was not found.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 41001,
        Level = LogLevel.Warning,
        Message = "Drag-and-drop aborted: node type '{nodeTypeName}' is not registered in CanvasNodeTypes")]
    public static partial IGenericMessage UnknownDragNodeType(ILogger logger, string nodeTypeName);

    // ── Real-pipeline load/save path (canvas-wired) ────────────────────────────

    /// <summary>
    /// Logs that the pipeline canvas model is being projected for display.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="id">The identifier of the pipeline being projected.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11032,
        Level = LogLevel.Trace,
        Message = "Projecting pipeline '{id}' to canvas model")]
    public static partial IGenericMessage ProjectingToCanvas(ILogger logger, Guid id);

    /// <summary>
    /// Logs that the pipeline canvas model was built from a real pipeline configuration.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="id">The identifier of the pipeline.</param>
    /// <param name="nodeCount">The number of canvas nodes produced.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11033,
        Level = LogLevel.Information,
        Message = "Pipeline '{id}' projected to canvas: {nodeCount} node(s)")]
    public static partial IGenericMessage ProjectedToCanvas(ILogger logger, Guid id, int nodeCount);

    /// <summary>
    /// Logs that canvas save validation failed before persisting.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the pipeline that failed validation.</param>
    /// <param name="issueCount">The number of validation issues found.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 71013,
        Level = LogLevel.Warning,
        Message = "Pipeline '{name}' canvas validation rejected save: {issueCount} issue(s)")]
    public static partial IGenericMessage SaveRejectedByValidation(ILogger logger, string name, int issueCount);

    /// <summary>
    /// Logs that canvas-to-configuration projection failed before persisting.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the pipeline that failed projection.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 71014,
        Level = LogLevel.Error,
        Message = "Pipeline '{name}' canvas-to-configuration projection failed")]
    public static partial IGenericMessage CanvasProjectionFailed(ILogger logger, string name);

    /// <summary>
    /// Logs that no pipeline exists with the given id.
    /// </summary>
    /// <remarks>
    /// Why: <see cref="Pipelines.PipelineBuilderProvider.LoadExisting"/> resolves the route's Guid id
    /// to a pipeline name via <c>IPipelineClient.List</c> (the server contract is name-keyed) before
    /// calling <c>IPipelineClient.Get</c> — this logs the case where no summary in the list carries a
    /// matching id.
    /// </remarks>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="id">The identifier that was not found.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 71015,
        Level = LogLevel.Warning,
        Message = "No pipeline found with id '{id}'")]
    public static partial IGenericMessage PipelineNotFoundById(ILogger logger, Guid id);

    /// <summary>
    /// Logs that a registered <c>CanvasNodeTypes</c>/<c>CanvasEdgeTypes</c> member the pipeline canvas
    /// depends on is missing from the type collection.
    /// </summary>
    /// <remarks>
    /// Why: a configuration/deployment error (the type collection lost a seeded member), not a
    /// user-facing unknown-name case — see <see cref="UnknownDragNodeType"/> for that.
    /// </remarks>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The canvas node/edge type name that is not registered.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 91026,
        Level = LogLevel.Error,
        Message = "Canvas node/edge type '{typeName}' is not registered — the pipeline canvas cannot be built")]
    public static partial IGenericMessage CanvasNodeTypeUnregistered(ILogger logger, string typeName);

    /// <summary>
    /// Logs that the pipeline canvas's engine discriminator (PipelineType) was set from a user
    /// selection.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="pipelineType">The engine type that was set (e.g. "BatchCopy").</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11031,
        Level = LogLevel.Information,
        Message = "Pipeline canvas engine type set to '{pipelineType}'")]
    public static partial IGenericMessage PipelineTypeSet(ILogger logger, string pipelineType);

    /// <summary>
    /// Logs that pipeline engine types are being loaded from the API.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11029,
        Level = LogLevel.Trace,
        Message = "Loading pipeline engine types for pipeline builder")]
    public static partial IGenericMessage LoadingPipelineTypes(ILogger logger);

    /// <summary>
    /// Logs that pipeline engine types were loaded.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="count">The number of pipeline engine types that were loaded.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11030,
        Level = LogLevel.Information,
        Message = "Loaded {count} pipeline engine types for pipeline builder")]
    public static partial IGenericMessage LoadedPipelineTypes(ILogger logger, int count);

    /// <summary>
    /// Logs that loading pipeline engine types failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 71012,
        Level = LogLevel.Warning,
        Message = "Failed to load pipeline engine types for pipeline builder")]
    public static partial IGenericMessage LoadPipelineTypesFailed(ILogger logger);

    /// <summary>
    /// Logs that an exception occurred while loading pipeline engine types.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="exception">The exception that was thrown while loading pipeline engine types.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 91023,
        Level = LogLevel.Error,
        Message = "Failed to load pipeline engine types for pipeline builder")]
    public static partial IGenericMessage LoadPipelineTypesException(ILogger logger, Exception exception);

    /// <summary>
    /// Logs that no designer client capability exists to persist a real pipeline's tasks/connections.
    /// </summary>
    /// <remarks>
    /// Why: the designer CRUD surface (<c>FileSystemDesignerPipelineStore</c>) was retired and never
    /// replaced with a live endpoint; <c>IPipelineClient</c>'s Save/Publish path (via
    /// <c>PipelineCreateRequestProjection</c>) is the real, persistence-capable path used instead. See
    /// <see cref="Pipelines.PipelineBuilderProvider.Save"/>.
    /// </remarks>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the pipeline that could not be saved.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 91024,
        Level = LogLevel.Error,
        Message = "Cannot save pipeline '{name}': no designer client capability persists tasks/connections for a real pipeline — real pipeline save is unavailable")]
    public static partial IGenericMessage RealPipelineSaveUnavailable(ILogger logger, string name);

    /// <summary>
    /// Logs that a connection type's command-capability metadata (the per-capability
    /// <c>ConfigurationFields</c> / <c>BuilderComponentType</c> the properties panel needs to render
    /// the Command picker and its fields) is not available over the connections API.
    /// </summary>
    /// <remarks>
    /// Why: <c>IConnectionType.SupportedCommands</c> lives only on the server-side <c>ConnectionTypes</c>
    /// TypeCollection (Fdw.Services.Connections core), which Fdw.UI.Pages no longer references (canvas
    /// decoupling — UI.Pages depends only on *.Abstractions/*.Clients packages). Neither
    /// <c>ConnectionPayload</c> nor <c>ConnectionTypeCapabilitiesPayload</c> (connection-types/{name}/capabilities,
    /// which projects ContainerTypes/FieldTypes/WriteModes/PathFormats — a different concept) carries this
    /// metadata. Until a connection-types/{name}/command-capabilities endpoint ships, the Command picker,
    /// its per-capability fields, and the destination write-capability badge have no data source — render
    /// empty/omit rather than guessing at a hardcoded capability list or a fabricated "no write capability"
    /// verdict.
    /// </remarks>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="connectionTypeName">The connection type name whose capabilities are unavailable.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 91025,
        Level = LogLevel.Error,
        Message = "Command capabilities for connection type '{connectionTypeName}' are unavailable: the connections API does not project SupportedCommands — the Command picker and write-capability badge render empty")]
    public static partial IGenericMessage ConnectionCommandCapabilitiesUnavailable(ILogger logger, string connectionTypeName);

    /// <summary>
    /// Logs that the transform-operation type catalog (the dropdown + per-type
    /// <c>ConfigurationFields</c> the properties panel needs for a Transform task) is not available
    /// over any endpoint.
    /// </summary>
    /// <remarks>
    /// Why: <c>OperationTypes.All()</c>/<c>TransformationTypes.All()</c> live only in the server-side
    /// Fdw.Services.Transformations core, which Fdw.UI.Pages no longer references (canvas decoupling).
    /// <c>GetConfigurationTypesByCategoryEndpointBase</c> (configuration/types?category=) is NOT the right
    /// mechanism here — it discovers <c>[ManagedConfiguration]</c> containers by physical schema/table
    /// (and, per its own remarks, returns empty until Wave A6 adds SectionPath metadata to
    /// IDataContainer), not <c>ServiceTypeCollection</c> behavior catalogs. The same gap already forced
    /// Connection/Schedule/DataSet/DataStore off that generic endpoint onto their own dedicated
    /// GetXxxTypes endpoint+client method (see the "hits configuration/types?category=... (404)" /
    /// "returns 0 results" Why: comments on ConnectionWizardProvider, ConnectionProvider,
    /// ConnectionEditorProvider, ScheduleProvider, and DataSetWizardProvider) — even after Wave A6,
    /// category="Transformation" would resolve the single TransformationConfiguration container record,
    /// not the dozen TypeOption behaviors (Filter/Map/Join/Aggregate/…) the dropdown needs. Until a
    /// dedicated transform-types endpoint+client method ships (mirroring
    /// ConnectionApiClient.GetConnectionTypes / ScheduleClient.GetScheduleTypes), the transform-type
    /// dropdown has no data source — render empty rather than wiring a call that would silently return
    /// the wrong data forever.
    /// </remarks>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 91009,
        Level = LogLevel.Error,
        Message = "Transform-operation type catalog is unavailable: no endpoint projects the Transformation TypeCollection to the client — the Transform Type dropdown renders empty")]
    public static partial IGenericMessage TransformOperationTypesUnavailable(ILogger logger);

    /// <summary>
    /// Logs that a create-pipeline request could not be built because nothing carries a PipelineType
    /// value for the pipeline being saved.
    /// </summary>
    /// <remarks>
    /// Why: <c>CreatePipelineClientRequest.PipelineType</c> is a required engine discriminator (e.g.
    /// "BatchCopy") on the server contract, but nothing in the canvas model, edit context, or this
    /// provider carries a PipelineType value today — there is no wizard step or canvas metadata key
    /// for it. Rather than default it (masking the gap per FDW-556 Part 6.4 precedent), Save/Publish
    /// fail loud here until a real source exists (e.g. a Pipeline Wizard "engine type" step).
    /// </remarks>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="pipelineName">The name of the pipeline that could not be saved.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 91010,
        Level = LogLevel.Error,
        Message = "Cannot build create-pipeline request for '{pipelineName}': no canvas/builder mechanism carries a PipelineType value — pipeline type is unavailable")]
    public static partial IGenericMessage PipelineTypeUnavailable(ILogger logger, string pipelineName);
}
