using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Search.Translators.SearchTextTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class SearchTextTranslatorLog
{
    /// <summary>Trace: full-text search starting.</summary>
    [MessageLogging(EventId = 11155, Level = LogLevel.Trace,
        Message = "SearchTextTranslator searching for pattern '{pattern}' (isRegex={isRegex}, caseSensitive={caseSensitive}, maxResults={maxResults})")]
    public static partial IGenericMessage Searching(ILogger logger, string pattern, bool isRegex, bool caseSensitive, int maxResults);

    /// <summary>Error: Pattern was not supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.PatternRequired</c> (21009).</remarks>
    [MessageLogging(EventId = 21009, Level = LogLevel.Error,
        Message = "SearchTextTranslator: Pattern is required")]
    public static partial IGenericMessage PatternRequired(ILogger logger);

    /// <summary>Error: the supplied regex pattern failed to compile.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.InvalidRegexPattern</c> (20001).</remarks>
    [MessageLogging(EventId = 20001, Level = LogLevel.Error,
        Message = "SearchTextTranslator: invalid regex pattern '{pattern}' — {errorMessage}")]
    public static partial IGenericMessage InvalidRegexPattern(ILogger logger, string pattern, string errorMessage);

    /// <summary>Information: the search completed.</summary>
    [MessageLogging(EventId = 11156, Level = LogLevel.Information,
        Message = "SearchTextTranslator found {matchCount} match(es) in {fileCount} file(s)")]
    public static partial IGenericMessage Found(ILogger logger, int matchCount, int fileCount);
}
