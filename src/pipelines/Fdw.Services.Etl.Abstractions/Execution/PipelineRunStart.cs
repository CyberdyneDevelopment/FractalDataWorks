using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// What a run that starts another pipeline states about the run it starts (FDW-1381).
/// </summary>
/// <remarks>
/// <para>
/// Nothing here has a default: the starter refuses a start that does not state the run it belongs under, the tenant, the roles,
/// what caused it and the pipeline it names, and never fills one in from the surroundings.
/// </para>
/// <para>
/// The pipeline is named by exactly one of <see cref="PipelineId"/> (an orchestration node names its members by id) and
/// <see cref="PipelineName"/> (a StartPipeline stage names its target the way a person does).
/// </para>
/// </remarks>
public sealed class PipelineRunStart
{
    /// <summary>
    /// Gets the identifier of the execution the new run is started under: it becomes the new run's parent and its correlation.
    /// </summary>
    public required Guid ParentExecutionId { get; init; }

    /// <summary>Gets the identifier of the pipeline to run; null when it is named by <see cref="PipelineName"/>.</summary>
    public Guid? PipelineId { get; init; }

    /// <summary>Gets the name of the pipeline to run; null when it is named by <see cref="PipelineId"/>.</summary>
    public string? PipelineName { get; init; }

    /// <summary>Gets the tenant the new run belongs to, which is its starter's tenant.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Gets the roles the new run acts with, which are its starter's roles.</summary>
    public required IReadOnlyList<string> RunRoles { get; init; }

    /// <summary>Gets what started the run, recorded on its execution (for example <c>Node:Nightly</c> or <c>Pipeline:Orders</c>).</summary>
    public required string TriggerSource { get; init; }

    /// <summary>
    /// Gets the run parameters as name and text value pairs. The starter reads each as the type the pipeline declares for it and
    /// refuses an undeclared name, a missing required one and a value its type cannot read.
    /// </summary>
    public required IReadOnlyList<KeyValuePair<string, string>> Parameters { get; init; }

    /// <summary>
    /// Gets whether the starter keeps the run's completion for <see cref="IPipelineRunStarter.Await"/>. A caller that sets it
    /// must await, so the completion is released.
    /// </summary>
    public required bool AwaitCompletion { get; init; }
}
