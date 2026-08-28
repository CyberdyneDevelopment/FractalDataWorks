using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.TokenManagers.Abstractions;
using Fdw.Services.TokenManagers.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// Mints a signed JWT asserting what a completed flow established.
/// </summary>
/// <remarks>
/// Follows the access-token profile in RFC 9068, so a resource server can verify one without
/// agreeing a private convention first. It mints and nothing else — what may be minted was settled
/// before this is reached.
/// </remarks>
public sealed class JwtTokenIssuer : ITokenIssuer
{
    private readonly JwtTokenIssuerConfiguration _configuration;
    private readonly ISigningCredentialProvider _credentials;
    private readonly ILogger<JwtTokenIssuer> _logger;

    /// <summary>Initializes a new instance of the <see cref="JwtTokenIssuer"/> class.</summary>
    /// <param name="configuration">Issuer identity and token lifetime.</param>
    /// <param name="credentials">Supplies the current signing key.</param>
    /// <param name="logger">The logger.</param>
    public JwtTokenIssuer(
        JwtTokenIssuerConfiguration configuration,
        ISigningCredentialProvider credentials,
        ILogger<JwtTokenIssuer>? logger = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        _logger = logger ?? NullLogger<JwtTokenIssuer>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IssuedToken>> Issue(
        IssuanceRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            return GenericResult<IssuedToken>.Failure(IssuerLog.RequestMissing(_logger));

        if (string.IsNullOrWhiteSpace(request.Audience))
            return GenericResult<IssuedToken>.Failure(IssuerLog.AudienceMissing(_logger));

        var signing = await _credentials.Current(cancellationToken).ConfigureAwait(false);
        if (signing.IsFailure)
            return signing.ToNewResult<IssuedToken>();

        var issuedAt = DateTimeOffset.UtcNow;
        var expiresAt = issuedAt.Add(_configuration.Lifetime);

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [ClaimDefinitions.sub.Name] = request.PrincipalId.ToString(),

            // ClaimDefinitions rather than a literal: the RLS path reads the tenant through the
            // same definition, and a token minting "tid" while the session-context builder looks
            // for "tenantId" produces a request that reaches the database with no tenant scoping
            // at all. One place decides what a claim is called.
            [ClaimDefinitions.tenantId.Name] = request.TenantId.ToString(),

            // RFC 9068 §2.2 — a JWT access token is typed, so a resource server can refuse an
            // id_token presented in its place rather than accepting one that happens to verify.
            ["scope"] = string.Join(' ', request.Scopes),
        };

        if (request.AuthenticationMethods.Count > 0)
            claims["amr"] = request.AuthenticationMethods;

        if (!string.IsNullOrWhiteSpace(request.Acr))
            claims["acr"] = request.Acr;

        foreach (var (type, value) in request.Claims)
            claims.TryAdd(type, value);

        IssuerLog.Minting(_logger, request.Audience, claims.Count);

        var token = new JsonWebTokenHandler
        {
            SetDefaultTimesOnTokenCreation = false,
        }
        .CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _configuration.Issuer,
            Audience = request.Audience,
            Claims = claims,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = signing.Value,
            TokenType = "at+jwt",
        });

        IssuerLog.Issued(_logger, request.Audience, request.PrincipalId, request.Acr ?? "none");

        return GenericResult<IssuedToken>.Success(new IssuedToken
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresAt = expiresAt,
        });
    }
}
