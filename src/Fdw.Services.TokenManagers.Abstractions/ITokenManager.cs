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
public interface ITokenManager : IServiceOption
{
    /// <summary>
    /// Issues a token for the grant described by <paramref name="request"/>. Credential validation
    /// for first-party grants (password/agent_key) happens upstream via <c>IUserCredentialService</c>;
    /// provider-specific validation (e.g. client_credentials secret check) happens inside this method.
    /// Returns the thin <see cref="ClaimsPrincipal"/> the provider will use to mint the actual token
    /// (e.g. via an OpenIddict SignIn) — not the serialized token string itself.
    /// </summary>
    /// <param name="request">The token issuance request describing the grant.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult<ClaimsPrincipal>> Issue(TokenIssuanceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a bearer token (signature, expiry, and any provider-specific invalidation check,
    /// e.g. a revocation-list lookup) and returns the principal it carries.
    /// </summary>
    /// <param name="token">The bearer token to validate.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult<ClaimsPrincipal>> Validate(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates a previously issued token so subsequent <see cref="Validate"/> calls reject it.
    /// </summary>
    /// <param name="token">The bearer token to invalidate.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult> Invalidate(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts the identity/claims carried by an already-validated token.
    /// </summary>
    /// <param name="token">The bearer token to extract claims from.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult<ClaimsPrincipal>> ExtractClaims(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes every active server-side session/authorization for <paramref name="subjectId"/> (e.g. an
    /// OpenIddict authorization and its refresh tokens). Distinct from <see cref="Invalidate"/>, which
    /// only deny-lists a single presented token — this tears down ALL of the subject's sessions.
    /// </summary>
    /// <param name="subjectId">The subject (user/service principal) identifier whose sessions are revoked.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult> Logout(string subjectId, CancellationToken cancellationToken = default);
}
