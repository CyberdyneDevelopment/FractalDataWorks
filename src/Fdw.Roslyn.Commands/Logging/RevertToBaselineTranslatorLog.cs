using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Workspace.Translators.RevertToBaselineTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class RevertToBaselineTranslatorLog
{
    /// <summary>Trace: revert-to-baseline starting.</summary>
    [MessageLogging(EventId = 11177, Level = LogLevel.Trace,
        Message = "RevertToBaselineTranslator reverting workspace to baseline")]
    public static partial IGenericMessage Reverting(ILogger logger);

    /// <summary>Error: no baseline has been set.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoBaselineSet</c> (40000).</remarks>
    [MessageLogging(EventId = 40000, Level = LogLevel.Error,
        Message = "RevertToBaselineTranslator: no baseline has been set")]
    public static partial IGenericMessage NoBaselineSet(ILogger logger);

    /// <summary>Information: the revert completed.</summary>
    [MessageLogging(EventId = 11178, Level = LogLevel.Information,
        Message = "RevertToBaselineTranslator reverted to baseline with {projectCount} project(s)")]
    public static partial IGenericMessage Reverted(ILogger logger, int projectCount);
}
