namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// What one link between two components of a run in flight has carried so far (FDW-3778). Counts only; no row values.
/// </summary>
public sealed class PipelineRunLinkResponse
{
    /// <summary>Gets or sets the name of the component the rows leave.</summary>
    public required string From { get; set; }

    /// <summary>Gets or sets the name of the component the rows arrive at.</summary>
    public required string To { get; set; }

    /// <summary>Gets or sets the rows the link has carried.</summary>
    public long Rows { get; set; }
}
