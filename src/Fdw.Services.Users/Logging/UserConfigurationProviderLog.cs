using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;
using System;

namespace Fdw.Services.Users.Logging;

/// <summary>
/// MessageLogging for UserConfigurationProvider and UserTenantConfigurationProvider operations.
/// EventId range: 7861-7879
/// </summary>
[MessageLoggingTypeCode("USERS")]
public static partial class UserConfigurationProviderLog
{
    /// <summary>
    /// Logs that a user configuration is being loaded by its identifier.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="userId">The identifier of the user configuration being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "Loading user configuration by id '{userId}'")]
    public static partial IGenericMessage LoadByIdTrace(ILogger logger, Guid userId);

    /// <summary>
    /// Logs that a user configuration is being loaded by username.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="username">The username of the user configuration being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "Loading user configuration by username '{username}'")]
    public static partial IGenericMessage LoadByUsernameTrace(ILogger logger, string username);

    /// <summary>
    /// Logs that all user configurations were loaded, reporting the count.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="count">The number of user configurations that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Debug,
        Message = "Loaded {count} user configurations")]
    public static partial IGenericMessage LoadAllLoaded(ILogger logger, int count);

    /// <summary>
    /// Logs that loading users from the provider failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "Failed to load users from provider")]
    public static partial IGenericMessage LoadFailed(ILogger logger);

    /// <summary>
    /// Logs that tenant memberships are being loaded for a user.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="userId">The identifier of the user whose tenant memberships are being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace,
        Message = "Loading tenant memberships for user '{userId}'")]
    public static partial IGenericMessage LoadTenantsTrace(ILogger logger, Guid userId);

    /// <summary>
    /// Logs that tenant memberships were loaded for a user, reporting the count.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="count">The number of tenant memberships that were loaded.</param>
    /// <param name="userId">The identifier of the user whose tenant memberships were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11004, Level = LogLevel.Debug,
        Message = "Loaded {count} tenant memberships for user '{userId}'")]
    public static partial IGenericMessage LoadTenantsLoaded(ILogger logger, int count, Guid userId);

    /// <summary>
    /// Logs that loading tenant memberships for a user failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="userId">The identifier of the user whose tenant memberships failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "Failed to load tenant memberships for user '{userId}'")]
    public static partial IGenericMessage LoadTenantsFailed(ILogger logger, Guid userId);

    /// <summary>
    /// Logs that the default tenant is being loaded for a user.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="userId">The identifier of the user whose default tenant is being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace,
        Message = "Loading default tenant for user '{userId}'")]
    public static partial IGenericMessage LoadDefaultTenantTrace(ILogger logger, Guid userId);

    /// <summary>
    /// Logs that loading the default tenant for a user failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="userId">The identifier of the user whose default tenant failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error,
        Message = "Failed to load default tenant for user '{userId}'")]
    public static partial IGenericMessage LoadDefaultTenantFailed(ILogger logger, Guid userId);

    /// <summary>
    /// Logs that a tenant is being granted access to a user.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="tenantId">The identifier of the tenant being granted.</param>
    /// <param name="userId">The identifier of the user being granted access.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11006, Level = LogLevel.Trace,
        Message = "Granting tenant '{tenantId}' access to user '{userId}'")]
    public static partial IGenericMessage GrantTenantTrace(ILogger logger, Guid tenantId, Guid userId);

    /// <summary>
    /// Logs that granting a tenant access to a user failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="tenantId">The identifier of the tenant that failed to be granted.</param>
    /// <param name="userId">The identifier of the user the grant failed for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71003, Level = LogLevel.Error,
        Message = "Failed to grant tenant '{tenantId}' access to user '{userId}'")]
    public static partial IGenericMessage GrantTenantFailed(ILogger logger, Guid tenantId, Guid userId);

    /// <summary>
    /// Logs that a tenant's access is being revoked from a user.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="tenantId">The identifier of the tenant being revoked.</param>
    /// <param name="userId">The identifier of the user whose access is being revoked.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11007, Level = LogLevel.Trace,
        Message = "Revoking tenant '{tenantId}' access from user '{userId}'")]
    public static partial IGenericMessage RevokeTenantTrace(ILogger logger, Guid tenantId, Guid userId);

    /// <summary>
    /// Logs that revoking a tenant's access from a user failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="tenantId">The identifier of the tenant that failed to be revoked.</param>
    /// <param name="userId">The identifier of the user the revoke failed for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71004, Level = LogLevel.Error,
        Message = "Failed to revoke tenant '{tenantId}' access from user '{userId}'")]
    public static partial IGenericMessage RevokeTenantFailed(ILogger logger, Guid tenantId, Guid userId);

    /// <summary>
    /// Logs that a user is not a member of a tenant and therefore the tenant cannot be set as the default.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="userId">The identifier of the user that is not a tenant member.</param>
    /// <param name="tenantId">The identifier of the tenant the user is not a member of.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 41000, Level = LogLevel.Warning,
        Message = "User '{userId}' is not a member of tenant '{tenantId}'; cannot set as default")]
    public static partial IGenericMessage SetDefaultNotMember(ILogger logger, Guid userId, Guid tenantId);

    /// <summary>
    /// Logs that setting the default tenant for a user failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="tenantId">The identifier of the tenant that failed to be set as default.</param>
    /// <param name="userId">The identifier of the user the default tenant failed to set for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71005, Level = LogLevel.Error,
        Message = "Failed to set default tenant '{tenantId}' for user '{userId}'")]
    public static partial IGenericMessage SetDefaultFailed(ILogger logger, Guid tenantId, Guid userId);

    /// <summary>
    /// Logs that loading user configurations from the gateway failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71006, Level = LogLevel.Error,
        Message = "Failed to load user configurations from gateway")]
    public static partial IGenericMessage GatewayQueryFailed(ILogger logger);

    /// <summary>
    /// Logs that loading user tenant configurations from the gateway failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71007, Level = LogLevel.Error,
        Message = "Failed to load user tenant configurations from gateway")]
    public static partial IGenericMessage TenantGatewayQueryFailed(ILogger logger);

    /// <summary>
    /// Logs that user preferences are being loaded for a user.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="userId">The identifier of the user whose preferences are being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11008, Level = LogLevel.Trace,
        Message = "Loading user preferences for user '{userId}'")]
    public static partial IGenericMessage LoadPreferencesTrace(ILogger logger, Guid userId);

    /// <summary>
    /// Logs that loading user preferences for a user failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="userId">The identifier of the user whose preferences failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71008, Level = LogLevel.Error,
        Message = "Failed to load user preferences for user '{userId}'")]
    public static partial IGenericMessage LoadPreferencesFailed(ILogger logger, Guid userId);

    /// <summary>
    /// Logs that a user is being created.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="username">The username of the user being created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11009, Level = LogLevel.Trace,
        Message = "Creating user '{username}'")]
    public static partial IGenericMessage CreateUserTrace(ILogger logger, string username);

    /// <summary>
    /// Logs that a user is being updated.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="userId">The identifier of the user being updated.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11010, Level = LogLevel.Trace,
        Message = "Updating user '{userId}'")]
    public static partial IGenericMessage UpdateUserTrace(ILogger logger, Guid userId);

    /// <summary>
    /// Logs that a user is being deleted.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="userId">The identifier of the user being deleted.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11011, Level = LogLevel.Trace,
        Message = "Deleting user '{userId}'")]
    public static partial IGenericMessage DeleteUserTrace(ILogger logger, Guid userId);
}
