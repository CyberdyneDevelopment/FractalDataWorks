using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Workspace.Translators.CreateSnapshotTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class CreateSnapshotTranslatorLog
{
    /// <summary>Trace: snapshot creation starting.</summary>
    [MessageLogging(EventId = 11164, Level = LogLevel.Trace,
        Message = "CreateSnapshotTranslator creating snapshot '{snapshotName}'")]
    public static partial IGenericMessage Creating(ILogger logger, string snapshotName);

    /// <summary>Error: SnapshotName was not supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.SnapshotNameRequired</c> (21013).</remarks>
    [MessageLogging(EventId = 21013, Level = LogLevel.Error,
        Message = "CreateSnapshotTranslator: SnapshotName is required")]
    public static partial IGenericMessage SnapshotNameRequired(ILogger logger);

    /// <summary>Information: the snapshot was created.</summary>
    [MessageLogging(EventId = 11165, Level = LogLevel.Information,
        Message = "CreateSnapshotTranslator created snapshot '{snapshotName}' ({projectCount} project(s), {documentCount} document(s))")]
    public static partial IGenericMessage Created(ILogger logger, string snapshotName, int projectCount, int documentCount);
}
