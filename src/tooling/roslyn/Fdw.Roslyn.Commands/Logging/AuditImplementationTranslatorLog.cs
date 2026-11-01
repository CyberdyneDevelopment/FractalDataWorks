using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Conventions.Translators.AuditImplementationTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class AuditImplementationTranslatorLog
{
    /// <summary>Trace: the audit is starting.</summary>
    [MessageLogging(EventId = 10001, Level = LogLevel.Trace,
        Message = "AuditImplementationTranslator auditing project '{projectName}' against the guides under '{guidesPath}'")]
    public static partial IGenericMessage Auditing(ILogger logger, string projectName, string guidesPath);

    /// <summary>Information: the audit completed.</summary>
    [MessageLogging(EventId = 10002, Level = LogLevel.Information,
        Message = "AuditImplementationTranslator found {findings} finding(s) across {guides} guide(s) for project '{projectName}'")]
    public static partial IGenericMessage Audited(ILogger logger, int findings, int guides, string projectName);

    /// <summary>Error: GuidesPath was not supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.GuidesPathRequired</c> (21024).</remarks>
    [MessageLogging(EventId = 21024, Level = LogLevel.Error,
        Message = "AuditImplementationTranslator: GuidesPath is required")]
    public static partial IGenericMessage GuidesPathRequired(ILogger logger);

    /// <summary>Error: ProjectName was not supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ProjectNameRequired</c> (21026).</remarks>
    [MessageLogging(EventId = 21026, Level = LogLevel.Error,
        Message = "AuditImplementationTranslator: ProjectName is required")]
    public static partial IGenericMessage ProjectNameRequired(ILogger logger);

    /// <summary>Error: no guide applies to the project.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoImplementationGuide</c> (31037).</remarks>
    [MessageLogging(EventId = 31037, Level = LogLevel.Error,
        Message = "AuditImplementationTranslator: no implementation guide applies to project '{projectName}'")]
    public static partial IGenericMessage NoImplementationGuide(ILogger logger, string projectName);
}
