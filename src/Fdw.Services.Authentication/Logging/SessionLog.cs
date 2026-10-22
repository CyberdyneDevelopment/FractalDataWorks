using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for server-side sessions (FDW-903): the store, the Session scheme that reads them,
/// the CSRF protections that guard cookie-authenticated writes, and cookie delivery.
/// </summary>
/// <remarks>
/// A session id never appears here at any level. It is a bearer credential: anything that can read a
/// log line carrying one can act as the session. The session's <c>Id</c> (the <c>sid</c> claim) is
/// safe to log, because it is not what the browser presents.
/// </remarks>
[MessageLoggingTypeCode("SESSION")]
public static partial class SessionLog
{
    /// <summary>A session was created.</summary>
    [MessageLogging(EventId = 11501, Level = LogLevel.Information,
        Message = "Session {sessionId} created in service '{serviceName}' by flow '{flowName}', ending no later than {absoluteExpiresAt}")]
    public static partial IGenericMessage SessionCreated(
        ILogger logger, Guid sessionId, string serviceName, string flowName, DateTimeOffset absoluteExpiresAt);

    /// <summary>A session was used, and its idle end moved forward.</summary>
    [MessageLogging(EventId = 11502, Level = LogLevel.Trace,
        Message = "Session {sessionId} used in service '{serviceName}'; idle end moved to {idleExpiresAt}")]
    public static partial IGenericMessage SessionUsed(
        ILogger logger, Guid sessionId, string serviceName, DateTimeOffset idleExpiresAt);

    /// <summary>A session was revoked.</summary>
    [MessageLogging(EventId = 11503, Level = LogLevel.Information,
        Message = "Session {sessionId} revoked")]
    public static partial IGenericMessage SessionRevoked(ILogger logger, Guid sessionId);

    /// <summary>A session was put to sleep because a session above it is in use.</summary>
    [MessageLogging(EventId = 11513, Level = LogLevel.Information,
        Message = "Session {sessionId} put to sleep")]
    public static partial IGenericMessage SessionSlept(ILogger logger, Guid sessionId);

    /// <summary>A session was woken and given a new id.</summary>
    [MessageLogging(EventId = 11514, Level = LogLevel.Information,
        Message = "Session {sessionId} woken with a new id; idle end moved to {idleExpiresAt}")]
    public static partial IGenericMessage SessionWoken(ILogger logger, Guid sessionId, DateTimeOffset idleExpiresAt);

    /// <summary>A CSRF token was issued for a session.</summary>
    [MessageLogging(EventId = 11504, Level = LogLevel.Debug,
        Message = "CSRF token issued for session {sessionId}")]
    public static partial IGenericMessage CsrfTokenIssued(ILogger logger, Guid sessionId);

    /// <summary>A request was authenticated by its session.</summary>
    [MessageLogging(EventId = 11505, Level = LogLevel.Debug,
        Message = "Session {sessionId} accepted by service '{serviceName}' for user '{userId}'")]
    public static partial IGenericMessage SessionAccepted(ILogger logger, Guid sessionId, string serviceName, Guid userId);

    /// <summary>A cookie-authenticated write passed its CSRF protection.</summary>
    [MessageLogging(EventId = 11506, Level = LogLevel.Trace,
        Message = "CSRF protection '{protection}' passed for a {method} to '{path}' in service '{serviceName}'")]
    public static partial IGenericMessage CsrfPassed(ILogger logger, string protection, string method, string path, string serviceName);

    /// <summary>A session cookie was set on a response.</summary>
    [MessageLogging(EventId = 11507, Level = LogLevel.Debug,
        Message = "Session cookie '{cookieName}' set for service '{serviceName}', expiring {expiresAt}")]
    public static partial IGenericMessage SessionCookieSet(ILogger logger, string cookieName, string serviceName, DateTimeOffset expiresAt);

    /// <summary>A session cookie was removed from the browser.</summary>
    [MessageLogging(EventId = 11508, Level = LogLevel.Debug,
        Message = "Session cookie '{cookieName}' cleared for service '{serviceName}'")]
    public static partial IGenericMessage SessionCookieCleared(ILogger logger, string cookieName, string serviceName);

    /// <summary>An outbound call carries the caller's own bearer token.</summary>
    [MessageLogging(EventId = 11509, Level = LogLevel.Trace,
        Message = "Forwarding the caller's own bearer token for an outbound call from '{path}'")]
    public static partial IGenericMessage CallerTokenForwarded(ILogger logger, string path);

    /// <summary>An outbound call carries nothing, because the caller presented no credential to forward.</summary>
    [MessageLogging(EventId = 11510, Level = LogLevel.Trace,
        Message = "No credential to forward for an outbound call from '{path}': the caller holds neither a bearer token nor a session")]
    public static partial IGenericMessage NoCallerCredential(ILogger logger, string path);

