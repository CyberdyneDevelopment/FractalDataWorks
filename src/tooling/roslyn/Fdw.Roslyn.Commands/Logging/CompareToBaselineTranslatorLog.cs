using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Workspace.Translators.CompareToBaselineTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class CompareToBaselineTranslatorLog
{
    /// <summary>Trace: baseline comparison starting.</summary>
    [MessageLogging(EventId = 11161, Level = LogLevel.Trace,
        Message = "CompareToBaselineTranslator comparing current workspace to baseline")]
    public static partial IGenericMessage Comparing(ILogger logger);

    /// <summary>Debug: no baseline has been set, so the comparison cannot run.</summary>
    [MessageLogging(EventId = 11162, Level = LogLevel.Debug,
        Message = "CompareToBaselineTranslator: no baseline set, cannot compare")]
    public static partial IGenericMessage NoBaseline(ILogger logger);

    /// <summary>Information: comparison completed.</summary>
    [MessageLogging(EventId = 11163, Level = LogLevel.Information,
        Message = "CompareToBaselineTranslator found {changeCount} change(s) from baseline")]
    public static partial IGenericMessage Compared(ILogger logger, int changeCount);
}
