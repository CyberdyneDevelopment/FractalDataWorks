using System.Threading;
using System.Threading.Tasks;
using Fdw.Aegis.Abstractions;
using Fdw.Results;
using Fdw.Services.SecretManagers;

namespace Fdw.Aegis;

/// <summary>
/// A pluggable downstream target that <see cref="AegisInjector"/> hands a resolved
/// <see cref="SecretValue"/> to, below the boundary, for exactly the duration of one injection call.
/// </summary>
/// <remarks>
/// Why a seam here: Phase 1 ships <see cref="Targets.HttpHeaderInjectionTarget"/> (an outbound HTTP
/// header). Future targets (a spawned CLI's environment, a different header shape) implement this
/// same interface — <see cref="AegisInjector"/> never changes to add one.
/// </remarks>
public interface IAegisInjectionTarget
{
    /// <summary>
    /// Injects <paramref name="secret"/> into the downstream call described by
    /// <paramref name="request"/> and returns a sanitized outcome.
    /// </summary>
    /// <param name="request">The approved request (connection/command/parameters — never a secret value).</param>
    /// <param name="secret">The resolved secret, valid only for the duration of this call.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A sanitized <see cref="AegisInjectionOutcome"/> — never the secret.</returns>
    Task<IGenericResult<AegisInjectionOutcome>> Inject(
        ApprovalRequest request,
        SecretValue secret,
        CancellationToken cancellationToken = default);
}
