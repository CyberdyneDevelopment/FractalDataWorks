using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Search.Translators.FindUnusedTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class FindUnusedTranslatorLog
{
    /// <summary>Trace: unused-member scan starting.</summary>
    [MessageLogging(EventId = 11149, Level = LogLevel.Trace,
        Message = "FindUnusedTranslator scanning the solution (includePrivate={includePrivate}, includeInternal={includeInternal}, maxResults={maxResults})")]
    public static partial IGenericMessage Scanning(ILogger logger, bool includePrivate, bool includeInternal, int maxResults);

    /// <summary>Warning: a per-symbol reference check failed; the symbol is conservatively treated as used.</summary>
    [MessageLogging(EventId = 81100, Level = LogLevel.Warning,
        Message = "FindUnusedTranslator: reference check for '{symbolName}' failed ({exceptionType}); treated as used")]
    public static partial IGenericMessage ReferenceCheckFailed(ILogger logger, string symbolName, string exceptionType);

    /// <summary>Information: the scan completed.</summary>
    [MessageLogging(EventId = 11150, Level = LogLevel.Information,
        Message = "FindUnusedTranslator found {count} unused member(s)")]
    public static partial IGenericMessage Found(ILogger logger, int count);
}
