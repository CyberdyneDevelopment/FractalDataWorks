using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Logging;

/// <summary>
/// MessageLogging for reading what a person's roles grant (FDW-2688).
/// EventId ranges: 12797 (done), 62797 (more rows than one read carries, configuration), 92797 (a row without a column the read needs, internal).
/// </summary>
[MessageLoggingTypeCode("AUTHORIZATION")]
public static partial class UserPermissionGrantsLog
{
    /// <summary>Logs that the grants of one person were read.</summary>
    [MessageLogging(EventId = 12797, Level = LogLevel.Debug, Message = "User '{userId}' has {count} role grant rows")]
    public static partial IGenericMessage Read(ILogger logger, string userId, int count);

    /// <summary>Logs a person whose roles grant more rows than a read carries.</summary>
    [MessageLogging(EventId = 62797, Level = LogLevel.Error, Message = "User '{userId}' has more than {maximum} role grant rows, which is more than one read carries")]
    public static partial IGenericMessage TooManyRows(ILogger logger, string userId, int maximum);

    /// <summary>Logs a grant row that lacks a column, or holds a value that is not the kind the column holds.</summary>
    [MessageLogging(EventId = 92797, Level = LogLevel.Error, Message = "A role grant row of user '{userId}' cannot be read: {reason}")]
    public static partial IGenericMessage RowUnreadable(ILogger logger, string userId, string reason);
}
