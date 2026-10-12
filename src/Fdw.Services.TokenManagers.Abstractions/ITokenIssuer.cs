using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.TokenManagers.Abstractions;

/// <summary>
/// Mints tokens. Flow-time only.
/// </summary>
/// <remarks>
/// Separate from validation because the two are not the same capability. Either this platform is the
/// authorization server or it is not — issuance is exclusive, not pluggable. Validation is genuinely
/// interchangeable: local verification, introspection at the issuer, a sender-constrained scheme.
/// Fusing them made a resource server implement minting it has no business performing to obtain
/// checking it needs on every request.
/// <para>
/// Takes only what a token asserts, not the authentication context. The runner maps one to the
/// other, so neither this nor the pipeline depends on the other.
/// </para>
/// </remarks>
public interface ITokenIssuer
{
    /// <summary>Issues a token asserting what <paramref name="request"/> names.</summary>
    /// <param name="request">What the token is to assert.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult<IssuedToken>> Issue(
        IssuanceRequest request, CancellationToken cancellationToken = default);
}
