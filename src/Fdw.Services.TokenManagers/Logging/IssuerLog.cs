using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.TokenManagers.Logging;

/// <summary>
/// MessageLogging for minting tokens.
/// </summary>
/// <remarks>
/// EventId range: 91180–91194, 91199–91200, 91204, 91209–91210. The token itself never appears here. It is a
/// bearer credential, and anything that reaches a log reaches whoever can read logs.
/// </remarks>
[MessageLoggingTypeCode("TOKENMANAGER")]
internal static partial class IssuerLog
{
    /// <summary>A token was minted.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="audience">Who it was minted for.</param>
    /// <param name="principalId">Who it names.</param>
    /// <param name="acr">The assurance the flow reached.</param>
    [MessageLogging(EventId = 91180, Level = LogLevel.Information,
        Message = "Issued a token for audience '{audience}' naming principal {principalId} at assurance '{acr}'")]
    internal static partial IGenericMessage Issued(
        ILogger<JwtTokenIssuer> logger, string audience, Guid principalId, string acr);

    /// <summary>The signing key was fetched.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="keyName">Its name in the secret manager.</param>
    [MessageLogging(EventId = 91183, Level = LogLevel.Debug,
        Message = "Loaded signing key '{keyName}'")]
    internal static partial IGenericMessage SigningKeyLoaded(
        ILogger<SecretManagerSigningCredentialProvider> logger, string keyName);

    /// <summary>The signing key could not be parsed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="keyName">Its name in the secret manager.</param>
    /// <param name="exception">What the parser reported.</param>
    // Critical, not Error: Without a signing key nothing can be minted, by any flow, and no retry recovers it. The service is running and cannot do the
    // one thing it exists for.
    [MessageLogging(EventId = 91184, Level = LogLevel.Critical,
        Message = "Signing key '{keyName}' is not a readable PEM private key")]
    internal static partial IGenericMessage SigningKeyUnreadable(
        ILogger<SecretManagerSigningCredentialProvider> logger, Exception exception, string keyName);

    /// <summary>A token is about to be minted.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="audience">Who it is for.</param>
    /// <param name="claimCount">How many claims it will carry.</param>
    [MessageLogging(EventId = 91185, Level = LogLevel.Trace,
        Message = "Minting a token for '{audience}' carrying {claimCount} claim(s)")]
    internal static partial IGenericMessage Minting(
        ILogger<JwtTokenIssuer> logger, string audience, int claimCount);

    /// <summary>The claim types assembled for a token, ahead of minting it.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="audience">Who it is for.</param>
    /// <param name="claimTypes">The claim type names present (values withheld — perm/roles can be
    /// read from PermissionBakingLog.BakedDetail; this exists to show the token's SHAPE, not
    /// duplicate its content).</param>
    // Why type names and not values: the values that matter (perm, roles) already have a home in
    // PermissionBakingLog.BakedDetail, closer to where they were resolved. Repeating them here
    // would drift the two lists apart the first time either changes. What this line answers that
    // nothing else does is which claim TYPES actually reached the issuer -- e.g. whether "amr" or
    // "acr" made it through from the flow, which a bare count can't show.
    [MessageLogging(EventId = 91199, Level = LogLevel.Trace,
        Message = "Token for '{audience}' carries claim types: {claimTypes}")]
    internal static partial IGenericMessage ClaimTypesAssembled(
        ILogger<JwtTokenIssuer> logger, string audience, string claimTypes);

    /// <summary>The cached signing key was reused rather than refetched.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="keyName">Its name in the secret manager.</param>
    [MessageLogging(EventId = 91186, Level = LogLevel.Trace,
        Message = "Reused the cached signing key '{keyName}'")]
    internal static partial IGenericMessage SigningKeyReused(
        ILogger<SecretManagerSigningCredentialProvider> logger, string keyName);

