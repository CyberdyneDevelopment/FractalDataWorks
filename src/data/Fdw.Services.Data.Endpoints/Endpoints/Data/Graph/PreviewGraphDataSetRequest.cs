namespace Fdw.Services.Data.Endpoints;

/// <summary>The body of <c>POST /datasets/graph/preview</c> (FDW-5454): the body of <c>POST /datasets/graph</c> and what to read of it.</summary>
/// <remarks>
/// <see cref="MaxRowsPerSource"/> is the most rows read from each source; left out, the host's configured number applies. <see cref="Component"/> and
/// <see cref="Output"/> are stated together to read at an output in the middle of the graph; left out, the rows are read at the draft's own stated fields-out output.
/// </remarks>
public sealed class PreviewGraphDataSetRequest : CreateGraphDataSetRequest
{
    /// <summary>Gets or sets the most rows read from each source; null states that the configured number applies.</summary>
    public int? MaxRowsPerSource { get; set; }

    /// <summary>Gets or sets the component whose output the rows are read at; null reads at the draft's stated fields-out output.</summary>
    public string? Component { get; set; }

    /// <summary>Gets or sets the output of <see cref="Component"/>; stated with it or not at all.</summary>
    public string? Output { get; set; }
}
