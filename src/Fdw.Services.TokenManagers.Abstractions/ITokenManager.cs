using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.TokenManagers.Abstractions.Tokens;

namespace Fdw.Services.TokenManagers.Abstractions;

/// <summary>
/// Provider axis for token issuance/validation/invalidation/claims-extraction. One
/// <see cref="ITokenManager"/> implementation backs one concrete token provider (e.g. OpenIddict,
/// Entra) registered as a <c>TokenManagerTypes</c> <c>[ServiceTypeOption]</c>. The generic authN
/// service resolves the active token manager and delegates to these four operations — it never
/// knows which provider is behind the interface.
/// </summary>
/// <summary>
/// A token scheme — the thing that mints, checks and revokes tokens of one kind.
/// </summary>
/// <remarks>
/// <para>
/// Composes the three capabilities rather than declaring five loose methods, so a consumer depends
/// on the narrow interface it actually needs. A resource server takes <see cref="ITokenValidator"/>
/// and never sees <c>Issue</c>; a login flow takes <see cref="ITokenIssuer"/> and never sees
/// revocation. The same instance is registered under all four.
/// </para>
/// <para>
/// An implementation of a scheme legitimately does all three — OpenIddict mints and checks and
/// revokes. What was wrong before was making every <em>consumer</em> see all of it.
/// </para>
/// </remarks>
public interface ITokenManager : IServiceOption, ITokenIssuer, ITokenValidator, ITokenRevoker
{
    /// <summary>Authenticates and mints in one call, dispatching on the request's grant type.</summary>
    /// <param name="request">The grant request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// This is what the step pipeline replaces. Each grant type it switches on — password, agent
    /// key, external identity, client credentials — is a sequence of steps expressed here as a
    /// branch, which is why adding a fifth means editing this method rather than adding a
    /// configuration row.
    /// <para>
    /// It stays until flows cover the same ground, because the token endpoint calls it today.
    /// Nothing new should take a dependency on it.
    /// </para>
    /// </remarks>
    Task<IGenericResult<ClaimsPrincipal>> AuthenticateAndIssue(
        TokenIssuanceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Reads a token's claims without asserting the token is currently valid.</summary>
    /// <param name="token">The token.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// Separate from <see cref="ITokenValidator.Validate"/> and deliberately weaker: this parses,
    /// that verifies. A caller wanting to know whether a token is good must use the validator —
    /// reading claims from an unverified token is how forged tokens get trusted.
    /// </remarks>
    Task<IGenericResult<ClaimsPrincipal>> ExtractClaims(
        string token, CancellationToken cancellationToken = default);
}
