using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;
using System;

namespace Fdw.Services.Users.Logging;

/// <summary>
/// MessageLogging for User operations.
/// EventId range: 7800-7849
/// </summary>
[MessageLoggingTypeCode("USERS")]
public static partial class UserLog
{
    // ========================================================================
    // Scopes for structured logging context
    // ========================================================================

    /// <summary>
    /// Creates a logging scope for user operations.
    /// </summary>
    public static IDisposable? BeginUserScope(this ILogger logger, string username)
        => logger.BeginScope(new { Username = username, Operation = "User" });

    /// <summary>
    /// Creates a logging scope for user operations by ID.
    /// </summary>
    public static IDisposable? BeginUserScope(this ILogger logger, Guid userId)
        => logger.BeginScope(new { UserId = userId, Operation = "User" });

    /// <summary>
    /// Creates a logging scope for credential validation.
    /// </summary>
    public static IDisposable? BeginCredentialValidationScope(this ILogger logger, string username)
        => logger.BeginScope(new { Username = username, Operation = "CredentialValidation" });

    /// <summary>
    /// Creates a logging scope for tenant-scoped user operations.
    /// </summary>
    public static IDisposable? BeginTenantUserScope(this ILogger logger, Guid tenantId)
        => logger.BeginScope(new { TenantId = tenantId, Operation = "TenantUserQuery" });

    /// <summary>
    /// Creates a logging scope for role operations.
    /// </summary>
    public static IDisposable? BeginRoleScope(this ILogger logger, Guid userId, string role)
        => logger.BeginScope(new { UserId = userId, Role = role, Operation = "Role" });

    /// <summary>
    /// Creates a logging scope for tenant access operations.
    /// </summary>
    public static IDisposable? BeginTenantAccessScope(this ILogger logger, Guid userId, Guid tenantId)
        => logger.BeginScope(new { UserId = userId, TenantId = tenantId, Operation = "TenantAccess" });

    // ========================================================================
    // Query Operations
    // ========================================================================

    /// <summary>
    /// Logs that a user query failed.
    /// </summary>
    [MessageLogging(
        EventId = 71009,
        Level = LogLevel.Error,
        Message = "Failed to query user '{username}'")]
    public static partial IGenericMessage UserQueryFailed(ILogger logger, string username);

    /// <summary>
    /// Logs that a user query by ID returned a failure result.
    /// </summary>
    [MessageLogging(
        EventId = 71010,
        Level = LogLevel.Warning,
        Message = "User query for ID '{userId}' failed: {error}")]
    public static partial IGenericMessage UserQueryByIdResultFailed(ILogger logger, Guid userId, string error);

    /// <summary>
    /// Logs user query exception by ID.
    /// </summary>
    [MessageLogging(
        EventId = 71011,
        Level = LogLevel.Error,
        Message = "Exception querying user by ID '{userId}'")]
    public static partial IGenericMessage UserQueryByIdFailed(ILogger logger, Exception ex, Guid userId);

    /// <summary>
    /// Logs user query exception by username.
    /// </summary>
    [MessageLogging(
        EventId = 71012,
        Level = LogLevel.Error,
        Message = "Exception querying user by username '{username}'")]
    public static partial IGenericMessage UserQueryByUsernameFailed(ILogger logger, Exception ex, string username);

    /// <summary>
    /// Logs failure to query all users.
    /// </summary>
    [MessageLogging(
        EventId = 71013,
        Level = LogLevel.Error,
        Message = "Failed to query all users")]
    public static partial IGenericMessage UserQueryAllFailed(ILogger logger, Exception ex);

    /// <summary>
    /// Logs tenant-filtered user query failure.
    /// </summary>
    [MessageLogging(
        EventId = 71014,
        Level = LogLevel.Error,
        Message = "Failed to query users for tenant '{tenantId}'")]
    public static partial IGenericMessage TenantUserQueryFailed(ILogger logger, Exception ex, Guid tenantId);

    /// <summary>
    /// Logs failure to look up a single user by ID within a batch operation.
    /// </summary>
    [MessageLogging(
        EventId = 71015,
        Level = LogLevel.Warning,
        Message = "Failed to look up user '{userId}': {error}")]
    public static partial IGenericMessage UserLookupFailed(ILogger logger, Guid userId, string error);

