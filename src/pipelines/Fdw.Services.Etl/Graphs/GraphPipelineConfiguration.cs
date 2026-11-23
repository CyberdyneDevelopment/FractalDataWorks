using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// The implementation row of a Graph pipeline (FDW-1396): components wired by named input and output nodes, with the outputs
/// that are deliberately not linked.
/// </summary>
/// <remarks>
/// <para>
/// A graph is one list of components (FDW-2041): sources, transformations, joins, destinations, actions and loops, told apart by their
/// <see cref="PipelineComponentConfiguration.ComponentType"/>. A component with a <see cref="PipelineComponentConfiguration.ParentComponent"/> is in the body of that loop.
/// A row moves from one component's output node to another's input node along a <see cref="PipelineLinkConfiguration"/>; both ends of a link
/// have the same parent. The <see cref="PipelineComponentConfiguration.Stage"/> of a component says when it runs.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline", ServiceType = "Graph")]
public sealed partial class GraphPipelineConfiguration : IEtlPipelineImplementationConfiguration, IGraphPipelineConfiguration, IApprovalGatedPipelineConfiguration, IParallelRunPipelineConfiguration, IClaimKeyPipelineConfiguration, IHalfDoneRulePipelineConfiguration, IRunTimeLimitPipelineConfiguration, ITemplatePipelineConfiguration, IDeclaresRunParameters
{
    /// <summary>Gets or sets the durable identity of the pipeline, the domain row's.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the pipeline's name.</summary>
    /// <remarks>Set by the domain provider from the domain row; never persisted.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain name.</summary>
    /// <remarks>Set by the domain provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Gets or sets the implementation name.</summary>
    /// <remarks>Set by the domain provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the pipeline is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets what the pipeline is for.</summary>
    // Why [NotStored]: the description is kept in the descriptions table in the language of the caller's tenant (FDW-7323), which the {t} table cannot hold, so the
    // column is gone; the provider fills the member for every set it reads. Off the mapper, it is no column of the table.
    [NotStored]
    public string? Description { get; set; }

    /// <inheritdoc />
    // Why [NotStored]: the parameters the description provider reaches through the pipeline are the ones in Parameters, so this is derived and no column of the table.
    [NotStored]
    public IReadOnlyList<IDescribedPipelineParameter> DescribedParameters => [.. Parameters];

    /// <summary>
    /// Gets or sets how many batches each link buffers before its producer waits. There is no default: a graph states it, and one
    /// that does not is refused.
    /// </summary>
    public int LinkCapacity { get; set; }

    /// <summary>
    /// Gets or sets whether a run of the pipeline waits until someone who holds <c>pipelines:approve</c> approves it (FDW-1937). A payload that
    /// omits it states false.
    /// </summary>
    public bool RequireApprovalToRun { get; set; }

    /// <summary>
    /// Gets or sets the template, over the pipeline's declared parameters, of the key a run claims beside the tables it writes (FDW-6141). Null when the pipeline
    /// claims no key of its own; no default.
    /// </summary>
    public string? ClaimKeyTemplate { get; set; }

    /// <summary>
    /// Gets or sets the name of the <c>HalfDoneRules</c> option that says what happens to a run found half-done after a restart (FDW-6141). Null when the pipeline
    /// states none; a pipeline type may require it.
    /// </summary>
    public string? HalfDoneRule { get; set; }

    /// <summary>
    /// Gets or sets the most seconds a run may take (FDW-6141). Null when the pipeline states none; a pipeline type may require it.
    /// </summary>
    public int? MaxRunSeconds { get; set; }

    /// <summary>
    /// Gets or sets the most steps a run may take (FDW-6141): a step is a graph fact. Null when the pipeline states none; a pipeline type may require it.
    /// </summary>
    public int? MaxSteps { get; set; }

    /// <summary>
    /// Gets or sets how many runs of the pipeline may run at once (FDW-3117). There is no default: a graph states it, and one that does not
    /// is refused.
    /// </summary>
    public int MaxParallelRuns { get; set; }

