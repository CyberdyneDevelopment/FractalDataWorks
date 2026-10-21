using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// MessageLogging for the acronyms the glossary keeps (FDW-1682).
/// EventId ranges: 22730-22734 (a request that states an acronym wrongly, 400), 42730 (the name is already taken, 409).
/// </summary>
[MessageLoggingTypeCode("CATALOG")]
public static partial class AcronymLog
{
    /// <summary>Logs a term whose "stands for" is empty.</summary>
    [MessageLogging(EventId = 22730, Level = LogLevel.Warning, Message = "Glossary term '{term}' states what it stands for, but the text is empty")]
    public static partial IGenericMessage StandsForEmpty(ILogger logger, string term);

    /// <summary>Logs a term whose "stands for" is longer than the column holds.</summary>
    [MessageLogging(EventId = 22731, Level = LogLevel.Warning, Message = "Glossary term '{term}' states what it stands for in {length} characters; at most {maximum} are kept")]
    public static partial IGenericMessage StandsForTooLong(ILogger logger, string term, int length, int maximum);

    /// <summary>Logs an acronym that does not say whether it is approved.</summary>
    [MessageLogging(EventId = 22732, Level = LogLevel.Warning, Message = "Glossary term '{term}' states what it stands for, so it is an acronym and must also state isApproved")]
    public static partial IGenericMessage ApprovalNotStated(ILogger logger, string term);

    /// <summary>Logs a term that states isApproved but is not an acronym.</summary>
    [MessageLogging(EventId = 22733, Level = LogLevel.Warning, Message = "Glossary term '{term}' states isApproved but not what it stands for; only an acronym has an approved state")]
    public static partial IGenericMessage ApprovalWithoutStandsFor(ILogger logger, string term);

    /// <summary>Logs a list filter that is not true or false.</summary>
    [MessageLogging(EventId = 22734, Level = LogLevel.Warning, Message = "The glossary filter '{filter}' is '{value}'; it is true or false")]
    public static partial IGenericMessage FilterNotBoolean(ILogger logger, string filter, string value);

    /// <summary>Logs a glossary term whose name another term already has, ignoring case.</summary>
    [MessageLogging(EventId = 42730, Level = LogLevel.Warning, Message = "A glossary term named '{term}' already exists; names are compared ignoring case")]
    public static partial IGenericMessage NameTaken(ILogger logger, string term);
}
