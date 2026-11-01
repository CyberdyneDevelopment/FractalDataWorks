using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>One filtered output of a conditional split (FDW-1396).</summary>
public class GraphSplitOutputPayload
{
    /// <summary>Gets or sets the output node's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the filter expression; a row that meets it goes through this output.</summary>
    public string Filter { get; set; } = string.Empty;
}