    /// <summary>
    /// Gets or sets the name of the option of <c>OverflowBehaviors</c> that says what happens to a run triggered while <see cref="MaxParallelRuns"/>
    /// runs are active (FDW-3117). There is no default: a graph states it, and one that does not is refused.
    /// </summary>
    public string OverflowBehavior { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the seconds a run held by the <c>DelayedStart</c> behavior waits before it joins the queue (FDW-3129). Null unless that behavior
    /// states a delay; it states exactly one of this and <see cref="DelayedStartTimeOfDay"/>.
    /// </summary>
    public int? DelayedStartSeconds { get; set; }

    /// <summary>Gets or sets the time of day, <c>HH:mm</c> on 24 hours, a run held by the <c>DelayedStart</c> behavior joins the queue (FDW-3129). It goes with <see cref="DelayedStartTimeZoneId"/>.</summary>
    public string? DelayedStartTimeOfDay { get; set; }

    /// <summary>Gets or sets the time zone, a <see cref="TimeZoneInfo"/> id, <see cref="DelayedStartTimeOfDay"/> is read in (FDW-3129). A tenant has no time zone to read, so the pipeline states its own.</summary>
    public string? DelayedStartTimeZoneId { get; set; }

    /// <summary>Gets or sets whether the pipeline is a template (FDW-3379): it is never triggered or scheduled, and a new pipeline is made from it.</summary>
    public bool IsTemplate { get; set; }

    /// <summary>Gets or sets the name of the template the pipeline was made from (FDW-3379); null when it was not made from one.</summary>
    public string? TemplateName { get; set; }

    /// <summary>Gets or sets the version of that template the pipeline was made from, the time the template was last changed (FDW-3379); null when it was not made from one.</summary>
    public DateTimeOffset? TemplateVersion { get; set; }

    /// <summary>
    /// Gets or sets when the failure rule disabled the pipeline (FDW-3864); null when it did not. Only the judge writes it, in the save that disables the pipeline; a save through the
    /// API never carries it, so any save by a person clears it.
    /// </summary>
    public DateTimeOffset? DisabledByFailureRuleAt { get; set; }

    /// <summary>Gets or sets the run whose end passed the threshold (FDW-3864); the record of the failures that counted is on that run. Set and cleared with <see cref="DisabledByFailureRuleAt"/>.</summary>
    public Guid? DisabledByFailureRuleRunId { get; set; }

    /// <summary>Gets or sets the organization the pipeline belongs to.</summary>
    public Guid? OrgId { get; set; }

    /// <summary>Gets or sets when the row was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets when the row was last changed.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the components, top level and loop bodies together. Cascade children (<c>pipe.PipelineComponent</c>).</summary>
    public IList<PipelineComponentConfiguration> Components { get; set; } = [];

    /// <summary>Gets or sets the links between the components' nodes. Cascade children (<c>pipe.PipelineLink</c>).</summary>
    public IList<PipelineLinkConfiguration> Links { get; set; } = [];

    /// <summary>Gets or sets the outputs that are deliberately not linked. Cascade children (<c>pipe.PipelineDroppedOutput</c>).</summary>
    public IList<PipelineDroppedOutputConfiguration> DroppedOutputs { get; set; } = [];

    /// <summary>Gets or sets the variables the graph declares. Cascade children (<c>pipe.PipelineVariable</c>).</summary>
    public IList<PipelineVariableConfiguration> Variables { get; set; } = [];

    /// <summary>Gets or sets the outputs the graph gives back to the pipeline that starts it (FDW-6136). Cascade children (<c>pipe.PipelineOutput</c>).</summary>
    public IList<PipelineOutputConfiguration> Outputs { get; set; } = [];

    /// <summary>Gets or sets the parameters the graph declares: typed values whoever starts a run supplies by name (FDW-1549).</summary>
    public IList<PipelineParameterConfiguration> Parameters { get; set; } = [];

    /// <inheritdoc />
    IReadOnlyList<IPipelineRunParameter> IDeclaresRunParameters.DeclaredRunParameters => [.. Parameters];
}
