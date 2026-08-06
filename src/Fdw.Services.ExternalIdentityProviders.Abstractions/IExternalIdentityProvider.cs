using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Abstractions;

/// <summary>
/// Provider axis for "a thing that validates an external IdP token and produces a
/// <see cref="ClaimsPrincipal"/>". One <see cref="IExternalIdentityProvider"/> implementation backs
/// one concrete external identity provider (e.g. a standard OIDC authority, Azure AD, Auth0)
/// registered as an <c>ExternalIdentityProviderTypes</c> <c>[ServiceTypeOption]</c>. Unlike
/// <c>ITokenManager</c> (a "declared choice" domain with exactly one active provider per host),
/// multiple <see cref="IExternalIdentityProvider"/> configurations may be simultaneously active — the
/// caller selects one by name or, when exactly one is active, that one is used implicitly.
/// </summary>
public interface IExternalIdentityProvider : IServiceOption
{
    /// <summary>
    /// Validates an externally-issued token (e.g. an OIDC JWT from the configured Authority) and
    /// returns the <see cref="ClaimsPrincipal"/> it carries. Validation covers signature, issuer,
    /// audience, and lifetime at minimum; a missing/invalid token, an unreachable signing-key source,
    /// or a failed check is a structured, logged failure — never a defaulted/empty principal.
    /// </summary>
    /// <param name="token">The external token to validate.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult<ClaimsPrincipal>> ValidateExternalToken(string token, CancellationToken cancellationToken = default);
}
