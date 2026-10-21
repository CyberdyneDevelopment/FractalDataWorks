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

    /// <summary>Logs what a graph pipeline reads and writes, read from its uses.</summary>
    [MessageLogging(
        EventId = 11017,
        Level = LogLevel.Debug,
        Message = "Pipeline {name} reads {dataSetsRead} data sets and {containersRead} containers, writes {dataSetsWritten} data sets and {containersWritten} containers")]
    public static partial IGenericMessage PipelineLinkageExtracted(
        ILogger logger,
        string name,
        int dataSetsRead,
        int containersRead,
        int dataSetsWritten,
        int containersWritten);

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
