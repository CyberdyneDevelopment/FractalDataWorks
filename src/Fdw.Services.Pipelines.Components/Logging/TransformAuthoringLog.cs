using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Components.Logging;

/// <summary>
/// MessageLogging for transform ConfigPayload authoring — <c>TransformConfigPayloadSerializer</c>'s
/// To/FromConfigPayload, and the <c>PipelineCanvasEditContext</c> authoring methods
/// (<c>PopulateTransformPorts</c>, <c>SetFilterExpression</c>, <c>SetLookup</c>) that write through it.
/// EventId range: 4572-4590, 4594-4595
/// </summary>
public static partial class TransformAuthoringLog
{
    // ── ToConfigPayload (serialize) ───────────────────────────────────────────

    /// <summary>Logs that a transform node's ConfigPayload is being serialized.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="operationType">The transform's OperationType.</param>
    /// <param name="nodeId">The identifier of the transform node.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4572, Level = LogLevel.Trace, Message = "Serializing {operationType} ConfigPayload for transform node '{nodeId}'")]
    public static partial IGenericMessage SerializingConfigPayload(ILogger logger, string operationType, string nodeId);

    /// <summary>Logs that a transform node's ConfigPayload was serialized.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="operationType">The transform's OperationType.</param>
    /// <param name="nodeId">The identifier of the transform node.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4573, Level = LogLevel.Information, Message = "Serialized {operationType} ConfigPayload for transform node '{nodeId}'")]
    public static partial IGenericMessage ConfigPayloadSerialized(ILogger logger, string operationType, string nodeId);

    /// <summary>Logs that a field-mapping edge's source/destination port id could not be resolved to a field name.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="edgeId">The identifier of the field-mapping edge.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4574, Level = LogLevel.Warning, Message = "Field mapping edge '{edgeId}' has an unresolvable source/destination port id — ConfigPayload cannot be built")]
    public static partial IGenericMessage PortFieldUnresolvable(ILogger logger, string edgeId);

    /// <summary>Logs that a Filter transform's filter expression is empty.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="context">The transform node id (serialize) or operation type (deserialize) this failure occurred for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4575, Level = LogLevel.Warning, Message = "Filter expression is empty for '{context}' — ConfigPayload cannot be built/parsed")]
    public static partial IGenericMessage FilterExpressionEmpty(ILogger logger, string context);

    /// <summary>Logs that an Aggregate transform has no aggregation configuration.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="context">The transform node id (serialize) or operation type (deserialize) this failure occurred for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4576, Level = LogLevel.Warning, Message = "Aggregation configuration is missing for '{context}' — ConfigPayload cannot be built/parsed")]
    public static partial IGenericMessage AggregationMissing(ILogger logger, string context);

    /// <summary>Logs that an aggregation item is missing its SourceField/Function/OutputField.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="context">The transform node id (serialize) or operation type (deserialize) this failure occurred for.</param>
    /// <param name="index">The zero-based index of the incomplete aggregation item.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4577, Level = LogLevel.Warning, Message = "Aggregation item {index} for '{context}' is missing SourceField/Function/OutputField — ConfigPayload cannot be built/parsed")]
    public static partial IGenericMessage AggregationItemIncomplete(ILogger logger, string context, int index);

    /// <summary>Logs that a Calculate transform has no calculation configuration.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="context">The transform node id (serialize) or operation type (deserialize) this failure occurred for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4578, Level = LogLevel.Warning, Message = "Calculation configuration is missing for '{context}' — ConfigPayload cannot be built/parsed")]
    public static partial IGenericMessage CalculationMissing(ILogger logger, string context);

    /// <summary>Logs that a computed column is missing its OutputField or Formula.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="context">The transform node id (serialize) or operation type (deserialize) this failure occurred for.</param>
    /// <param name="index">The zero-based index of the incomplete computed column.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4579, Level = LogLevel.Warning, Message = "Computed column {index} for '{context}' is missing OutputField or Formula — ConfigPayload cannot be built/parsed")]
    public static partial IGenericMessage ComputedColumnIncomplete(ILogger logger, string context, int index);

    /// <summary>Logs that a Lookup transform has no lookup configuration.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="context">The transform node id (serialize) or operation type (deserialize) this failure occurred for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4580, Level = LogLevel.Warning, Message = "Lookup configuration is missing for '{context}' — ConfigPayload cannot be built/parsed")]
    public static partial IGenericMessage LookupMissing(ILogger logger, string context);

    /// <summary>Logs that a required Lookup field is missing.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="context">The transform node id (serialize) or operation type (deserialize) this failure occurred for.</param>
    /// <param name="fieldName">The name of the required Lookup field that was missing.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4581, Level = LogLevel.Warning, Message = "Lookup field '{fieldName}' is required but missing for '{context}' — ConfigPayload cannot be built/parsed")]
    public static partial IGenericMessage LookupFieldMissing(ILogger logger, string context, string fieldName);

    /// <summary>Logs that a transform node's OperationType has no known ConfigPayload shape to serialize.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="nodeId">The identifier of the transform node.</param>
    /// <param name="operationType">The unrecognised OperationType.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4582, Level = LogLevel.Warning, Message = "Transform node '{nodeId}' has unrecognised operation type '{operationType}' — ConfigPayload cannot be built")]
    public static partial IGenericMessage ConfigPayloadOperationTypeUnrecognized(ILogger logger, string nodeId, string operationType);

