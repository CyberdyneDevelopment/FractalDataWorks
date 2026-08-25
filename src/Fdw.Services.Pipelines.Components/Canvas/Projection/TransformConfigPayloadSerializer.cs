using System;
using System.Collections.Generic;
using System.Text.Json;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Components.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Components.Canvas.Projection;

/// <summary>
/// Serialises/deserialises a pipeline transform node's <see cref="PipelineCanvasMetadataKeys.ConfigPayload"/>
/// metadata to and from the exact JSON shapes <see cref="PipelineCreateRequestProjection"/>'s
/// <c>ApplyConfigPayload</c> reads.
/// </summary>
/// <remarks>
/// <para>
/// This is the write-side counterpart of that read-only projection — closes the historical
/// reader-only asymmetry where nothing on the canvas ever wrote a Map/Filter/Aggregate/Calculate/
/// Lookup ConfigPayload, only <c>PipelineCreateRequestProjection</c> read one.
/// </para>
/// <para>
/// Uses the identical <see cref="JsonSerializerOptions"/> (<c>PropertyNameCaseInsensitive = true</c>)
/// as the reader so payloads round-trip byte-for-byte through both directions.
/// </para>
/// <para>
/// Every required piece is validated before a value is produced — no fallback/defaulted values are
/// ever substituted for a missing piece; the first missing/invalid piece fails the whole call.
/// </para>
/// </remarks>
public static class TransformConfigPayloadSerializer
{
    private static readonly JsonSerializerOptions PayloadOptions = new() { PropertyNameCaseInsensitive = true };

    private const string InPortPrefix = "in:";
    private const string OutPortPrefix = "out:";

    // ── ToConfigPayload ───────────────────────────────────────────────────────

    /// <summary>
    /// Serialises a transform node's per-operation configuration to its ConfigPayload JSON string.
    /// </summary>
    /// <param name="operationType">The transform's OperationType (Map, Filter, Aggregate, Calculate, or Lookup).</param>
    /// <param name="transformNode">The transform node the payload is for (used for its id in log messages).</param>
    /// <param name="fieldMappingEdges">
    /// The FieldMapping edges representing this transform's mappings (used for <c>Map</c>). Ignored
    /// for other operation types — pass <c>[]</c> when not applicable.
    /// </param>
    /// <param name="aggregation">The aggregation parameters (used for <c>Aggregate</c>).</param>
    /// <param name="calculation">The calculation parameters (used for <c>Calculate</c>).</param>
    /// <param name="filterExpression">The filter expression (used for <c>Filter</c>).</param>
    /// <param name="lookup">The lookup parameters (used for <c>Lookup</c>).</param>
    /// <param name="logger">Optional logger; defaults to <see cref="NullLogger"/> when null.</param>
    /// <returns>A result containing the serialised ConfigPayload JSON on success, or the first validation failure.</returns>
    public static IGenericResult<string> ToConfigPayload(
        string operationType,
        PipelineCanvasNode transformNode,
        IReadOnlyList<PipelineCanvasEdge> fieldMappingEdges,
        AggregationClientRequest? aggregation = null,
        CalculationClientRequest? calculation = null,
        string? filterExpression = null,
        LookupClientRequest? lookup = null,
        ILogger? logger = null)
    {
        var log = logger ?? NullLogger.Instance;
        TransformAuthoringLog.SerializingConfigPayload(log, operationType, transformNode.Id);

        switch (operationType)
        {
            case "Map":
                return ToMapPayload(transformNode, fieldMappingEdges, log);

            case "Filter":
                return ToFilterPayload(transformNode.Id, filterExpression, log);

            case "Aggregate":
                return ToAggregatePayload(transformNode.Id, aggregation, log);

            case "Calculate":
                return ToCalculatePayload(transformNode.Id, calculation, log);

            case "Lookup":
                return ToLookupPayload(transformNode.Id, lookup, log);

            default:
                return GenericResult<string>.Failure(
                    TransformAuthoringLog.ConfigPayloadOperationTypeUnrecognized(log, transformNode.Id, operationType));
        }
    }

    // ── FromConfigPayload ─────────────────────────────────────────────────────

