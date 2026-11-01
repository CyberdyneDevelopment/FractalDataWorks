using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Conventions.Guides.ImplementationGuideLoader"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class ImplementationGuideLoaderLog
{
    /// <summary>Trace: the guide document is being read.</summary>
    [MessageLogging(EventId = 10001, Level = LogLevel.Trace,
        Message = "ImplementationGuideLoader reading the guide document under '{guidesPath}'")]
    public static partial IGenericMessage Reading(ILogger logger, string guidesPath);

    /// <summary>Information: the guide document was read.</summary>
    [MessageLogging(EventId = 10002, Level = LogLevel.Information,
        Message = "ImplementationGuideLoader read {count} guide(s) verified against {repo} {commit}")]
    public static partial IGenericMessage Read(ILogger logger, int count, string repo, string commit);

    /// <summary>Error: the guide document is not under the guides path.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.GuideDocumentNotFound</c> (31035).</remarks>
    [MessageLogging(EventId = 31035, Level = LogLevel.Error,
        Message = "ImplementationGuideLoader: guide document not found under '{guidesPath}'")]
    public static partial IGenericMessage GuideDocumentNotFound(ILogger logger, string guidesPath);

    /// <summary>Error: the guide document is not valid guide JSON.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.GuideDocumentInvalid</c> (90003).</remarks>
    [MessageLogging(EventId = 90003, Level = LogLevel.Error,
        Message = "ImplementationGuideLoader: guide document under '{guidesPath}' could not be parsed: {reason}")]
    public static partial IGenericMessage GuideDocumentInvalid(ILogger logger, string guidesPath, string reason);
}
