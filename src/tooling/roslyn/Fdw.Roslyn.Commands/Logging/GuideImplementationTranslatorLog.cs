using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Conventions.Translators.GuideImplementationTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GuideImplementationTranslatorLog
{
    /// <summary>Trace: guide resolution starting.</summary>
    [MessageLogging(EventId = 10001, Level = LogLevel.Trace,
        Message = "GuideImplementationTranslator resolving guides from '{guidesPath}'")]
    public static partial IGenericMessage Resolving(ILogger logger, string guidesPath);

    /// <summary>Information: guide resolution completed.</summary>
    [MessageLogging(EventId = 10002, Level = LogLevel.Information,
        Message = "GuideImplementationTranslator found {count} guide(s) for {target}")]
    public static partial IGenericMessage Resolved(ILogger logger, int count, string target);

    /// <summary>Error: GuidesPath was not supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.GuidesPathRequired</c> (21024).</remarks>
    [MessageLogging(EventId = 21024, Level = LogLevel.Error,
        Message = "GuideImplementationTranslator: GuidesPath is required")]
    public static partial IGenericMessage GuidesPathRequired(ILogger logger);

    /// <summary>Error: neither TypeName nor ProjectName was supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.EitherTypeNameOrProjectNameRequired</c> (21025).</remarks>
    [MessageLogging(EventId = 21025, Level = LogLevel.Error,
        Message = "GuideImplementationTranslator: either TypeName or ProjectName is required")]
    public static partial IGenericMessage EitherTypeNameOrProjectNameRequired(ILogger logger);

    /// <summary>Error: no guide applies to the target.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoImplementationGuide</c> (31037).</remarks>
    [MessageLogging(EventId = 31037, Level = LogLevel.Error,
        Message = "GuideImplementationTranslator: no implementation guide applies to {target}")]
    public static partial IGenericMessage NoImplementationGuide(ILogger logger, string target);
}
