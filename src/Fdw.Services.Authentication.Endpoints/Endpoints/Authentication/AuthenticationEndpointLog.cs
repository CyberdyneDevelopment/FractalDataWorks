using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Endpoints;

/// <summary>
/// MessageLogging for authentication endpoint operations.
/// EventId range: 7110-7130
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS4")]
public static partial class AuthenticationEndpointLog
{
    /// <summary>
    /// Logged at Information level when a user authentication attempt begins.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Information,
        Message = "Authenticating user '{username}'")]
    public static partial IGenericMessage AuthenticatingUser(
        ILogger logger,
        string username);

    /// <summary>
    /// Logged at Information level when authentication succeeds.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "Authentication succeeded for user '{username}'")]
    public static partial IGenericMessage AuthenticationSucceeded(
        ILogger logger,
        string username);

    /// <summary>
    /// Logged at Warning level when authentication fails.
    /// </summary>
    [MessageLogging(
        EventId = 51000,
        Level = LogLevel.Warning,
        Message = "Authentication failed for user '{username}'")]
    public static partial IGenericMessage AuthenticationFailed(
        ILogger logger,
        string username);

    /// <summary>
    /// Logged at Debug level when a token refresh is requested.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Debug,
        Message = "Refreshing authentication token")]
    public static partial IGenericMessage RefreshingToken(
        ILogger logger);

    /// <summary>
    /// Logged at Information level when a token is refreshed successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Information,
        Message = "Authentication token refreshed successfully")]
    public static partial IGenericMessage TokenRefreshed(
        ILogger logger);

    /// <summary>
    /// Logged at Warning level when a token refresh fails.
    /// </summary>
    [MessageLogging(
        EventId = 51001,
        Level = LogLevel.Warning,
        Message = "Token refresh failed: {reason}")]
    public static partial IGenericMessage TokenRefreshFailed(
        ILogger logger,
        string reason);

    /// <summary>
    /// Logged at Debug level when a logout is requested.
    /// </summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Debug,
        Message = "Logging out user '{username}'")]
    public static partial IGenericMessage LoggingOut(
        ILogger logger,
        string username);

    /// <summary>
    /// Logged at Information level when a logout completes.
    /// </summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Information,
        Message = "Logout completed for user '{username}'")]
    public static partial IGenericMessage LogoutCompleted(
        ILogger logger,
        string username);

    /// <summary>
    /// Logged at Debug level when user info is being retrieved.
    /// </summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Debug,
        Message = "Getting user info for current user")]
    public static partial IGenericMessage GettingUserInfo(
        ILogger logger);

    /// <summary>
    /// Logged at Debug level when user info is retrieved.
    /// </summary>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Debug,
        Message = "User info retrieved for '{username}'")]
    public static partial IGenericMessage UserInfoRetrieved(
        ILogger logger,
        string username);

    /// <summary>
    /// Logged at Error level when an exception occurs during authentication operations.
    /// </summary>
    [MessageLogging(
        EventId = 51002,
        Level = LogLevel.Error,
        Message = "Authentication exception during '{operation}'")]
    public static partial IGenericMessage AuthenticationException(
        ILogger logger,
        Exception ex,
        string operation);

    /// <summary>
    /// Logged at Warning level when the authentication service is not available.
    /// </summary>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Warning,
        Message = "Authentication service not available: {reason}")]
    public static partial IGenericMessage AuthenticationServiceNotAvailable(
        ILogger logger,
        string reason);

    /// <summary>
    /// Logged at Warning level when the user identity is not found for an authenticated request.
    /// </summary>
    [MessageLogging(
        EventId = 51003,
        Level = LogLevel.Warning,
        Message = "User identity not found in authentication context")]
    public static partial IGenericMessage UserIdentityNotFound(
        ILogger logger);

    // ═══════════════════════════════════════════════════════════════════════════
    // Password operations (7123-7126)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logged at Trace level when a password change is requested.
    /// </summary>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Trace,
        Message = "Password change requested for user '{username}'")]
    public static partial IGenericMessage PasswordChangeRequested(
        ILogger logger,
        string username);

    /// <summary>
    /// Logged at Information level when a password is changed successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Information,
        Message = "Password changed successfully for user '{username}'")]
    public static partial IGenericMessage PasswordChanged(
        ILogger logger,
        string username);

    /// <summary>
    /// Logged at Warning level when a password change fails.
    /// </summary>
    [MessageLogging(
        EventId = 51004,
        Level = LogLevel.Warning,
        Message = "Password change failed for user '{username}'")]
    public static partial IGenericMessage PasswordChangeFailed(
        ILogger logger,
        string username);

    /// <summary>
    /// Logged at Trace level when a password reset is requested by an admin.
    /// </summary>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Trace,
        Message = "Password reset requested for user '{userId}' by admin '{admin}'")]
    public static partial IGenericMessage PasswordResetRequested(
        ILogger logger,
        string userId,
        string admin);

    /// <summary>
    /// Logged at Information level when a password reset is completed.
    /// </summary>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Information,
        Message = "Password reset completed for user '{userId}'")]
    public static partial IGenericMessage PasswordResetCompleted(
        ILogger logger,
        string userId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Personal access token operations (7128-7131)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logged at Trace level when creating a personal access token.
    /// </summary>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Trace,
        Message = "Creating personal access token for user '{username}'")]
    public static partial IGenericMessage CreatingPersonalAccessToken(
        ILogger logger,
        string username);

    /// <summary>
    /// Logged at Information level when a personal access token is created.
    /// </summary>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Information,
        Message = "Personal access token created for user '{username}'")]
    public static partial IGenericMessage PersonalAccessTokenCreated(
        ILogger logger,
        string username);

    /// <summary>
    /// Logged at Trace level when listing personal access tokens.
    /// </summary>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Trace,
        Message = "Listing personal access tokens for user '{username}'")]
    public static partial IGenericMessage ListingPersonalAccessTokens(
        ILogger logger,
        string username);

    /// <summary>
    /// Logged at Trace level when revoking a personal access token.
    /// </summary>
    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Trace,
        Message = "Revoking personal access token '{tokenId}' for user '{username}'")]
    public static partial IGenericMessage RevokingPersonalAccessToken(
        ILogger logger,
        string tokenId,
        string username);

    // ═══════════════════════════════════════════════════════════════════════════
    // Agent key operations (7132-7135)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logged at Trace level when creating an agent key.
    /// </summary>
    [MessageLogging(
        EventId = 11016,
        Level = LogLevel.Trace,
        Message = "Creating agent key '{label}'")]
    public static partial IGenericMessage CreatingAgentKey(
        ILogger logger,
        string label);

    /// <summary>
    /// Logged at Information level when an agent key is created.
    /// </summary>
    [MessageLogging(
        EventId = 11017,
        Level = LogLevel.Information,
        Message = "Agent key '{label}' created")]
    public static partial IGenericMessage AgentKeyCreated(
        ILogger logger,
        string label);

    /// <summary>
    /// Logged at Trace level when listing agent keys.
    /// </summary>
    [MessageLogging(
        EventId = 11018,
        Level = LogLevel.Trace,
        Message = "Listing agent keys")]
    public static partial IGenericMessage ListingAgentKeys(
        ILogger logger);

    /// <summary>
    /// Logged at Trace level when deleting an agent key.
    /// </summary>
    [MessageLogging(
        EventId = 11019,
        Level = LogLevel.Trace,
        Message = "Deleting agent key '{keyId}'")]
    public static partial IGenericMessage DeletingAgentKey(
        ILogger logger,
        string keyId);

    // ═══════════════════════════════════════════════════════════════════════════
    // User preference operations (7136-7137)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logged at Trace level when getting user preferences.
    /// </summary>
    [MessageLogging(
        EventId = 11020,
        Level = LogLevel.Trace,
        Message = "Getting preferences for user '{username}'")]
    public static partial IGenericMessage GettingUserPreferences(
        ILogger logger,
        string username);

    /// <summary>
    /// Logged at Trace level when updating user preferences.
    /// </summary>
    [MessageLogging(
        EventId = 11021,
        Level = LogLevel.Trace,
        Message = "Updating preferences for user '{username}'")]
    public static partial IGenericMessage UpdatingUserPreferences(
        ILogger logger,
        string username);
}
