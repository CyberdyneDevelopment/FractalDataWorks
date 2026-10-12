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

    /// <summary>Traces the field values a save writes beyond the record's mapped properties.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace,
        Message = "[ConfigurationSaveCommand] Save of '{entityType}' also writes fields [{fields}]")]
    public static partial IGenericMessage FieldValuesIncluded(ILogger logger, string entityType, string fields);
}
