using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// High-performance MessageLogging for authorization endpoint operations.
/// EventId range: 3113-3130
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS5")]
public static partial class AuthorizationEndpointLog
{
    /// <summary>
    /// Logs when listing all permissions.
    /// </summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "Listing all permissions")]
    public static partial IGenericMessage ListingPermissions(ILogger logger);

    /// <summary>
    /// Logs when permissions have been listed.
    /// </summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information,
        Message = "Listed {count} permissions")]
    public static partial IGenericMessage ListedPermissions(ILogger logger, int count);

    /// <summary>
    /// Logs when getting role permissions.
    /// </summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Debug,
        Message = "Getting permissions for role '{roleName}'")]
    public static partial IGenericMessage GettingRolePermissions(ILogger logger, string roleName);

    /// <summary>
    /// Logs when setting role permissions.
    /// </summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug,
        Message = "Setting permissions for role '{roleName}'")]
    public static partial IGenericMessage SettingRolePermissions(ILogger logger, string roleName);

    /// <summary>
    /// Logs when role permissions have been updated.
    /// </summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information,
        Message = "Role '{roleName}' permissions updated with {count} permissions")]
    public static partial IGenericMessage RolePermissionsUpdated(ILogger logger, string roleName, int count);

    /// <summary>
    /// Logs when assigning a role to a user.
    /// </summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Debug,
        Message = "Assigning role '{roleName}' to user '{userId}'")]
    public static partial IGenericMessage AssigningUserRole(ILogger logger, string roleName, string userId);

    /// <summary>
    /// Logs when a role has been assigned to a user.
    /// </summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Information,
        Message = "Role '{roleName}' assigned to user '{userId}'")]
    public static partial IGenericMessage UserRoleAssigned(ILogger logger, string roleName, string userId);

    /// <summary>
    /// Logs when revoking a role from a user.
    /// </summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Debug,
        Message = "Revoking role '{roleName}' from user '{userId}'")]
    public static partial IGenericMessage RevokingUserRole(ILogger logger, string roleName, string userId);

    /// <summary>
    /// Logs when a role has been revoked from a user.
    /// </summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Information,
        Message = "Role '{roleName}' revoked from user '{userId}'")]
    public static partial IGenericMessage UserRoleRevoked(ILogger logger, string roleName, string userId);

    /// <summary>
    /// Logs when an authorization endpoint operation fails.
    /// </summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error,
        Message = "Authorization endpoint operation '{operation}' failed for context '{context}'")]
    public static partial IGenericMessage OperationFailed(
        ILogger logger,
        System.Exception exception,
        string operation,
        string context);


    /// <summary>
    /// Logs when the transactional role change is rolled back because a step (role write or stamp bump)
    /// failed. Nothing was persisted — the caller receives this structured failure.
    /// </summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "Atomic role change rolled back for '{context}': {reason}")]
    public static partial IGenericMessage AtomicRoleChangeFailed(ILogger logger, string context, string? reason);

    /// <summary>
    /// Logs when a transaction scope cannot be opened for a role change operation.
    /// </summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "Failed to open transaction for role change for '{context}': {reason}")]
    public static partial IGenericMessage TransactionOpenFailed(ILogger logger, string context, string reason);

    /// <summary>
    /// Logs when rolling back the transaction after a failed role/permission write itself fails,
    /// leaving the transaction outcome indeterminate.
    /// </summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error,
        Message = "Rollback after a failed role change for '{context}' did not succeed: {reason}")]
    public static partial IGenericMessage RollbackFailed(ILogger logger, string context, string? reason);

    /// <summary>
    /// Logs when the role assignments could not be read for the whole-store listing.
    /// </summary>
    [MessageLogging(EventId = 71003, Level = LogLevel.Error,
        Message = "Role assignments could not be read, so no user-role listing can be produced: {reason}")]
    public static partial IGenericMessage UserRoleAssignmentsUnreadable(ILogger logger, string reason);

    /// <summary>
    /// Logs a completed whole-store listing of role assignments.
    /// </summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Information,
        Message = "Listed role assignments for {userCount} user(s) across {roleCount} role(s)")]
    public static partial IGenericMessage UserRoleAssignmentsListed(ILogger logger, int userCount, int roleCount);

    /// <summary>
    /// Logs when a Role/Permission/RolePermission domain provider read fails structurally (as
    /// opposed to answering that the row is genuinely absent).
    /// </summary>
    [MessageLogging(EventId = 71004, Level = LogLevel.Error,
        Message = "Authorization domain read failed for '{context}': {reason}")]
    public static partial IGenericMessage AuthorizationReadFailed(ILogger logger, string context, string? reason);

    /// <summary>
    /// Logs a role change refused because the caller's token names no tenant. A role is held per
    /// tenant (FDW-825) and a role change acts in the tenant the caller is acting in, so without one
    /// there is no tenant to grant or revoke the role in.
    /// </summary>
    [MessageLogging(EventId = 71005, Level = LogLevel.Error,
        Message = "Role change for user {userId} refused: the caller's token carries no tenant")]
    public static partial IGenericMessage RoleChangeWithoutTenant(ILogger logger, string userId);

    /// <summary>
    /// Logs a permission set refused because one or more of the names it carries do not resolve to
    /// a permission the caller can assign -- either no permission has that name, or the name
    /// carries another tenant's prefix. The set is refused whole rather than applied in part.
    /// </summary>
    [MessageLogging(EventId = 71006, Level = LogLevel.Error,
        Message = "Permission set for role '{roleName}' refused: {count} name(s) did not resolve: {names}")]
    public static partial IGenericMessage PermissionNamesNotResolved(ILogger logger, string roleName, int count, string names);

    /// <summary>
    /// Logs a request answered "not found" because no role carries the name it asked for. Recorded
    /// at Information because an absent row is a legitimate answer to a caller's question, not a
    /// failure of the system -- but it is recorded, so a 404 is never a silent outcome.
    /// </summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Information,
        Message = "Role '{roleName}' was not found")]
    public static partial IGenericMessage RoleNotFound(ILogger logger, string roleName);

    /// <summary>
    /// Logs the start of a single-role read.
    /// </summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Trace,
        Message = "Getting role {roleName}")]
    public static partial IGenericMessage GettingRole(ILogger logger, string roleName);

    /// <summary>
    /// Logs the start of a role update.
    /// </summary>
    [MessageLogging(EventId = 11012, Level = LogLevel.Trace,
        Message = "Updating role {roleName}")]
    public static partial IGenericMessage UpdatingRole(ILogger logger, string roleName);

    /// <summary>
    /// Logs the start of a role delete.
    /// </summary>
    [MessageLogging(EventId = 11013, Level = LogLevel.Trace,
        Message = "Deleting role {roleName}")]
    public static partial IGenericMessage DeletingRole(ILogger logger, string roleName);

    /// <summary>
    /// Logs the start of a permission-set replacement, with the number of names the caller sent
    /// (before any of them are resolved).
    /// </summary>
    [MessageLogging(EventId = 11014, Level = LogLevel.Trace,
        Message = "Replacing the permission set of role {roleName} with {requestedCount} requested name(s)")]
    public static partial IGenericMessage ReplacingRolePermissions(ILogger logger, string roleName, int requestedCount);

    /// <summary>
    /// Logs the name-to-id resolution of the role an operation will act on.
    /// </summary>
    [MessageLogging(EventId = 11015, Level = LogLevel.Debug,
        Message = "Role {roleName} resolved to {roleId}")]
    public static partial IGenericMessage RoleResolved(ILogger logger, string roleName, Guid roleId);

    /// <summary>
    /// Logs how many permissions the catalogue offered to resolve names against.
    /// </summary>
    [MessageLogging(EventId = 11016, Level = LogLevel.Debug,
        Message = "Loaded {count} permission(s) to resolve against")]
    public static partial IGenericMessage PermissionCatalogLoaded(ILogger logger, int count);

    /// <summary>
    /// Logs how many mappings the role carried before a replacement.
    /// </summary>
    [MessageLogging(EventId = 11017, Level = LogLevel.Debug,
        Message = "Role {roleName} carries {count} existing permission mapping(s)")]
    public static partial IGenericMessage ExistingRolePermissionsLoaded(ILogger logger, string roleName, int count);

    /// <summary>
    /// Logs what a replacement will do before it does it: how many mappings retire and how many
    /// are applied. Reading this next to 11017 says whether a set actually changed anything.
    /// </summary>
    [MessageLogging(EventId = 11018, Level = LogLevel.Debug,
        Message = "Role {roleName} permission set: retiring {retiring}, applying {applying}")]
    public static partial IGenericMessage RolePermissionDelta(ILogger logger, string roleName, int retiring, int applying);

    /// <summary>
    /// Logs a completed single-role read.
    /// </summary>
    [MessageLogging(EventId = 11019, Level = LogLevel.Information,
        Message = "Retrieved role {roleName} with {permissionCount} permission(s)")]
    public static partial IGenericMessage RoleRetrieved(ILogger logger, string roleName, int permissionCount);

    /// <summary>
    /// Logs a completed role update.
    /// </summary>
    [MessageLogging(EventId = 11020, Level = LogLevel.Information,
        Message = "Updated role {roleName}")]
    public static partial IGenericMessage RoleUpdated(ILogger logger, string roleName);

    /// <summary>
    /// Logs a completed role delete.
    /// </summary>
    [MessageLogging(EventId = 11021, Level = LogLevel.Information,
        Message = "Deleted role {roleName}")]
    public static partial IGenericMessage RoleDeleted(ILogger logger, string roleName);

    /// <summary>
    /// Logs a role write that the store refused. Update and Delete previously answered 400/500 with
    /// nothing recorded at all, so the reason the write failed was lost.
    /// </summary>
    [MessageLogging(EventId = 71007, Level = LogLevel.Error,
        Message = "Failed to {operation} role '{roleName}': {reason}")]
    public static partial IGenericMessage RoleWriteFailed(ILogger logger, string operation, string roleName, string? reason);

    /// <summary>
    /// Logs that the permission catalogue is EMPTY -- the read succeeded and returned nothing.
    /// Critical because it is not a caller error and not a transient one: no permission name can
    /// ever resolve, so every permission set is refused and no role can hold a permission. It means
    /// the permission rows are missing or the entry point registered no permission options, and it
    /// will not recover on its own.
    /// </summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Critical,
        Message = "The permission catalogue is empty; no permission set for role '{roleName}' can be resolved")]
    public static partial IGenericMessage PermissionCatalogEmpty(ILogger logger, string roleName);

    /// <summary>
    /// Logs that the caller omitted the permissions field entirely, which leaves the role's set
    /// untouched. Distinct from an empty list, which is a real request to strip every permission --
    /// the two must never read the same in a log.
    /// </summary>
    [MessageLogging(EventId = 11022, Level = LogLevel.Debug,
        Message = "No permissions field sent; role {roleName} keeps its current set")]
    public static partial IGenericMessage RolePermissionsLeftUnchanged(ILogger logger, string roleName);

    /// <summary>
    /// Logs when a role was removed from a user but their live sessions could not be ended, so tokens
    /// already issued to them still carry the role until they expire.
    /// </summary>
    [MessageLogging(EventId = 3125, Level = LogLevel.Error,
        Message = "Role '{roleName}' was revoked from user '{userId}' but their live sessions could not be ended; existing tokens keep the role until they expire")]
    public static partial IGenericMessage RoleRevokedSessionsNotEnded(ILogger logger, string roleName, string userId);

    /// <summary>
    /// Logs when a role change was made but could not be written to the administrative audit.
    /// </summary>
    [MessageLogging(EventId = 71010, Level = LogLevel.Error,
        Message = "The {changeKind} of role '{roleName}' for user '{userId}' was made but could not be recorded in the administrative audit: {reason}")]
    public static partial IGenericMessage RoleChangeNotAudited(ILogger logger, string changeKind, string roleName, string userId, string? reason);

    /// <summary>
    /// Logs that a change to an organization's roles or members was made but could not be recorded in the administrative audit (FDW-1593).
    /// </summary>
    [MessageLogging(EventId = 71012, Level = LogLevel.Error,
        Message = "The {changeKind} of '{itemId}' in organization '{organizationId}' was made but could not be recorded in the administrative audit: {reason}")]
    public static partial IGenericMessage OrganizationChangeNotAudited(ILogger logger, string changeKind, string itemId, Guid organizationId, string? reason);

    /// <summary>
    /// Logs when a change to a role itself was made but could not be written to the administrative audit.
    /// </summary>
    [MessageLogging(EventId = 71011, Level = LogLevel.Error,
        Message = "The {changeKind} of role '{roleName}' was made but could not be recorded in the administrative audit: {reason}")]
    public static partial IGenericMessage RoleDefinitionNotAudited(ILogger logger, string changeKind, string roleName, string? reason);

    /// <summary>Logs that every holder of a changed or deleted role had their sessions ended.</summary>
    [MessageLogging(EventId = 11595, Level = LogLevel.Information,
        Message = "Ended the live sessions of {holderCount} holder(s) of role '{roleName}'")]
    public static partial IGenericMessage RoleHolderSessionsEnded(ILogger logger, string roleName, int holderCount);

    /// <summary>Logs that the holders of a role could not be read, so their sessions were not ended.</summary>
    [MessageLogging(EventId = 71008, Level = LogLevel.Error,
        Message = "The holders of role '{roleName}' could not be read; their live sessions were not ended")]
    public static partial IGenericMessage RoleHoldersUnreadable(ILogger logger, string roleName);

    /// <summary>Logs that one holder's sessions could not be ended after their role changed.</summary>
    [MessageLogging(EventId = 71009, Level = LogLevel.Error,
        Message = "Role '{roleName}' changed but user '{userId}' still has live sessions carrying the old grants")]
    public static partial IGenericMessage RoleHolderSessionsNotEnded(ILogger logger, string roleName, string userId);

    /// <summary>Logs a holder of a changed role that is not a user account, so it has no session to end.</summary>
    /// <remarks>
    /// Why Information: a holder named by something other than a user's Guid -- a service identity -- is never
    /// issued a locally signed token or a session, so there is nothing of its to end. The other holders still are.
    /// </remarks>
    [MessageLogging(EventId = 11596, Level = LogLevel.Information,
        Message = "Role '{roleName}' changed; holder '{holderId}' is not a user account and has no session to end")]
    public static partial IGenericMessage RoleHolderHasNoSession(ILogger logger, string roleName, string holderId);

    /// <summary>Logs a refused change to a shared role's permissions by a caller who is not a platform administrator.</summary>
    [MessageLogging(EventId = 51020, Level = LogLevel.Warning,
        Message = "Role '{roleName}' is shared by every tenant; only a platform administrator may change its permissions")]
    public static partial IGenericMessage SharedRoleChangeRefused(ILogger logger, string roleName);

    /// <summary>Logs a refused change to a tenant's own role by a caller acting in another tenant.</summary>
    [MessageLogging(EventId = 51021, Level = LogLevel.Warning,
        Message = "Role '{roleName}' belongs to tenant '{roleTenantId}'; it cannot be changed while acting in tenant '{actingTenantId}'")]
    public static partial IGenericMessage TenantRoleChangeRefused(ILogger logger, string roleName, string roleTenantId, string actingTenantId);

    /// <summary>Logs a role whose owning tenant is not recorded, so its permissions cannot be written to it.</summary>
    [MessageLogging(EventId = 61020, Level = LogLevel.Error,
        Message = "Role '{roleName}' has no owning tenant recorded; its permissions cannot be changed until it does")]
    public static partial IGenericMessage RoleTenantNotRecorded(ILogger logger, string roleName);

    /// <summary>Logs that Role was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30001, Level = LogLevel.Warning, Message = "Role was not found, or is not visible to the caller", ClientMessage = "Role was not found")]
    public static partial IGenericMessage GetRolePermissionsRoleNotFound(ILogger logger);

    /// <summary>Logs that User was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30002, Level = LogLevel.Warning, Message = "User was not found, or is not visible to the caller", ClientMessage = "User was not found")]
    public static partial IGenericMessage GetUserRolesUserNotFound(ILogger logger);

    /// <summary>Logs that User was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30003, Level = LogLevel.Warning, Message = "User was not found, or is not visible to the caller", ClientMessage = "User was not found")]
    public static partial IGenericMessage AssignUserRoleUserNotFound(ILogger logger);

    /// <summary>Logs that Role was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30004, Level = LogLevel.Warning, Message = "Role was not found, or is not visible to the caller", ClientMessage = "Role was not found")]
    public static partial IGenericMessage AssignUserRoleRoleNotFound(ILogger logger);

    /// <summary>Logs that User was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30005, Level = LogLevel.Warning, Message = "User was not found, or is not visible to the caller", ClientMessage = "User was not found")]
    public static partial IGenericMessage RevokeUserRoleUserNotFound(ILogger logger);

    /// <summary>Logs that Role was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30006, Level = LogLevel.Warning, Message = "Role was not found, or is not visible to the caller", ClientMessage = "Role was not found")]
    public static partial IGenericMessage RevokeUserRoleRoleNotFound(ILogger logger);

    /// <summary>Logs that User was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30007, Level = LogLevel.Warning, Message = "User was not found, or is not visible to the caller", ClientMessage = "User was not found")]
    public static partial IGenericMessage GiveOrganizationMemberRoleUserNotFound(ILogger logger);

    /// <summary>Logs that The all roles read answered a success and no value.</summary>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error, Message = "The all roles read answered a success and no value", ClientMessage = "The all roles could not be read")]
    public static partial IGenericMessage GetUserRolesAllRolesReturnedNothing(ILogger logger);

    /// <summary>Logs that The user roles read answered a success and no value.</summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error, Message = "The user roles read answered a success and no value", ClientMessage = "The user roles could not be read")]
    public static partial IGenericMessage GetUserRolesUserRolesReturnedNothing(ILogger logger);

    /// <summary>Logs that The all roles read answered a success and no value.</summary>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error, Message = "The all roles read answered a success and no value", ClientMessage = "The all roles could not be read")]
    public static partial IGenericMessage AssignUserRoleAllRolesReturnedNothing(ILogger logger);

    /// <summary>Logs that The user roles read answered a success and no value.</summary>
    [MessageLogging(EventId = 91005, Level = LogLevel.Error, Message = "The user roles read answered a success and no value", ClientMessage = "The user roles could not be read")]
    public static partial IGenericMessage AssignUserRoleUserRolesReturnedNothing(ILogger logger);

    /// <summary>Logs that The all permissions read answered a success and no value.</summary>
    [MessageLogging(EventId = 91006, Level = LogLevel.Error, Message = "The all permissions read answered a success and no value", ClientMessage = "The all permissions could not be read")]
    public static partial IGenericMessage ListPermissionsGroupedAllPermissionsReturnedNothing(ILogger logger);

    /// <summary>Logs that The user roles read answered a success and no value.</summary>
    [MessageLogging(EventId = 91007, Level = LogLevel.Error, Message = "The user roles read answered a success and no value", ClientMessage = "The user roles could not be read")]
    public static partial IGenericMessage RevokeUserRoleUserRolesReturnedNothing(ILogger logger);

    /// <summary>Logs that The existing mappings read answered a success and no value.</summary>
    [MessageLogging(EventId = 91008, Level = LogLevel.Error, Message = "The existing mappings read answered a success and no value", ClientMessage = "The existing mappings could not be read")]
    public static partial IGenericMessage SetRolePermissionsExistingMappingsReturnedNothing(ILogger logger);

    /// <summary>Logs that The current read answered a success and no value.</summary>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error, Message = "The current read answered a success and no value", ClientMessage = "The current could not be read")]
    public static partial IGenericMessage SetRolePermissionsCurrentReturnedNothing(ILogger logger);

    /// <summary>Logs that The current permissions read answered a success and no value.</summary>
    [MessageLogging(EventId = 91010, Level = LogLevel.Error, Message = "The current permissions read answered a success and no value", ClientMessage = "The current permissions could not be read")]
    public static partial IGenericMessage SetRolePermissionsCurrentPermissionsReturnedNothing(ILogger logger);

    /// <summary>Logs that The response read answered a success and no value.</summary>
    [MessageLogging(EventId = 91011, Level = LogLevel.Error, Message = "The response read answered a success and no value", ClientMessage = "The response could not be read")]
    public static partial IGenericMessage GetRoleResponseReturnedNothing(ILogger logger);

    /// <summary>Logs that The role permissions read answered a success and no value.</summary>
    [MessageLogging(EventId = 91012, Level = LogLevel.Error, Message = "The role permissions read answered a success and no value", ClientMessage = "The role permissions could not be read")]
    public static partial IGenericMessage GetRolePermissionsRolePermissionsReturnedNothing(ILogger logger);

    /// <summary>Logs that The permissions read answered a success and no value.</summary>
    [MessageLogging(EventId = 91013, Level = LogLevel.Error, Message = "The permissions read answered a success and no value", ClientMessage = "The permissions could not be read")]
    public static partial IGenericMessage GetRolePermissionsPermissionsReturnedNothing(ILogger logger);

    /// <summary>Logs that Role 'roleName' to update was not found.</summary>
    [MessageLogging(EventId = 30008, Level = LogLevel.Warning, Message = "Role '{roleName}' to update was not found", ClientMessage = "Role '{roleName}' was not found")]
    public static partial IGenericMessage UpdateRoleRoleNotFound(ILogger logger, string roleName);

    /// <summary>Logs that Role 'roleName' was not found.</summary>
    [MessageLogging(EventId = 30009, Level = LogLevel.Warning, Message = "Role '{roleName}' was not found", ClientMessage = "Role '{roleName}' was not found")]
    public static partial IGenericMessage GetRoleRoleNotFound2(ILogger logger, string roleName);

    /// <summary>Logs that Role 'roleName' to delete was not found.</summary>
    [MessageLogging(EventId = 30010, Level = LogLevel.Warning, Message = "Role '{roleName}' to delete was not found", ClientMessage = "Role '{roleName}' was not found")]
    public static partial IGenericMessage DeleteRoleRoleNotFound(ILogger logger, string roleName);

    /// <summary>Logs that Role 'roleName' whose permissions are set was not found.</summary>
    [MessageLogging(EventId = 30011, Level = LogLevel.Warning, Message = "Role '{roleName}' whose permissions are set was not found", ClientMessage = "Role '{roleName}' was not found")]
    public static partial IGenericMessage SetRolePermissionsRoleNotFound(ILogger logger, string roleName);

    /// <summary>Logs that Role 'roleName' is a built-in system role; its permissions were not changed.</summary>
    [MessageLogging(EventId = 100001, Level = LogLevel.Warning, Message = "Role '{roleName}' is a built-in system role; its permissions were not changed", ClientMessage = "Role '{roleName}' is a built-in system role and its permissions cannot be modified through this endpoint")]
    public static partial IGenericMessage SetRolePermissionsSystemRoleImmutable(ILogger logger, string roleName);

    /// <summary>Logs that Permission set for role 'roleName' refused: names did not resolve.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Warning, Message = "Permission set for role '{roleName}' refused: {names} did not resolve", ClientMessage = "Permission(s) not found: {names}")]
    public static partial IGenericMessage SetRolePermissionsUnknownPermission(ILogger logger, string roleName, string names);

    /// <summary>Logs that Role 'roleName' is shared by every tenant; only a platform administrator may change its permissions.</summary>
    [MessageLogging(EventId = 100002, Level = LogLevel.Warning, Message = "Role '{roleName}' is shared by every tenant; only a platform administrator may change its permissions", ClientMessage = "Role '{roleName}' is shared by every tenant; only a platform administrator may change its permissions")]
    public static partial IGenericMessage SetRolePermissionsSharedRoleRoleIsSharedByEvery(ILogger logger, string roleName);

    /// <summary>Logs that Role 'roleName' belongs to another tenant and cannot be changed from the acting tenant.</summary>
    [MessageLogging(EventId = 100003, Level = LogLevel.Warning, Message = "Role '{roleName}' belongs to another tenant and cannot be changed from the acting tenant", ClientMessage = "Role '{roleName}' belongs to another tenant and cannot be changed from this one")]
    public static partial IGenericMessage SetRolePermissionsOtherTenantRoleRoleBelongsToAnotherTenant(ILogger logger, string roleName);

    /// <summary>Logs that The permission catalogue read for role 'roleName' answered a success and no value.</summary>
    [MessageLogging(EventId = 91014, Level = LogLevel.Error, Message = "The permission catalogue read for role '{roleName}' answered a success and no value", ClientMessage = "The permissions could not be read")]
    public static partial IGenericMessage SetRolePermissionsPermissionCatalogReturnedNothing(ILogger logger, string roleName);

    /// <summary>Logs that The permission catalogue is empty, so no permission of role 'roleName' can resolve.</summary>
    [MessageLogging(EventId = 91015, Level = LogLevel.Error, Message = "The permission catalogue is empty, so no permission of role '{roleName}' can resolve", ClientMessage = "The permission catalogue is empty, so no permission can be set")]
    public static partial IGenericMessage SetRolePermissionsPermissionCatalogEmpty(ILogger logger, string roleName);

    /// <summary>Logs that Role 'roleName' records no tenant, so its change cannot be placed.</summary>
    [MessageLogging(EventId = 91016, Level = LogLevel.Error, Message = "Role '{roleName}' records no tenant, so its change cannot be placed", ClientMessage = "The role records no tenant, so its permissions cannot be changed")]
    public static partial IGenericMessage SetRolePermissionsRoleTenantNotRecorded(ILogger logger, string roleName);

    /// <summary>Logs that The request to revoke user role was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 100004, Level = LogLevel.Warning, Message = "The request to revoke user role was refused: the caller is not allowed", ClientMessage = "You are not allowed to revoke user role")]
    public static partial IGenericMessage RevokeUserRoleForbidden(ILogger logger);

    /// <summary>Logs that The request to assign user role was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 100005, Level = LogLevel.Warning, Message = "The request to assign user role was refused: the caller is not allowed", ClientMessage = "You are not allowed to assign user role")]
    public static partial IGenericMessage AssignUserRoleForbidden(ILogger logger);

    /// <summary>Logs that A role was not created: IsTenantScoped was not stated.</summary>
    [MessageLogging(EventId = 20002, Level = LogLevel.Warning, Message = "A role was not created: IsTenantScoped was not stated", ClientMessage = "IsTenantScoped is required")]
    public static partial IGenericMessage CreateRoleIsTenantScopedRequired(ILogger logger);

    /// <summary>Logs that The user-role assignments read answered a success and no value.</summary>
    [MessageLogging(EventId = 91017, Level = LogLevel.Error, Message = "The user-role assignments read answered a success and no value", ClientMessage = "The user roles could not be read")]
    public static partial IGenericMessage ListAllUserRolesAssignmentsReturnedNothing(ILogger logger);

    /// <summary>Logs that The roles read for the user-roles list answered a success and no value.</summary>
    [MessageLogging(EventId = 91018, Level = LogLevel.Error, Message = "The roles read for the user-roles list answered a success and no value", ClientMessage = "The user roles could not be read")]
    public static partial IGenericMessage ListAllUserRolesRolesReturnedNothing(ILogger logger);

    /// <summary>Logs that get user roles failed unexpectedly.</summary>
    [MessageLogging(EventId = 91019, Level = LogLevel.Error, Message = "Get user roles failed unexpectedly", ClientMessage = "Get user roles failed unexpectedly")]
    public static partial IGenericMessage GetUserRolesFaulted(ILogger logger);

    /// <summary>Logs that revoke user role failed unexpectedly.</summary>
    [MessageLogging(EventId = 91020, Level = LogLevel.Error, Message = "Revoke user role failed unexpectedly", ClientMessage = "Revoke user role failed unexpectedly")]
    public static partial IGenericMessage RevokeUserRoleFaulted(ILogger logger);

    /// <summary>Logs that assign user role failed unexpectedly.</summary>
    [MessageLogging(EventId = 91021, Level = LogLevel.Error, Message = "Assign user role failed unexpectedly", ClientMessage = "Assign user role failed unexpectedly")]
    public static partial IGenericMessage AssignUserRoleFaulted(ILogger logger);
}
