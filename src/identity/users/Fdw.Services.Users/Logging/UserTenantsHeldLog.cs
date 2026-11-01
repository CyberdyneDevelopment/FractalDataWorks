using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Logging;

/// <summary>
/// MessageLogging for reading the tenants one person is a member of (FDW-2676).
/// EventId ranges: 12796 (done), 62796 (more memberships than a person may hold, configuration), 92796 (a row without a column the read needs, internal).
/// </summary>
[MessageLoggingTypeCode("USERS")]
public static partial class UserTenantsHeldLog
{
    /// <summary>Logs that the tenants of one person were read.</summary>
    [MessageLogging(EventId = 12796, Level = LogLevel.Debug, Message = "User '{userId}' is a member of {count} tenants")]
    public static partial IGenericMessage Read(ILogger logger, string userId, int count);

    /// <summary>Logs a person who holds more memberships than a read carries.</summary>
    [MessageLogging(EventId = 62796, Level = LogLevel.Error, Message = "User '{userId}' is a member of more than {maximum} tenants, which is more than one read carries")]
    public static partial IGenericMessage TooManyMemberships(ILogger logger, string userId, int maximum);

    /// <summary>Logs a membership row that lacks a column, or holds a value that is not the kind the column holds.</summary>
    [MessageLogging(EventId = 92796, Level = LogLevel.Error, Message = "A tenant membership of user '{userId}' cannot be read: {reason}")]
    public static partial IGenericMessage MembershipUnreadable(ILogger logger, string userId, string reason);
}
