using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;
using System;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// High-performance MessageLogging for user endpoint operations.
/// EventId range: 7890-7899
/// </summary>
[MessageLoggingTypeCode("USERSENDPOINTS")]
public static partial class UserEndpointLog
{
    /// <summary>
    /// Logs when a user endpoint operation fails with an unhandled exception.
    /// </summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error,
        Message = "User endpoint operation '{operation}' failed for user '{userId}'")]
    public static partial IGenericMessage OperationFailed(
        ILogger logger,
        Exception exception,
        string operation,
        string userId);

    /// <summary>
    /// Logs when getting user preferences fails.
    /// </summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error,
        Message = "Failed to get preferences for user '{userId}'")]
    public static partial IGenericMessage GetPreferencesFailed(
        ILogger logger,
        Exception exception,
        string userId);

    /// <summary>
    /// Logs when updating user preferences fails.
    /// </summary>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error,
        Message = "Failed to update preferences for user '{userId}'")]
    public static partial IGenericMessage UpdatePreferencesFailed(
        ILogger logger,
        Exception exception,
        string userId);

    /// <summary>
    /// Logs when a role required during user creation is not found.
    /// </summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Error,
        Message = "Role '{roleName}' not found during user creation")]
    public static partial IGenericMessage RoleNotFoundDuringCreate(
        ILogger logger,
        string roleName);
}
