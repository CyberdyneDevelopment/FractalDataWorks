using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// MessageLogging for Web.Api CRUD endpoint operations.
/// EventId range: 4500-4530
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS3")]
public static partial class ApiEndpointLog
{

    /// <summary>Logs that a lineage graph build is starting for a specific entity.</summary>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Trace,
        Message = "Building lineage graph for {entityType} '{entityName}'")]
    public static partial IGenericMessage BuildingLineageGraph(
        ILogger logger,
        string entityType,
        string entityName);

    /// <summary>Logs the result of a lineage graph build with node and edge counts.</summary>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Information,
        Message = "Lineage graph built: {nodeCount} nodes, {edgeCount} edges")]
    public static partial IGenericMessage LineageGraphBuilt(
        ILogger logger,
        int nodeCount,
        int edgeCount);

    /// <summary>Logs a warning that the requested entity was not found in the lineage graph.</summary>
    [MessageLogging(
        EventId = 31001,
        Level = LogLevel.Warning,
        Message = "{entityType} '{entityName}' not found in lineage graph")]
    public static partial IGenericMessage EntityNotFoundInLineageGraph(
        ILogger logger,
        string entityType,
        string entityName);

    /// <summary>Logs that a field-level lineage build is starting for a specific field.</summary>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Trace,
        Message = "Building field lineage for {entityType} '{entityName}' field '{fieldName}'")]
    public static partial IGenericMessage BuildingFieldLineage(
        ILogger logger,
        string entityType,
        string entityName,
        string fieldName);

    /// <summary>Logs the number of pipeline headers loaded for lineage composition.</summary>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Debug,
        Message = "Loaded {count} pipeline headers for lineage")]
    public static partial IGenericMessage PipelineHeadersLoaded(
        ILogger logger,
        int count);

    /// <summary>Logs that a single pipeline aggregate is being composed (kind + engine body) for lineage.</summary>
    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Trace,
        Message = "Composing pipeline aggregate {name} ({id})")]
    public static partial IGenericMessage ComposingPipelineAggregate(
        ILogger logger,
        string name,
        Guid id);

    /// <summary>Logs that a pipeline aggregate was successfully composed, naming the resolved engine.</summary>
    [MessageLogging(
        EventId = 11016,
        Level = LogLevel.Debug,
        Message = "Composed pipeline {name}, engine {engineType}")]
    public static partial IGenericMessage PipelineAggregateComposed(
        ILogger logger,
        string name,
        string engineType);

    /// <summary>Logs the linkage extracted from a composed pipeline aggregate's engine body.</summary>
    [MessageLogging(
        EventId = 11017,
        Level = LogLevel.Debug,
        Message = "Pipeline {name} linkage src={sourceDataSet} dst={destinationDataSet} srcConn={sourceConnection} dstConn={destinationConnection}")]
    public static partial IGenericMessage PipelineLinkageExtracted(
        ILogger logger,
        string name,
        string sourceDataSet,
        string destinationDataSet,
        string sourceConnection,
        string destinationConnection);

    /// <summary>Logs the per-kind edge counts produced while building a lineage graph.</summary>
    [MessageLogging(
        EventId = 11018,
        Level = LogLevel.Information,
        Message = "Lineage edges created: Consumes={consumes} ProducesDataSet={producesDataSet} WritesTo={writesTo} ReadsFrom={readsFrom} DerivesFrom={derivesFrom}")]
    public static partial IGenericMessage LineageEdgesCreated(
        ILogger logger,
        int consumes,
        int producesDataSet,
        int writesTo,
        int readsFrom,
        int derivesFrom);

    /// <summary>Logs the total number of pipelines projected for lineage, and how many were node-only.</summary>
    [MessageLogging(
        EventId = 11019,
        Level = LogLevel.Debug,
        Message = "Projected {projected} pipelines for lineage ({nodeOnly} node-only)")]
    public static partial IGenericMessage PipelinesProjectedForLineage(
        ILogger logger,
        int projected,
        int nodeOnly);

    /// <summary>Logs that a pipeline header has no composed engine body and will render node-only (normal, not-yet-composed lifecycle state).</summary>
    [MessageLogging(
        EventId = 31002,
        Level = LogLevel.Debug,
        Message = "Pipeline {name} has no composed engine body; rendering node-only with no lineage edges")]
    public static partial IGenericMessage PipelineNodeOnlyNoBody(
        ILogger logger,
        string name);

    /// <summary>Logs an error that composing a pipeline aggregate failed; the pipeline renders node-only.</summary>
    [MessageLogging(
        EventId = 31003,
        Level = LogLevel.Error,
        Message = "Composing pipeline {name} failed: {error}; rendering node-only")]
    public static partial IGenericMessage PipelineAggregateComposeFailed(
        ILogger logger,
        string name,
        string error);
}
