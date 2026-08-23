using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Identity.Abstractions;

/// <summary>
/// Obtains a short-lived token proving that <em>this process</em> is the subject it claims to be,
/// scoped to a named audience. One implementation backs one mechanism for proving that (client
/// credentials, or a signed JWT assertion), registered as an <c>IdentityServiceTypes</c>
/// <c>[ServiceTypeOption]</c>. The mechanism is the axis, not the authorization server: both are
/// standard grants and work against any server implementing them.
/// </summary>
/// <remarks>
/// <para>
/// This is the OUTBOUND, service-is-the-subject axis, and it is deliberately distinct from every
/// other authentication domain in FDW:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <c>ITokenManager</c> is FDW acting as an authorization SERVER — it mints and validates tokens for
/// inbound callers. This interface is FDW acting as a CLIENT; the token it returns was signed by an
/// external authority FDW cannot mint for.
/// </description></item>
/// <item><description>
/// <c>IExternalIdentityProvider</c> federates HUMAN login inbound. This federates SERVICE identity
/// outbound. Both may point at the same deployment; they share no code path.
/// </description></item>
/// <item><description>
/// <c>ISecretManager</c> returns the value stored under a key — the caller is not the subject and
/// nothing is minted. Here nothing is stored, the caller IS the subject, and audience/scope/expiry
/// are first-class. An implementation may READ its own credential through <c>ISecretManager</c>;
/// that makes it a consumer, not a specialization.
/// </description></item>
/// </list>
/// <para>
/// Callers normally never touch this interface. It feeds <c>IAccessTokenProvider</c>, the seam every
/// FDW typed HTTP client already goes through, so outbound calls acquire a token without any call
/// site changing.
/// </para>
/// </remarks>
public interface IIdentityService : IServiceOption
{
    /// <summary>
    /// Acquires a token asserting this process's own identity for the audience and scopes described
    /// by <paramref name="request"/>.
    /// </summary>
    /// <param name="request">What is being asked for — the audience the token must be valid at, and the scopes requested.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    /// <returns>
    /// The issued token on success. On failure the result carries the reason — no configuration, the
    /// provider was unreachable, or the credential was rejected are distinct outcomes and are never
    /// collapsed into a single empty value.
    /// </returns>
    Task<IGenericResult<IssuedIdentityToken>> Acquire(IdentityTokenRequest request, CancellationToken cancellationToken = default);
}
