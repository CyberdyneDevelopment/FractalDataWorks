using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Workspace.Translators.SetBaselineTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class SetBaselineTranslatorLog
{
    /// <summary>Trace: baseline set starting.</summary>
    [MessageLogging(EventId = 11179, Level = LogLevel.Trace,
        Message = "SetBaselineTranslator setting baseline from the current workspace")]
    public static partial IGenericMessage Setting(ILogger logger);

    /// <summary>Information: the baseline was set.</summary>
    [MessageLogging(EventId = 11180, Level = LogLevel.Information,
        Message = "SetBaselineTranslator set baseline with {projectCount} project(s), {documentCount} document(s)")]
    public static partial IGenericMessage Set(ILogger logger, int projectCount, int documentCount);
}
