using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using Fdw.Claims.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Authentication.Validation;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Fdw.Services.Authentication.Sessions;

/// <summary>
/// The two directions between a server-side session and the principal it authenticates: the claims
/// the principal carries (FDW-903), and the assertion those claims amount to when a token has to be
/// minted for the same caller (FDW-925).
/// </summary>
/// <remarks>
/// Both live here so they cannot drift. Every claim <see cref="Claims"/> writes, <see cref="Assertion"/>
/// reads back under the same name; a claim added to one and not the other would silently drop out of
/// every token minted for a session caller, and the downstream would refuse what the caller may do.
/// </remarks>
internal static class SessionPrincipal
{
    // RFC 9068 names it; JwtRegisteredClaimNames does not carry it.
    private const string ScopeClaim = "scope";

    // What the principal carries about itself rather than what the assertion asserts. Each is either
    // set by the issuer from the request's own fields or names this session, never a caller's claim.
    private static readonly HashSet<string> Bookkeeping = new(StringComparer.Ordinal)
    {
        ClaimDefinitions.sub.Name,
        ClaimTypes.NameIdentifier,
        ClaimDefinitions.sid.Name,
        ClaimDefinitions.tenantId.Name,
        ScopeClaim,
        JwtRegisteredClaimNames.Amr,
        JwtRegisteredClaimNames.Acr,
        JwtRegisteredClaimNames.Aud,
        JwtRegisteredClaimNames.Exp,
    };

    /// <summary>The claims a session's principal carries.</summary>
    /// <param name="session">The live session.</param>
    /// <returns>The same claims JwtTokenIssuer puts in a token minted from the same assertion, plus <c>sid</c>.</returns>
    /// <remarks>
    /// A principal cannot tell which way it arrived except by <c>sid</c>. A claim the assertion repeats
    /// under a name the issuer already set (<c>sub</c>, <c>tenantId</c>) is not added twice, as the
    /// issuer's TryAdd does not. <c>aud</c> and <c>exp</c> are what a bearer principal carries from its
    /// token: the audience the sign-in flow was configured for, and when the session ends however active
    /// it stays -- which the store already caps at the moment the claims stop being true.
    /// </remarks>
    public static IReadOnlyList<Claim> Claims(SessionRecord session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var assertion = session.Assertion;
        var claims = new List<Claim>
        {
            new(ClaimDefinitions.sub.Name, assertion.PrincipalId.ToString()),
            new(ClaimTypes.NameIdentifier, assertion.PrincipalId.ToString()),
            new(ClaimDefinitions.sid.Name, session.Id.ToString()),
            new(ClaimDefinitions.tenantId.Name, assertion.TenantId.ToString()),
            new(ScopeClaim, string.Join(' ', assertion.Scopes)),
            new(JwtRegisteredClaimNames.Aud, assertion.Audience),
            new(JwtRegisteredClaimNames.Exp,
                session.AbsoluteExpiresAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
        };

        claims.AddRange(assertion.AuthenticationMethods.Select(method => new Claim(JwtRegisteredClaimNames.Amr, method)));

        if (!string.IsNullOrWhiteSpace(assertion.Acr))
            claims.Add(new Claim(JwtRegisteredClaimNames.Acr, assertion.Acr));

        var set = claims.Select(c => c.Type).ToHashSet(StringComparer.Ordinal);
        foreach (var (type, values) in assertion.Claims)
        {
            if (set.Contains(type))
                continue;

            claims.AddRange(values.Select(value => new Claim(type, value)));
        }

        return claims;
    }

    /// <summary>The identity a Session scheme authenticated, or null when no session authenticated the principal.</summary>
    /// <param name="principal">The request's principal.</param>
    /// <returns>The authenticated session identity, or null.</returns>
    public static ClaimsIdentity? Identity(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.Identities.FirstOrDefault(identity =>
            identity.IsAuthenticated
            && identity.AuthenticationType is { } scheme
            && scheme.StartsWith(SessionAuthenticationType.SchemePrefix, StringComparison.Ordinal));
    }

    /// <summary>Reads back what a session identity asserts, to mint a token for the same caller.</summary>
    /// <param name="identity">An identity <see cref="Identity"/> returned.</param>
    /// <param name="notAfter">The latest the minted token may live; the session's own end caps it further.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The assertion, or a failure when the identity is not one <see cref="Claims"/> built.</returns>
    /// <remarks>
    /// Nothing the caller sends reaches this: every value comes from the claims the session row held,
    /// restored by the Session scheme, so the token asserts exactly what the session already does.
    /// </remarks>
    public static IGenericResult<IssuanceRequest> Assertion(ClaimsIdentity identity, DateTimeOffset notAfter, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(identity);

        if (!Guid.TryParse(identity.FindFirst(ClaimDefinitions.sub.Name)?.Value, out var principalId))
            return Unreadable(logger, $"'{ClaimDefinitions.sub.Name}' is not a principal id");

        if (!Guid.TryParse(identity.FindFirst(ClaimDefinitions.tenantId.Name)?.Value, out var tenantId))
            return Unreadable(logger, $"'{ClaimDefinitions.tenantId.Name}' is not a tenant id");

        if (identity.FindFirst(JwtRegisteredClaimNames.Aud)?.Value is not { Length: > 0 } audience)
            return Unreadable(logger, $"it carries no '{JwtRegisteredClaimNames.Aud}'");

        if (!long.TryParse(identity.FindFirst(JwtRegisteredClaimNames.Exp)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ends))
            return Unreadable(logger, $"'{JwtRegisteredClaimNames.Exp}' is not a moment");

        if (identity.FindFirst(ScopeClaim) is not { } scope)
            return Unreadable(logger, $"it carries no '{ScopeClaim}'");

        var sessionEnds = DateTimeOffset.FromUnixTimeSeconds(ends);

        return GenericResult<IssuanceRequest>.Success(new IssuanceRequest
        {
            PrincipalId = principalId,
            TenantId = tenantId,
            Audience = audience,
            Scopes = scope.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries),
            AuthenticationMethods = [.. identity.FindAll(JwtRegisteredClaimNames.Amr).Select(c => c.Value)],
            Acr = identity.FindFirst(JwtRegisteredClaimNames.Acr)?.Value,
            ValidUntil = sessionEnds < notAfter ? sessionEnds : notAfter,
            Claims = identity.Claims
                .Where(c => !Bookkeeping.Contains(c.Type))
                .GroupBy(c => c.Type, StringComparer.Ordinal)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<string>)[.. g.Select(c => c.Value)],
                    StringComparer.Ordinal),
        });
    }

    private static IGenericResult<IssuanceRequest> Unreadable(ILogger logger, string reason)
        => GenericResult<IssuanceRequest>.Failure(SessionLog.SessionPrincipalUnreadable(logger, reason));
}
