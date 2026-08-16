using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Workspace.Translators.GetBaselineTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GetBaselineTranslatorLog
{
    /// <summary>Trace: baseline retrieval starting.</summary>
    [MessageLogging(EventId = 11166, Level = LogLevel.Trace,
        Message = "GetBaselineTranslator retrieving baseline")]
    public static partial IGenericMessage Getting(ILogger logger);

    /// <summary>Debug: no baseline has been set.</summary>
    [MessageLogging(EventId = 11167, Level = LogLevel.Debug,
        Message = "GetBaselineTranslator: no baseline has been set")]
    public static partial IGenericMessage NoBaseline(ILogger logger);

    /// <summary>Information: baseline retrieval completed.</summary>
    [MessageLogging(EventId = 11168, Level = LogLevel.Information,
        Message = "GetBaselineTranslator retrieved baseline: {projectCount} project(s), {documentCount} document(s)")]
    public static partial IGenericMessage Retrieved(ILogger logger, int projectCount, int documentCount);
}