    /// <summary>
    /// Deserialises a ConfigPayload JSON string into a <see cref="TransformAuthoringState"/>.
    /// </summary>
    /// <param name="operationType">The operation type the payload was serialised for.</param>
    /// <param name="payload">The ConfigPayload JSON string.</param>
    /// <param name="logger">Optional logger; defaults to <see cref="NullLogger"/> when null.</param>
    /// <returns>A result containing the parsed authoring state on success, or the first validation failure.</returns>
    public static IGenericResult<TransformAuthoringState> FromConfigPayload(
        string operationType,
        string payload,
        ILogger? logger = null)
    {
        var log = logger ?? NullLogger.Instance;
        TransformAuthoringLog.DeserializingConfigPayload(log, operationType);

        try
        {
            switch (operationType)
            {
                case "Map":
                    return FromMapPayload(payload, log);

                case "Filter":
                    return FromFilterPayload(payload, log);

                case "Aggregate":
                    return FromAggregatePayload(payload, log);

                case "Calculate":
                    return FromCalculatePayload(payload, log);

                case "Lookup":
                    return FromLookupPayload(payload, log);

                default:
                    return GenericResult<TransformAuthoringState>.Failure(
                        TransformAuthoringLog.ConfigPayloadOperationTypeUnrecognizedOnRead(log, operationType));
            }
        }
        catch (JsonException ex)
        {
            return GenericResult<TransformAuthoringState>.Failure(
                TransformAuthoringLog.ConfigPayloadUnparseable(log, ex, operationType));
        }
    }

    // ── Private helpers: Map ──────────────────────────────────────────────────

    private static IGenericResult<string> ToMapPayload(
        PipelineCanvasNode transformNode,
        IReadOnlyList<PipelineCanvasEdge> fieldMappingEdges,
        ILogger log)
    {
        var mappings = new List<PipelineFieldMappingClientRequest>();

        foreach (var edge in fieldMappingEdges)
        {
            var fieldNamesResult = ResolveMappingFieldNames(edge, log);
            if (!fieldNamesResult.IsSuccess)
                return fieldNamesResult.ToNewResult<string>();

            var (sourceField, destinationField) = fieldNamesResult.Value;

            var isRequiredResult = ResolveMappingIsRequired(edge, log);
            if (!isRequiredResult.IsSuccess)
                return isRequiredResult.ToNewResult<string>();

            var isEnabledResult = ResolveMappingIsEnabled(edge, log);
            if (!isEnabledResult.IsSuccess)
                return isEnabledResult.ToNewResult<string>();

            var mappingNameResult = ResolveMappingName(edge, sourceField, destinationField, log);
            if (!mappingNameResult.IsSuccess)
                return mappingNameResult.ToNewResult<string>();

            var optionalMetadataResult = ResolveOptionalMappingMetadata(edge);
            if (!optionalMetadataResult.IsSuccess)
                return optionalMetadataResult.ToNewResult<string>();

            var optionalMetadata = optionalMetadataResult.Value;

            mappings.Add(new PipelineFieldMappingClientRequest
            {
                Name = mappingNameResult.Value!,
                SourceField = sourceField,
                DestinationField = destinationField,
                TargetType = optionalMetadata.TargetType,
                TransformExpression = optionalMetadata.TransformExpression,
                IsRequired = isRequiredResult.Value,
                DefaultValue = optionalMetadata.DefaultValue,
                IsEnabled = isEnabledResult.Value,
            });
        }

        var json = JsonSerializer.Serialize(mappings, PayloadOptions);
        TransformAuthoringLog.ConfigPayloadSerialized(log, "Map", transformNode.Id);
        return GenericResult<string>.Success(json);
    }

    // Why: extracted from ToMapPayload (FDW007 — cyclomatic complexity 17 vs threshold 15). Each
    // helper resolves exactly one piece of per-field-mapping metadata and fails loud on its own
    // corrupt-data case, so the loop in ToMapPayload reads as a flat sequence of named resolution
    // steps instead of one long branchy block. Behaviour (including every fail-loud path and its
    // MessageLogging call) is unchanged — only the shape moved.
    private static IGenericResult<(string SourceField, string DestinationField)> ResolveMappingFieldNames(
        PipelineCanvasEdge edge, ILogger log)
    {
        if (edge.SourcePortId is null || !edge.SourcePortId.StartsWith(InPortPrefix, StringComparison.Ordinal)
            || edge.TargetPortId is null || !edge.TargetPortId.StartsWith(OutPortPrefix, StringComparison.Ordinal))
        {
            return GenericResult<(string, string)>.Failure(TransformAuthoringLog.PortFieldUnresolvable(log, edge.Id));
        }

        var sourceField = edge.SourcePortId.Substring(InPortPrefix.Length);
        var destinationField = edge.TargetPortId.Substring(OutPortPrefix.Length);

        if (string.IsNullOrWhiteSpace(sourceField) || string.IsNullOrWhiteSpace(destinationField))
            return GenericResult<(string, string)>.Failure(TransformAuthoringLog.PortFieldUnresolvable(log, edge.Id));

        return GenericResult<(string, string)>.Success((sourceField, destinationField));
    }

