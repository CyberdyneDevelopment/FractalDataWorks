using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Authorization.Components.Logging;

/// <summary>
/// MessageLogging methods for RoleProvider operations.
/// Provider-specific messages with domain context baked into templates.
/// EventId range: 8900-8919
/// </summary>
// Why (FDW-583): every *Failed/*Exception method below reports an operation that could not
// complete (the caught exception path and the non-exception failure path report the SAME
// outcome) — Error, not Warning.
[MessageLoggingTypeCode("COMPONENTS6")]
public static partial class RoleProviderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Load Roles (8900-8901)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading the roles list fails.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "RoleProvider: Failed to load roles list")]
    public static partial IGenericMessage LoadRolesFailed(
        ILogger logger);

    /// <summary>Logs when loading the roles list fails with exception.</summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "RoleProvider: Failed to load roles list")]
    public static partial IGenericMessage LoadRolesException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Load Permission Groups (8902-8903)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading permission groups fails.</summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error,
        Message = "RoleProvider: Failed to load permission groups list")]
    public static partial IGenericMessage LoadPermissionGroupsFailed(
        ILogger logger);

    /// <summary>Logs when loading permission groups fails with exception.</summary>
    [MessageLogging(EventId = 71003, Level = LogLevel.Error,
        Message = "RoleProvider: Failed to load permission groups list")]
    public static partial IGenericMessage LoadPermissionGroupsException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Role Detail (8904-8905)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading role details fails.</summary>
    [MessageLogging(EventId = 71004, Level = LogLevel.Error,
        Message = "RoleProvider: Failed to load role detail for '{roleName}'")]
    public static partial IGenericMessage RoleDetailLoadFailed(
        ILogger logger,
        string roleName);

    /// <summary>Logs when loading role details fails with exception.</summary>
    [MessageLogging(EventId = 71005, Level = LogLevel.Error,
        Message = "RoleProvider: Failed to load role detail")]
    public static partial IGenericMessage RoleDetailLoadException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Create Role (8906-8908)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when a role is created successfully.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Information,
        Message = "RoleProvider: Created role '{roleName}'")]
    public static partial IGenericMessage RoleCreated(
        ILogger logger,
        string roleName);

    /// <summary>Logs when creating a role fails.</summary>
    [MessageLogging(EventId = 71006, Level = LogLevel.Error,
        Message = "RoleProvider: Failed to create role")]
    public static partial IGenericMessage RoleCreateFailed(
        ILogger logger);

    /// <summary>Logs when creating a role fails with exception.</summary>
    [MessageLogging(EventId = 71007, Level = LogLevel.Error,
        Message = "RoleProvider: Failed to create role")]
    public static partial IGenericMessage RoleCreateException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Update Role (8909-8911)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when a role is updated successfully.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information,
        Message = "RoleProvider: Updated role '{roleName}'")]
    public static partial IGenericMessage RoleUpdated(
        ILogger logger,
        string roleName);

    /// <summary>Logs when updating a role fails.</summary>
    [MessageLogging(EventId = 71008, Level = LogLevel.Error,
        Message = "RoleProvider: Failed to update role '{roleName}'")]
    public static partial IGenericMessage RoleUpdateFailed(
        ILogger logger,
        string roleName);

    /// <summary>Logs when updating a role fails with exception.</summary>
    [MessageLogging(EventId = 71009, Level = LogLevel.Error,
        Message = "RoleProvider: Failed to update role")]
    public static partial IGenericMessage RoleUpdateException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Delete Role (8912)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when deleting a role fails.</summary>
    [MessageLogging(EventId = 71010, Level = LogLevel.Error,
        Message = "RoleProvider: Failed to delete role '{roleName}'")]
    public static partial IGenericMessage RoleDeleteFailed(
        ILogger logger,
        string roleName);

    /// <summary>Logs when deleting a role fails with exception.</summary>
    [MessageLogging(EventId = 71011, Level = LogLevel.Error,
        Message = "RoleProvider: Failed to delete role")]
    public static partial IGenericMessage RoleDeleteException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Save Permissions (8914)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when saving role permissions fails with exception.</summary>
    [MessageLogging(EventId = 71012, Level = LogLevel.Error,
        Message = "RoleProvider: Failed to save permissions for role '{roleName}'")]
    public static partial IGenericMessage SavePermissionsException(
        ILogger logger,
        Exception exception,
        string roleName);
}