    /// <summary>A token was minted so a session caller's outbound calls carry its own identity (FDW-925).</summary>
    [MessageLogging(EventId = 11511, Level = LogLevel.Debug,
        Message = "Minted a token for session principal '{principalId}' in tenant '{tenantId}' for audience '{audience}', expiring {expiresAt}")]
    public static partial IGenericMessage DelegationTokenMinted(
        ILogger logger, Guid principalId, Guid tenantId, string audience, DateTimeOffset expiresAt);

    /// <summary>A later outbound call of the same request reuses the token already minted for it.</summary>
    [MessageLogging(EventId = 11512, Level = LogLevel.Trace,
        Message = "Reusing the token already minted for this request for an outbound call from '{path}'")]
    public static partial IGenericMessage DelegationTokenReused(ILogger logger, string path);

    /// <summary>Create was called without what a session asserts.</summary>
    [MessageLogging(EventId = 21501, Level = LogLevel.Error,
        Message = "A session needs a service name, a flow name and an assertion; one was missing")]
    public static partial IGenericMessage CreateArgumentsMissing(ILogger logger);

    /// <summary>Use was called without an id.</summary>
    [MessageLogging(EventId = 21502, Level = LogLevel.Debug,
        Message = "No session id was presented to service '{serviceName}'")]
    public static partial IGenericMessage SessionIdMissing(ILogger logger, string serviceName);

    /// <summary>A session operation was asked of a request no session authenticated.</summary>
    [MessageLogging(EventId = 21503, Level = LogLevel.Warning,
        Message = "Request to '{path}' was not authenticated by a server-side session, so it has no session to {operation}")]
    public static partial IGenericMessage NotASessionRequest(ILogger logger, string path, string operation);

    /// <summary>A presented session id resolves to no live session of the service.</summary>
    /// <remarks>
    /// The reason is logged for the operator and never returned to the caller as anything but this
    /// failure: telling an expired session from an unknown one would tell the caller the id was real.
    /// </remarks>
    [MessageLogging(EventId = 51501, Level = LogLevel.Warning,
        Message = "No live session for the id presented to service '{serviceName}': {reason}")]
    public static partial IGenericMessage SessionNotLive(ILogger logger, string serviceName, string reason);

    /// <summary>The claims a session would assert have already expired.</summary>
    [MessageLogging(EventId = 51502, Level = LogLevel.Warning,
        Message = "Session for principal {principalId} from flow '{flowName}' not created: its claims were valid only until {validUntil}")]
    public static partial IGenericMessage ClaimsAlreadyExpired(ILogger logger, string flowName, Guid principalId, DateTimeOffset validUntil);

    /// <summary>A live session's user has been revoked wholesale since the session began.</summary>
    [MessageLogging(EventId = 51503, Level = LogLevel.Warning,
        Message = "Session {sessionId} in service '{serviceName}' refused: every session of user '{userId}' begun before a later revocation is ended")]
    public static partial IGenericMessage SessionUserRevoked(ILogger logger, Guid sessionId, string serviceName, Guid userId);

    /// <summary>A cookie-authenticated write failed its CSRF protection.</summary>
    [MessageLogging(EventId = 51504, Level = LogLevel.Warning,
        Message = "CSRF protection '{protection}' refused a {method} to '{path}' in service '{serviceName}': {reason}")]
    public static partial IGenericMessage CsrfRefused(
        ILogger logger, string protection, string method, string path, string serviceName, string reason);

    /// <summary>A server setting a session needs is missing, inactive, or not a positive whole number.</summary>
    [MessageLogging(EventId = 61501, Level = LogLevel.Error,
        Message = "Server setting '{settingName}' cannot size a session: {reason}. It must be an active Int setting greater than zero; there is no default")]
    public static partial IGenericMessage SettingUnusable(ILogger logger, string settingName, string reason);

    /// <summary>A session-delivered flow names no key to encrypt the session's claims.</summary>
    [MessageLogging(EventId = 61502, Level = LogLevel.Error,
        Message = "Flow '{flowName}' delivers a session but names no key (SecretManagerName/SecretKeyName) to encrypt its claims")]
    public static partial IGenericMessage NoSessionKey(ILogger logger, string flowName);

    /// <summary>A Session service's configuration cannot serve.</summary>
    [MessageLogging(EventId = 61503, Level = LogLevel.Error,
        Message = "Session service '{serviceName}' is misconfigured: {reason}")]
    public static partial IGenericMessage SessionServiceInvalid(ILogger logger, string serviceName, string reason);

    /// <summary>A flow names a Session service this host has not declared.</summary>
    [MessageLogging(EventId = 61504, Level = LogLevel.Error,
        Message = "Flow '{flowName}' delivers into Session service '{serviceName}', which this host does not declare as an enabled Session service")]
    public static partial IGenericMessage SessionServiceNotDeclared(ILogger logger, string flowName, string serviceName);

