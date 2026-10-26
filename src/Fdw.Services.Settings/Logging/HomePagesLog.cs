using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Logging;

/// <summary>
/// MessageLogging for the home pages (FDW-2325).
/// EventId ranges: 12902-12904 (done), 22902 (invalid input, 400), 42901 (conflict, 409).
/// </summary>
[MessageLoggingTypeCode("SETTINGS")]
public static partial class HomePagesLog
{
    /// <summary>Logs that a draft was saved.</summary>
    [MessageLogging(EventId = 12902, Level = LogLevel.Information, Message = "The {kind} {scopeId} saved a home page draft")]
    public static partial IGenericMessage DraftSaved(ILogger logger, string kind, string scopeId);

    /// <summary>Logs that a draft was published.</summary>
    [MessageLogging(EventId = 12903, Level = LogLevel.Information, Message = "The {kind} {scopeId} published its home page")]
    public static partial IGenericMessage Published(ILogger logger, string kind, string scopeId);

    /// <summary>Logs which page applied.</summary>
    [MessageLogging(EventId = 12904, Level = LogLevel.Debug, Message = "The home page for user {userId} comes from: {source}")]
    public static partial IGenericMessage Applied(ILogger logger, string userId, string source);

    /// <summary>Refuses a body that is not a page.</summary>
    [MessageLogging(EventId = 22902, Level = LogLevel.Warning, Message = "A home page was refused: {reason}")]
    public static partial IGenericMessage BodyInvalid(ILogger logger, string reason);

    /// <summary>Refuses to publish when there is no draft.</summary>
    [MessageLogging(EventId = 42901, Level = LogLevel.Warning, Message = "The {kind} {scopeId} has no draft to publish")]
    public static partial IGenericMessage NoDraft(ILogger logger, string kind, string scopeId);
}
