using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.PostgreSql.Logging;

/// <summary>
/// Message logging for PostgreSQL data command translators.
/// </summary>
[MessageLoggingTypeCode("PGSQL")]
public static partial class PostgreSqlTranslatorLog
{
    /// <summary>
    /// Logs when a translator receives an invalid command type.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Translator '{translatorName}' expected {expectedType} but received {actualType}",
        TypeCode = new[] { 'P', 'G', 'S' })]
    public static partial IGenericMessage InvalidCommandType(
        ILogger logger,
        string translatorName,
        string expectedType,
        string actualType);
}
