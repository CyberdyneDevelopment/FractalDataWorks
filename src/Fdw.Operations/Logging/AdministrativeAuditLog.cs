using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Logging;

/// <summary>
/// MessageLogging for the record of administrative changes.
/// EventId ranges: 12700-12705 (recorded), 22700-22706 (refused), 72700-72701 (storage).
/// </summary>
[MessageLoggingTypeCode("OPERATIONS")]
public static partial class AdministrativeAuditLog
{
    /// <summary>
    /// Logs that an administrative change was recorded.
    /// </summary>
    [MessageLogging(EventId = 12700, Level = LogLevel.Information, Message = "Recorded administrative change '{changeKind}' on {itemKind} '{itemId}' at the {level} level by '{actorName}'")]
    public static partial IGenericMessage Recorded(ILogger logger, string changeKind, string itemKind, string itemId, string level, string actorName);

    /// <summary>
    /// Logs that a change was not recorded because the caller is not a signed-in person.
    /// </summary>
    [MessageLogging(EventId = 22700, Level = LogLevel.Warning, Message = "Administrative change '{changeKind}' was not recorded: the caller is not a signed-in person with a user id and a name")]
    public static partial IGenericMessage ActorRequired(ILogger logger, string changeKind);

    /// <summary>
    /// Logs that a change was not recorded because the caller acts in no tenant.
    /// </summary>
    [MessageLogging(EventId = 22701, Level = LogLevel.Warning, Message = "Administrative change '{changeKind}' was not recorded: the caller acts in no tenant")]
    public static partial IGenericMessage TenantRequired(ILogger logger, string changeKind);

    /// <summary>
    /// Logs that a change was not recorded because a value the entry must state was not stated.
    /// </summary>
    [MessageLogging(EventId = 22702, Level = LogLevel.Warning, Message = "An administrative change was not recorded: '{field}' was not stated")]
    public static partial IGenericMessage FieldRequired(ILogger logger, string field);

    /// <summary>
    /// Logs that a change was not recorded because its level is not one the system knows.
    /// </summary>
    [MessageLogging(EventId = 22703, Level = LogLevel.Warning, Message = "Administrative change '{changeKind}' was not recorded: its level is not Platform, Tenant or Organization")]
    public static partial IGenericMessage LevelUnknown(ILogger logger, string changeKind);

    /// <summary>
    /// Logs that a change at the Organization level was not recorded because it names no organization.
    /// </summary>
    [MessageLogging(EventId = 22704, Level = LogLevel.Warning, Message = "Administrative change '{changeKind}' was not recorded: it is made at the Organization level and names no organization")]
    public static partial IGenericMessage OrganizationRequired(ILogger logger, string changeKind);

    /// <summary>
    /// Logs that a change above the Organization level was not recorded because it names an organization.
    /// </summary>
    [MessageLogging(EventId = 22705, Level = LogLevel.Warning, Message = "Administrative change '{changeKind}' was not recorded: it names an organization but is made at the {level} level")]
    public static partial IGenericMessage OrganizationNotAllowed(ILogger logger, string changeKind, string level);

    /// <summary>
    /// Logs that a read of the changes was refused because the page asked for is not a page.
    /// </summary>
    [MessageLogging(EventId = 22706, Level = LogLevel.Warning, Message = "Administrative changes were not read: page {page} of {pageSize} is not a page (both start at 1)")]
    public static partial IGenericMessage PageInvalid(ILogger logger, int page, int pageSize);

    /// <summary>
    /// Logs that storing a change failed.
    /// </summary>
    [MessageLogging(EventId = 72700, Level = LogLevel.Error, Message = "Administrative change '{changeKind}' on '{itemId}' could not be stored: {errorMessage}")]
    public static partial IGenericMessage StoreFailed(ILogger logger, string changeKind, string itemId, string? errorMessage);

    /// <summary>
    /// Logs that reading the changes failed.
    /// </summary>
    [MessageLogging(EventId = 72701, Level = LogLevel.Error, Message = "Administrative changes could not be read: {errorMessage}")]
    public static partial IGenericMessage ReadFailed(ILogger logger, string? errorMessage);
}
