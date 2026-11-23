using System;
using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Abstractions.Runs;

/// <summary>
/// What a caller states about a pipeline run it starts through <see cref="IPipelineRuns.Start"/> (FDW-6131): a person's trigger, a webhook, an approval, a
/// run that starts another pipeline, an orchestration node.
/// </summary>
/// <remarks>
/// <para>
/// Nothing here has a default. The pipeline is named by exactly one of <see cref="PipelineId"/> and <see cref="PipelineName"/>; the tenant, the roles,
/// what caused the run and its parameters are the caller's to state, and a start that leaves one out is refused, never given the surroundings'.
/// </para>
/// <para>
/// A caller that has already opened the run's execution record (the trigger endpoint opens it before it judges the roles and what the run reaches)
/// names it in <see cref="Opened"/>; the run mode then queues that execution and opens no second record. Without it the run mode opens the record.
/// </para>
/// </remarks>
public sealed record PipelineStart
{
    /// <summary>Gets the identifier of the pipeline to run; null when it is named by <see cref="PipelineName"/>.</summary>
    public Guid? PipelineId { get; init; }

    /// <summary>Gets the name of the pipeline to run; null when it is named by <see cref="PipelineId"/>.</summary>
    public string? PipelineName { get; init; }

    /// <summary>Gets what started the run, recorded on its execution (for example <c>Scheduled</c>, <c>Node:Nightly</c> or <c>Pipeline:Orders</c>).</summary>
    public required string TriggerSource { get; init; }

    /// <summary>Gets the tenant the run belongs to.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Gets the roles the run acts with.</summary>
    public required IReadOnlyList<string> RunRoles { get; init; }

    /// <summary>
    /// Gets the run parameters as name and text value pairs. The run mode reads each as the type the pipeline declares for it and refuses an undeclared
    /// name, a missing required one and a value its type cannot read. A start that names an <see cref="Opened"/> execution carries its values in <see cref="PipelineStartOpened.ResolvedParameters"/> instead.
    /// </summary>
    public required IReadOnlyList<KeyValuePair<string, string>> Parameters { get; init; }

    /// <summary>Gets the execution the run is started under, which becomes its parent and its correlation; null for a run no other run started.</summary>
    public Guid? ParentExecutionId { get; init; }

    /// <summary>Gets the name of the schedule whose fire started the run; null for a run no schedule started.</summary>
    public string? ScheduleName { get; init; }

    /// <summary>Gets the Id of the schedule whose fire started the run; null for a run no schedule started.</summary>
    public Guid? ScheduleId { get; init; }

    /// <summary>Gets how long the run may take once it is running, in seconds; null states no time limit.</summary>
    public int? TimeoutSeconds { get; init; }

    /// <summary>Gets how many rows each source reads in a test run; null for a real run.</summary>
    public int? TestRowsPerSource { get; init; }

    /// <summary>Gets whether the run's completion is kept for <see cref="IPipelineRuns.Await"/>. A caller that sets it must await, so the completion is released.</summary>
    public required bool AwaitCompletion { get; init; }

    /// <summary>Gets the execution record the caller has already opened for the run; null when the run mode opens it.</summary>
    public PipelineStartOpened? Opened { get; init; }

    /// <summary>
    /// Gets the person the run acts as (FDW-6137), or null for a run that acts as no person. <see cref="IPipelineRuns.Start"/> fills it from the
    /// authenticated caller and from nothing else: no request body, parameter or component names a person. A start a run makes states the person of the
    /// run that makes it, and <see cref="IPipelineRuns.Start"/> refuses one that names any other person than its caller.
    /// </summary>
    public Guid? PersonId { get; init; }

    /// <summary>
    /// Gets the run mode the caller requires for this run (FDW-6701), or null when the started pipeline's type decides it. A start a run makes under an at-once run requires
    /// <c>AtOnce</c>, so the pipeline it starts is carried out at once whatever its own type says. Nothing stands in for a mode: one that no run mode option declares, or that no
    /// runner of the host carries out, is refused.
    /// </summary>
    public string? RequiredRunMode { get; init; }

    /// <summary>
    /// Gets the time and step limits of the at-once run that makes this start (FDW-6701), or null when no at-once run makes it. The run takes its limits from them: its deadline
    /// can only be the parent's or earlier, and its steps are counted against the parent's.
    /// </summary>
    public PipelineRunTimeAndStepLimits? ParentLimits { get; init; }
}
