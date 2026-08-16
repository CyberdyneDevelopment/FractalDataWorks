using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for <see cref="ConfigurationSaveCommand{T}"/> construction.
/// </summary>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class ConfigurationSaveCommandLog
{
    /// <summary>Traces a configuration save command being constructed.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace,
        Message = "[ConfigurationSaveCommand] Created for entity type '{entityType}'")]
    public static partial IGenericMessage CommandCreated(ILogger logger, string entityType);

    /// <summary>Logs when a save carries extra column=value pairs beyond the POCO's mapped columns.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Debug,
        Message = "[ConfigurationSaveCommand] Save for entity type '{entityType}' merges {columnCount} additional column(s)")]
    public static partial IGenericMessage AdditionalColumnsIncluded(ILogger logger, string entityType, int columnCount);
}
