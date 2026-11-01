using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Multitenancy.Abstractions;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// Answers whether a principal may hold a token at all.
/// </summary>
public interface IIssuanceEligibility
{
    /// <summary>Decides for <paramref name="principal"/>.</summary>
    /// <param name="principal">The resolved principal.</param>
    /// <param name="tenants">
    /// The run's tenant provider, the one the flow runner hands every step -- it answers whether the
    /// principal may act in the tenant the token is for. Passed per call rather than injected: a
    /// tenant provider is request-scoped, and the step that asks is created once for the host.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// A denial is a successful result carrying a refusing decision, not a failure. The distinction
    /// matters: a failure means the question could not be answered, which is not the same as
    /// answering no, and a caller retries one but not the other.
    /// </remarks>
    Task<IGenericResult<Decision>> MayBeIssued(
        Principal principal, ITenantProvider tenants, CancellationToken cancellationToken = default);
}
