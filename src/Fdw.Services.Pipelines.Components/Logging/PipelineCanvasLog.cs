using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Components.Logging;

/// <summary>
/// MessageLogging for pipeline canvas model operations.
/// EventId range: 4545-4571, 4591-4593
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class PipelineCanvasLog
{
    // ── AddNode ───────────────────────────────────────────────────────────────

    /// <summary>Logs that a node is being added to the canvas.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="nodeType">The type name of the node being added.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4545, Level = LogLevel.Trace, Message = "Adding {nodeType} node to pipeline canvas")]
    public static partial IGenericMessage AddingNode(ILogger logger, string nodeType);

    /// <summary>Logs that a node was added to the canvas.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="nodeId">The identifier of the new node.</param>
    /// <param name="nodeType">The type name of the node that was added.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4546, Level = LogLevel.Information, Message = "Added {nodeType} node '{nodeId}' to pipeline canvas")]
    public static partial IGenericMessage NodeAdded(ILogger logger, string nodeId, string nodeType);

    // ── Connect ───────────────────────────────────────────────────────────────

    /// <summary>Logs that nodes are being connected with an edge.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="sourceNodeId">The identifier of the source node.</param>
    /// <param name="targetNodeId">The identifier of the target node.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4547, Level = LogLevel.Trace, Message = "Connecting nodes '{sourceNodeId}' → '{targetNodeId}' on pipeline canvas")]
    public static partial IGenericMessage ConnectingNodes(ILogger logger, string sourceNodeId, string targetNodeId);

    /// <summary>Logs that an edge was created.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="edgeId">The identifier of the new edge.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4548, Level = LogLevel.Information, Message = "Created edge '{edgeId}' on pipeline canvas")]
    public static partial IGenericMessage EdgeCreated(ILogger logger, string edgeId);

    // ── MoveNode ──────────────────────────────────────────────────────────────

    /// <summary>Logs that a node is being moved.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="nodeId">The identifier of the node being moved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4549, Level = LogLevel.Trace, Message = "Moving node '{nodeId}' on pipeline canvas")]
    public static partial IGenericMessage MovingNode(ILogger logger, string nodeId);

    // ── DeleteNode ────────────────────────────────────────────────────────────

    /// <summary>Logs that a node is being deleted.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="nodeId">The identifier of the node being deleted.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4550, Level = LogLevel.Trace, Message = "Deleting node '{nodeId}' from pipeline canvas")]
    public static partial IGenericMessage DeletingNode(ILogger logger, string nodeId);

    /// <summary>Logs that a node was deleted.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="nodeId">The identifier of the deleted node.</param>
    /// <param name="edgesRemoved">The number of edges that were removed along with the node.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4551, Level = LogLevel.Information, Message = "Deleted node '{nodeId}' and {edgesRemoved} connected edge(s) from pipeline canvas")]
    public static partial IGenericMessage NodeDeleted(ILogger logger, string nodeId, int edgesRemoved);

    // ── DeleteEdge ────────────────────────────────────────────────────────────

    /// <summary>Logs that an edge was deleted.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="edgeId">The identifier of the deleted edge.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4552, Level = LogLevel.Information, Message = "Deleted edge '{edgeId}' from pipeline canvas")]
    public static partial IGenericMessage EdgeDeleted(ILogger logger, string edgeId);

    // ── Failure cases ─────────────────────────────────────────────────────────

    /// <summary>Logs that a referenced node was not found on the canvas.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="nodeId">The identifier that was not found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4553, Level = LogLevel.Warning, Message = "Node '{nodeId}' not found on pipeline canvas")]
    public static partial IGenericMessage NodeNotFound(ILogger logger, string nodeId);

    /// <summary>Logs that a referenced edge was not found on the canvas.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="edgeId">The identifier that was not found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4554, Level = LogLevel.Warning, Message = "Edge '{edgeId}' not found on pipeline canvas")]
    public static partial IGenericMessage EdgeNotFound(ILogger logger, string edgeId);

    // ── UpdateNodeMetadata ────────────────────────────────────────────────────

    /// <summary>Logs that node metadata was updated.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="nodeId">The identifier of the node whose metadata was updated.</param>
    /// <param name="keyCount">The number of metadata keys that were merged.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4563, Level = LogLevel.Trace, Message = "Updated metadata on node '{nodeId}' ({keyCount} key(s))")]
    public static partial IGenericMessage NodeMetadataUpdated(ILogger logger, string nodeId, int keyCount);

    /// <summary>Logs that a node metadata update was rejected because the node was not found.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="nodeId">The identifier that was not found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4564, Level = LogLevel.Warning, Message = "Cannot update metadata: node '{nodeId}' not found on pipeline canvas")]
    public static partial IGenericMessage NodeMetadataUpdateNodeNotFound(ILogger logger, string nodeId);

    // ── Projection ────────────────────────────────────────────────────────────

    /// <summary>Logs that an ETL pipeline configuration is being projected to a canvas model.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="pipelineName">The name of the pipeline being projected.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4555, Level = LogLevel.Trace, Message = "Projecting ETL pipeline '{pipelineName}' to canvas model")]
    public static partial IGenericMessage ProjectingToCanvas(ILogger logger, string pipelineName);

    /// <summary>Logs that canvas validation failed when projecting back to configuration.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="issueCount">The number of validation issues found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4556, Level = LogLevel.Warning, Message = "Pipeline canvas has {issueCount} validation issue(s) — projection to configuration rejected")]
    public static partial IGenericMessage ValidationFailed(ILogger logger, int issueCount);

    /// <summary>Logs that the canvas model has no source DataSet node.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4557, Level = LogLevel.Warning, Message = "Pipeline canvas has no source DataSet node")]
    public static partial IGenericMessage NoSourceDataSetNode(ILogger logger);

    /// <summary>Logs that the canvas model has no sink DataSet node.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4558, Level = LogLevel.Warning, Message = "Pipeline canvas has no sink DataSet node")]
    public static partial IGenericMessage NoSinkDataSetNode(ILogger logger);

    /// <summary>Logs that the EtlPipelineConfiguration typed body is missing from the pipeline configuration.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="pipelineName">The name of the pipeline.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4559, Level = LogLevel.Error, Message = "ETL pipeline configuration typed body is missing for pipeline '{pipelineName}'")]
    public static partial IGenericMessage EtlConfigurationMissing(ILogger logger, string pipelineName);

    /// <summary>Logs that the engine configuration typed body is missing from the ETL pipeline configuration.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="pipelineName">The name of the pipeline.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4560, Level = LogLevel.Error, Message = "Engine configuration typed body is missing for ETL pipeline '{pipelineName}'")]
    public static partial IGenericMessage EngineConfigurationMissing(ILogger logger, string pipelineName);

    /// <summary>Logs that an unrecognised engine configuration type was encountered during projection.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The CLR type name of the unrecognised engine configuration.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4561, Level = LogLevel.Error, Message = "Unrecognised engine configuration type '{typeName}' — projection to canvas aborted")]
    public static partial IGenericMessage UnrecognisedEngineConfigurationType(ILogger logger, string typeName);

    /// <summary>Logs that the flow path could not be resolved during projection to configuration.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4562, Level = LogLevel.Warning, Message = "Flow path could not be resolved on pipeline canvas — transforms will be empty")]
    public static partial IGenericMessage FlowPathUnresolved(ILogger logger);

    // ── Designer-contract projection (ToCanvas / ToDetail) ───────────────────────

    /// <summary>Logs that a task's TaskType is not registered in CanvasNodeTypes during projection.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="taskType">The unregistered task type name.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4565, Level = LogLevel.Warning, Message = "Task type '{taskType}' is not registered in CanvasNodeTypes — task node skipped during canvas projection")]
    public static partial IGenericMessage UnknownTaskNodeType(ILogger logger, string taskType);

    /// <summary>Logs that a connection's EdgeKind is not registered in CanvasEdgeTypes during projection.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="edgeKind">The unregistered edge kind name.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4566, Level = LogLevel.Warning, Message = "Edge kind '{edgeKind}' is not registered in CanvasEdgeTypes — connection skipped during canvas projection")]
    public static partial IGenericMessage UnknownEdgeKind(ILogger logger, string edgeKind);

    // ── CreatePipelineClientRequest projection (PipelineCreateRequestProjection) ─────────

    /// <summary>Logs that a source/sink DataSet node is missing a required metadata key.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="role">The DataSet role ("Source" or "Sink").</param>
    /// <param name="metadataKey">The required metadata key that was missing.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4567, Level = LogLevel.Warning, Message = "Pipeline canvas {role} DataSet node is missing required metadata '{metadataKey}' — create-pipeline request cannot be built")]
    public static partial IGenericMessage RequiredDataSetMetadataMissing(ILogger logger, string role, string metadataKey);

    /// <summary>Logs that a transform node has no OperationType metadata set.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="nodeId">The identifier of the transform node.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4568, Level = LogLevel.Warning, Message = "Pipeline canvas transform node '{nodeId}' has no OperationType metadata set — create-pipeline request cannot be built")]
    public static partial IGenericMessage RequiredTransformOperationTypeMissing(ILogger logger, string nodeId);

    /// <summary>Logs that a transform node has a missing or non-numeric ExecutionOrder metadata value.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="nodeId">The identifier of the transform node.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4569, Level = LogLevel.Warning, Message = "Pipeline canvas transform node '{nodeId}' has a missing or non-numeric ExecutionOrder metadata value — create-pipeline request cannot be built")]
    public static partial IGenericMessage TransformExecutionOrderInvalid(ILogger logger, string nodeId);

    /// <summary>Logs that a transform node's ConfigPayload JSON could not be parsed for its OperationType.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="exception">The exception thrown while parsing the payload.</param>
    /// <param name="nodeId">The identifier of the transform node.</param>
    /// <param name="operationType">The transform's OperationType.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4570, Level = LogLevel.Error, Message = "Pipeline canvas transform node '{nodeId}' ConfigPayload could not be parsed for operation type '{operationType}'")]
    public static partial IGenericMessage TransformConfigPayloadUnparseable(ILogger logger, Exception exception, string nodeId, string operationType);

    /// <summary>Logs that a transform node's OperationType has no known ConfigPayload shape to parse into.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="nodeId">The identifier of the transform node.</param>
    /// <param name="operationType">The unrecognised OperationType.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4571, Level = LogLevel.Warning, Message = "Pipeline canvas transform node '{nodeId}' has unrecognised operation type '{operationType}' — its ConfigPayload cannot be parsed")]
    public static partial IGenericMessage TransformOperationTypeUnrecognized(ILogger logger, string nodeId, string operationType);

    // ── PipelineDetailResponse projection (load existing pipeline) ────────────

    /// <summary>Logs that a loaded pipeline is being projected to a canvas model.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="pipelineName">The name of the pipeline being projected.</param>
    /// <param name="id">The identifier of the pipeline being projected.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4591, Level = LogLevel.Trace, Message = "Projecting loaded pipeline '{pipelineName}' ({id}) to canvas model")]
    public static partial IGenericMessage ProjectingPipelineDetailToCanvas(ILogger logger, string pipelineName, Guid id);

    /// <summary>Logs that a loaded pipeline has no PipelineType set and cannot be projected.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="pipelineName">The name of the pipeline.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4592, Level = LogLevel.Error, Message = "Loaded pipeline '{pipelineName}' has no PipelineType set — cannot project to canvas")]
    public static partial IGenericMessage LoadedPipelineTypeMissing(ILogger logger, string pipelineName);

    /// <summary>Logs that a loaded pipeline was projected to a canvas model.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="pipelineName">The name of the pipeline that was projected.</param>
    /// <param name="nodeCount">The number of canvas nodes produced.</param>
    /// <param name="edgeCount">The number of canvas edges produced.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4593, Level = LogLevel.Information, Message = "Loaded pipeline '{pipelineName}' projected to canvas: {nodeCount} node(s), {edgeCount} edge(s)")]
    public static partial IGenericMessage LoadedPipelineProjectedToCanvas(ILogger logger, string pipelineName, int nodeCount, int edgeCount);
}
