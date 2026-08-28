using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.TokenManagers.Abstractions.Tokens;

namespace Fdw.Services.TokenManagers.Abstractions;

/// <summary>
/// The generic, provider-agnostic authN service — the single "front door" a host injects for
/// authentication. Holds the active <see cref="ITokenManager"/> plus the credential vault
/// (<c>IUserCredentialService</c>); OpenIddict-free by design. Two entry points mirror the two
/// authentication shapes a caller presents: a grant to be issued a new token, or an existing bearer
/// token to be validated.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a grant and issues a token. First-party credential grants (password/agent_key)
    /// are verified against the credential vault before delegating to the active
    /// <see cref="ITokenManager"/>'s <c>Issue</c>; other grants (e.g. client_credentials) are
    /// validated entirely inside the token manager.
    /// </summary>
    /// <param name="request">The token issuance request describing the grant.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult<ClaimsPrincipal>> Authenticate(TokenIssuanceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates an existing bearer token: validates it against the active
    /// <see cref="ITokenManager"/>, then extracts its claims.
    /// </summary>
    /// <param name="token">The bearer token to authenticate.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult<ClaimsPrincipal>> Authenticate(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fully logs out the bearer of <paramref name="token"/>: resolves the active
    /// <see cref="ITokenManager"/>, extracts the token's subject, revokes every server-side
    /// session/authorization for that subject (<see cref="ITokenRevoker.Logout"/>), then deny-lists the
    /// presented token itself (<see cref="ITokenRevoker.Revoke"/>) so it is rejected immediately.
    /// </summary>
    /// <param name="token">The bearer token presented for logout.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult> Logout(string token, CancellationToken cancellationToken = default);
}
