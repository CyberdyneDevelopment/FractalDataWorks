using System.Collections.Generic;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// A graph pipeline as the API states it (FDW-1396, FDW-1403): the request that saves one and the response that reads one.
/// </summary>
/// <remarks>
/// One shape for both. A component's <c>settings</c> are named as the component catalogue names them
/// (<c>GET /type-collections/ComponentTypes/values</c>), actions and loops included, so a new kind needs no change here.
/// </remarks>
public class GraphPayload
{
    /// <summary>Gets or sets the pipeline's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what the pipeline is for.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether the pipeline is enabled; a save states it.</summary>
    public bool? IsEnabled { get; set; }

    /// <summary>Gets or sets how many batches each link buffers; a save states it.</summary>
    public int? LinkCapacity { get; set; }

    /// <summary>
    /// Gets or sets whether a run of the pipeline waits until someone who holds <c>pipelines:approve</c> approves it (FDW-1937); omitted, it is
    /// false.
    /// </summary>
    public bool RequireApprovalToRun { get; set; }

    /// <summary>Gets or sets how many runs of the pipeline may run at once; a save states it (FDW-3117).</summary>
    public int? MaxParallelRuns { get; set; }

    /// <summary>Gets or sets what happens to a run triggered while that many are active, the name of an option of <c>OverflowBehaviors</c>; a save states it (FDW-3117).</summary>
    public string? OverflowBehavior { get; set; }

    /// <summary>Gets or sets the seconds a held run waits, for the <c>DelayedStart</c> behavior with a delay (FDW-3129).</summary>
    public int? DelayedStartSeconds { get; set; }

    /// <summary>Gets or sets the time of day (<c>HH:mm</c>) a held run joins the queue, for the <c>DelayedStart</c> behavior with a time of day (FDW-3129).</summary>
    public string? DelayedStartTimeOfDay { get; set; }

    /// <summary>Gets or sets the time zone the time of day is read in (FDW-3129).</summary>
    public string? DelayedStartTimeZoneId { get; set; }

    /// <summary>Gets or sets the variables the graph declares, in scope in every component.</summary>
    public IList<GraphVariablePayload> Variables { get; set; } = [];

    /// <summary>Gets or sets the parameters the graph declares at design time (FDW-1549).</summary>
    public IList<GraphParameterPayload> Parameters { get; set; } = [];

    /// <summary>
    /// Gets or sets the components of the pipeline, one list (FDW-2041): sources, transformations, joins, destinations, actions and loops. A
    /// component in the body of a loop names it as its <see cref="GraphComponentPayload.Parent"/>. The order the components run in is
    /// the stage each one states, not their place in the list.
    /// </summary>
    public IList<GraphComponentPayload> Components { get; set; } = [];

    /// <summary>Gets or sets the links between the components' nodes; both ends of a link have the same parent.</summary>
    public IList<GraphLinkPayload> Links { get; set; } = [];

    /// <summary>Gets or sets the outputs that are deliberately not linked: their rows are discarded.</summary>
    public IList<GraphNodePayload> Dropped { get; set; } = [];
}