    /// <summary>No request was supplied.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91181, Level = LogLevel.Error,
        Message = "A request must be supplied to issue a token")]
    internal static partial IGenericMessage RequestMissing(ILogger<JwtTokenIssuer> logger);

    /// <summary>The request named no audience.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91182, Level = LogLevel.Error,
        Message = "A token cannot be issued without an audience")]
    internal static partial IGenericMessage AudienceMissing(ILogger<JwtTokenIssuer> logger);

    /// <summary>The configured issuer is not an absolute http(s) URI, so it cannot be normalised
    /// into the form the validation side declares (see IssuerName.Read).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">What was configured.</param>
    [MessageLogging(EventId = 91200, Level = LogLevel.Critical,
        Message = "Token manager's Issuer '{issuer}' is not an absolute http(s) URI, so it cannot be minted")]
    internal static partial IGenericMessage IssuerNotAbsolute(ILogger<JwtTokenIssuer> logger, string issuer);

    /// <summary>Issuance configuration is being resolved for the first time this process.</summary>
    /// <param name="logger">The logger.</param>
    // Fires once per process (the resolver caches forever after) -- this is the one line that shows
    // WHEN the auth.TokenManager/auth.JwtTokenManager read actually happened, ahead of the
    // IssuanceResolved milestone that reports what it found.
    [MessageLogging(EventId = 91204, Level = LogLevel.Trace,
        Message = "Resolving issuance configuration (first use since host start)")]
    internal static partial IGenericMessage ResolvingIssuance(ILogger<JwtIssuanceResolver> logger);

    /// <summary>The issuance configuration was resolved.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The token manager row's name.</param>
    /// <param name="issuer">What it mints into <c>iss</c>.</param>
    [MessageLogging(EventId = 91187, Level = LogLevel.Information,
        Message = "Resolved token manager '{name}' issuing as '{issuer}'")]
    internal static partial IGenericMessage IssuanceResolved(
        ILogger<JwtIssuanceResolver> logger, string name, string issuer);

    /// <summary>No token manager row declares the Jwt option.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="declared">The option types that were found.</param>
    // Critical: the host is running and cannot mint, and no request will fix it.
    [MessageLogging(EventId = 91188, Level = LogLevel.Critical,
        Message = "No enabled auth.TokenManager row declares Implementation 'Jwt' (found: {declared})")]
    internal static partial IGenericMessage NoJwtTokenManager(
        ILogger<JwtIssuanceResolver> logger, string declared);

    /// <summary>The header named no secret manager or key.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The token manager row's name.</param>
    [MessageLogging(EventId = 91189, Level = LogLevel.Critical,
        Message = "Token manager '{name}' names no SecretManagerName/SecretKeyName, so its signing key cannot be located")]
    internal static partial IGenericMessage SigningKeyNotLocatable(
        ILogger<JwtIssuanceResolver> logger, string name);

    /// <summary>The typed body row was missing.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The token manager row's name.</param>
    /// <param name="reason">What the read reported.</param>
    [MessageLogging(EventId = 91190, Level = LogLevel.Critical,
        Message = "Token manager '{name}' has no readable auth.JwtTokenManager body: {reason}")]
    internal static partial IGenericMessage TypedBodyUnreadable(
        ILogger<JwtIssuanceResolver> logger, string name, string? reason);

    /// <summary>The typed body named no issuer.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The token manager row's name.</param>
    [MessageLogging(EventId = 91191, Level = LogLevel.Critical,
        Message = "Token manager '{name}' names no Issuer, and a token minted without one matches no validator")]
    internal static partial IGenericMessage IssuerMissing(
        ILogger<JwtIssuanceResolver> logger, string name);

    /// <summary>The configured lifetime was not an ISO 8601 duration.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The token manager row's name.</param>
    /// <param name="exception">What the parser reported.</param>
    /// <param name="value">What was configured.</param>
    [MessageLogging(EventId = 91192, Level = LogLevel.Critical,
        Message = "Token manager '{name}' has AccessTokenLifetime '{value}', which is not an ISO 8601 duration")]
    internal static partial IGenericMessage LifetimeUnreadable(
        ILogger<JwtIssuanceResolver> logger, Exception exception, string name, string value);

    /// <summary>No lifetime was configured.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The token manager row's name.</param>
    [MessageLogging(EventId = 91194, Level = LogLevel.Critical,
        Message = "Token manager '{name}' names no AccessTokenLifetime, and a token with no expiry never stops being usable")]
    internal static partial IGenericMessage LifetimeMissing(
        ILogger<JwtIssuanceResolver> logger, string name);

    /// <summary>The token manager headers could not be read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="reason">What the read reported.</param>
    [MessageLogging(EventId = 91193, Level = LogLevel.Critical,
        Message = "Could not read auth.TokenManager rows: {reason}")]
    internal static partial IGenericMessage HeadersUnreadable(
        ILogger<JwtIssuanceResolver> logger, string? reason);

    /// <summary>A token's expiry was brought forward to when its claims stop being true.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="audience">Who it is for.</param>
    /// <param name="principalId">Who it names.</param>
    /// <param name="configured">The expiry the configured lifetime gave.</param>
    /// <param name="validUntil">When the claims stop being true, now the expiry.</param>
    [MessageLogging(EventId = 91209, Level = LogLevel.Debug,
        Message = "Token for '{audience}' naming principal {principalId} expires at {validUntil} instead of {configured}: its claims stop being true then")]
    internal static partial IGenericMessage ExpiryCapped(
        ILogger<JwtTokenIssuer> logger, string audience, Guid principalId, DateTimeOffset configured, DateTimeOffset validUntil);

    /// <summary>The claims a token would carry stopped being true before it could be minted.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="audience">Who it was for.</param>
    /// <param name="principalId">Who it would name.</param>
    /// <param name="validUntil">When the claims stopped being true.</param>
    // Warning, not Error: nothing is broken. A role assignment expired between resolving permissions
    // and minting, and the caller signs in again to get a token for what it holds now.
    [MessageLogging(EventId = 91210, Level = LogLevel.Warning,
        Message = "Refused to mint a token for '{audience}' naming principal {principalId}: its claims stopped being true at {validUntil}")]
    internal static partial IGenericMessage ClaimsAlreadyExpired(
        ILogger<JwtTokenIssuer> logger, string audience, Guid principalId, DateTimeOffset validUntil);
}
