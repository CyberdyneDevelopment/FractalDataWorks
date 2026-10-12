using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Search.Translators.SearchSymbolsTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class SearchSymbolsTranslatorLog
{
    /// <summary>Trace: symbol search starting.</summary>
    [MessageLogging(EventId = 11153, Level = LogLevel.Trace,
        Message = "SearchSymbolsTranslator searching for pattern '{pattern}' (maxResults={maxResults})")]
    public static partial IGenericMessage Searching(ILogger logger, string pattern, int maxResults);

    /// <summary>Error: Pattern was not supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.PatternRequired</c> (21009).</remarks>
    [MessageLogging(EventId = 21009, Level = LogLevel.Error,
        Message = "SearchSymbolsTranslator: Pattern is required")]
    public static partial IGenericMessage PatternRequired(ILogger logger);

    /// <summary>Information: the search completed.</summary>
    [MessageLogging(EventId = 11154, Level = LogLevel.Information,
        Message = "SearchSymbolsTranslator found {count} symbol(s) matching '{pattern}'")]
    public static partial IGenericMessage Found(ILogger logger, string pattern, int count);
}
