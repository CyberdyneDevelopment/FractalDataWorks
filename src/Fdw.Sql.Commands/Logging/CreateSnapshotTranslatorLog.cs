using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Sql.Commands.Logging;

/// <summary>Message logging for <see cref="Fdw.Sql.Commands.Workspace.Translators.CreateSnapshotTranslator"/>.</summary>
[MessageLoggingTypeCode("SQL")]
public static partial class CreateSnapshotTranslatorLog
{
    /// <summary>Logs translator entry.</summary>
    [MessageLogging(
        EventId = 11047,
        Level = LogLevel.Trace,
        Message = "CreateSnapshotTranslator translating CreateSnapshotCommand for '{snapshotName}'")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string snapshotName);

    /// <summary>Logs that the command's SnapshotName was missing.</summary>
    [MessageLogging(
        EventId = 21004,
        Level = LogLevel.Error,
        Message = "CreateSnapshotTranslator: SnapshotName is required")]
    public static partial IGenericMessage SnapshotNameRequired(
        ILogger logger);

    /// <summary>Logs a successfully captured snapshot placeholder.</summary>
    [MessageLogging(
        EventId = 13013,
        Level = LogLevel.Information,
        Message = "CreateSnapshotTranslator created snapshot '{snapshotName}' with placeholder id '{placeholderId}'")]
    public static partial IGenericMessage SnapshotCreated(
        ILogger logger,
        string snapshotName,
        string placeholderId);
}
