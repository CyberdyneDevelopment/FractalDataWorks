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
/// <item>At Project level, the parent is <see cref="IServerPolicyDefaults"/>.</item>
/// </list>
/// </remarks>
public interface IEffectivePolicyResolver
{
    /// <summary>
    /// Resolves the effective policy for a project, inheriting from server defaults for NULL fields.
    /// </summary>
    /// <param name="project">The project configuration.</param>
    /// <returns>The fully resolved policy snapshot with no null values.</returns>
    ExecutionPolicySnapshot ResolveForProject(ProjectConfiguration project);

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
    ExecutionPolicySnapshot ResolveForNode(OrchestrationNodeConfiguration node, ExecutionPolicySnapshot parentEffective);
}
