using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// Supplies the flows this host serves.
/// </summary>
/// <remarks>
/// Flows are <c>ServerConfiguration</c>: which providers a host accepts is that host's business, so
/// two hosts in one tenant legitimately differ. A binding between a provider subject and a user is
/// the opposite — a fact about the tenant — and lives elsewhere.
/// </remarks>
public interface IAuthenticationFlowProvider
{
    /// <summary>Returns the flow named <paramref name="flowName"/>.</summary>
    /// <param name="flowName">What the caller selected.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// Fails when no such flow is configured, which is what makes an unknown selection a clear
    /// refusal rather than a login that silently does nothing.
    /// </remarks>
    Task<IGenericResult<AuthenticationFlow>> Get(
        string flowName, CancellationToken cancellationToken = default);

    /// <summary>Loads every configured flow and validates each one.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// Run at startup. A flow ordering a step before its requirement, or naming a step whose package
    /// was removed, is caught here — naming the flow and what is missing — rather than at a login.
    /// Each flow is judged on its own: one flow failing this check never stops another, valid flow
    /// from loading. This only fails outright when the configuration itself can't be read at all.
    /// </remarks>
    Task<IGenericResult> LoadAndValidate(CancellationToken cancellationToken = default);
}
