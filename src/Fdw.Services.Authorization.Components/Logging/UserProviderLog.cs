using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Authorization.Components.Logging;

/// <summary>
/// MessageLogging methods for UserProvider operations.
/// Provider-specific messages with domain context baked into the templates.
/// EventId range: 8915-8919
/// </summary>
[MessageLoggingTypeCode("COMPONENTS6")]
public static partial class UserProviderLog
{
    /// <summary>Logs when loading the users list fails.</summary>
    [MessageLogging(EventId = 71013, Level = LogLevel.Error,
        Message = "UserProvider: Failed to load users list")]
    public static partial IGenericMessage LoadFailed(ILogger logger);

    /// <summary>Logs when loading the users list fails with an exception.</summary>
    [MessageLogging(EventId = 71014, Level = LogLevel.Error,
        Message = "UserProvider: Failed to load users list")]
    public static partial IGenericMessage LoadException(ILogger logger, Exception exception);

    /// <summary>Logs when creating a user fails.</summary>
    [MessageLogging(EventId = 71015, Level = LogLevel.Error,
        Message = "UserProvider: Failed to create user")]
    public static partial IGenericMessage CreateFailed(ILogger logger);

    /// <summary>Logs when creating a user fails with an exception.</summary>
    [MessageLogging(EventId = 71016, Level = LogLevel.Error,
        Message = "UserProvider: Failed to create user")]
    public static partial IGenericMessage CreateException(ILogger logger, Exception exception);

    /// <summary>Logs when updating a user fails.</summary>
    [MessageLogging(EventId = 71017, Level = LogLevel.Error,
        Message = "UserProvider: Failed to update user '{userId}'")]
    public static partial IGenericMessage UpdateFailed(ILogger logger, string userId);

    /// <summary>Logs when updating a user fails with an exception.</summary>
    [MessageLogging(EventId = 71018, Level = LogLevel.Error,
        Message = "UserProvider: Failed to update user")]
    public static partial IGenericMessage UpdateException(ILogger logger, Exception exception);

    /// <summary>Logs when deleting a user fails.</summary>
    [MessageLogging(EventId = 71019, Level = LogLevel.Error,
        Message = "UserProvider: Failed to delete user '{userId}'")]
    public static partial IGenericMessage DeleteFailed(ILogger logger, string userId);

    /// <summary>Logs when deleting a user fails with an exception.</summary>
    [MessageLogging(EventId = 71020, Level = LogLevel.Error,
        Message = "UserProvider: Failed to delete user")]
    public static partial IGenericMessage DeleteException(ILogger logger, Exception exception);

    /// <summary>Logs when granting a role to a user fails.</summary>
    [MessageLogging(EventId = 71021, Level = LogLevel.Error,
        Message = "UserProvider: Failed to assign role '{roleName}' to user '{userId}'")]
    public static partial IGenericMessage AssignRoleFailed(ILogger logger, string roleName, string userId);

    /// <summary>Logs when revoking a role from a user fails.</summary>
    [MessageLogging(EventId = 71022, Level = LogLevel.Error,
        Message = "UserProvider: Failed to revoke role '{roleName}' from user '{userId}'")]
    public static partial IGenericMessage RevokeRoleFailed(ILogger logger, string roleName, string userId);

    /// <summary>Logs when an administrative password reset fails.</summary>
    [MessageLogging(EventId = 71023, Level = LogLevel.Error,
        Message = "UserProvider: Failed to reset the password for user '{userId}'")]
    public static partial IGenericMessage ResetPasswordFailed(ILogger logger, string userId);

    /// <summary>Logs when an administrative password reset fails with an exception.</summary>
    [MessageLogging(EventId = 71024, Level = LogLevel.Error,
        Message = "UserProvider: Failed to reset the password for a user")]
    public static partial IGenericMessage ResetPasswordException(ILogger logger, Exception exception);
}
