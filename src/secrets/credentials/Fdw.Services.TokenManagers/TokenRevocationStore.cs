using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.TokenManagers.Abstractions;
using Fdw.Services.TokenManagers.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// Writes and checks <c>auth.RevokedAccessToken</c> and <c>auth.UserTokenRevocation</c> (AuthDb) directly through
/// the configuration gateway for <see cref="TokenManagerTypes.RevocationConnection"/> — no <c>TokenManagerTypes</c>
/// dispatch, matching how <see cref="JwtIssuanceResolver"/> already bypasses it for issuance.
/// </summary>
/// <remarks>
/// AuthDb is a connection the host declares in its <c>configurationSchema.json</c>, so it is reached only through
/// its configuration gateway: no ConfigurationDb row names it, and no data route a user can call resolves it.
/// </remarks>
internal sealed class TokenRevocationStore : ITokenRevocationStore
{
    private const string PathName = "auth";
    private const string TokenContainerName = "RevokedAccessToken";
    private const string UserContainerName = "UserTokenRevocation";

    private readonly IConfigurationGatewayProvider _gateways;
    private readonly IAuthenticationContextAccessor _authContextAccessor;

    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="TokenRevocationStore"/> class.</summary>
    /// <param name="gateways">Holds the gateway for the host-declared AuthDb connection, the only path to the revocation tables (FDW-1084).</param>
    /// <param name="authContextAccessor">
    /// Required. Every read and write here runs under <see cref="SystemAuthenticationContextScope"/>,
    /// because the revocation tables are System-owned. It was optional once, and without it every
    /// operation ran as whoever called and failed to resolve AuthDb.
    /// </param>
    /// <param name="logger">The logger.</param>
    public TokenRevocationStore(
        IConfigurationGatewayProvider gateways,
        IAuthenticationContextAccessor authContextAccessor,
        ILogger<TokenRevocationStore>? logger = null)
    {
        _gateways = gateways ?? throw new ArgumentNullException(nameof(gateways));
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _logger = logger ?? NullLogger<TokenRevocationStore>.Instance;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Runs under <see cref="SystemAuthenticationContextScope"/>, like <see cref="IsRevoked"/>. The
    /// revocation tables and the AuthDb connection that reaches them are platform rows owned by the
    /// System tenant, so no caller's own context can resolve them: a Global-tenant admin cannot, and a
    /// tenant user logging out cannot either. Run as the caller, the write failed on "Configuration not
    /// found" for the AuthDb connection and nothing was revoked (FDW-875, found live 2026-09-23).
    /// </remarks>
    public async Task<IGenericResult> Revoke(
        Guid jti, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        using var scope = new SystemAuthenticationContextScope(_authContextAccessor);

        var call = Insert.Into<RevokedAccessTokenEntry>(TokenContainerName)
            .DataStore(TokenManagerTypes.RevocationConnection)
            .Path(PathName)
            .Value(new RevokedAccessTokenEntry { Jti = jti, ExpiresAt = expiresAt });

        var result = await _gateways.Execute<int>(TokenManagerTypes.RevocationConnection, call, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return GenericResult.Failure(RevocationLog.RevokeFailed(_logger, jti));

        RevocationLog.Revoked(_logger, jti);
        return GenericResult.Success();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Runs under <see cref="SystemAuthenticationContextScope"/> for the same reason as
    /// <see cref="Revoke"/>: the row lands in a System-owned table whatever tenant the caller acts in.
    /// </remarks>
    public async Task<IGenericResult> RevokeAll(
        Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
    {
        using var scope = new SystemAuthenticationContextScope(_authContextAccessor);

        var call = Insert.Into<UserTokenRevocationEntry>(UserContainerName)
            .DataStore(TokenManagerTypes.RevocationConnection)
            .Path(PathName)
            .Value(new UserTokenRevocationEntry { UserId = userId, RevokedAt = revokedAt });

        var result = await _gateways.Execute<int>(TokenManagerTypes.RevocationConnection, call, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return GenericResult.Failure(RevocationLog.UserRevokeFailed(_logger, userId));

        RevocationLog.UserRevoked(_logger, userId, revokedAt);
        return GenericResult.Success();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Runs under <see cref="SystemAuthenticationContextScope"/> when an accessor is available: this
    /// is called from inside <c>LocalKeyAuthenticationHandler.AuthenticateAsync</c>, deciding whether
    /// the very token being authenticated is still good — which runs during <c>UseAuthentication()</c>,
    /// before <c>RequestContextMiddleware</c> has turned that principal into the ambient tenant/RLS
    /// context later stages read. With no ambient context yet, the read opened as the deny principal
    /// and <c>data.DataPath</c>/<c>data.DataContainer</c> resolved zero rows for AuthDb even though the
    /// registration was correct and globally-scoped (<c>TenantId IS NULL</c>) — the same bootstrap gap
    /// <see cref="SystemAuthenticationContextScope"/> already covers for host startup, here at
    /// per-request granularity instead.
    /// <para>
    /// A user cut-off rejects a token when any row for its subject is at or after its issued-at. The
    /// <c>iat</c> claim has whole-second precision, so a token issued in the same second as a
    /// revocation but after it is rejected too; the holder signs in again. Erring that way is the
    /// safe direction.
    /// </para>
    /// </remarks>
    public async Task<IGenericResult<bool>> IsRevoked(
        Guid jti, Guid userId, DateTimeOffset issuedAt, CancellationToken cancellationToken = default)
    {
        RevocationLog.Checking(_logger, jti);

        using var scope = new SystemAuthenticationContextScope(_authContextAccessor);

        var tokenCall = Query.From<RevokedAccessTokenEntry>(TokenManagerTypes.RevocationConnection, PathName, TokenContainerName)
            .Where(r => r.Jti).Equal(jti)
            .Build();

        var tokenResult = await _gateways.Execute<IEnumerable<RevokedAccessTokenEntry>>(TokenManagerTypes.RevocationConnection, tokenCall, cancellationToken)
            .ConfigureAwait(false);
        if (tokenResult.IsFailure)
            return GenericResult<bool>.Failure(RevocationLog.CheckFailed(_logger, jti));

        if (tokenResult.Value!.Any())
        {
            RevocationLog.PresentedRevoked(_logger, jti);
            return GenericResult<bool>.Success(true);
        }

        var userCall = Query.From<UserTokenRevocationEntry>(TokenManagerTypes.RevocationConnection, PathName, UserContainerName)
            .Where(r => r.UserId).Equal(userId)
            .Where(r => r.RevokedAt).GreaterThanOrEqual(issuedAt)
            .Build();

        var userResult = await _gateways.Execute<IEnumerable<UserTokenRevocationEntry>>(TokenManagerTypes.RevocationConnection, userCall, cancellationToken)
            .ConfigureAwait(false);
        if (userResult.IsFailure)
            return GenericResult<bool>.Failure(RevocationLog.CheckFailed(_logger, jti));

        if (userResult.Value!.Any())
        {
            RevocationLog.PresentedUserRevoked(_logger, jti, userId);
            return GenericResult<bool>.Success(true);
        }

        return GenericResult<bool>.Success(false);
    }
}