    // ── FromConfigPayload (deserialize) ───────────────────────────────────────

    /// <summary>Logs that a ConfigPayload is being deserialized.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="operationType">The operation type the payload is being parsed for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4583, Level = LogLevel.Trace, Message = "Deserializing {operationType} ConfigPayload")]
    public static partial IGenericMessage DeserializingConfigPayload(ILogger logger, string operationType);

    /// <summary>Logs that a ConfigPayload was deserialized.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="operationType">The operation type the payload was parsed for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4584, Level = LogLevel.Information, Message = "Deserialized {operationType} ConfigPayload")]
    public static partial IGenericMessage ConfigPayloadDeserialized(ILogger logger, string operationType);

    /// <summary>Logs that a ConfigPayload's JSON could not be parsed.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="exception">The exception thrown while parsing the payload.</param>
    /// <param name="operationType">The operation type the payload was being parsed for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4585, Level = LogLevel.Error, Message = "ConfigPayload for operation type '{operationType}' could not be parsed")]
    public static partial IGenericMessage ConfigPayloadUnparseable(ILogger logger, Exception exception, string operationType);

    /// <summary>Logs that a ConfigPayload's OperationType has no known shape to parse into.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="operationType">The unrecognised operation type.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4586, Level = LogLevel.Warning, Message = "ConfigPayload operation type '{operationType}' is not recognised — cannot parse")]
    public static partial IGenericMessage ConfigPayloadOperationTypeUnrecognizedOnRead(ILogger logger, string operationType);

    // ── PipelineCanvasEditContext authoring methods ───────────────────────────

    /// <summary>Logs that PopulateTransformPorts was called with no input or output fields.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="nodeId">The identifier of the transform node.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4587, Level = LogLevel.Warning, Message = "Cannot populate ports for transform node '{nodeId}': both inputFields and outputFields are empty")]
    public static partial IGenericMessage TransformPortsNoFields(ILogger logger, string nodeId);

    /// <summary>Logs that SetFilterExpression was called on a node whose OperationType is not Filter.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="nodeId">The identifier of the node.</param>
    /// <param name="operationType">The node's actual OperationType.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4588, Level = LogLevel.Warning, Message = "Cannot set filter expression on node '{nodeId}': its OperationType is '{operationType}', not 'Filter'")]
    public static partial IGenericMessage SetFilterExpressionWrongOperationType(ILogger logger, string nodeId, string operationType);

    /// <summary>Logs that SetLookup was called on a node whose OperationType is not Lookup.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="nodeId">The identifier of the node.</param>
    /// <param name="operationType">The node's actual OperationType.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4589, Level = LogLevel.Warning, Message = "Cannot set lookup configuration on node '{nodeId}': its OperationType is '{operationType}', not 'Lookup'")]
    public static partial IGenericMessage SetLookupWrongOperationType(ILogger logger, string nodeId, string operationType);

    /// <summary>Logs that SetFilterExpression/SetLookup was called on a node with no OperationType metadata set.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="nodeId">The identifier of the node.</param>
    /// <param name="expectedOperationType">The operation type the caller expected the node to carry.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4590, Level = LogLevel.Warning, Message = "Cannot set {expectedOperationType} configuration on node '{nodeId}': it has no OperationType metadata set")]
    public static partial IGenericMessage OperationTypeMetadataMissing(ILogger logger, string nodeId, string expectedOperationType);

    // ── Field-mapping edge metadata corruption (ToMapPayload) ─────────────────

    /// <summary>
    /// Logs that a field-mapping edge's IsRequired/IsEnabled metadata is present but not a valid
    /// boolean. Absence is a legitimate "not yet overridden" state (see
    /// <c>PipelineCanvasEdgeMetadataKeys</c>); a present-but-unparseable value is corrupt data and
    /// must never be silently coerced to the opposite of what the caller wrote.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="edgeId">The identifier of the field-mapping edge.</param>
    /// <param name="metadataKey">The metadata key whose value could not be parsed.</param>
    /// <param name="value">The unparseable metadata value.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4594, Level = LogLevel.Error, Message = "Field mapping edge '{edgeId}' has an unparseable '{metadataKey}' value '{value}' — ConfigPayload cannot be built")]
    public static partial IGenericMessage MappingBooleanMetadataUnparseable(ILogger logger, string edgeId, string metadataKey, string value);

    /// <summary>
    /// Logs that a field-mapping edge's MappingName metadata is present but blank. Absence is a
    /// legitimate "not yet named" state (a deterministic name is derived from the source/destination
    /// fields); a present-but-blank value is an explicit invalid override.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="edgeId">The identifier of the field-mapping edge.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4595, Level = LogLevel.Error, Message = "Field mapping edge '{edgeId}' has a blank MappingName override — ConfigPayload cannot be built")]
    public static partial IGenericMessage MappingNameBlank(ILogger logger, string edgeId);

    /// <summary>Logs that a Map transform's ConfigPayload carried no field-mapping collection.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="context">The transform node id (serialize) or operation type (deserialize) this failure occurred for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 4596, Level = LogLevel.Warning, Message = "Field mappings are missing for '{context}' — ConfigPayload cannot be built/parsed")]
    public static partial IGenericMessage MappingsMissing(ILogger logger, string context);
}
