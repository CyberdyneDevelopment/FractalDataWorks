using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>Where the console draws a component (FDW-1396); presentation only.</summary>
public class GraphLayoutPayload
{
    /// <summary>Gets or sets the horizontal position.</summary>
    public int X { get; set; }

    /// <summary>Gets or sets the vertical position.</summary>
    public int Y { get; set; }
}
