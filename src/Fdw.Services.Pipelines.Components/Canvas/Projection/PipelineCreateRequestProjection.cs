using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Components.Logging;
using Fdw.UI.Abstractions.Canvas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Components.Canvas.Projection;

/// <summary>
/// Pure, render-agnostic projection from a <see cref="PipelineCanvasModel"/> to the real,
/// persistence-capable client-request contract (<see cref="CreatePipelineClientRequest"/>) consumed
/// by <c>IPipelineClient.CreatePipeline</c>/<c>UpdatePipeline</c>.
/// </summary>
/// <remarks>
/// <para>
/// Distinct from <see cref="PipelineCanvasProjection.ToDetail"/>, which projects to
/// <see cref="Fdw.UI.Pipelines.Clients.Models.PipelineDetailPayload"/> — the designer task/connection graph
/// contract that currently has no live persistence endpoint (see the Why remarks on
/// <c>PipelineBuilderProvider.Save</c>). This projection targets the contract that actually persists.
/// </para>
/// <para>
/// Reads exclusively via <see cref="PipelineCanvasMetadataKeys"/> conventions: one Source DataSet
/// node, one Sink DataSet node, and zero or more Transform nodes ordered by their
/// <see cref="PipelineCanvasMetadataKeys.ExecutionOrder"/> metadata. Every required piece is
/// validated before a value is produced — no fallback/defaulted values are ever substituted for a
/// missing piece; the first missing/invalid piece fails the whole projection.
/// </para>
/// </remarks>
public static class PipelineCreateRequestProjection
{
    /// <summary>
    /// Projects a <see cref="PipelineCanvasModel"/> into a <see cref="CreatePipelineClientRequest"/>.
    /// </summary>
    /// <remarks>
    /// Why: <see cref="CreatePipelineClientRequest.PipelineType"/> is a required engine discriminator
    /// (e.g. "BatchCopy") on the server contract. It is read from <see cref="PipelineCanvasModel.PipelineType"/>
    /// (set from a loaded pipeline's <c>PipelineDetailResponse.PipelineType</c>, or from a canvas
    /// toolbar engine picker for a new pipeline). This projection validates everything it can resolve
    /// from the canvas first, then fails loud via <see cref="PipelineBuilderProviderLog.PipelineTypeUnavailable"/>
    /// only when <see cref="PipelineCanvasModel.PipelineType"/> is genuinely null/empty — no default
    /// is ever substituted for a missing value.
    /// </remarks>
    /// <param name="model">The canvas model to project.</param>
    /// <param name="pipelineName">The pipeline name for the resulting request.</param>
    /// <param name="pipelineDescription">The optional pipeline description.</param>
    /// <param name="logger">Optional logger; defaults to <see cref="NullLogger"/> when null.</param>
    /// <returns>A result containing the projected request on success, or the first validation failure.</returns>
    public static IGenericResult<CreatePipelineClientRequest> ToCreateRequest(
        PipelineCanvasModel model,
        string pipelineName,
        string? pipelineDescription,
        ILogger? logger = null)
    {
        var log = logger ?? NullLogger.Instance;

        var sourceResult = FindDataSetNode(model, PipelineCanvasMetadataKeys.RoleSource, log);
        if (!sourceResult.IsSuccess)
            return sourceResult.ToNewResult<CreatePipelineClientRequest>();

        var sourceConnectionResult = RequireDataSetMetadata(sourceResult.Value!, PipelineCanvasMetadataKeys.RoleSource, PipelineCanvasMetadataKeys.ConnectionName, log);
        if (!sourceConnectionResult.IsSuccess)
            return sourceConnectionResult.ToNewResult<CreatePipelineClientRequest>();

        var sourceDataSetResult = RequireDataSetMetadata(sourceResult.Value!, PipelineCanvasMetadataKeys.RoleSource, PipelineCanvasMetadataKeys.DataSetName, log);
        if (!sourceDataSetResult.IsSuccess)
            return sourceDataSetResult.ToNewResult<CreatePipelineClientRequest>();

        var sinkResult = FindDataSetNode(model, PipelineCanvasMetadataKeys.RoleSink, log);
        if (!sinkResult.IsSuccess)
            return sinkResult.ToNewResult<CreatePipelineClientRequest>();

        var sinkConnectionResult = RequireDataSetMetadata(sinkResult.Value!, PipelineCanvasMetadataKeys.RoleSink, PipelineCanvasMetadataKeys.ConnectionName, log);
        if (!sinkConnectionResult.IsSuccess)
            return sinkConnectionResult.ToNewResult<CreatePipelineClientRequest>();

        var sinkDataSetResult = RequireDataSetMetadata(sinkResult.Value!, PipelineCanvasMetadataKeys.RoleSink, PipelineCanvasMetadataKeys.DataSetName, log);
        if (!sinkDataSetResult.IsSuccess)
            return sinkDataSetResult.ToNewResult<CreatePipelineClientRequest>();

        var transformsResult = BuildTransforms(model, log);
        if (!transformsResult.IsSuccess)
            return transformsResult.ToNewResult<CreatePipelineClientRequest>();

        if (string.IsNullOrWhiteSpace(model.PipelineType))
        {
            return GenericResult<CreatePipelineClientRequest>.Failure(
                PipelineBuilderProviderLog.PipelineTypeUnavailable(log, pipelineName));
        }

        return GenericResult<CreatePipelineClientRequest>.Success(new CreatePipelineClientRequest
        {
            Name = pipelineName,
            PipelineType = model.PipelineType,
            SourceConnectionName = sourceConnectionResult.Value!,
            SourceDataSet = sourceDataSetResult.Value,
            DestinationConnectionName = sinkConnectionResult.Value!,
            DestinationDataSet = sinkDataSetResult.Value,
            Description = pipelineDescription,
            Transforms = transformsResult.Value!,
        });
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static IGenericResult<ICanvasNode> FindDataSetNode(PipelineCanvasModel model, string role, ILogger log)
    {
        var node = model.Nodes
            .Where(n => string.Equals(n.NodeType.Name, "DataSet", StringComparison.Ordinal))
            .FirstOrDefault(n => n.Metadata.TryGetValue(PipelineCanvasMetadataKeys.DataSetRole, out var r)
                                  && string.Equals(r, role, StringComparison.Ordinal));

        if (node is not null)
            return GenericResult<ICanvasNode>.Success(node);

        return string.Equals(role, PipelineCanvasMetadataKeys.RoleSource, StringComparison.Ordinal)
            ? GenericResult<ICanvasNode>.Failure(PipelineCanvasLog.NoSourceDataSetNode(log))
            : GenericResult<ICanvasNode>.Failure(PipelineCanvasLog.NoSinkDataSetNode(log));
    }

    private static IGenericResult<string> RequireDataSetMetadata(ICanvasNode node, string role, string metadataKey, ILogger log)
    {
        if (node.Metadata.TryGetValue(metadataKey, out var value) && !string.IsNullOrWhiteSpace(value))
            return GenericResult<string>.Success(value);

        return GenericResult<string>.Failure(PipelineCanvasLog.RequiredDataSetMetadataMissing(log, role, metadataKey));
    }

    private static IGenericResult<IList<PipelineTransformClientRequest>> BuildTransforms(PipelineCanvasModel model, ILogger log)
    {
        var transformNodes = model.Nodes
            .Where(n => string.Equals(n.NodeType.Name, "Transform", StringComparison.Ordinal))
            .ToList();

        var ordered = new List<(ICanvasNode Node, int ExecutionOrder)>();
        foreach (var node in transformNodes)
        {
            if (!node.Metadata.TryGetValue(PipelineCanvasMetadataKeys.ExecutionOrder, out var orderText)
                || !int.TryParse(orderText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var executionOrder))
            {
                return GenericResult<IList<PipelineTransformClientRequest>>.Failure(
                    PipelineCanvasLog.TransformExecutionOrderInvalid(log, node.Id));
            }

            ordered.Add((node, executionOrder));
        }

        var transforms = new List<PipelineTransformClientRequest>();
        foreach (var (node, executionOrder) in ordered.OrderBy(n => n.ExecutionOrder))
        {
            if (!node.Metadata.TryGetValue(PipelineCanvasMetadataKeys.OperationType, out var operationType)
                || string.IsNullOrWhiteSpace(operationType))
            {
                return GenericResult<IList<PipelineTransformClientRequest>>.Failure(
                    PipelineCanvasLog.RequiredTransformOperationTypeMissing(log, node.Id));
            }

            var transform = new PipelineTransformClientRequest
            {
                Name = node.Label,
                OperationType = operationType,
                ExecutionOrder = executionOrder,
            };

            if (node.Metadata.TryGetValue(PipelineCanvasMetadataKeys.ConfigPayload, out var payload) && !string.IsNullOrWhiteSpace(payload))
            {
                var payloadResult = ApplyConfigPayload(transform, operationType, payload, node.Id, log);
                if (!payloadResult.IsSuccess)
                    return payloadResult.ToNewResult<IList<PipelineTransformClientRequest>>();
            }

            transforms.Add(transform);
        }

        return GenericResult<IList<PipelineTransformClientRequest>>.Success(transforms);
    }

    private static IGenericResult ApplyConfigPayload(
        PipelineTransformClientRequest transform,
        string operationType,
        string payload,
        string nodeId,
        ILogger log)
    {
        var stateResult = TransformConfigPayloadSerializer.FromConfigPayload(operationType, payload, log);
        if (!stateResult.IsSuccess)
            return stateResult;

        var state = stateResult.Value!;
        switch (operationType)
        {
            case "Map":
                transform.FieldMappings = state.Mappings.ToList();
                return GenericResult.Success();

            case "Filter":
                transform.FilterExpression = state.FilterExpression;
                return GenericResult.Success();

            case "Aggregate":
                transform.Aggregation = state.Aggregation;
                return GenericResult.Success();

            case "Lookup":
                transform.Lookup = state.Lookup;
                return GenericResult.Success();

            case "Calculate":
                transform.Calculation = state.Calculation;
                return GenericResult.Success();

            default:
                return GenericResult.Failure(PipelineCanvasLog.TransformOperationTypeUnrecognized(log, nodeId, operationType));
        }
    }
}
