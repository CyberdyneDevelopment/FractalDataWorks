using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// MessageLogging for the versions of a glossary term held at places (FDW-2331).
/// EventId ranges: 12740-12742 (done), 22741 (a version whose text is not acceptable, 400), 32740 and 32742 (not found, 404).
/// </summary>
[MessageLoggingTypeCode("CATALOG")]
public static partial class GlossaryTermVersionLog
{
    /// <summary>Logs that the versions of a term were listed.</summary>
    [MessageLogging(EventId = 12740, Level = LogLevel.Debug, Message = "Glossary term '{term}' has {count} versions at or above {place}")]
    public static partial IGenericMessage Listed(ILogger logger, string term, int count, string place);

    /// <summary>Logs that a version of a term was saved at a place.</summary>
    [MessageLogging(EventId = 12741, Level = LogLevel.Information, Message = "A version of glossary term '{term}' was saved at {place}")]
    public static partial IGenericMessage Saved(ILogger logger, string term, string place);

    /// <summary>Logs that a version of a term was deleted from a place.</summary>
    [MessageLogging(EventId = 12742, Level = LogLevel.Information, Message = "The version of glossary term '{term}' at {place} was deleted")]
    public static partial IGenericMessage Deleted(ILogger logger, string term, string place);

    /// <summary>Logs a version whose text is not acceptable.</summary>
    [MessageLogging(EventId = 22741, Level = LogLevel.Warning, Message = "The version of glossary term '{term}' was refused: {reason}")]
    public static partial IGenericMessage VersionInvalid(ILogger logger, string term, string reason);

    /// <summary>Logs a term that does not exist.</summary>
    [MessageLogging(EventId = 32740, Level = LogLevel.Warning, Message = "There is no glossary term '{term}'")]
    public static partial IGenericMessage TermNotFound(ILogger logger, string term);

    /// <summary>Logs a version that does not exist.</summary>
    [MessageLogging(EventId = 32742, Level = LogLevel.Warning, Message = "Glossary term '{term}' has no version at {place}")]
    public static partial IGenericMessage VersionNotFound(ILogger logger, string term, string place);
}
