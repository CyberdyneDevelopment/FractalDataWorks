using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Analysis.Translators.FindNamespaceMismatchesTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class FindNamespaceMismatchesTranslatorLog
{
    /// <summary>Trace: the scan is starting.</summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Trace,
        Message = "FindNamespaceMismatchesTranslator scanning (scope='{scope}', includeTests={includeTests})")]
    public static partial IGenericMessage Scanning(ILogger logger, string scope, bool includeTests);

    /// <summary>Error: the command argument was null.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.CommandCannotBeNull</c> (21000).</remarks>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error,
        Message = "FindNamespaceMismatchesTranslator: command was null")]
    public static partial IGenericMessage CommandCannotBeNull(ILogger logger);

    /// <summary>Error: no namespace mismatches survived the kind filter.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NamespaceMismatchesNotFound</c> (31019).</remarks>
    [MessageLogging(EventId = 31019, Level = LogLevel.Error,
        Message = "FindNamespaceMismatchesTranslator: no namespace mismatches found (scope='{scope}')")]
    public static partial IGenericMessage NamespaceMismatchesNotFound(ILogger logger, string scope);

    /// <summary>Information: the scan completed with mismatches reported.</summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Information,
        Message = "FindNamespaceMismatchesTranslator scanned {typesScanned} type(s), found {mismatchCount} mismatch(es) in {groupCount} group(s)")]
    public static partial IGenericMessage Completed(ILogger logger, int typesScanned, int mismatchCount, int groupCount);
}
