using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Refactoring.Translators.RemoveGlobalUsingsTranslator"/>.
/// </summary>
/// <remarks>
/// Every failure branch of the translator funnels through its own private <c>Fail(string code, ...)</c>
/// helper, keyed by the <c>RoslynResultCodes</c> name rather than a compile-time enum. <see cref="Failed"/>
/// is the single log call added at that helper so every refusal is visible without restructuring the
/// translator's existing per-branch calls into distinct typed methods.
/// </remarks>
[MessageLoggingTypeCode("RCMD")]
public static partial class RemoveGlobalUsingsTranslatorLog
{
    /// <summary>Trace: global-using removal starting.</summary>
    [MessageLogging(EventId = 11136, Level = LogLevel.Trace,
        Message = "RemoveGlobalUsingsTranslator removing global usings from project '{projectName}' (dryRun={dryRun})")]
    public static partial IGenericMessage Removing(ILogger logger, string projectName, bool dryRun);

    /// <summary>Error: the translator refused, naming the RoslynResultCodes code it refused with.</summary>
    [MessageLogging(EventId = 21101, Level = LogLevel.Error,
        Message = "RemoveGlobalUsingsTranslator refused: {code}")]
    public static partial IGenericMessage Failed(ILogger logger, string code);

    /// <summary>Information: global-using removal completed.</summary>
    [MessageLogging(EventId = 11137, Level = LogLevel.Information,
        Message = "RemoveGlobalUsingsTranslator removed {removedCount} global using(s) from '{projectName}'; {repairedCount} file(s) repaired")]
    public static partial IGenericMessage Removed(ILogger logger, string projectName, int removedCount, int repairedCount);
}
