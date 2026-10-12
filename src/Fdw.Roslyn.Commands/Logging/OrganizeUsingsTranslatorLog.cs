using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Formatting.Translators.OrganizeUsingsTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class OrganizeUsingsTranslatorLog
{
    /// <summary>Trace: using-directive organization starting.</summary>
    [MessageLogging(EventId = 11063, Level = LogLevel.Trace,
        Message = "OrganizeUsingsTranslator organizing usings in '{filePath}' (systemFirst={systemFirst})")]
    public static partial IGenericMessage Organizing(ILogger logger, string filePath, bool systemFirst);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "OrganizeUsingsTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "OrganizeUsingsTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the document's syntax root could not be retrieved.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToGetSyntaxRoot</c> (91006).</remarks>
    [MessageLogging(EventId = 91006, Level = LogLevel.Error,
        Message = "OrganizeUsingsTranslator: failed to get syntax root for '{filePath}'")]
    public static partial IGenericMessage FailedToGetSyntaxRoot(ILogger logger, string filePath);

    /// <summary>Debug: there were no using directives to organize.</summary>
    [MessageLogging(EventId = 11064, Level = LogLevel.Debug,
        Message = "OrganizeUsingsTranslator: '{filePath}' has no using directives to organize")]
    public static partial IGenericMessage NoUsingsToOrganize(ILogger logger, string filePath);

    /// <summary>Information: organization completed.</summary>
    [MessageLogging(EventId = 11065, Level = LogLevel.Information,
        Message = "OrganizeUsingsTranslator organized {usingCount} using directive(s) in '{filePath}'")]
    public static partial IGenericMessage Organized(ILogger logger, string filePath, int usingCount);
}
