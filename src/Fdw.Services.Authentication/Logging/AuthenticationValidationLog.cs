using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for the inbound-token validation domain — the declaration of authentication
/// services, the schemes they register, and the per-request routing between them.
/// </summary>
[MessageLoggingTypeCode("AUTHVALIDATION")]
public static partial class AuthenticationValidationLog
{
    /// <summary>Logs an AuthenticationServices entry that names no mechanism.</summary>
    [MessageLogging(EventId = 71100, Level = LogLevel.Error,
        Message = "Authentication service at '{sectionPath}' declares no ServiceOptionType. Set it to a REGISTERED member of AuthenticationServiceTypes - 'OpenIddict' for tokens this host issues, 'JwtBearer' for tokens a remote authority issues. A value that is not a registered member registers no scheme at all, and every request then falls through to UnmatchedIssuerHandler")]
    public static partial IGenericMessage EntryMissingServiceOptionType(ILogger logger, string sectionPath);

    /// <summary>Logs an enabled AuthenticationServices entry with no name.</summary>
    [MessageLogging(EventId = 71101, Level = LogLevel.Error,
        Message = "Authentication service at '{sectionPath}' declares no Name. Set Name to a unique label for this issuer - it becomes the ASP.NET scheme name and the identifier in every routing and rejection log line")]
    public static partial IGenericMessage EntryMissingName(ILogger logger, string sectionPath);

    /// <summary>Logs an enabled AuthenticationServices entry with no authority.</summary>
    [MessageLogging(EventId = 71102, Level = LogLevel.Error,
        Message = "Authentication service '{serviceName}' declares no Authority. Set Authority to the absolute issuer URL its tokens carry in their 'iss' claim - this host's own authority for OpenIddict, or the remote authority for JwtBearer (e.g. https://login.example.dev/application/o/{{slug}}/). There is NO default: without it no scheme is registered and every token from this issuer is rejected. The trailing slash is compared literally - take the value from the issuer's own /.well-known/openid-configuration rather than typing it")]
    public static partial IGenericMessage EntryMissingAuthority(ILogger logger, string serviceName);

    /// <summary>Logs an authority that is not an absolute http(s) URL.</summary>
    [MessageLogging(EventId = 71103, Level = LogLevel.Error,
        Message = "Authentication service '{serviceName}' declares Authority '{authority}', which is not an absolute http(s) URL. It must be the full issuer URL including scheme - https://host/path, not a host name, a relative path, or a bare name")]
    public static partial IGenericMessage AuthorityNotAbsolute(ILogger logger, string serviceName, string authority);

    /// <summary>Logs a JwtBearer entry with no audience.</summary>
    [MessageLogging(EventId = 71104, Level = LogLevel.Error,
        Message = "JwtBearer authentication service '{serviceName}' declares no Audience. Set Audience to the value this host's tokens carry in their 'aud' claim - decode a real token and read it rather than assuming, since a token minted for an application carries the client id while one minted for an exposed API carries that API's identifier. Without it, the roles below would be conferred on EVERY token this issuer mints, for any audience")]
    public static partial IGenericMessage JwtBearerMissingAudience(ILogger logger, string serviceName);

    /// <summary>Logs a JwtBearer entry with no roles.</summary>
    [MessageLogging(EventId = 71105, Level = LogLevel.Error,
        Message = "JwtBearer authentication service '{serviceName}' declares no Roles. Set Roles to the local role names a caller from this issuer receives - they must exist in authz.Role, and their permissions are what every role-gated endpoint checks. Registration is refused rather than authenticating a caller into nothing: with no roles the caller would pass authentication and be denied by every route it reaches")]
    public static partial IGenericMessage JwtBearerMissingRoles(ILogger logger, string serviceName);

    /// <summary>Logs a host that registered a validation mechanism but declared no authentication services.</summary>
    [MessageLogging(EventId = 71106, Level = LogLevel.Error,
        Message = "No enabled entries in '{sectionName}'. This host registered a token-validation mechanism and declared no issuer for it to trust, so it accepts no tokens at all. Add at least one entry naming a ServiceOptionType and an Authority, and confirm its Enabled flag is set - a declared-but-disabled entry reads as absent here")]
    public static partial IGenericMessage NoAuthenticationServicesDeclared(ILogger logger, string sectionName);

    /// <summary>Logs a mechanism that returned success without producing a scheme binding.</summary>
    [MessageLogging(EventId = 71107, Level = LogLevel.Error,
        Message = "Authentication service '{serviceName}' ({mechanism}) reported success without producing a scheme binding")]
    public static partial IGenericMessage SchemeNotProduced(ILogger logger, string serviceName, string mechanism);

    /// <summary>Logs an AuthenticationServices section that could not be read.</summary>
    [MessageLogging(EventId = 71108, Level = LogLevel.Error,
        Message = "The AuthenticationServices declarations for mechanism '{mechanism}' reported success with no entries. The section was readable and empty - check that an entry names ServiceOptionType '{mechanism}' exactly, since a mechanism name that matches no registered option is silently skipped rather than rejected")]
    public static partial IGenericMessage SectionUnreadable(ILogger logger, string mechanism);

    /// <summary>Logs a validated token that carried no claims identity.</summary>
    [MessageLogging(EventId = 71109, Level = LogLevel.Error,
        Message = "Authentication service '{serviceName}' validated a token that produced no claims identity")]
    public static partial IGenericMessage ValidatedTokenHasNoIdentity(ILogger logger, string serviceName);

