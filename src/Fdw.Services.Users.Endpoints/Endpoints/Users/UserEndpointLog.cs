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

    /// <summary>Refuses a colour mode that is not one of the three a person can choose.</summary>
    [MessageLogging(EventId = 21040, Level = LogLevel.Warning,
        Message = "Colour mode '{colorMode}' was refused: it is not light, dark or system",
        ClientMessage = "colorMode must be light, dark or system; '{colorMode}' is not one of them")]
    public static partial IGenericMessage ColorModeNotOffered(ILogger logger, string colorMode);

    /// <summary>Refuses an update that states a colour mode and a dark mode flag that mean different things.</summary>
    [MessageLogging(EventId = 21041, Level = LogLevel.Warning,
        Message = "Colour mode '{colorMode}' was refused: darkMode {darkMode} states the opposite",
        ClientMessage = "colorMode '{colorMode}' and darkMode {darkMode} disagree; send one of them, or darkMode true only with colorMode dark")]
    public static partial IGenericMessage ColorModeAndDarkModeDisagree(ILogger logger, string colorMode, bool darkMode);

    /// <summary>Refuses a time zone the server cannot resolve.</summary>
    [MessageLogging(EventId = 21042, Level = LogLevel.Warning,
        Message = "Time zone '{timeZoneId}' was refused: the server does not know it",
        ClientMessage = "timezone '{timeZoneId}' is not a time zone this server knows; send an id such as America/Chicago or UTC")]
    public static partial IGenericMessage TimeZoneNotKnown(ILogger logger, string timeZoneId);

    /// <summary>Refuses an empty time zone.</summary>
    [MessageLogging(EventId = 21043, Level = LogLevel.Warning,
        Message = "An empty time zone was refused",
        ClientMessage = "timezone is empty; leave it out to keep the stored one, or send an id such as America/Chicago or UTC")]
    public static partial IGenericMessage TimeZoneEmpty(ILogger logger);

    /// <summary>
    /// Logs when the user store could not be read at all.
    /// </summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "The user store could not be read, so no user list can be produced: {reason}")]
    public static partial IGenericMessage UserStoreUnreadable(
        ILogger logger,
        string reason);

    /// <summary>
    /// Logs when a role required during user creation is not found.
    /// </summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Error,
        Message = "Role '{roleName}' not found during user creation")]
    public static partial IGenericMessage RoleNotFoundDuringCreate(
        ILogger logger,
        string roleName);

    /// <summary>
    /// Logs when a password reset is refused because the caller is not the platform administrator.
    /// </summary>
    [MessageLogging(EventId = 50001, Level = LogLevel.Warning,
        Message = "Refused to reset the password of '{user}': only the platform administrator (an Admin acting in the global tenant) may set a user's password")]
    public static partial IGenericMessage ResetPasswordRequiresPlatformAdmin(
        ILogger logger,
        string user);

    /// <summary>
    /// Logs when a caller asked to require a password change on an account whose home tenant is not
    /// the one they are acting in.
    /// </summary>
    [MessageLogging(EventId = 50002, Level = LogLevel.Warning,
        Message = "Refused to require a password change for '{user}': the account's home tenant is not the tenant the caller is acting in")]
    public static partial IGenericMessage RequirePasswordChangeNotHomeTenant(
        ILogger logger,
        string user);

    /// <summary>
    /// Logs that an account was marked to change its password at next login.
    /// </summary>
    [MessageLogging(EventId = 10001, Level = LogLevel.Information,
        Message = "Account '{user}' must change its password at next login")]
    public static partial IGenericMessage PasswordChangeRequired(
        ILogger logger,
        string user);

    /// <summary>
    /// Logs when the sign-in methods of an account could not be read.
    /// </summary>
    [MessageLogging(EventId = 90001, Level = LogLevel.Error,
        Message = "The sign-in methods of '{user}' could not be read")]
    public static partial IGenericMessage SignInMethodsReadFailed(
        ILogger logger,
        string user);

    /// <summary>
    /// Logs when a first-password grant is refused because the caller administers neither the
    /// account's home tenant nor the platform.
    /// </summary>
    [MessageLogging(EventId = 50003, Level = LogLevel.Warning,
        Message = "Refused to set a first password for '{user}': the caller administers neither the account's home tenant nor the platform")]
    public static partial IGenericMessage PasswordLoginNotPermitted(
        ILogger logger,
        string user);

    /// <summary>
    /// Logs when a first-password grant is refused because the account already has a password.
    /// </summary>
    [MessageLogging(EventId = 50004, Level = LogLevel.Warning,
        Message = "Refused to set a first password for '{user}': a password is already set, and replacing one is a platform-administrator reset")]
    public static partial IGenericMessage PasswordLoginAlreadySet(
        ILogger logger,
        string user);

    /// <summary>
    /// Logs when a password would be issued to an account that no enabled password flow admits (FDW-910).
    /// </summary>
    // Warning: refused as designed. The operator limited password sign-in to some users and this
    // account is not one of them; a password here would be a credential nobody can use.
    [MessageLogging(EventId = 40000, Level = LogLevel.Warning,
        Message = "Refused to {operation} for '{user}': no enabled password sign-in flow admits this account. Grant it the permission the flow requires first")]
    public static partial IGenericMessage PasswordNotUsable(
        ILogger logger,
        string operation,
        string user);

    /// <summary>
    /// Logs when whether an account may sign in with a password could not be decided (FDW-910).
    /// </summary>
    [MessageLogging(EventId = 90002, Level = LogLevel.Error,
        Message = "Could not decide whether '{user}' may sign in with a password, so no password was issued")]
    public static partial IGenericMessage PasswordAdmissionUndecided(
        ILogger logger,
        string user);

    /// <summary>
    /// Logs when removing a password login is refused to a caller who does not administer the account (FDW-910).
    /// </summary>
    [MessageLogging(EventId = 50006, Level = LogLevel.Warning,
        Message = "Refused to remove the password login of '{user}': the caller administers neither the account's home tenant nor the platform")]
    public static partial IGenericMessage PasswordRemovalNotPermitted(
        ILogger logger,
        string user);

    /// <summary>
    /// Logs when a password login is asked to be removed from an account that has none (FDW-910).
    /// </summary>
    [MessageLogging(EventId = 30000, Level = LogLevel.Information,
        Message = "Account '{user}' has no password login to remove")]
    public static partial IGenericMessage PasswordLoginAbsent(
        ILogger logger,
        string user);

    /// <summary>
    /// Logs that an account's password login was removed (FDW-910).
    /// </summary>
    [MessageLogging(EventId = 10007, Level = LogLevel.Information,
        Message = "Account '{user}' no longer signs in with a password")]
    public static partial IGenericMessage PasswordLoginRemoved(
        ILogger logger,
        string user);

    /// <summary>
    /// Logs that an account was given its first password.
    /// </summary>
    [MessageLogging(EventId = 10002, Level = LogLevel.Information,
        Message = "Account '{user}' was given a first password and must change it at next sign-in")]
    public static partial IGenericMessage PasswordLoginGranted(
        ILogger logger,
        string user);

    /// <summary>
    /// Logs when an external identity is bound to an account.
    /// </summary>
    [MessageLogging(EventId = 10003, Level = LogLevel.Information,
        Message = "Bound '{issuer}' subject to account '{user}'")]
    public static partial IGenericMessage ExternalIdentityBound(
        ILogger logger,
        string issuer,
        string user);

    /// <summary>
    /// Logs when an external-identity binding is removed from an account.
    /// </summary>
    [MessageLogging(EventId = 10004, Level = LogLevel.Information,
        Message = "Removed an external-identity binding from account '{user}'")]
    public static partial IGenericMessage ExternalIdentityUnbound(
        ILogger logger,
        string user);

    /// <summary>
    /// Logs when an API key operation is refused because the caller administers neither the
    /// account's home tenant nor the platform, and is not the account itself.
    /// </summary>
    [MessageLogging(EventId = 50005, Level = LogLevel.Warning,
        Message = "Refused an API key operation on '{user}': the caller is neither the account, its home-tenant administrator, nor a platform administrator")]
    public static partial IGenericMessage ApiKeyOperationNotPermitted(
        ILogger logger,
        string user);

    /// <summary>
    /// Logs that an API key was minted for an account.
    /// </summary>
    [MessageLogging(EventId = 10005, Level = LogLevel.Information,
        Message = "Minted a '{kind}' API key for account '{user}'")]
    public static partial IGenericMessage ApiKeyMinted(
        ILogger logger,
        string kind,
        string user);

    /// <summary>
    /// Logs that an API key was revoked.
    /// </summary>
    [MessageLogging(EventId = 10006, Level = LogLevel.Information,
        Message = "Revoked a '{kind}' API key of account '{user}'")]
    public static partial IGenericMessage ApiKeyRevoked(
        ILogger logger,
        string kind,
        string user);

    /// <summary>
    /// Logs a binding refused because it names an issuer this platform cannot resolve.
    /// </summary>
    [MessageLogging(EventId = 20002, Level = LogLevel.Warning,
        Message = "Refused an external identity naming '{issuer}': it is not an issuer this platform can resolve, so the binding could never match a sign-in")]
    public static partial IGenericMessage ExternalIssuerUnknown(
        ILogger logger,
        string issuer);

    /// <summary>
    /// Logs a binding that could be neither accepted nor honestly refused, because an authority
    /// could not state its issuer.
    /// </summary>
    [MessageLogging(EventId = 70002, Level = LogLevel.Error,
        Message = "Could not determine whether '{issuer}' is resolvable: an authority could not state its issuer, so the binding was neither accepted nor refused")]
    public static partial IGenericMessage ExternalIssuersUnreadable(
        ILogger logger,
        string issuer);

    /// <summary>
    /// Logs that someone other than the platform administrator asked for a password link (FDW-941).
    /// </summary>
    /// <remarks>
    /// A link lets whoever holds it set the password, so creating one takes the same authority as
    /// setting the password directly.
    /// </remarks>
    [MessageLogging(EventId = 50007, Level = LogLevel.Warning,
        Message = "Refused to create a password link for '{user}': only the platform administrator (an Admin acting in the global tenant) may set a user's password")]
    public static partial IGenericMessage PasswordLinkRequiresPlatformAdmin(
        ILogger logger,
        string user);

    /// <summary>Logs that a password link was created (FDW-941). The link itself is never logged.</summary>
    [MessageLogging(EventId = 10008, Level = LogLevel.Information,
        Message = "Created a password link for '{user}' that works until {expiresAt}")]
    public static partial IGenericMessage PasswordLinkCreated(
        ILogger logger,
        string user,
        DateTimeOffset expiresAt);

    /// <summary>Logs that a password link was emailed to its user (FDW-941).</summary>
    [MessageLogging(EventId = 10009, Level = LogLevel.Information,
        Message = "Emailed the password link to '{user}'")]
    public static partial IGenericMessage PasswordLinkSent(
        ILogger logger,
        string user);

    /// <summary>
    /// Logs that a password link was created but could not be emailed (FDW-941). The link is still
    /// returned, so the administrator can share it another way.
    /// </summary>
    [MessageLogging(EventId = 70003, Level = LogLevel.Warning,
        Message = "The password link for '{user}' was created but not emailed: {reason}")]
    public static partial IGenericMessage PasswordLinkNotSent(
        ILogger logger,
        string user,
        string reason);

    /// <summary>Logs that a read of an account's sign-ins is refused (FDW-2729).</summary>
    [MessageLogging(EventId = 50008, Level = LogLevel.Warning,
        Message = "Refused a read of the sign-ins of '{user}': the caller is neither the account, its home-tenant administrator, nor a platform administrator")]
    public static partial IGenericMessage SignInsNotPermitted(
        ILogger logger,
        string user);

    /// <summary>Logs that a page of an account's sign-ins was read (FDW-2729).</summary>
    [MessageLogging(EventId = 10010, Level = LogLevel.Debug,
        Message = "Read a page of {count} sign-in(s) of user '{user}'")]
    public static partial IGenericMessage SignInsListed(
        ILogger logger,
        string user,
        int count);

    /// <summary>Logs that a setting a password link needs is not set (FDW-941).</summary>
    [MessageLogging(EventId = 60001, Level = LogLevel.Error,
        Message = "No password link can be created: the setting '{setting}' is not set or not valid")]
    public static partial IGenericMessage PasswordLinkSettingMissing(
        ILogger logger,
        string setting);
}
