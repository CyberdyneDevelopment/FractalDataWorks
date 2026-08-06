using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.UI.Components.Logging;

/// <summary>
/// MessageLogging for CalculatedDesigner node properties panel operations.
/// EventId range: 9723-9726
/// </summary>
/// <remarks>
/// Why: the 4190-4199 block specified in the task spec is occupied by
/// VisualizationProviderLog and CalculationCacheLog in Data.Components.
/// 9723-9726 is the next free block after DataSetDetailProviderLog (9700-9722)
/// per EVENTID-ALLOCATION.md.
/// </remarks>
[MessageLoggingTypeCode("UICOMPONENTS")]
public static partial class DesignerNodePropertiesPanelLog
{
    /// <summary>Logs a node configuration validation failure.</summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "Node configuration validation failed for node type '{nodeType}': {reason}")]
    public static partial IGenericMessage NodeConfigurationValidationFailed(
        ILogger logger,
        string nodeType,
        string reason);

    /// <summary>Logs that a required field in a node's configuration is incomplete.</summary>
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "Node configuration incomplete for node type '{nodeType}' — required field '{field}' is missing")]
    public static partial IGenericMessage NodeFieldsIncomplete(
        ILogger logger,
        string nodeType,
        string field);

    /// <summary>Logs that a field referenced in a Join node was not found in the named source.</summary>
    [MessageLogging(
        EventId = 31001,
        Level = LogLevel.Error,
        Message = "Join field '{fieldName}' not found in source type '{sourceType}'")]
    public static partial IGenericMessage JoinFieldNotFound(
        ILogger logger,
        string sourceType,
        string fieldName);

    /// <summary>Logs that an Aggregate node's specification is invalid.</summary>
    [MessageLogging(
        EventId = 21002,
        Level = LogLevel.Error,
        Message = "Aggregate specification is invalid: {reason}")]
    public static partial IGenericMessage AggregateSpecInvalid(
        ILogger logger,
        string reason);
}
