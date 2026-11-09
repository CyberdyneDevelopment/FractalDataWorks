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
        Message = "Account '{user}' has no password login to remove", ClientMessage = "Account '{user}' has no password login to remove")]
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

    /// <summary>Logs that the request failed: API key kind must be 'user' or 'agent'.</summary>
    [MessageLogging(EventId = 21044, Level = LogLevel.Warning, Message = "API key kind must be 'user' or 'agent'.", ClientMessage = "API key kind must be 'user' or 'agent'.")]
    public static partial IGenericMessage RevokeApiKeyApiKeyKindMustBe(ILogger logger);

    /// <summary>Logs that the request failed: The key could not be revoked.</summary>
    [MessageLogging(EventId = 21045, Level = LogLevel.Warning, Message = "The key could not be revoked.", ClientMessage = "The key could not be revoked.")]
    public static partial IGenericMessage RevokeApiKeyTheKeyCouldNotBe(ILogger logger);

    /// <summary>Logs that the request failed: Name exactly one of subject (bind now) or invitedEmail (invite).</summary>
    [MessageLogging(EventId = 21046, Level = LogLevel.Warning, Message = "Name exactly one of subject (bind now) or invitedEmail (invite).", ClientMessage = "Name exactly one of subject (bind now) or invitedEmail (invite).")]
    public static partial IGenericMessage BindExternalIdentityNameExactlyOneOfSubject(ILogger logger);

    /// <summary>Logs that the request failed: The issuers this platform trusts could not be read.</summary>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error, Message = "The issuers this platform trusts could not be read.", ClientMessage = "The issuers this platform trusts could not be read.")]
    public static partial IGenericMessage BindExternalIdentityReturnedNothing(ILogger logger);

    /// <summary>Logs that the request failed: Whether this platform can resolve that issuer could not be determined: an authority could not be read.</summary>
    [MessageLogging(EventId = 91005, Level = LogLevel.Error, Message = "Whether this platform can resolve that issuer could not be determined: an authority could not be read.", ClientMessage = "Whether this platform can resolve that issuer could not be determined: an authority could not be read.")]
    public static partial IGenericMessage BindExternalIdentityWhetherThisPlatformCanResolve(ILogger logger);

    /// <summary>Logs that the request failed: The binding could not be removed.</summary>
    [MessageLogging(EventId = 21047, Level = LogLevel.Warning, Message = "The binding could not be removed.", ClientMessage = "The binding could not be removed.")]
    public static partial IGenericMessage UnbindExternalIdentityTheBindingCouldNotBe(ILogger logger);

    /// <summary>Logs that the request failed: Password links are not configured on this host.</summary>
    [MessageLogging(EventId = 91006, Level = LogLevel.Error, Message = "Password links are not configured on this host.", ClientMessage = "Password links are not configured on this host.")]
    public static partial IGenericMessage CreatePasswordLinkPasswordLinksAreNotConfigured(ILogger logger);

    /// <summary>Logs that the request failed: The administrator creating the link could not be identified.</summary>
    [MessageLogging(EventId = 91007, Level = LogLevel.Error, Message = "The administrator creating the link could not be identified.", ClientMessage = "The administrator creating the link could not be identified.")]
    public static partial IGenericMessage CreatePasswordLinkTheAdministratorCreatingTheLink(ILogger logger);

    /// <summary>Logs that the request failed: The password link could not be created.</summary>
    [MessageLogging(EventId = 91008, Level = LogLevel.Error, Message = "The password link could not be created.", ClientMessage = "The password link could not be created.")]
    public static partial IGenericMessage CreatePasswordLinkReturnedNothing(ILogger logger);

    /// <summary>Logs that the request failed: No enabled password sign-in flow admits this account; grant it the permission the flow requires first.</summary>
    [MessageLogging(EventId = 40001, Level = LogLevel.Warning, Message = "No enabled password sign-in flow admits this account; grant it the permission the flow requires first.", ClientMessage = "No enabled password sign-in flow admits this account; grant it the permission the flow requires first.")]
    public static partial IGenericMessage CreatePasswordLinkNoEnabledPasswordSignIn(ILogger logger);

    /// <summary>Logs that the request failed: No enabled password sign-in flow admits this account; grant it the permission the flow requires first.</summary>
    [MessageLogging(EventId = 40002, Level = LogLevel.Warning, Message = "No enabled password sign-in flow admits this account; grant it the permission the flow requires first.", ClientMessage = "No enabled password sign-in flow admits this account; grant it the permission the flow requires first.")]
    public static partial IGenericMessage ResetPasswordNoEnabledPasswordSignIn(ILogger logger);

    /// <summary>Logs that the request failed: Failed to reset password.</summary>
    [MessageLogging(EventId = 21048, Level = LogLevel.Warning, Message = "Failed to reset password.", ClientMessage = "Failed to reset password.")]
    public static partial IGenericMessage ResetPasswordFailedToResetPassword(ILogger logger);

    /// <summary>Logs that the request failed: The account could not be marked to change its password.</summary>
    [MessageLogging(EventId = 21049, Level = LogLevel.Warning, Message = "The account could not be marked to change its password.", ClientMessage = "The account could not be marked to change its password.")]
    public static partial IGenericMessage RequirePasswordChangeTheAccountCouldNotBe(ILogger logger);

    /// <summary>Logs that the request failed: No enabled password sign-in flow admits this account; grant it the permission the flow requires first.</summary>
    [MessageLogging(EventId = 40003, Level = LogLevel.Warning, Message = "No enabled password sign-in flow admits this account; grant it the permission the flow requires first.", ClientMessage = "No enabled password sign-in flow admits this account; grant it the permission the flow requires first.")]
    public static partial IGenericMessage SetPasswordLoginNoEnabledPasswordSignIn(ILogger logger);

    /// <summary>Logs that the request failed: A password is already set; only a platform administrator may reset it.</summary>
    [MessageLogging(EventId = 100001, Level = LogLevel.Warning, Message = "A password is already set; only a platform administrator may reset it.", ClientMessage = "A password is already set; only a platform administrator may reset it.")]
    public static partial IGenericMessage SetPasswordLoginAPasswordIsAlreadySet(ILogger logger);

    /// <summary>Logs that the request failed: The password could not be set.</summary>
    [MessageLogging(EventId = 21050, Level = LogLevel.Warning, Message = "The password could not be set.", ClientMessage = "The password could not be set.")]
    public static partial IGenericMessage SetPasswordLoginThePasswordCouldNotBe(ILogger logger);

    /// <summary>Logs that the request failed: API key kind must be 'user' or 'agent'.</summary>
    [MessageLogging(EventId = 21051, Level = LogLevel.Warning, Message = "API key kind must be 'user' or 'agent'.", ClientMessage = "API key kind must be 'user' or 'agent'.")]
    public static partial IGenericMessage CreateApiKeyApiKeyKindMustBe(ILogger logger);

    /// <summary>Logs that the request failed: The key could not be minted.</summary>
    [MessageLogging(EventId = 21052, Level = LogLevel.Warning, Message = "The key could not be minted.", ClientMessage = "The key could not be minted.")]
    public static partial IGenericMessage CreateApiKeyReturnedNothing(ILogger logger);

    /// <summary>Logs that User was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Warning, Message = "User was not found, or is not visible to the caller", ClientMessage = "User was not found")]
    public static partial IGenericMessage RevokeApiKeyUserNotFound(ILogger logger);

    /// <summary>Logs that User was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 31002, Level = LogLevel.Warning, Message = "User was not found, or is not visible to the caller", ClientMessage = "User was not found")]
    public static partial IGenericMessage ListApiKeysUserNotFound(ILogger logger);

    /// <summary>Logs that User was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 31003, Level = LogLevel.Warning, Message = "User was not found, or is not visible to the caller", ClientMessage = "User was not found")]
    public static partial IGenericMessage RemovePasswordLoginUserNotFound(ILogger logger);

    /// <summary>Logs that User was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 31004, Level = LogLevel.Warning, Message = "User was not found, or is not visible to the caller", ClientMessage = "User was not found")]
    public static partial IGenericMessage ListUserSignInsUserNotFound(ILogger logger);

    /// <summary>Logs that User was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 31005, Level = LogLevel.Warning, Message = "User was not found, or is not visible to the caller", ClientMessage = "User was not found")]
    public static partial IGenericMessage RequirePasswordChangeUserNotFound(ILogger logger);

    /// <summary>Logs that User was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 31006, Level = LogLevel.Warning, Message = "User was not found, or is not visible to the caller", ClientMessage = "User was not found")]
    public static partial IGenericMessage ResetPasswordUserNotFound(ILogger logger);

    /// <summary>Logs that User was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 31007, Level = LogLevel.Warning, Message = "User was not found, or is not visible to the caller", ClientMessage = "User was not found")]
    public static partial IGenericMessage SetPasswordLoginUserNotFound(ILogger logger);

    /// <summary>Logs that User was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 31008, Level = LogLevel.Warning, Message = "User was not found, or is not visible to the caller", ClientMessage = "User was not found")]
    public static partial IGenericMessage BindExternalIdentityUserNotFound(ILogger logger);

    /// <summary>Logs that User was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 31009, Level = LogLevel.Warning, Message = "User was not found, or is not visible to the caller", ClientMessage = "User was not found")]
    public static partial IGenericMessage UnbindExternalIdentityUserNotFound(ILogger logger);

    /// <summary>Logs that User was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 31010, Level = LogLevel.Warning, Message = "User was not found, or is not visible to the caller", ClientMessage = "User was not found")]
    public static partial IGenericMessage CreateApiKeyUserNotFound(ILogger logger);

    /// <summary>Logs that User was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 31011, Level = LogLevel.Warning, Message = "User was not found, or is not visible to the caller", ClientMessage = "User was not found")]
    public static partial IGenericMessage GetSignInMethodsUserNotFound(ILogger logger);

    /// <summary>Logs that User was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 31012, Level = LogLevel.Warning, Message = "User was not found, or is not visible to the caller", ClientMessage = "User was not found")]
    public static partial IGenericMessage CreatePasswordLinkUserNotFound(ILogger logger);

    /// <summary>Logs that User 'identifier' was not found.</summary>
    [MessageLogging(EventId = 31013, Level = LogLevel.Warning, Message = "User '{identifier}' was not found", ClientMessage = "User '{identifier}' was not found")]
    public static partial IGenericMessage GetUserUserNotFound(ILogger logger, string identifier);

    /// <summary>Logs that User 'identifier' to delete was not found.</summary>
    [MessageLogging(EventId = 31014, Level = LogLevel.Warning, Message = "User '{identifier}' to delete was not found", ClientMessage = "User '{identifier}' was not found")]
    public static partial IGenericMessage DeleteUserUserNotFound(ILogger logger, string identifier);

    /// <summary>Logs that A list of sign-ins was refused: skip skip or take take is out of range (take is 1 to maxTake).</summary>
    [MessageLogging(EventId = 21053, Level = LogLevel.Warning, Message = "A list of sign-ins was refused: skip {skip} or take {take} is out of range (take is 1 to {maxTake})", ClientMessage = "skip must be 0 or more, and take must be from 1 to {maxTake}")]
    public static partial IGenericMessage ListUserSignInsPagingInvalid(ILogger logger, int skip, int take, int maxTake);

    /// <summary>Logs that Creating user 'username' answered a success and no outcome.</summary>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error, Message = "Creating user '{username}' answered a success and no outcome", ClientMessage = "The user could not be created")]
    public static partial IGenericMessage CreateUserReturnedNothing(ILogger logger, string username);

    /// <summary>Logs that User 'username' was created (id userId) but could not be re-read for the response body.</summary>
    [MessageLogging(EventId = 91010, Level = LogLevel.Error, Message = "User '{username}' was created (id {userId}) but could not be re-read for the response body", ClientMessage = "User '{username}' was created (id {userId}) but could not be re-read for the response body")]
    public static partial IGenericMessage CreateUserCreatedButNotReRead(ILogger logger, string username, Guid userId);

    /// <summary>Logs that 'issuer' is not an issuer this platform can resolve (accepted: accepted).</summary>
    [MessageLogging(EventId = 21054, Level = LogLevel.Warning, Message = "'{issuer}' is not an issuer this platform can resolve (accepted: {accepted})", ClientMessage = "'{issuer}' is not an issuer this platform can resolve. Accepted: {accepted}.")]
    public static partial IGenericMessage BindExternalIdentityIssuerUnknown(ILogger logger, string issuer, string accepted);

    /// <summary>Logs that A user with the username 'username' already exists (usernames match ignoring case).</summary>
    [MessageLogging(EventId = 40004, Level = LogLevel.Warning, Message = "A user with the username '{username}' already exists (usernames match ignoring case)", ClientMessage = "A user with this username already exists. Usernames are matched ignoring case, so a name that differs only by case is the same user.")]
    public static partial IGenericMessage CreateUserUsernameTaken(ILogger logger, string username);

    /// <summary>Logs that None of the roles of user 'username' allows password sign-in.</summary>
    [MessageLogging(EventId = 40005, Level = LogLevel.Warning, Message = "None of the roles of user '{username}' allows password sign-in", ClientMessage = "None of these roles allows password sign-in: no enabled password sign-in flow admits an account holding them. Add a role that carries what the password flow requires, or create the account without a password.")]
    public static partial IGenericMessage CreateUserPasswordNotUsable(ILogger logger, string username);

    /// <summary>Logs that User 'username' was created (id userId) but a later step failed.</summary>
    [MessageLogging(EventId = 91011, Level = LogLevel.Error, Message = "User '{username}' was created (id {userId}) but a later step failed", ClientMessage = "User '{username}' was created (id {userId}) but a later step failed; the response lists what landed and what did not")]
    public static partial IGenericMessage CreateUserPartiallyCreated(ILogger logger, string username, object userId);

    /// <summary>Logs that The request to get user preferences was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 50009, Level = LogLevel.Warning, Message = "The request to get user preferences was refused: the caller is not signed in", ClientMessage = "You must be signed in to get user preferences")]
    public static partial IGenericMessage GetUserPreferencesNotSignedIn(ILogger logger);

    /// <summary>Logs that The request to list user sign ins was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 100002, Level = LogLevel.Warning, Message = "The request to list user sign ins was refused: the caller is not allowed", ClientMessage = "You are not allowed to list user sign ins")]
    public static partial IGenericMessage ListUserSignInsForbidden(ILogger logger);

    /// <summary>Logs that The request to bind external identity was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 100003, Level = LogLevel.Warning, Message = "The request to bind external identity was refused: the caller is not allowed", ClientMessage = "You are not allowed to bind external identity")]
    public static partial IGenericMessage BindExternalIdentityForbidden(ILogger logger);

    /// <summary>Logs that The request to reset password was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 100004, Level = LogLevel.Warning, Message = "The request to reset password was refused: the caller is not allowed", ClientMessage = "You are not allowed to reset password")]
    public static partial IGenericMessage ResetPasswordForbidden(ILogger logger);

    /// <summary>Logs that The request to create password link was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 100005, Level = LogLevel.Warning, Message = "The request to create password link was refused: the caller is not allowed", ClientMessage = "You are not allowed to create password link")]
    public static partial IGenericMessage CreatePasswordLinkForbidden(ILogger logger);

    /// <summary>Logs that The request to update user preferences was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 50010, Level = LogLevel.Warning, Message = "The request to update user preferences was refused: the caller is not signed in", ClientMessage = "You must be signed in to update user preferences")]
    public static partial IGenericMessage UpdateUserPreferencesNotSignedIn(ILogger logger);

    /// <summary>Logs that The request to set password login was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 100006, Level = LogLevel.Warning, Message = "The request to set password login was refused: the caller is not allowed", ClientMessage = "You are not allowed to set password login")]
    public static partial IGenericMessage SetPasswordLoginForbidden(ILogger logger);

    /// <summary>Logs that The request to unbind external identity was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 100007, Level = LogLevel.Warning, Message = "The request to unbind external identity was refused: the caller is not allowed", ClientMessage = "You are not allowed to unbind external identity")]
    public static partial IGenericMessage UnbindExternalIdentityForbidden(ILogger logger);

    /// <summary>Logs that The request to remove password login was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 100008, Level = LogLevel.Warning, Message = "The request to remove password login was refused: the caller is not allowed", ClientMessage = "You are not allowed to remove password login")]
    public static partial IGenericMessage RemovePasswordLoginForbidden(ILogger logger);

    /// <summary>Logs that The request to create api key was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 100009, Level = LogLevel.Warning, Message = "The request to create api key was refused: the caller is not allowed", ClientMessage = "You are not allowed to create api key")]
    public static partial IGenericMessage CreateApiKeyForbidden(ILogger logger);

    /// <summary>Logs that The request to revoke api key was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 100010, Level = LogLevel.Warning, Message = "The request to revoke api key was refused: the caller is not allowed", ClientMessage = "You are not allowed to revoke api key")]
    public static partial IGenericMessage RevokeApiKeyForbidden(ILogger logger);

    /// <summary>Logs that The request to require password change was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 100011, Level = LogLevel.Warning, Message = "The request to require password change was refused: the caller is not allowed", ClientMessage = "You are not allowed to require password change")]
    public static partial IGenericMessage RequirePasswordChangeForbidden(ILogger logger);

    /// <summary>Logs that The request to list api keys was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 100012, Level = LogLevel.Warning, Message = "The request to list api keys was refused: the caller is not allowed", ClientMessage = "You are not allowed to list api keys")]
    public static partial IGenericMessage ListApiKeysForbidden(ILogger logger);

    /// <summary>Logs that Unbind external identity was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 31015, Level = LogLevel.Warning, Message = "Unbind external identity was not found, or is not visible to the caller", ClientMessage = "Unbind external identity was not found")]
    public static partial IGenericMessage UnbindExternalIdentityNotFound(ILogger logger);

    /// <summary>Logs that update user preferences failed unexpectedly.</summary>
    [MessageLogging(EventId = 91012, Level = LogLevel.Error, Message = "Update user preferences failed unexpectedly", ClientMessage = "Update user preferences failed unexpectedly")]
    public static partial IGenericMessage UpdateUserPreferencesFaulted(ILogger logger);

    /// <summary>Logs that create password link failed unexpectedly.</summary>
    [MessageLogging(EventId = 91013, Level = LogLevel.Error, Message = "Create password link failed unexpectedly", ClientMessage = "Create password link failed unexpectedly")]
    public static partial IGenericMessage CreatePasswordLinkFaulted(ILogger logger);

    /// <summary>Logs that reset password failed unexpectedly.</summary>
    [MessageLogging(EventId = 91014, Level = LogLevel.Error, Message = "Reset password failed unexpectedly", ClientMessage = "Reset password failed unexpectedly")]
    public static partial IGenericMessage ResetPasswordFaulted(ILogger logger);

    /// <summary>Logs that get user preferences failed unexpectedly.</summary>
    [MessageLogging(EventId = 91015, Level = LogLevel.Error, Message = "Get user preferences failed unexpectedly", ClientMessage = "Get user preferences failed unexpectedly")]
    public static partial IGenericMessage GetUserPreferencesFaulted(ILogger logger);
}
