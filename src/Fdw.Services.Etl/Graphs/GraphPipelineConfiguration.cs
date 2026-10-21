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
public sealed partial class GraphPipelineConfiguration : IEtlPipelineImplementationConfiguration, IApprovalGatedPipelineConfiguration, IDeclaresRunParameters
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
    public string? Description { get; set; }

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

    /// <summary>Gets or sets the parameters the graph declares: typed values whoever starts a run supplies by name (FDW-1549).</summary>
    public IList<PipelineParameterConfiguration> Parameters { get; set; } = [];

    /// <inheritdoc />
    IReadOnlyList<IPipelineRunParameter> IDeclaresRunParameters.DeclaredRunParameters => [.. Parameters];
}
