using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Services.Resiliency.Abstractions;

/// <summary>
/// Resolves the effective resiliency policy for a stage by walking the hierarchy:
/// step → stage → project → tenant → server default.
/// </summary>
/// <remarks>
/// Returns the first non-null <c>ResiliencyPolicyId</c> found walking upward.
/// The server default must be validated at startup (fail-fast if missing in non-Development).
/// </remarks>
public interface IEffectiveResiliencyResolver
{
    /// <summary>
    /// Resolves the effective resiliency policy identifier for the given stage.
    /// </summary>
    /// <param name="stageId">The stage identifier to resolve the policy for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The effective <see cref="Guid"/> policy identifier, or <c>null</c> if no policy is configured
    /// at any level and no server default is set.
    /// </returns>
    Task<Guid?> ResolveForStage(Guid stageId, CancellationToken cancellationToken = default);
}
