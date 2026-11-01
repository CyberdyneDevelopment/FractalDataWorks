using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Sql.Commands.Logging;

/// <summary>Message logging for <see cref="Fdw.Sql.Commands.Project.Translators.ListObjectsTranslator"/>.</summary>
[MessageLoggingTypeCode("SQL")]
public static partial class ListObjectsTranslatorLog
{
    /// <summary>Logs translator entry.</summary>
    [MessageLogging(
        EventId = 11029,
        Level = LogLevel.Trace,
        Message = "ListObjectsTranslator translating ListObjectsCommand (kind='{kind}', schema='{schema}')")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string kind,
        string schema);

    /// <summary>Logs completion with the number of objects returned.</summary>
    [MessageLogging(
        EventId = 13001,
        Level = LogLevel.Information,
        Message = "ListObjectsTranslator found {count} object(s)")]
    public static partial IGenericMessage ObjectsFound(
        ILogger logger,
        int count);
}
