using System;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Projects.Policy;

/// <summary>
/// Resolves the effective (fully-inherited) policy snapshot for a Project, Stage, or Step
/// by walking the parent chain and applying NULL-means-inherit semantics.
/// </summary>
public sealed class EffectivePolicyResolver : IEffectivePolicyResolver
{
    private readonly IServerPolicyDefaultConfigurationProvider _serverDefaults;

    /// <summary>
    /// Initializes a new instance of the <see cref="EffectivePolicyResolver"/> class.
    /// </summary>
    public EffectivePolicyResolver(IServerPolicyDefaultConfigurationProvider serverDefaults)
    {
        _serverDefaults = serverDefaults;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<ExecutionPolicySnapshot>> ResolveForProject(ProjectConfiguration project, CancellationToken cancellationToken = default)
    {
        var server = await ServerSnapshot(cancellationToken).ConfigureAwait(false);
        if (server.IsFailure || server.Value is not { } parent)
        {
            return GenericResult<ExecutionPolicySnapshot>.Failure(server.NoValueReason());
        }

        return GenericResult<ExecutionPolicySnapshot>.Success(new ExecutionPolicySnapshot(
            StepFailurePolicy: project.StepFailurePolicy ?? parent.StepFailurePolicy,
            StageFailurePolicy: project.StageFailurePolicy ?? parent.StageFailurePolicy,
            MaxParallelPipelines: project.MaxParallelPipelines ?? parent.MaxParallelPipelines,
            RequireApprovalToRun: project.RequireApprovalToRun ?? parent.RequireApprovalToRun,
            AllowResume: project.AllowResume ?? parent.AllowResume,
            AllowCrossTenant: project.AllowCrossTenant ?? parent.AllowCrossTenant,
            ResiliencyPolicyId: project.ResiliencyPolicyId ?? parent.ResiliencyPolicyId));
    }

    /// <inheritdoc/>
    public ExecutionPolicySnapshot ResolveForStage(StageConfiguration stage, ExecutionPolicySnapshot parentProjectEffective)
    {
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
    public ExecutionPolicySnapshot ResolveForNode(IOrchestrationNodeImplementationConfiguration node, ExecutionPolicySnapshot parentEffective)
    {
        return new ExecutionPolicySnapshot(
            StepFailurePolicy: node.StepFailurePolicy ?? parentEffective.StepFailurePolicy,
            StageFailurePolicy: node.StageFailurePolicy ?? parentEffective.StageFailurePolicy,
            MaxParallelPipelines: node.MaxParallelPipelines ?? parentEffective.MaxParallelPipelines,
            RequireApprovalToRun: node.RequireApprovalToRun ?? parentEffective.RequireApprovalToRun,
            AllowResume: node.AllowResume ?? parentEffective.AllowResume,
            AllowCrossTenant: node.AllowCrossTenant ?? parentEffective.AllowCrossTenant,
            ResiliencyPolicyId: node.ResiliencyPolicyId ?? parentEffective.ResiliencyPolicyId);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<ExecutionPolicySnapshot>> ResolveForRootNode(IOrchestrationNodeImplementationConfiguration node, CancellationToken cancellationToken = default)
    {
        var server = await ServerSnapshot(cancellationToken).ConfigureAwait(false);
        return server.IsFailure || server.Value is not { } parent
            ? GenericResult<ExecutionPolicySnapshot>.Failure(server.NoValueReason())
            : GenericResult<ExecutionPolicySnapshot>.Success(ResolveForNode(node, parent));
    }

    // The server's defaults as the parent snapshot of a project or a root node, read from their provider now.
    private async Task<IGenericResult<ExecutionPolicySnapshot>> ServerSnapshot(CancellationToken cancellationToken)
    {
        var defaults = await _serverDefaults.GetCurrent(cancellationToken).ConfigureAwait(false);
        if (defaults.IsFailure || defaults.Value is not { } row)
        {
            return GenericResult<ExecutionPolicySnapshot>.Failure(defaults.NoValueReason());
        }

        return GenericResult<ExecutionPolicySnapshot>.Success(new ExecutionPolicySnapshot(
            StepFailurePolicy: row.StepFailurePolicy,
            StageFailurePolicy: row.StageFailurePolicy,
            MaxParallelPipelines: row.MaxParallelPipelines,
            RequireApprovalToRun: row.RequireApprovalToRun,
            AllowResume: row.AllowResume,
            AllowCrossTenant: row.AllowCrossTenant,
            ResiliencyPolicyId: row.ResiliencyPolicyId));
    }
}
