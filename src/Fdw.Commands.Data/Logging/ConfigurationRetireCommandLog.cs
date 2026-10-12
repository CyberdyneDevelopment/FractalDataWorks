using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for <see cref="ConfigurationRetireCommand"/> construction.
/// </summary>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class ConfigurationRetireCommandLog
{
    /// <summary>Traces a retire that matches rows on field values.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "[ConfigurationRetireCommand] Created to retire the rows matching fields [{fields}] (markDeleted={markDeleted})")]
    public static partial IGenericMessage FieldMatchCreated(ILogger logger, string fields, bool markDeleted);

    /// <summary>Traces a retire that matches a row on one declared key.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "[ConfigurationRetireCommand] Created to retire the row matching its '{keyType}' key ({valueCount} value(s), markDeleted={markDeleted})")]
    public static partial IGenericMessage KeyMatchCreated(ILogger logger, string keyType, int valueCount, bool markDeleted);
}
