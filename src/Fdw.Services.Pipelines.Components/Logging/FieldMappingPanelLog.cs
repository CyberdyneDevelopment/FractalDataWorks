using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.UI.Components.Blazor.Logging;

/// <summary>
/// MessageLogging methods for FieldMappingPanel component operations.
/// EventId range: 7010-7019
/// </summary>
[MessageLoggingTypeCode("COMPONENTS12")]
public static partial class FieldMappingPanelLog
{
    /// <summary>Logs when auto-mapping fields by name.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Information,
        Message = "FieldMappingPanel: Auto-mapped {mappedCount} of {sourceCount} source fields")]
    public static partial IGenericMessage AutoMapCompleted(
        ILogger logger,
        int mappedCount,
        int sourceCount);

    /// <summary>Logs when a mapping is added.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "FieldMappingPanel: Added mapping '{sourceField}' -> '{targetField}'")]
    public static partial IGenericMessage MappingAdded(
        ILogger logger,
        string sourceField,
        string targetField);

    /// <summary>Logs when a mapping is removed.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "FieldMappingPanel: Removed mapping at index {index}")]
    public static partial IGenericMessage MappingRemoved(
        ILogger logger,
        int index);
}
