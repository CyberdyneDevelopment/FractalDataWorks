using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Sql.Commands.Logging;

/// <summary>Message logging for <see cref="Fdw.Sql.Commands.Workspace.Translators.RestoreSnapshotTranslator"/>.</summary>
[MessageLoggingTypeCode("SQL")]
public static partial class RestoreSnapshotTranslatorLog
{
    /// <summary>Logs translator entry.</summary>
    [MessageLogging(
        EventId = 11050,
        Level = LogLevel.Trace,
        Message = "RestoreSnapshotTranslator translating RestoreSnapshotCommand for snapshot '{snapshotId}'")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string snapshotId);

    /// <summary>Logs that the command's SnapshotId was missing.</summary>
    [MessageLogging(
        EventId = 21005,
        Level = LogLevel.Error,
        Message = "RestoreSnapshotTranslator: SnapshotId is required")]
    public static partial IGenericMessage SnapshotIdRequired(
        ILogger logger);

    /// <summary>Logs that the workspace failed to restore the requested snapshot.</summary>
    [MessageLogging(
        EventId = 70001,
        Level = LogLevel.Error,
        Message = "RestoreSnapshotTranslator: restore of snapshot '{snapshotId}' failed: {reason}")]
    public static partial IGenericMessage RestoreFailed(
        ILogger logger,
        string snapshotId,
        string reason);

    /// <summary>Logs a successful snapshot restore.</summary>
    [MessageLogging(
        EventId = 13012,
        Level = LogLevel.Information,
        Message = "RestoreSnapshotTranslator restored snapshot '{snapshotId}'")]
    public static partial IGenericMessage SnapshotRestored(
        ILogger logger,
        string snapshotId);
}