    // Why: absent IsRequired metadata legitimately means "not yet overridden" — the edge starts with
    // no per-mapping overrides (see PipelineCanvasEdge's own remarks), so fall back to the DTO's own
    // declared default (false) ONLY when the key is genuinely absent. A key that IS present but fails
    // to parse as a bool is corrupt data, not an unset override — fail loud rather than silently
    // coercing it to the opposite of whatever the caller actually wrote (the "a disabled mapping
    // persists as enabled" bug).
    private static IGenericResult<bool> ResolveMappingIsRequired(PipelineCanvasEdge edge, ILogger log)
    {
        if (!edge.Metadata.TryGetValue(PipelineCanvasEdgeMetadataKeys.IsRequired, out var isRequiredText))
            return GenericResult<bool>.Success(false);

        if (!bool.TryParse(isRequiredText, out var isRequired))
        {
            return GenericResult<bool>.Failure(TransformAuthoringLog.MappingBooleanMetadataUnparseable(
                log, edge.Id, PipelineCanvasEdgeMetadataKeys.IsRequired, isRequiredText));
        }

        return GenericResult<bool>.Success(isRequired);
    }

    // Why: same reasoning as ResolveMappingIsRequired, but IsEnabled's declared default is true (a
    // mapping is enabled unless explicitly disabled).
    private static IGenericResult<bool> ResolveMappingIsEnabled(PipelineCanvasEdge edge, ILogger log)
    {
        if (!edge.Metadata.TryGetValue(PipelineCanvasEdgeMetadataKeys.IsEnabled, out var isEnabledText))
            return GenericResult<bool>.Success(true);

        if (!bool.TryParse(isEnabledText, out var isEnabled))
        {
            return GenericResult<bool>.Failure(TransformAuthoringLog.MappingBooleanMetadataUnparseable(
                log, edge.Id, PipelineCanvasEdgeMetadataKeys.IsEnabled, isEnabledText));
        }

        return GenericResult<bool>.Success(isEnabled);
    }

    // Why: Name is a display/identity label, not a domain identifier — when the key is genuinely
    // absent (e.g. right after a two-click port connect, before the Inspector panel is used), derive
    // it deterministically from the already-validated source/destination field names. But a key that
    // IS present and blank is an explicit invalid override, not an unset one — fail loud instead of
    // silently overwriting it with the generated name.
    private static IGenericResult<string> ResolveMappingName(
        PipelineCanvasEdge edge, string sourceField, string destinationField, ILogger log)
    {
        var hasMappingName = edge.Metadata.TryGetValue(PipelineCanvasEdgeMetadataKeys.MappingName, out var mappingName);
        if (hasMappingName && string.IsNullOrWhiteSpace(mappingName))
            return GenericResult<string>.Failure(TransformAuthoringLog.MappingNameBlank(log, edge.Id));

        return GenericResult<string>.Success(hasMappingName ? mappingName! : $"{sourceField}->{destinationField}");
    }

    // Why: TargetType/TransformExpression/DefaultValue are always optional per-mapping overrides —
    // a genuinely absent key is never a failure, only blank-as-present is normalized to null. Returns
    // IGenericResult<T> for parity with the sibling resolvers above (and so a future validation rule
    // on these fields has somewhere to fail loud); there is no failing path today.
    private static IGenericResult<(string? TargetType, string? TransformExpression, string? DefaultValue)> ResolveOptionalMappingMetadata(
        PipelineCanvasEdge edge)
    {
        edge.Metadata.TryGetValue(PipelineCanvasEdgeMetadataKeys.TargetType, out var targetType);
        edge.Metadata.TryGetValue(PipelineCanvasEdgeMetadataKeys.TransformExpression, out var transformExpression);
        edge.Metadata.TryGetValue(PipelineCanvasEdgeMetadataKeys.DefaultValue, out var defaultValue);

        return GenericResult<(string?, string?, string?)>.Success((
            string.IsNullOrWhiteSpace(targetType) ? null : targetType,
            string.IsNullOrWhiteSpace(transformExpression) ? null : transformExpression,
            defaultValue));
    }

