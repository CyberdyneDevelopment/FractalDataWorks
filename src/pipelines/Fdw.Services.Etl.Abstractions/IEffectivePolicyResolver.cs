using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Abstractions;

/// <summary>
/// Resolves the effective (inherited) policy snapshot for a Project, Stage, or Step by
/// walking the parent chain and applying NULL-means-inherit semantics.
/// </summary>
/// <remarks>
/// The resolution algorithm for each field:
/// <list type="number">
/// <item>If the entity has an explicit (non-null) value, use it.</item>
/// <item>Otherwise, use the parent's effective value.</item>
/// <item>At Project level and at a root node, the parent is the server's defaults, read from
/// <see cref="IServerPolicyDefaultConfigurationProvider"/> when the policy is resolved (FDW-3580).</item>
/// </list>
/// </remarks>
public interface IEffectivePolicyResolver
{
    /// <summary>
    /// Resolves the effective policy for a project, inheriting from server defaults for NULL fields.
    /// </summary>
    /// <param name="project">The project configuration.</param>
    /// <param name="cancellationToken">Cancels the read of the server defaults.</param>
    /// <returns>The fully resolved policy snapshot with no null values, or the failed read of the server defaults.</returns>
    Task<IGenericResult<ExecutionPolicySnapshot>> ResolveForProject(ProjectConfiguration project, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the effective policy for a stage, inheriting from the parent project's effective policy for NULL fields.
    /// </summary>
    /// <param name="stage">The stage configuration.</param>
    /// <param name="parentProjectEffective">The already-resolved effective policy of the parent project.</param>
    /// <returns>The fully resolved policy snapshot with no null values.</returns>
    ExecutionPolicySnapshot ResolveForStage(StageConfiguration stage, ExecutionPolicySnapshot parentProjectEffective);

    /// <summary>
    /// Resolves the effective policy for a step, inheriting from the parent stage's effective policy for NULL fields.
    /// </summary>
    /// <param name="step">The step configuration.</param>
    /// <param name="parentStageEffective">The already-resolved effective policy of the parent stage.</param>
    /// <returns>The fully resolved policy snapshot with no null values.</returns>
    ExecutionPolicySnapshot ResolveForStep(StepConfiguration step, ExecutionPolicySnapshot parentStageEffective);

    /// <summary>
    /// Resolves the effective policy for an orchestration node, inheriting from the parent node's
    /// already-resolved effective policy for NULL fields. At root nodes, supply server defaults
    /// wrapped in a policy snapshot.
    /// </summary>
    /// <param name="node">The orchestration node configuration.</param>
    /// <param name="parentEffective">The already-resolved effective policy of the parent node (or server defaults at root).</param>
    /// <returns>The fully resolved policy snapshot with no null values.</returns>
    ExecutionPolicySnapshot ResolveForNode(IOrchestrationNodeImplementationConfiguration node, ExecutionPolicySnapshot parentEffective);

    /// <summary>
    /// Resolves the effective policy for a node that has no parent node (a project at the root of its tree), inheriting
    /// from the server defaults for NULL fields (FDW-1740).
    /// </summary>
    /// <param name="node">The root orchestration node configuration.</param>
    /// <param name="cancellationToken">Cancels the read of the server defaults.</param>
    /// <returns>The fully resolved policy snapshot with no null values, or the failed read of the server defaults.</returns>
    Task<IGenericResult<ExecutionPolicySnapshot>> ResolveForRootNode(IOrchestrationNodeImplementationConfiguration node, CancellationToken cancellationToken = default);
}
