using System;

namespace Fdw.Services.Etl.Projects.Abstractions;

/// <summary>
/// Immutable snapshot of the resolved effective policy for a Project, Stage, or Step.
/// All fields are non-nullable: NULL columns in the database have been resolved to
/// their effective values by walking the inheritance chain from server defaults upward.
/// </summary>
/// <param name="StepFailurePolicy">What to do when a Pipeline within a Step fails.</param>
/// <param name="StageFailurePolicy">What to do when a Stage within a Project fails.</param>
/// <param name="MaxParallelPipelines">Maximum parallel pipelines within a Step.</param>
/// <param name="RequireApprovalToRun">Whether explicit approval is required before execution starts.</param>
/// <param name="AllowResume">Whether a failed execution can be resumed from a checkpoint.</param>
/// <param name="AllowCrossTenant">Whether pipelines from different tenants may be composed.</param>
/// <param name="ResiliencyPolicyId">
/// The effective resiliency policy identifier, or null if no policy is configured at any level.
/// When non-null, the orchestrator passes this to <c>IResiliencyExecutor.Execute</c> to wrap stage execution.
/// </param>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record ExecutionPolicySnapshot(
    string StepFailurePolicy,
    string StageFailurePolicy,
    int MaxParallelPipelines,
    bool RequireApprovalToRun,
    bool AllowResume,
    bool AllowCrossTenant,
    Guid? ResiliencyPolicyId);
