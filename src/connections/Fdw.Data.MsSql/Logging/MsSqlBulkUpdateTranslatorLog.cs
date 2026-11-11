using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.MsSql.Logging;

/// <summary>
/// Message logging for the MsSql bulk update translator (FDW-4953).
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlBulkUpdateTranslatorLog
{
    /// <summary>Traces a translation starting.</summary>
    [MessageLogging(EventId = 12082, Level = LogLevel.Trace,
        Message = "[MsSqlBulkUpdateTranslator] translating a bulk update of {rowCount} rows for container '{container}'")]
    public static partial IGenericMessage Translating(ILogger logger, string container, int rowCount);

    /// <summary>Records a translation that produced one set-based statement.</summary>
    [MessageLogging(EventId = 12083, Level = LogLevel.Information,
        Message = "[MsSqlBulkUpdateTranslator] built one set-based update of {rowCount} rows for container '{container}' ({keyCount} key columns, {setCount} set columns, {collapsed} rows collapsed onto a later row with the same key)")]
    public static partial IGenericMessage Translated(ILogger logger, string container, int rowCount, int keyCount, int setCount, int collapsed);

    /// <summary>Records a bulk update the translator refuses.</summary>
    [MessageLogging(EventId = 21014, Level = LogLevel.Error,
        Message = "[MsSqlBulkUpdateTranslator] the bulk update for container '{container}' is refused: {reason}")]
    public static partial IGenericMessage Refused(ILogger logger, string container, string reason);

    /// <summary>Records an unanticipated translation error.</summary>
    [MessageLogging(EventId = 91012, Level = LogLevel.Error,
        Message = "[MsSqlBulkUpdateTranslator] failed to translate the bulk update for container '{container}': {errorMessage}")]
    public static partial IGenericMessage TranslationFailed(ILogger logger, System.Exception exception, string container, string errorMessage);
}