    private static IGenericResult<TransformAuthoringState> FromMapPayload(string payload, ILogger log)
    {
        var mappings = JsonSerializer.Deserialize<IReadOnlyList<PipelineFieldMappingClientRequest>>(payload, PayloadOptions);
        if (mappings is null)
            return GenericResult<TransformAuthoringState>.Failure(TransformAuthoringLog.MappingsMissing(log, "Map"));

        TransformAuthoringLog.ConfigPayloadDeserialized(log, "Map");
        return GenericResult<TransformAuthoringState>.Success(new TransformAuthoringState { Mappings = mappings });
    }

    // ── Private helpers: Filter ───────────────────────────────────────────────

    private static IGenericResult<string> ToFilterPayload(string nodeId, string? filterExpression, ILogger log)
    {
        if (string.IsNullOrWhiteSpace(filterExpression))
            return GenericResult<string>.Failure(TransformAuthoringLog.FilterExpressionEmpty(log, nodeId));

        var json = JsonSerializer.Serialize(filterExpression, PayloadOptions);
        TransformAuthoringLog.ConfigPayloadSerialized(log, "Filter", nodeId);
        return GenericResult<string>.Success(json);
    }

    private static IGenericResult<TransformAuthoringState> FromFilterPayload(string payload, ILogger log)
    {
        var filterExpression = JsonSerializer.Deserialize<string>(payload, PayloadOptions);
        if (string.IsNullOrWhiteSpace(filterExpression))
            return GenericResult<TransformAuthoringState>.Failure(TransformAuthoringLog.FilterExpressionEmpty(log, "Filter"));

        TransformAuthoringLog.ConfigPayloadDeserialized(log, "Filter");
        return GenericResult<TransformAuthoringState>.Success(new TransformAuthoringState { FilterExpression = filterExpression });
    }

    // ── Private helpers: Aggregate ────────────────────────────────────────────

    private static IGenericResult<string> ToAggregatePayload(string nodeId, AggregationClientRequest? aggregation, ILogger log)
    {
        var validation = ValidateAggregation(nodeId, aggregation, log);
        if (!validation.IsSuccess)
            return validation.ToNewResult<string>();

        var json = JsonSerializer.Serialize(aggregation, PayloadOptions);
        TransformAuthoringLog.ConfigPayloadSerialized(log, "Aggregate", nodeId);
        return GenericResult<string>.Success(json);
    }

    private static IGenericResult<TransformAuthoringState> FromAggregatePayload(string payload, ILogger log)
    {
        var aggregation = JsonSerializer.Deserialize<AggregationClientRequest>(payload, PayloadOptions);
        var validation = ValidateAggregation("Aggregate", aggregation, log);
        if (!validation.IsSuccess)
            return validation.ToNewResult<TransformAuthoringState>();

        TransformAuthoringLog.ConfigPayloadDeserialized(log, "Aggregate");
        return GenericResult<TransformAuthoringState>.Success(new TransformAuthoringState { Aggregation = aggregation });
    }

    private static IGenericResult ValidateAggregation(string context, AggregationClientRequest? aggregation, ILogger log)
    {
        if (aggregation is null)
            return GenericResult.Failure(TransformAuthoringLog.AggregationMissing(log, context));

        for (var i = 0; i < aggregation.Aggregations.Count; i++)
        {
            var item = aggregation.Aggregations[i];
            if (string.IsNullOrWhiteSpace(item.SourceField) || string.IsNullOrWhiteSpace(item.Function) || string.IsNullOrWhiteSpace(item.OutputField))
                return GenericResult.Failure(TransformAuthoringLog.AggregationItemIncomplete(log, context, i));
        }

        return GenericResult.Success();
    }

    // ── Private helpers: Calculate ────────────────────────────────────────────