    /// <summary>Logs declared roles that could not be expanded to permissions.</summary>
    [MessageLogging(EventId = 71110, Level = LogLevel.Error,
        Message = "Authentication service '{serviceName}' could not resolve its declared roles '{roles}' to permissions: {reason}. Each name must match a row in authz.Role, and that role needs grants in authz.RolePermission - a role that exists with no grants resolves to an empty permission set and authorises nothing")]
    public static partial IGenericMessage DeclaredRolesNotResolved(ILogger logger, string serviceName, string roles, string reason);

    /// <summary>Logs a bearer token whose issuer no authentication service declared.</summary>
    [MessageLogging(EventId = 51100, Level = LogLevel.Warning,
        Message = "No authentication service declares issuer '{issuer}' (declared: {declaredIssuers}); rejecting {path}")]
    public static partial IGenericMessage IssuerNotDeclared(ILogger logger, string issuer, string declaredIssuers, string path);

    /// <summary>Logs a request carrying no readable bearer token.</summary>
    [MessageLogging(EventId = 11100, Level = LogLevel.Debug,
        Message = "No readable bearer token on {path}; no authentication scheme can be selected")]
    public static partial IGenericMessage NoReadableBearerToken(ILogger logger, string path);

    /// <summary>Logs an entry that is present but disabled.</summary>
    [MessageLogging(EventId = 11101, Level = LogLevel.Debug,
        Message = "Authentication service '{serviceName}' ({mechanism}) is declared but not enabled")]
    public static partial IGenericMessage EntryDisabled(ILogger logger, string serviceName, string mechanism);

    /// <summary>Logs a scheme registered for one declared authentication service.</summary>
    [MessageLogging(EventId = 31100, Level = LogLevel.Information,
        Message = "Authentication service '{serviceName}' ({mechanism}) validates issuer '{issuer}' on scheme '{schemeName}'")]
    public static partial IGenericMessage SchemeRegistered(ILogger logger, string serviceName, string mechanism, string schemeName, string issuer);

    /// <summary>Logs the routing registration once every mechanism has contributed its schemes.</summary>
    [MessageLogging(EventId = 31101, Level = LogLevel.Information,
        Message = "Routing {bindingCount} trusted issuer(s) through the '{selectorScheme}' policy scheme")]
    public static partial IGenericMessage RoutingRegistered(ILogger logger, int bindingCount, string selectorScheme);

    /// <summary>Logs the scheme a request was routed to.</summary>
    [MessageLogging(EventId = 11102, Level = LogLevel.Debug,
        Message = "Issuer '{issuer}' routed to scheme '{schemeName}' (authentication service '{serviceName}')")]
    public static partial IGenericMessage IssuerRouted(ILogger logger, string issuer, string schemeName, string serviceName);

    /// <summary>Logs the roles and permissions conferred on a validated external token.</summary>
    [MessageLogging(EventId = 31102, Level = LogLevel.Information,
        Message = "Authentication service '{serviceName}' conferred {roleCount} role(s) and {permissionCount} permission(s) on subject '{subject}'")]
    public static partial IGenericMessage DeclaredRolesConferred(ILogger logger, string serviceName, int roleCount, int permissionCount, string subject);

    /// <summary>Logs a bearer token whose payload segment could not be read.</summary>
    [MessageLogging(EventId = 11103, Level = LogLevel.Debug,
        Message = "The bearer token on {path} has an unreadable payload; no issuer to select a scheme by")]
    public static partial IGenericMessage BearerTokenPayloadUnreadable(ILogger logger, System.Exception exception, string path);

    /// <summary>Logs an OpenIddict host whose server options carry no issuer.</summary>
    [MessageLogging(EventId = 71111, Level = LogLevel.Error,
        Message = "OpenIddict stamped no issuer on its server options; there is nothing to match this host's declared authority against")]
    public static partial IGenericMessage OpenIddictIssuerNotStamped(ILogger logger);

    /// <summary>Logs a declared authority that does not match the issuer OpenIddict stamps on its tokens.</summary>
    [MessageLogging(EventId = 71112, Level = LogLevel.Error,
        Message = "Authentication service '{serviceName}' declares authority '{declared}', but OpenIddict stamps issuer '{stamped}' — every token this host issues would route to no scheme")]
    public static partial IGenericMessage OpenIddictIssuerMismatch(ILogger logger, string serviceName, string declared, string stamped);

    /// <summary>Logs a host that carries the validation domain with no mechanism registered in it.</summary>
    [MessageLogging(EventId = 11104, Level = LogLevel.Debug,
        Message = "No token-validation mechanism is registered; this host validates no tokens of its own")]
    public static partial IGenericMessage NoMechanismsRegistered(ILogger logger);

    /// <summary>The signing key this host validates its own tokens with could not be read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="serviceName">The entry whose scheme needed it.</param>
    // Why Error and why registration fails on it: a scheme that cannot check a signature refuses
    // every token it exists to accept. Failing here stops the host with the cause named, rather
    // than starting one that 401s everything and says nothing about why.
    [MessageLogging(EventId = 71113, Level = LogLevel.Error,
        Message = "No local signing key is available for '{serviceName}', so its tokens cannot be validated")]
    public static partial IGenericMessage LocalSigningKeyUnavailable(ILogger logger, string serviceName);

    /// <summary>Logs a LocalKey entry that declared no audience.</summary>
    // Separate from JwtBearerMissingAudience so the message can say what a LocalKey audience is
    // for. Here it is what this host mints into its own tokens, not a value agreed with a remote
    // issuer, so the thing to go change is the flow rather than the provider registration.
    [MessageLogging(EventId = 71114, Level = LogLevel.Error,
        Message = "LocalKey authentication service '{serviceName}' declares no Audience. Set it to the audience this host's flows mint - a token is only accepted for the audience it names, so a mismatch rejects every token this host issued")]
    public static partial IGenericMessage LocalKeyMissingAudience(ILogger logger, string serviceName);
}
