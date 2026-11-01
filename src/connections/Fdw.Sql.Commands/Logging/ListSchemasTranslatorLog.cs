using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Sql.Commands.Logging;

/// <summary>Message logging for <see cref="Fdw.Sql.Commands.Project.Translators.ListSchemasTranslator"/>.</summary>
[MessageLoggingTypeCode("SQL")]
public static partial class ListSchemasTranslatorLog
{
    /// <summary>Logs translator entry.</summary>
    [MessageLogging(
        EventId = 11030,
        Level = LogLevel.Trace,
        Message = "ListSchemasTranslator translating ListSchemasCommand")]
    public static partial IGenericMessage Translating(
        ILogger logger);

    /// <summary>Logs completion with the number of schemas returned.</summary>
    [MessageLogging(
        EventId = 13003,
        Level = LogLevel.Information,
        Message = "ListSchemasTranslator found {count} schema(s)")]
    public static partial IGenericMessage SchemasFound(
        ILogger logger,
        int count);
}
