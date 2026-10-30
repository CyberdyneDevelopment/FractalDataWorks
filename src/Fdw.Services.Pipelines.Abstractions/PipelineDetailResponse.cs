using System;
using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Detailed DTO for a pipeline: what it is and the run parameters it declares. A pipeline's components are read through the graph
/// endpoints.
/// </summary>
public class PipelineDetailResponse
{
    /// <summary>Gets or sets the pipeline unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the pipeline name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the pipeline type (the engine discriminator, "Graph").</summary>
    public required string PipelineType { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether the pipeline is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets whether a run of the pipeline waits until someone who holds <c>pipelines:approve</c> approves it (FDW-1937).</summary>
    public bool RequireApprovalToRun { get; set; }

    /// <summary>Gets or sets how many runs of the pipeline may run at once (FDW-3117); <see langword="null"/> for a pipeline kind that does not state it.</summary>
    public int? MaxParallelRuns { get; set; }

    /// <summary>Gets or sets what happens to a run triggered while that many are active, the name of an option of <c>OverflowBehaviors</c> (FDW-3117); <see langword="null"/> for a pipeline kind that does not state it.</summary>
    public string? OverflowBehavior { get; set; }

    /// <summary>Gets or sets the seconds a held run waits, for the <c>DelayedStart</c> behavior with a delay (FDW-3129).</summary>
    public int? DelayedStartSeconds { get; set; }

    /// <summary>Gets or sets the time of day (<c>HH:mm</c>) a held run joins the queue, for the <c>DelayedStart</c> behavior with a time of day (FDW-3129).</summary>
    public string? DelayedStartTimeOfDay { get; set; }

    /// <summary>Gets or sets the time zone the time of day is read in (FDW-3129).</summary>
    public string? DelayedStartTimeZoneId { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets the declared run parameters.</summary>
    public IList<PipelineParameterDeclaration>? Parameters { get; set; }
}
