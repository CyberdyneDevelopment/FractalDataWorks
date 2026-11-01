using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Logging;

/// <summary>
/// MessageLogging for UserRoleConfigurationProvider operations.
/// EventId range: 9410-9419
/// </summary>
[MessageLoggingTypeCode("AUTHORIZATION")]
public static partial class UserRoleConfigurationProviderLog
{
    /// <summary>
    /// Logs that the given number of user-role assignments were loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of user-role assignments loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11020, Level = LogLevel.Debug,
        Message = "Loaded {count} user-role assignments")]
    public static partial IGenericMessage AllUserRolesLoaded(ILogger logger, int count);

    /// <summary>
    /// Logs that the given number of user-role assignments were loaded for the user.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of user-role assignments loaded for the user.</param>
    /// <param name="userId">The identifier of the user whose user-role assignments were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11021, Level = LogLevel.Debug,
        Message = "Loaded {count} user-role assignments for user '{userId}'")]
    public static partial IGenericMessage UserRolesForUserLoaded(ILogger logger, int count, string userId);
}
