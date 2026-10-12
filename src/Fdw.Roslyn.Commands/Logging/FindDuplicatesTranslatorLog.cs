using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Search.Translators.FindDuplicatesTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class FindDuplicatesTranslatorLog
{
    /// <summary>Trace: duplicate-code scan starting.</summary>
    [MessageLogging(EventId = 11145, Level = LogLevel.Trace,
        Message = "FindDuplicatesTranslator scanning the solution (minLines={minLines}, minTokens={minTokens})")]
    public static partial IGenericMessage Scanning(ILogger logger, int minLines, int minTokens);

    /// <summary>Information: the scan completed.</summary>
    [MessageLogging(EventId = 11146, Level = LogLevel.Information,
        Message = "FindDuplicatesTranslator found {duplicateGroupCount} duplicate code block group(s)")]
    public static partial IGenericMessage Found(ILogger logger, int duplicateGroupCount);
}
