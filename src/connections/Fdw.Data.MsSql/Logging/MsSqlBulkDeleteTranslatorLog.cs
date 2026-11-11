using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.MsSql.Logging;

/// <summary>
/// Message logging for the MsSql bulk delete translator (FDW-4954).
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlBulkDeleteTranslatorLog
{
    /// <summary>Traces a translation starting.</summary>
    [MessageLogging(EventId = 12084, Level = LogLevel.Trace,
        Message = "[MsSqlBulkDeleteTranslator] translating a bulk delete of {rowCount} keys for container '{container}'")]
    public static partial IGenericMessage Translating(ILogger logger, string container, int rowCount);

    /// <summary>Records a translation that produced one set-based statement.</summary>
    [MessageLogging(EventId = 12085, Level = LogLevel.Information,
        Message = "[MsSqlBulkDeleteTranslator] built one set-based {kind} of {rowCount} keys for container '{container}' ({keyCount} key columns)")]
    public static partial IGenericMessage Translated(ILogger logger, string kind, string container, int rowCount, int keyCount);

    /// <summary>Records a bulk delete the translator refuses.</summary>
    [MessageLogging(EventId = 21015, Level = LogLevel.Error,
        Message = "[MsSqlBulkDeleteTranslator] the bulk delete for container '{container}' is refused: {reason}")]
    public static partial IGenericMessage Refused(ILogger logger, string container, string reason);

    /// <summary>Records an unanticipated translation error.</summary>
    [MessageLogging(EventId = 91013, Level = LogLevel.Error,
        Message = "[MsSqlBulkDeleteTranslator] failed to translate the bulk delete for container '{container}': {errorMessage}")]
    public static partial IGenericMessage TranslationFailed(ILogger logger, System.Exception exception, string container, string errorMessage);
}
