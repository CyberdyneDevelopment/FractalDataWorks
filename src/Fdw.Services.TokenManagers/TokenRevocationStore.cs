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
/// Writes and checks <c>auth.RevokedAccessToken</c> (AuthDb) directly through
/// <see cref="IDataGateway"/> — no <c>TokenManagerTypes</c> dispatch, matching how
/// <see cref="JwtIssuanceResolver"/> already bypasses it for issuance.
/// </summary>
internal sealed class TokenRevocationStore : ITokenRevocationStore
{
    private const string DataStoreName = "AuthDb";
    private const string PathName = "auth";
    private const string ContainerName = "RevokedAccessToken";

    private readonly IDataGatewayProvider _dataGateways;
    private readonly IAuthenticationContextAccessor? _authContextAccessor;

    // Why resolved here rather than injected: the gateway is scoped and this is not, so holding one
    // would be a captive dependency. The provider is asked when a call is actually being made.
    private IDataGateway Gateway => _dataGateways.ByName("Main");
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="TokenRevocationStore"/> class.</summary>
    /// <param name="dataGateways">Supplies the gateway AuthDb is reached through.</param>
    /// <param name="authContextAccessor">
    /// Optional. Elevates <see cref="IsRevoked"/>'s read to <see cref="SystemAuthenticationContextScope"/>
    /// when supplied — see the remarks on <see cref="IsRevoked"/> for why that read needs it.
    /// </param>
    /// <param name="logger">The logger.</param>
    public TokenRevocationStore(
        IDataGatewayProvider dataGateways,
        IAuthenticationContextAccessor? authContextAccessor = null,
        ILogger<TokenRevocationStore>? logger = null)
    {
        _dataGateways = dataGateways ?? throw new ArgumentNullException(nameof(dataGateways));
        _authContextAccessor = authContextAccessor;
        _logger = logger ?? NullLogger<TokenRevocationStore>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Revoke(
        Guid jti, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        var call = Insert.Into<RevokedAccessTokenEntry>(ContainerName)
            .DataStore(DataStoreName)
            .Path(PathName)
            .Value(new RevokedAccessTokenEntry { Jti = jti, ExpiresAt = expiresAt });

        var result = await Gateway.Execute<int>(call, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return GenericResult.Failure(
                RevocationLog.RevokeFailed(_logger, jti, result.CurrentMessage ?? "write failed"));

        RevocationLog.Revoked(_logger, jti);
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
    /// per-request granularity instead. <see cref="Revoke"/> is NOT wrapped: it runs from
    /// <c>LogoutEndpoint</c>, downstream of <c>RequestContextMiddleware</c>, where a real context
    /// already exists.
    /// </remarks>
    public async Task<IGenericResult<bool>> IsRevoked(Guid jti, CancellationToken cancellationToken = default)
    {
        var call = Query.From<RevokedAccessTokenEntry>(DataStoreName, PathName, ContainerName)
            .Where(r => r.Jti).Equal(jti)
            .Build();

        IGenericResult<IEnumerable<RevokedAccessTokenEntry>> result;
        if (_authContextAccessor is null)
        {
            result = await Gateway.Execute<IEnumerable<RevokedAccessTokenEntry>>(call, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            using var scope = new SystemAuthenticationContextScope(_authContextAccessor);
            result = await Gateway.Execute<IEnumerable<RevokedAccessTokenEntry>>(call, cancellationToken)
                .ConfigureAwait(false);
        }

        if (result.IsFailure)
            return GenericResult<bool>.Failure(
                RevocationLog.CheckFailed(_logger, jti, result.CurrentMessage ?? "read failed"));

        var revoked = result.Value?.Any() == true;
        if (revoked)
            RevocationLog.PresentedRevoked(_logger, jti);

        return GenericResult<bool>.Success(revoked);
    }
}
