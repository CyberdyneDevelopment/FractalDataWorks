using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Workspace.Translators.RestoreSnapshotTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class RestoreSnapshotTranslatorLog
{
    /// <summary>Trace: snapshot restore starting.</summary>
    [MessageLogging(EventId = 11175, Level = LogLevel.Trace,
        Message = "RestoreSnapshotTranslator restoring snapshot '{snapshotId}'")]
    public static partial IGenericMessage Restoring(ILogger logger, string snapshotId);

    /// <summary>Error: SnapshotId was not supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.SnapshotIdRequired</c> (21012).</remarks>
    [MessageLogging(EventId = 21012, Level = LogLevel.Error,
        Message = "RestoreSnapshotTranslator: SnapshotId is required")]
    public static partial IGenericMessage SnapshotIdRequired(ILogger logger);

    /// <summary>Error: the named snapshot does not exist.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.SnapshotNotFound</c> (31017).</remarks>
    [MessageLogging(EventId = 31017, Level = LogLevel.Error,
        Message = "RestoreSnapshotTranslator: snapshot '{snapshotId}' not found")]
    public static partial IGenericMessage SnapshotNotFound(ILogger logger, string snapshotId);

    /// <summary>Information: the snapshot was restored.</summary>
    [MessageLogging(EventId = 11176, Level = LogLevel.Information,
        Message = "RestoreSnapshotTranslator restored snapshot '{snapshotId}' ({projectCount} project(s), {documentCount} document(s))")]
    public static partial IGenericMessage Restored(ILogger logger, string snapshotId, int projectCount, int documentCount);
}