    private static IGenericResult<string> ToCalculatePayload(string nodeId, CalculationClientRequest? calculation, ILogger log)
    {
        var validation = ValidateCalculation(nodeId, calculation, log);
        if (!validation.IsSuccess)
            return validation.ToNewResult<string>();

        var json = JsonSerializer.Serialize(calculation, PayloadOptions);
        TransformAuthoringLog.ConfigPayloadSerialized(log, "Calculate", nodeId);
        return GenericResult<string>.Success(json);
    }

    private static IGenericResult<TransformAuthoringState> FromCalculatePayload(string payload, ILogger log)
    {
        var calculation = JsonSerializer.Deserialize<CalculationClientRequest>(payload, PayloadOptions);
        var validation = ValidateCalculation("Calculate", calculation, log);
        if (!validation.IsSuccess)
            return validation.ToNewResult<TransformAuthoringState>();

        TransformAuthoringLog.ConfigPayloadDeserialized(log, "Calculate");
        return GenericResult<TransformAuthoringState>.Success(new TransformAuthoringState { Calculation = calculation });
    }

    private static IGenericResult ValidateCalculation(string context, CalculationClientRequest? calculation, ILogger log)
    {
        if (calculation is null)
            return GenericResult.Failure(TransformAuthoringLog.CalculationMissing(log, context));

        for (var i = 0; i < calculation.ComputedColumns.Count; i++)
        {
            var column = calculation.ComputedColumns[i];
            if (string.IsNullOrWhiteSpace(column.OutputField) || string.IsNullOrWhiteSpace(column.Formula))
                return GenericResult.Failure(TransformAuthoringLog.ComputedColumnIncomplete(log, context, i));
        }

        return GenericResult.Success();
    }

    // ── Private helpers: Lookup ───────────────────────────────────────────────

    private static IGenericResult<string> ToLookupPayload(string nodeId, LookupClientRequest? lookup, ILogger log)
    {
        var validation = ValidateLookup(nodeId, lookup, log);
        if (!validation.IsSuccess)
            return validation.ToNewResult<string>();

        var json = JsonSerializer.Serialize(lookup, PayloadOptions);
        TransformAuthoringLog.ConfigPayloadSerialized(log, "Lookup", nodeId);
        return GenericResult<string>.Success(json);
    }

    private static IGenericResult<TransformAuthoringState> FromLookupPayload(string payload, ILogger log)
    {
        var lookup = JsonSerializer.Deserialize<LookupClientRequest>(payload, PayloadOptions);
        var validation = ValidateLookup("Lookup", lookup, log);
        if (!validation.IsSuccess)
            return validation.ToNewResult<TransformAuthoringState>();

        TransformAuthoringLog.ConfigPayloadDeserialized(log, "Lookup");
        return GenericResult<TransformAuthoringState>.Success(new TransformAuthoringState { Lookup = lookup });
    }

    private static IGenericResult ValidateLookup(string context, LookupClientRequest? lookup, ILogger log)
    {
        if (lookup is null)
            return GenericResult.Failure(TransformAuthoringLog.LookupMissing(log, context));

        if (string.IsNullOrWhiteSpace(lookup.LookupConnectionName))
            return GenericResult.Failure(TransformAuthoringLog.LookupFieldMissing(log, context, nameof(LookupClientRequest.LookupConnectionName)));

        if (string.IsNullOrWhiteSpace(lookup.LookupDataSet))
            return GenericResult.Failure(TransformAuthoringLog.LookupFieldMissing(log, context, nameof(LookupClientRequest.LookupDataSet)));

        if (string.IsNullOrWhiteSpace(lookup.LookupKeyField))
            return GenericResult.Failure(TransformAuthoringLog.LookupFieldMissing(log, context, nameof(LookupClientRequest.LookupKeyField)));

        if (string.IsNullOrWhiteSpace(lookup.SourceKeyField))
            return GenericResult.Failure(TransformAuthoringLog.LookupFieldMissing(log, context, nameof(LookupClientRequest.SourceKeyField)));

        if (string.IsNullOrWhiteSpace(lookup.JoinType))
            return GenericResult.Failure(TransformAuthoringLog.LookupFieldMissing(log, context, nameof(LookupClientRequest.JoinType)));

        return GenericResult.Success();
    }
}
