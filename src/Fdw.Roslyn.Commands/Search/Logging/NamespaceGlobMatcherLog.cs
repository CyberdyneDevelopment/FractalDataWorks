using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Search.Logging;

/// <summary>
/// MessageLogging methods for NamespaceGlobMatcher.
/// EventId range: 9120-9129.
/// </summary>
[MessageLoggingTypeCode("SEARCH")]
public static partial class NamespaceGlobMatcherLog
{
    /// <summary>Trace: created accept-all matcher (no pattern supplied).</summary>
    [MessageLogging(EventId = 11062, Level = LogLevel.Trace,
        Message = "Created accept-all NamespaceGlobMatcher (null/empty pattern)")]
    public static partial IGenericMessage CreateAcceptAll(ILogger logger);

    /// <summary>Trace: compiled a regex from a glob pattern.</summary>
    [MessageLogging(EventId = 11063, Level = LogLevel.Trace,
        Message = "Compiled NamespaceGlobMatcher pattern='{pattern}' regex='{regex}'")]
    public static partial IGenericMessage CompiledRegex(ILogger logger, string pattern, string regex);

    /// <summary>Trace: an IsMatch call result.</summary>
    [MessageLogging(EventId = 11064, Level = LogLevel.Trace,
        Message = "IsMatch ns='{ns}' result={matched}")]
    public static partial IGenericMessage IsMatchResult(ILogger logger, string ns, bool matched);
}
