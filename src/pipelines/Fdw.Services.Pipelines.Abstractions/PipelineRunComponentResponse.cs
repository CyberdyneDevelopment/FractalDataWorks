using System;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// One component of a pipeline run in flight, as the live view shows it (FDW-3382). A value the run has not reported is <see langword="null"/>, never zero.
/// </summary>
public class PipelineRunComponentResponse
{
    /// <summary>Gets or sets the component's name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the stage number the component runs in; 0 for a component that has none, which run first.</summary>
    public int Stage { get; set; }

    /// <summary>Gets or sets when the component started, or <see langword="null"/> while it waits for its stage or a completion line.</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>Gets or sets when the component ended, or <see langword="null"/> while it has not.</summary>
    public DateTimeOffset? EndedAt { get; set; }

    /// <summary>Gets or sets whether the component's run succeeded, or <see langword="null"/> until it ends.</summary>
    public bool? Succeeded { get; set; }

    /// <summary>Gets or sets the rows the component has passed on so far (for a component with no outputs, the rows that reached it), or <see langword="null"/> until it starts. No row values are ever sent.</summary>
    public long? Rows { get; set; }

    /// <summary>Gets or sets the rows that arrived at the component, whether or not it passes any on; null until it starts (FDW-3778).</summary>
    public long? RowsIn { get; set; }
}
