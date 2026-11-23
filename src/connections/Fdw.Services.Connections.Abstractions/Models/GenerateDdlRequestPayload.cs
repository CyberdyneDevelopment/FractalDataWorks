using System.Collections.Generic;

namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// Request payload for generating DDL from a connection's schema.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class GenerateDdlRequestPayload
{
    /// <summary>Gets or sets optional schema filter.</summary>
    public string? SchemaFilter { get; set; }

    /// <summary>Gets or sets the schemas discovery leaves out; an empty list is the choice to leave none out, and a missing one is refused.</summary>
    public IList<string>? ExcludedSchemas { get; set; }

    /// <summary>Gets or sets the only schemas discovery reads; null reads every schema not excluded.</summary>
    public IList<string>? IncludeOnlySchemas { get; set; }

    /// <summary>Gets or sets whether views are discovered; a missing choice is refused.</summary>
    public bool? DiscoverViews { get; set; }

    /// <summary>Gets or sets whether indexes are discovered; a missing choice is refused.</summary>
    public bool? DiscoverIndexes { get; set; }

    /// <summary>Gets or sets whether foreign keys are discovered; a missing choice is refused.</summary>
    public bool? DiscoverForeignKeys { get; set; }

    /// <summary>Gets or sets whether descriptions are discovered; a missing choice is refused.</summary>
    public bool? DiscoverDescriptions { get; set; }
}
