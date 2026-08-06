using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Logging;

/// <summary>
/// High-performance MessageLogging for authorization service operations.
/// EventId range: 3100-3199
/// </summary>
[MessageLoggingTypeCode("AUTHORIZATION")]
public static partial class AuthorizationLog
{
    /// <summary>
    /// Logs when authorization context is null.
    /// </summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Warning,
        Message = "Authorization context is null")]
    public static partial IGenericMessage AuthorizationContextNull(ILogger logger);

    /// <summary>
    /// Logs when resource parameter is required.
    /// </summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Warning,
        Message = "Resource parameter is required for authorization")]
    public static partial IGenericMessage ResourceRequired(ILogger logger);

    /// <summary>
    /// Logs when action parameter is required.
    /// </summary>
    [MessageLogging(EventId = 21002, Level = LogLevel.Warning,
        Message = "Action parameter is required for authorization")]
    public static partial IGenericMessage ActionRequired(ILogger logger);

    /// <summary>
    /// Logs when role parameter is required.
    /// </summary>
    [MessageLogging(EventId = 21003, Level = LogLevel.Warning,
        Message = "Role parameter is required")]
    public static partial IGenericMessage RoleRequired(ILogger logger);

    /// <summary>
    /// Logs when permission parameter is required.
    /// </summary>
    [MessageLogging(EventId = 21004, Level = LogLevel.Warning,
        Message = "Permission parameter is required")]
    public static partial IGenericMessage PermissionRequired(ILogger logger);

    /// <summary>
    /// Logs when authorization is denied due to not being authenticated.
    /// </summary>
    [MessageLogging(EventId = 51000, Level = LogLevel.Warning,
        Message = "Authorization denied for {resource}:{action} - user not authenticated")]
    public static partial IGenericMessage AuthorizationDeniedNotAuthenticated(
        ILogger logger,
        string resource,
        string action);

    /// <summary>
    /// Logs when authorization is denied for a specific permission.
    /// </summary>
    [MessageLogging(EventId = 51001, Level = LogLevel.Warning,
        Message = "Authorization denied for user '{userId}' on permission '{permission}'")]
    public static partial IGenericMessage AuthorizationDenied(
        ILogger logger,
        string userId,
        string permission);

    /// <summary>
    /// Logs when authorization is granted for a specific permission.
    /// </summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Debug,
        Message = "Authorization granted for user '{userId}' on permission '{permission}'")]
    public static partial IGenericMessage AuthorizationGranted(
        ILogger logger,
        string userId,
        string permission);

    /// <summary>
    /// Logs permission resolution data counts for debugging.
    /// </summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Debug,
        Message = "Permission resolution: userRoles={userRoleCount}, permissions={permissionCount}, rolePermissions={rolePermissionCount}, roles={roleCount}")]
    public static partial IGenericMessage PermissionResolutionDebug(
        ILogger logger,
        int userRoleCount,
        int permissionCount,
        int rolePermissionCount,
        int roleCount);

    /// <summary>Logs role-permission match count during effective permission resolution.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Debug,
        Message = "Role '{roleName}' (id={roleId}) matched {count} role-permission assignments")]
    public static partial IGenericMessage RolePermissionsMatched(
        ILogger logger, string roleName, string roleId, int count);

    /// <summary>Logs when a user's JWT role claim has no matching role definition.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning,
        Message = "Role '{roleName}' not found in loaded roles. Available: [{availableRoles}]")]
    public static partial IGenericMessage RoleNotFoundInLoadedRoles(
        ILogger logger, string roleName, string availableRoles);

    /// <summary>Logs when a RolePermission references a PermissionId that doesn't resolve.</summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Warning,
        Message = "RolePermission references unknown PermissionId '{permissionId}' (sample Permission.Id='{sampleId}')")]
    public static partial IGenericMessage PermissionIdUnresolved(
        ILogger logger, string permissionId, string sampleId);

    /// <summary>Logs effective permission count resolved for a user.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug,
        Message = "Resolved {count} effective permissions for user '{userId}'")]
    public static partial IGenericMessage EffectivePermissionsResolved(
        ILogger logger, int count, string userId);

    /// <summary>Logs when role provider query fails during permission resolution — authorization denied.</summary>
    // Why: Fails-closed. If we cannot load roles from the database, we must deny access
    // rather than proceeding with an empty set (which would skip all permission checks).
    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "Authorization denied: failed to load roles from provider")]
    public static partial IGenericMessage RoleProviderQueryFailed(ILogger logger);

    /// <summary>Logs when permission provider query fails during permission resolution — authorization denied.</summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "Authorization denied: failed to load permissions from provider")]
    public static partial IGenericMessage PermissionProviderQueryFailed(ILogger logger);

    /// <summary>Logs when role-permission provider query fails during permission resolution — authorization denied.</summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error,
        Message = "Authorization denied: failed to load role-permission assignments from provider")]
    public static partial IGenericMessage RolePermissionProviderQueryFailed(ILogger logger);

    // -- Org-tier (3-tier union) -- EventId range 3123-3130

    /// <summary>Logs the start of an org-access grant query.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace,
        Message = "Querying org access grants for userId={userId} orgId={orgId}")]
    public static partial IGenericMessage OrgAccessQueryStarted(ILogger logger, Guid userId, Guid orgId);

    /// <summary>Logs when the org-access query fails — org tier contributes zero grants.</summary>
    // Why: The org-access tier failure is non-fatal (we still have global and tenant tiers).
    // Log at Error so ops can detect a schema mismatch or missing TenantOrgAccess table,
    // but do not deny access entirely — missing org grants != zero permissions from all tiers.
    [MessageLogging(EventId = 71003, Level = LogLevel.Error,
        Message = "Org access query failed for userId={userId} orgId={orgId} — org tier contributes no grants")]
    public static partial IGenericMessage OrgAccessQueryFailed(ILogger logger, Guid userId, Guid orgId);

    /// <summary>Logs the count of org-access grants loaded.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Debug,
        Message = "Loaded {count} org access grants for userId={userId} orgId={orgId}")]
    public static partial IGenericMessage OrgAccessGrantsLoaded(ILogger logger, int count, Guid userId, Guid orgId);

    /// <summary>Logs effective permissions resolved including all three tiers.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Debug,
        Message = "3-tier effective permissions resolved: global={globalCount}, tenant={tenantCount}, org={orgCount}, total={total} for user '{userId}'")]
    public static partial IGenericMessage ThreeTierPermissionsResolved(
        ILogger logger, int globalCount, int tenantCount, int orgCount, int total, string userId);

    /// <summary>Logs when org context is not available — org tier skipped.</summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Trace,
        Message = "Org context not available for user '{userId}' — org tier skipped in permission resolution")]
    public static partial IGenericMessage OrgTierSkippedNoOrgContext(ILogger logger, string userId);

    /// <summary>Logs when tenant context is not available — tenant and org tiers skipped.</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Trace,
        Message = "Tenant context not available for user '{userId}' — tenant and org tiers skipped")]
    public static partial IGenericMessage TenantTierSkippedNoTenantContext(ILogger logger, string userId);

    /// <summary>Logs when the user ID cannot be parsed as a Guid for the org-access query.</summary>
    [MessageLogging(EventId = 21005, Level = LogLevel.Warning,
        Message = "Cannot parse userId '{userId}' as Guid for org-access query — org tier skipped")]
    public static partial IGenericMessage OrgTierSkippedNonGuidUserId(ILogger logger, string userId);

    // Why: EventIds 3130-3136 (SecurityStamp*) retired with SecurityStampService purge. Not reused.

    // -- User-role assignment tier (FDW-532 fix) -- EventId range 3137-3142

    /// <summary>Logs when user-role assignment load fails — fail-closed, token not issued.</summary>
    // Why: FDW-532 fix. If we cannot load the user's role assignments, we MUST fail
    // rather than falling back to the full catalog (which was the privilege-escalation bug).
    [MessageLogging(EventId = 71004, Level = LogLevel.Error,
        Message = "Authorization denied: failed to load role assignments for user '{userId}'")]
    public static partial IGenericMessage UserRoleAssignmentLoadFailed(ILogger logger, string userId);

    /// <summary>Logs count of role assignments found for a user.</summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Trace,
        Message = "Loaded {count} role assignments for user '{userId}'")]
    public static partial IGenericMessage UserRoleAssignmentsLoaded(ILogger logger, int count, string userId);

    /// <summary>Logs when a user has zero role assignments — zero permissions granted.</summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Debug,
        Message = "User '{userId}' has no role assignments — zero permissions granted")]
    public static partial IGenericMessage UserHasNoRoleAssignments(ILogger logger, string userId);

    /// <summary>Logs count of roles selected after filtering by user assignments.</summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Trace,
        Message = "Selected {count} roles for user '{userId}' from {total} in catalog after assignment filter")]
    public static partial IGenericMessage UserRolesSelected(ILogger logger, int count, string userId, int total);
}
