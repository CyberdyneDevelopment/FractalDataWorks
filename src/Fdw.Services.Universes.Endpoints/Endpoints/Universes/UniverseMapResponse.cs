using System.Collections.Generic;

namespace Fdw.Services.Universes.Endpoints;

/// <summary>A universe's resources and the relationships between them, as a graph.</summary>
/// <remarks>
/// A single object rather than a paged list envelope: this is one map, not a page of maps.
///
/// Nothing here is pre-laid-out beyond the stored coordinates. No clusters, no suggested
/// positions — where an unplaced node goes is a rendering decision and belongs to the client.
/// </remarks>
public class UniverseMapResponse
{
    /// <summary>Gets or sets the universe's resources.</summary>
    public IList<UniverseMapNodeDto> Nodes { get; set; } = [];

    /// <summary>Gets or sets the relationships declared between them.</summary>
    public IList<UniverseMapEdgeDto> Edges { get; set; } = [];
}
