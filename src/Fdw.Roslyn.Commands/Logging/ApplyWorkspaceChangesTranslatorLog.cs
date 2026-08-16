using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Workspace.Translators.ApplyWorkspaceChangesTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class ApplyWorkspaceChangesTranslatorLog
{
    /// <summary>Trace: the translator returned its placeholder; the real apply happens in the command handler.</summary>
    [MessageLogging(EventId = 11157, Level = LogLevel.Trace,
        Message = "ApplyWorkspaceChangesTranslator returning placeholder — actual apply is performed by the command handler")]
    public static partial IGenericMessage Applying(ILogger logger);
}
