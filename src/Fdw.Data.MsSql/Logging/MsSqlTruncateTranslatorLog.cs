using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.MsSql.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Data.MsSql.MsSqlTruncateTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlTruncateTranslatorLog
{
    [MessageLogging(
        EventId = 12060,
        Level = LogLevel.Trace,
        Message = "MsSqlTruncateTranslator translating TruncateCommand for container '{container}'")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 12061,
        Level = LogLevel.Information,
        Message = "MsSqlTruncateTranslator built unconditional DELETE for container '{container}'")]
    public static partial IGenericMessage Translated(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Error,
        Message = "MsSqlTruncateTranslator received a null container")]
    public static partial IGenericMessage ContainerNull(ILogger logger);

    [MessageLogging(
        EventId = 20001,
        Level = LogLevel.Error,
        Message = "MsSqlTruncateTranslator: container '{container}' path is not a database path")]
    public static partial IGenericMessage InvalidContainerPath(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 91003,
        Level = LogLevel.Error,
        Message = "MsSqlTruncateTranslator failed to translate truncate for container '{container}': {errorMessage}")]
    public static partial IGenericMessage TruncateTranslationFailed(
        ILogger logger,
        System.Exception exception,
        string container,
        string errorMessage);
}