    /// <summary>A session could not be written.</summary>
    [MessageLogging(EventId = 71501, Level = LogLevel.Error,
        Message = "Session store could not {operation}")]
    public static partial IGenericMessage SessionWriteFailed(ILogger logger, string operation);

    /// <summary>A session caller's outbound calls go without credentials, because no token could be minted for it.</summary>
    /// <remarks>
    /// 90001 is the catalog's OperationFailed, the number the sign-in path's IssuanceFailed code also
    /// carries: the caller is authenticated and its token could not be issued, so the fault is this
    /// host's. Warning, not Error: the outbound call still goes, and the downstream refuses it exactly
    /// as it would have before a token was minted at all (FDW-925). An issuer that throws rather than
    /// returning a failure is not caught here; the exception reaches the outbound call.
    /// </remarks>
    [MessageLogging(EventId = 90001, Level = LogLevel.Warning,
        Message = "No token could be minted for a session caller of '{path}', so its outbound calls carry no credential: {reason}")]
    public static partial IGenericMessage DelegationTokenNotIssued(ILogger logger, string path, string reason);

    /// <summary>The session store answered a use with success and no session.</summary>
    /// <remarks>The store answers a session or a refusal, so this is this host's fault, never the caller's.</remarks>
    [MessageLogging(EventId = 90000, Level = LogLevel.Error,
        Message = "The session store answered a use for service '{serviceName}' with no session")]
    public static partial IGenericMessage SessionReadReturnedNoValue(ILogger logger, string serviceName);

    /// <summary>A session's stored claims could not be read back.</summary>
    [MessageLogging(EventId = 91501, Level = LogLevel.Error,
        Message = "Session {sessionId} holds claims that cannot be read back")]
    public static partial IGenericMessage ClaimsUnreadable(ILogger logger, Guid sessionId);

    /// <summary>A session identity does not carry what its assertion needs to be read back.</summary>
    /// <remarks>The Session scheme builds every such identity, so this is this host's fault, never the caller's.</remarks>
    [MessageLogging(EventId = 91502, Level = LogLevel.Error,
        Message = "A session identity cannot be read back as an assertion: {reason}")]
    public static partial IGenericMessage SessionPrincipalUnreadable(ILogger logger, string reason);

    /// <summary>A session started a session of its own by acting with a role.</summary>
    [MessageLogging(EventId = 11515, Level = LogLevel.Information,
        Message = "Principal {principalId} acts with role '{role}' from session {parentSessionId}")]
    public static partial IGenericMessage ActingStarted(ILogger logger, Guid principalId, string role, Guid parentSessionId);

    /// <summary>Acting was refused because the role is not one the caller may act with: it does not exist.</summary>
    [MessageLogging(EventId = 101501, Level = LogLevel.Warning,
        Message = "Acting was refused: there is no role '{role}'",
        ClientMessage = "There is no role '{role}' to act with")]
    public static partial IGenericMessage ActingRoleUnknown(ILogger logger, string role);

    /// <summary>Acting was refused because the role belongs to another tenant than the one the session acts in.</summary>
    [MessageLogging(EventId = 101502, Level = LogLevel.Warning,
        Message = "Acting was refused: role '{role}' belongs to tenant '{roleTenantId}', not the session's tenant '{tenantId}'",
        ClientMessage = "Role '{role}' is not a role of the company you act in")]
    public static partial IGenericMessage ActingRoleOtherTenant(ILogger logger, string role, string roleTenantId, string tenantId);

    /// <summary>Acting was refused because the caller does not hold everything the role carries.</summary>
    [MessageLogging(EventId = 101503, Level = LogLevel.Warning,
        Message = "Acting was refused: the session does not hold {permissions}, which role '{role}' carries",
        ClientMessage = "You cannot act with role '{role}': it carries {permissions}, which you do not hold")]
    public static partial IGenericMessage ActingRightsNotHeld(ILogger logger, string role, string permissions);

    /// <summary>A person returned from acting: the sessions above a step ended and the step woke.</summary>
    [MessageLogging(EventId = 11516, Level = LogLevel.Information,
        Message = "Returned to session {sessionId}; {ended} acting session(s) ended")]
    public static partial IGenericMessage ActingReturned(ILogger logger, Guid sessionId, int ended);

    /// <summary>A return from acting was refused: the session is not acting, or the step is not one below it.</summary>
    [MessageLogging(EventId = 21504, Level = LogLevel.Warning,
        Message = "A return from acting was refused: {reason}",
        ClientMessage = "You cannot return: {reason}")]
    public static partial IGenericMessage ReturnRefused(ILogger logger, string reason);

    /// <summary>The chain of sessions a session was started through loops, ends at a missing row or is too deep.</summary>
    [MessageLogging(EventId = 91503, Level = LogLevel.Error,
        Message = "The chain of sessions behind session {sessionId} is broken: it loops, runs too deep or ends at a missing session")]
    public static partial IGenericMessage ChainBroken(ILogger logger, Guid sessionId);
}
