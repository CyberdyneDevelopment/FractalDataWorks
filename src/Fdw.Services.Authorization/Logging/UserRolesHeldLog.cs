using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Logging;

/// <summary>
/// MessageLogging for reading the roles one person holds (FDW-2668).
/// EventId ranges: 12795 (done), 62795 (more assignments than a person may hold, configuration), 92795 (a row without a column the read needs, internal).
/// </summary>
[MessageLoggingTypeCode("AUTHORIZATION")]
public static partial class UserRolesHeldLog
{
    /// <summary>Logs that the roles of one person were read.</summary>
    [MessageLogging(EventId = 12795, Level = LogLevel.Debug, Message = "User '{userId}' has {count} role assignments")]
    public static partial IGenericMessage Read(ILogger logger, string userId, int count);

    /// <summary>Logs a person who holds more assignments than a read carries.</summary>
    [MessageLogging(EventId = 62795, Level = LogLevel.Error, Message = "User '{userId}' has more than {maximum} role assignments, which is more than one read carries")]
    public static partial IGenericMessage TooManyAssignments(ILogger logger, string userId, int maximum);

    /// <summary>Logs an assignment row that lacks a column, or holds a value that is not the kind the column holds.</summary>
    [MessageLogging(EventId = 92795, Level = LogLevel.Error, Message = "A role assignment of user '{userId}' cannot be read: {reason}")]
    public static partial IGenericMessage AssignmentUnreadable(ILogger logger, string userId, string reason);
}
