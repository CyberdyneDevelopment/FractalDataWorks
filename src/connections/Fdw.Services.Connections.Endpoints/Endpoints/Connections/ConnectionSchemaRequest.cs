using System.Collections.Generic;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Request DTO that names a connection and states the choices a discovery of its schema makes (FDW-3823).
/// </summary>
/// <remarks>
/// Each choice is required for a request that discovers: no choice is defaulted, and one left out is refused, naming it. A read that
/// finds the connection's schema already stored does not discover and reads none of them.
/// </remarks>
public class ConnectionSchemaRequest : ConnectionNameRequest
{
    /// <summary>Gets or sets the schemas to leave out; an empty list leaves out none.</summary>
    public IList<string>? ExcludedSchemas { get; set; }

    /// <summary>Gets or sets the only schemas to read; null reads every schema that is not left out.</summary>
    public IList<string>? IncludeOnlySchemas { get; set; }

    /// <summary>Gets or sets whether views are read as well as tables.</summary>
    public bool? DiscoverViews { get; set; }

    /// <summary>Gets or sets whether indexes are read.</summary>
    public bool? DiscoverIndexes { get; set; }

    /// <summary>Gets or sets whether foreign keys are read.</summary>
    public bool? DiscoverForeignKeys { get; set; }

    /// <summary>Gets or sets whether descriptions are read.</summary>
    public bool? DiscoverDescriptions { get; set; }
}