    /// <summary>
    /// Logs that the security-columns lookup for a user during credential verification FAILED at the
    /// provider/gateway (e.g. a transient DB error) — distinct from a successful lookup that found no
    /// user. This must fail loud rather than be treated as "unknown account" (that would mask an
    /// infrastructure outage behind the anti-enumeration decoy).
    /// </summary>
    [MessageLogging(
        EventId = 71035,
        Level = LogLevel.Error,
        Message = "User security lookup failed for user '{userId}' during credential verification")]
    public static partial IGenericMessage UserSecurityLookupFailed(ILogger logger, Guid userId);

    // ========================================================================
    // Authentication Operations
    // ========================================================================

    /// <summary>
    /// Logs invalid credentials for a user.
    /// </summary>
    [MessageLogging(
        EventId = 51000,
        Level = LogLevel.Warning,
        Message = "Invalid credentials for user '{username}'")]
    public static partial IGenericMessage InvalidCredentials(ILogger logger, string username);

    /// <summary>
    /// Logs that a user account is inactive.
    /// </summary>
    [MessageLogging(
        EventId = 51001,
        Level = LogLevel.Warning,
        Message = "User '{username}' is inactive")]
    public static partial IGenericMessage UserInactive(ILogger logger, string username);

    /// <summary>
    /// Logs successful user authentication.
    /// </summary>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Information,
        Message = "User '{username}' authenticated successfully")]
    public static partial IGenericMessage UserAuthenticated(ILogger logger, string username);

    /// <summary>
    /// Logs credential validation failure.
    /// </summary>
    [MessageLogging(
        EventId = 51002,
        Level = LogLevel.Error,
        Message = "Credential validation failed for user '{username}'")]
    public static partial IGenericMessage CredentialValidationFailed(ILogger logger, Exception ex, string username);

    // ========================================================================
    // CRUD Operations
    // ========================================================================

    /// <summary>
    /// Logs successful user creation.
    /// </summary>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Information,
        Message = "User '{username}' created with ID {userId}")]
    public static partial IGenericMessage UserCreated(ILogger logger, string username, Guid userId);

    /// <summary>
    /// Logs user creation failure.
    /// </summary>
    [MessageLogging(
        EventId = 71016,
        Level = LogLevel.Error,
        Message = "Failed to create user '{username}'")]
    public static partial IGenericMessage UserCreateFailed(ILogger logger, Exception ex, string username);

    /// <summary>
    /// Logs user update failure.
    /// </summary>
    [MessageLogging(
        EventId = 71017,
        Level = LogLevel.Error,
        Message = "Failed to update user '{userId}'")]
    public static partial IGenericMessage UserUpdateFailed(ILogger logger, Exception ex, Guid userId);

    /// <summary>
    /// Logs user deletion failure.
    /// </summary>
    [MessageLogging(
        EventId = 71018,
        Level = LogLevel.Error,
        Message = "Failed to delete user '{userId}'")]
    public static partial IGenericMessage UserDeleteFailed(ILogger logger, Exception ex, Guid userId);

    /// <summary>
    /// Logs last login update failure.
    /// </summary>
    [MessageLogging(
        EventId = 71019,
        Level = LogLevel.Error,
        Message = "Failed to update last login for user '{userId}'")]
    public static partial IGenericMessage LastLoginUpdateFailed(ILogger logger, Exception ex, Guid userId);

    // ========================================================================
    // Role Operations
    // ========================================================================

    /// <summary>
    /// Logs role query failure.
    /// </summary>
    [MessageLogging(
        EventId = 71020,
        Level = LogLevel.Error,
        Message = "Failed to query roles for user '{userId}'")]
    public static partial IGenericMessage RoleQueryFailed(ILogger logger, Exception ex, Guid userId);

    /// <summary>
    /// Logs role assignment failure.
    /// </summary>
    [MessageLogging(
        EventId = 71021,
        Level = LogLevel.Error,
        Message = "Failed to assign role '{role}' to user '{userId}'")]
    public static partial IGenericMessage RoleAssignFailed(ILogger logger, Exception ex, string role, Guid userId);

    /// <summary>
    /// Logs role removal failure.
    /// </summary>
    [MessageLogging(
        EventId = 71022,
        Level = LogLevel.Error,
        Message = "Failed to remove role '{role}' from user '{userId}'")]
    public static partial IGenericMessage RoleRemoveFailed(ILogger logger, Exception ex, string role, Guid userId);

    // ========================================================================
    // Tenant Access Operations
    // ========================================================================

    /// <summary>
    /// Logs tenant query failure.
    /// </summary>
    [MessageLogging(
        EventId = 71023,
        Level = LogLevel.Error,
        Message = "Failed to query tenants for user '{userId}'")]
    public static partial IGenericMessage TenantQueryFailed(ILogger logger, Exception ex, Guid userId);

    /// <summary>
    /// Logs tenant access grant failure.
    /// </summary>
    [MessageLogging(
        EventId = 71024,
        Level = LogLevel.Error,
        Message = "Failed to grant tenant '{tenantId}' access to user '{userId}'")]
    public static partial IGenericMessage TenantGrantFailed(ILogger logger, Exception ex, Guid tenantId, Guid userId);

    /// <summary>
    /// Logs tenant access revocation failure.
    /// </summary>
    [MessageLogging(
        EventId = 71025,
        Level = LogLevel.Error,
        Message = "Failed to revoke tenant '{tenantId}' access from user '{userId}'")]
    public static partial IGenericMessage TenantRevokeFailed(ILogger logger, Exception ex, Guid tenantId, Guid userId);

    /// <summary>
    /// Logs failure to query the user's default tenant.
    /// </summary>
    [MessageLogging(
        EventId = 71026,
        Level = LogLevel.Error,
        Message = "Failed to query default tenant for user '{userId}'")]
    public static partial IGenericMessage DefaultTenantQueryFailed(ILogger logger, Exception ex, Guid userId);

    // ========================================================================
    // Password Operations (EventId 7820-7829)
    // ========================================================================

    /// <summary>
    /// Logs password reset failure.
    /// </summary>
    [MessageLogging(
        EventId = 71027,
        Level = LogLevel.Error,
        Message = "Failed to reset password for user '{userId}'")]
    public static partial IGenericMessage PasswordResetFailed(ILogger logger, Exception ex, Guid userId);

    // ========================================================================
    // User Preference Operations (EventId 7830-7849)
    // ========================================================================

    /// <summary>
    /// Logs preference query failure.
    /// </summary>
    [MessageLogging(
        EventId = 71028,
        Level = LogLevel.Error,
        Message = "Failed to query preferences for user '{userId}'")]
    public static partial IGenericMessage PreferenceQueryFailed(ILogger logger, Exception ex, Guid userId);

    /// <summary>
    /// Logs preference set failure.
    /// </summary>
    [MessageLogging(
        EventId = 71029,
        Level = LogLevel.Error,
        Message = "Failed to set preference '{key}' for user '{userId}'")]
    public static partial IGenericMessage PreferenceSetFailed(ILogger logger, Exception ex, string key, Guid userId);

    /// <summary>
    /// Logs preference delete failure.
    /// </summary>
    [MessageLogging(
        EventId = 71030,
        Level = LogLevel.Error,
        Message = "Failed to delete preference '{key}' for user '{userId}'")]
    public static partial IGenericMessage PreferenceDeleteFailed(ILogger logger, Exception ex, string key, Guid userId);

    // ========================================================================
    // Default Tenant Operations (EventId 7833-7834)
    // ========================================================================

    /// <summary>
    /// Logs failure to set the user's default tenant.
    /// </summary>
    [MessageLogging(
        EventId = 71031,
        Level = LogLevel.Error,
        Message = "Failed to set default tenant '{tenantId}' for user '{userId}'")]
    public static partial IGenericMessage SetDefaultTenantFailed(ILogger logger, Exception ex, Guid tenantId, Guid userId);

    /// <summary>
    /// Logs that the user is not a member of the requested default tenant.
    /// </summary>
    [MessageLogging(
        EventId = 41001,
        Level = LogLevel.Warning,
        Message = "User '{userId}' is not a member of tenant '{tenantId}'; cannot set as default")]
    public static partial IGenericMessage SetDefaultTenantNotMember(ILogger logger, Guid tenantId, Guid userId);

    // ========================================================================
    // Trace decision-flow (EventId 7835-7849)
    // ========================================================================

    /// <summary>Traces a user-by-id query about to execute.</summary>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Trace,
        Message = "User query by id: dataStore={dataStore} path={path} container={container} userId={userId}.")]
    public static partial IGenericMessage UserQueryByIdTrace(ILogger logger, string dataStore, string path, string container, Guid userId);

    /// <summary>Traces the outcome of a user-by-id query.</summary>
    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Trace,
        Message = "User query by id result: userId={userId} found={found}.")]
    public static partial IGenericMessage UserQueryByIdResultTrace(ILogger logger, Guid userId, bool found);

    /// <summary>Traces a user-by-username query about to execute.</summary>
    [MessageLogging(
        EventId = 11016,
        Level = LogLevel.Trace,
        Message = "User query by username: dataStore={dataStore} path={path} container={container} username={username}.")]
    public static partial IGenericMessage UserQueryByUsernameTrace(ILogger logger, string dataStore, string path, string container, string username);

    /// <summary>Traces the outcome of a user-by-username query.</summary>
    [MessageLogging(
        EventId = 11017,
        Level = LogLevel.Trace,
        Message = "User query by username result: username={username} found={found}.")]
    public static partial IGenericMessage UserQueryByUsernameResultTrace(ILogger logger, string username, bool found);

    /// <summary>Traces which list branch (tenant-scoped vs all-users) was taken.</summary>
    [MessageLogging(
        EventId = 11018,
        Level = LogLevel.Trace,
        Message = "User list branch: branch={branch} tenantId={tenantId}.")]
    public static partial IGenericMessage UserListBranchTrace(ILogger logger, string branch, Guid tenantId);

    /// <summary>Traces the number of users returned by a list query.</summary>
    [MessageLogging(
        EventId = 11019,
        Level = LogLevel.Trace,
        Message = "User list count: users={count}.")]
    public static partial IGenericMessage UserListCountTrace(ILogger logger, int count);

    /// <summary>Traces user-tenant membership lookup outcome.</summary>
    [MessageLogging(
        EventId = 11020,
        Level = LogLevel.Trace,
        Message = "User-tenant lookup: userId={userId} tenantsFound={count}.")]
    public static partial IGenericMessage UserTenantsTrace(ILogger logger, Guid userId, int count);

    /// <summary>Traces default-tenant resolution outcome.</summary>
    [MessageLogging(
        EventId = 11021,
        Level = LogLevel.Trace,
        Message = "Default tenant resolve: userId={userId} hasDefault={hasDefault} tenantId={tenantId}.")]
    public static partial IGenericMessage DefaultTenantTrace(ILogger logger, Guid userId, bool hasDefault, Guid tenantId);

    /// <summary>Traces a user-role assignment lookup outcome.</summary>
    [MessageLogging(
        EventId = 11022,
        Level = LogLevel.Trace,
        Message = "User-role lookup: userId={userId} rolesFound={count}.")]
    public static partial IGenericMessage UserRolesTrace(ILogger logger, Guid userId, int count);

    // ========================================================================
    // Credential Vault Command Operations (EventId 7844-7863)
    // ========================================================================

    /// <summary>
    /// Logs that the hash algorithm stored on the credential record is not registered in the
    /// PasswordHashAlgorithms TypeCollection. Indicates a data or deployment configuration error.
    /// </summary>
    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Error,
        Message = "Hash algorithm '{algorithmName}' not found in PasswordHashAlgorithms for user {userId} type {secretType}")]
    public static partial IGenericMessage VaultAlgorithmNotFound(ILogger logger, Guid userId, string secretType, string algorithmName);

    /// <summary>
    /// Logs that credential verification failed (plaintext did not match the stored hash).
    /// </summary>
    [MessageLogging(
        EventId = 51003,
        Level = LogLevel.Warning,
        Message = "Vault credential verification failed for user {userId} type {secretType}")]
    public static partial IGenericMessage VaultVerificationFailed(ILogger logger, Guid userId, string secretType);

    /// <summary>
    /// Logs that retiring the current credential row(s) failed before a create operation.
    /// </summary>
    [MessageLogging(
        EventId = 71032,
        Level = LogLevel.Error,
        Message = "Failed to retire current credential in vault for user {userId} type {secretType}")]
    public static partial IGenericMessage VaultRetireCurrentFailed(ILogger logger, Guid userId, string secretType);

    /// <summary>
    /// Logs that inserting the new credential record failed.
    /// </summary>
    [MessageLogging(
        EventId = 71033,
        Level = LogLevel.Error,
        Message = "Failed to store credential in vault for user {userId} type {secretType}")]
    public static partial IGenericMessage VaultStoreFailed(ILogger logger, Guid userId, string secretType);

    /// <summary>
    /// Logs that a new credential was successfully stored in the vault.
    /// </summary>
    [MessageLogging(
        EventId = 11024,
        Level = LogLevel.Information,
        Message = "Credential stored in vault for user {userId} type {secretType} algorithm {algorithmName}")]
    public static partial IGenericMessage VaultStored(ILogger logger, Guid userId, string secretType, string algorithmName);

    /// <summary>
    /// Logs that a retire command found no current credential — idempotent success.
    /// </summary>
    [MessageLogging(
        EventId = 11025,
        Level = LogLevel.Information,
        Message = "No current credential to retire in vault for user {userId} type {secretType} (idempotent)")]
    public static partial IGenericMessage VaultRetireNoCurrent(ILogger logger, Guid userId, string secretType);

    /// <summary>
    /// Logs that the current credential was successfully retired.
    /// </summary>
    [MessageLogging(
        EventId = 11026,
        Level = LogLevel.Information,
        Message = "Credential retired in vault for user {userId} type {secretType}")]
    public static partial IGenericMessage VaultRetired(ILogger logger, Guid userId, string secretType);

    /// <summary>
    /// Logs that the CredentialServiceName configuration is missing or blank — startup validation failure.
    /// </summary>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Critical,
        Message = "UsersServiceOptions.CredentialServiceName is missing or blank; credential operations cannot proceed")]
    public static partial IGenericMessage CredentialServiceNameMissing(ILogger logger);

    /// <summary>
    /// Logs that credential service resolution failed for the configured service name.
    /// </summary>
    [MessageLogging(
        EventId = 61001,
        Level = LogLevel.Error,
        Message = "Failed to resolve credential service '{serviceName}' for user {userId} type {secretType}")]
    public static partial IGenericMessage CredentialServiceResolveFailed(ILogger logger, string serviceName, Guid userId, string secretType);

    // ========================================================================
    // Credential edge: hash-on-arrival + outcome composition (EventId 7854-7860)
    // ========================================================================

    /// <summary>Traces the composed credential outcome for a user (internal diagnostics, not caller-facing).</summary>
    [MessageLogging(
        EventId = 11027,
        Level = LogLevel.Debug,
        Message = "Credential outcome for user {userId}: {outcome}")]
    public static partial IGenericMessage CredentialOutcomeComposed(ILogger logger, Guid userId, string outcome);

    /// <summary>
    /// Logs a uniform, generic authentication-denied message. Deliberately does NOT name the specific
    /// reason (invalid / expired / locked / unknown) so the log cannot enumerate accounts or cases.
    /// </summary>
    [MessageLogging(
        EventId = 51004,
        Level = LogLevel.Warning,
        Message = "Authentication denied for user {userId}")]
    public static partial IGenericMessage AuthenticationDenied(ILogger logger, Guid userId);

    /// <summary>Logs that consecutive failures crossed the threshold and the account was locked out.</summary>
    [MessageLogging(
        EventId = 51005,
        Level = LogLevel.Warning,
        Message = "Account locked for user {userId} after reaching the failed-attempt threshold")]
    public static partial IGenericMessage AccountLockedOut(ILogger logger, Guid userId);

    /// <summary>Logs that updating the lockout counter for a user failed.</summary>
    [MessageLogging(
        EventId = 71034,
        Level = LogLevel.Error,
        Message = "Failed to update the lockout counter for user {userId}")]
    public static partial IGenericMessage LockoutCounterUpdateFailed(ILogger logger, Guid userId);

    /// <summary>Logs that the user record (or its salt) was absent — the decoy KDF ran for uniform timing.</summary>
    [MessageLogging(
        EventId = 11028,
        Level = LogLevel.Debug,
        Message = "No password secret on file for user {userId}; ran decoy KDF for uniform timing")]
    public static partial IGenericMessage NoPasswordOnFileDecoy(ILogger logger, Guid userId);

    /// <summary>Logs that Verify was called for a secret type this edge does not vault-back (e.g. AgentKey).</summary>
    [MessageLogging(
        EventId = 61002,
        Level = LogLevel.Error,
        Message = "Credential verify is not supported for secret type '{secretType}' (user {userId}) — only password is vault-backed here")]
    public static partial IGenericMessage SecretTypeNotSupported(ILogger logger, Guid userId, string secretType);

    /// <summary>Logs that the password policy is misconfigured (e.g. a positive threshold with a non-positive duration).</summary>
    [MessageLogging(
        EventId = 61003,
        Level = LogLevel.Critical,
        Message = "Password policy is misconfigured: {detail}")]
    public static partial IGenericMessage PasswordPolicyInvalid(ILogger logger, string detail);
}
