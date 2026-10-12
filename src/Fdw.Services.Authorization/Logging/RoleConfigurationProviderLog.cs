using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Logging;

/// <summary>
/// MessageLogging for RoleConfigurationProvider operations.
/// EventId range: 9400-9409
/// </summary>
[MessageLoggingTypeCode("AUTHORIZATION")]
public static partial class RoleConfigurationProviderLog
{
    /// <summary>
    /// Logs that permissions are being loaded for the given role.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="roleName">The name of the role whose permissions are being loaded.</param>
    /// <param name="roleId">The identifier of the role whose permissions are being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11012, Level = LogLevel.Trace,
        Message = "Loading permissions for role '{roleName}' (id: {roleId})")]
    public static partial IGenericMessage LoadingPermissions(ILogger logger, string roleName, string roleId);

    /// <summary>
    /// Logs that the given number of permissions were assembled onto the role.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of permissions assembled onto the role.</param>
    /// <param name="roleName">The name of the role the permissions were assembled onto.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11013, Level = LogLevel.Debug,
        Message = "Assembled {count} permissions onto role '{roleName}'")]
    public static partial IGenericMessage PermissionsAssembled(ILogger logger, int count, string roleName);

    /// <summary>
    /// Logs that the role with the given name was not found — the query succeeded and simply
    /// returned no row (a found-but-absent outcome, not a query failure).
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="roleName">The name of the role that was not found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11014, Level = LogLevel.Debug,
        Message = "Role '{roleName}' not found")]
    public static partial IGenericMessage RoleNotFound(ILogger logger, string roleName);

    /// <summary>
    /// Logs that the role query itself failed (e.g. ConfigurationDb unreachable) while resolving a
    /// role with its permissions — distinct from a successful query that found no role.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="roleName">The name of the role being resolved.</param>
    /// <param name="error">The error describing why the role query failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71010, Level = LogLevel.Error,
        Message = "Failed to query role '{roleName}': {error}")]
    public static partial IGenericMessage RoleQueryFailed(ILogger logger, string roleName, string error);

    /// <summary>
    /// Logs that the given number of role permissions were loaded for the role.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of role permissions loaded.</param>
    /// <param name="roleId">The identifier of the role whose permissions were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11015, Level = LogLevel.Debug,
        Message = "Loaded {count} role permissions for role id '{roleId}'")]
    public static partial IGenericMessage RolePermissionsLoaded(ILogger logger, int count, string roleId);

    /// <summary>
    /// Logs that the given number of permissions were loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of permissions loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11016, Level = LogLevel.Debug,
        Message = "Loaded {count} permissions")]
    public static partial IGenericMessage AllPermissionsLoaded(ILogger logger, int count);


    /// <summary>
    /// Logs that authorization data is unavailable because the Gateway or DataStoreName is not initialized.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61000, Level = LogLevel.Error,
        Message = "Authorization data unavailable: Gateway or DataStoreName not initialized")]
    public static partial IGenericMessage GatewayNotInitialized(ILogger logger);

    /// <summary>
    /// Logs that the permission query failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="error">The error describing why the permission query failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71005, Level = LogLevel.Error,
        Message = "Failed to query permissions: {error}")]
    public static partial IGenericMessage PermissionQueryFailed(ILogger logger, string error);

    /// <summary>
    /// Logs that the role-permission query for the given role failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="roleId">The identifier of the role whose role-permission query failed.</param>
    /// <param name="error">The error describing why the role-permission query failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71006, Level = LogLevel.Error,
        Message = "Failed to query role-permissions for role '{roleId}': {error}")]
    public static partial IGenericMessage RolePermissionQueryFailed(ILogger logger, string roleId, string error);

    /// <summary>
    /// Logs that loading all roles from the provider failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71007, Level = LogLevel.Error,
        Message = "Failed to load roles from provider")]
    public static partial IGenericMessage RolesQueryFailed(ILogger logger);

    /// <summary>
    /// Logs that loading filtered roles from the provider failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71008, Level = LogLevel.Error,
        Message = "Failed to load filtered roles from provider")]
    public static partial IGenericMessage FilteredRolesQueryFailed(ILogger logger);
}
