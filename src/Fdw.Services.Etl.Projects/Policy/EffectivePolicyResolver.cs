using System;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Policy;

/// <summary>
/// Resolves the effective (fully-inherited) policy snapshot for a Project, Stage, or Step
/// by walking the parent chain and applying NULL-means-inherit semantics.
/// </summary>
public sealed class EffectivePolicyResolver : IEffectivePolicyResolver
{
    private readonly IServerPolicyDefaults _serverDefaults;

    /// <summary>
    /// Initializes a new instance of the <see cref="EffectivePolicyResolver"/> class.
    /// </summary>
    public EffectivePolicyResolver(IServerPolicyDefaults serverDefaults)
    {
        _serverDefaults = serverDefaults ?? throw new ArgumentNullException(nameof(serverDefaults));
    }

    /// <inheritdoc/>
    public ExecutionPolicySnapshot ResolveForProject(ProjectConfiguration project)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));

        return new ExecutionPolicySnapshot(
            StepFailurePolicy: project.StepFailurePolicy ?? _serverDefaults.StepFailurePolicy,
            StageFailurePolicy: project.StageFailurePolicy ?? _serverDefaults.StageFailurePolicy,
            MaxParallelPipelines: project.MaxParallelPipelines ?? _serverDefaults.MaxParallelPipelines,
            RequireApprovalToRun: project.RequireApprovalToRun ?? _serverDefaults.RequireApprovalToRun,
            AllowResume: project.AllowResume ?? _serverDefaults.AllowResume,
            AllowCrossTenant: project.AllowCrossTenant ?? _serverDefaults.AllowCrossTenant,
            ResiliencyPolicyId: project.ResiliencyPolicyId ?? _serverDefaults.ResiliencyPolicyId);
    }

    /// <inheritdoc/>
    public ExecutionPolicySnapshot ResolveForStage(StageConfiguration stage, ExecutionPolicySnapshot parentProjectEffective)
    {
        if (stage == null) throw new ArgumentNullException(nameof(stage));
        if (parentProjectEffective == null) throw new ArgumentNullException(nameof(parentProjectEffective));

        return new ExecutionPolicySnapshot(
            StepFailurePolicy: stage.StepFailurePolicy ?? parentProjectEffective.StepFailurePolicy,
            StageFailurePolicy: stage.StageFailurePolicy ?? parentProjectEffective.StageFailurePolicy,
            MaxParallelPipelines: stage.MaxParallelPipelines ?? parentProjectEffective.MaxParallelPipelines,
            RequireApprovalToRun: stage.RequireApprovalToRun ?? parentProjectEffective.RequireApprovalToRun,
            AllowResume: stage.AllowResume ?? parentProjectEffective.AllowResume,
            AllowCrossTenant: stage.AllowCrossTenant ?? parentProjectEffective.AllowCrossTenant,
            ResiliencyPolicyId: stage.ResiliencyPolicyId ?? parentProjectEffective.ResiliencyPolicyId);
    }

    /// <inheritdoc/>
    public ExecutionPolicySnapshot ResolveForStep(StepConfiguration step, ExecutionPolicySnapshot parentStageEffective)
    {
        if (step == null) throw new ArgumentNullException(nameof(step));
        if (parentStageEffective == null) throw new ArgumentNullException(nameof(parentStageEffective));

        return new ExecutionPolicySnapshot(
            StepFailurePolicy: step.StepFailurePolicy ?? parentStageEffective.StepFailurePolicy,
            StageFailurePolicy: step.StageFailurePolicy ?? parentStageEffective.StageFailurePolicy,
            MaxParallelPipelines: step.MaxParallelPipelines ?? parentStageEffective.MaxParallelPipelines,
            RequireApprovalToRun: step.RequireApprovalToRun ?? parentStageEffective.RequireApprovalToRun,
            AllowResume: step.AllowResume ?? parentStageEffective.AllowResume,
            AllowCrossTenant: step.AllowCrossTenant ?? parentStageEffective.AllowCrossTenant,
            ResiliencyPolicyId: step.ResiliencyPolicyId ?? parentStageEffective.ResiliencyPolicyId);
    }

    /// <inheritdoc/>
    public ExecutionPolicySnapshot ResolveForNode(OrchestrationNodeConfiguration node, ExecutionPolicySnapshot parentEffective)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        if (parentEffective == null) throw new ArgumentNullException(nameof(parentEffective));

        return new ExecutionPolicySnapshot(
            StepFailurePolicy: node.StepFailurePolicy ?? parentEffective.StepFailurePolicy,
            StageFailurePolicy: node.StageFailurePolicy ?? parentEffective.StageFailurePolicy,
            MaxParallelPipelines: node.MaxParallelPipelines ?? parentEffective.MaxParallelPipelines,
            RequireApprovalToRun: node.RequireApprovalToRun ?? parentEffective.RequireApprovalToRun,
            AllowResume: node.AllowResume ?? parentEffective.AllowResume,
            AllowCrossTenant: node.AllowCrossTenant ?? parentEffective.AllowCrossTenant,
            ResiliencyPolicyId: node.ResiliencyPolicyId ?? parentEffective.ResiliencyPolicyId);
    }
}
