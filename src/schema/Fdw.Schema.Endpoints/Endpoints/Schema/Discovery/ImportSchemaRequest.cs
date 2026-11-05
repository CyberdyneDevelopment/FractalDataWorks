using System.Collections.Generic;
using Fdw.Conventions;

namespace Fdw.Schema.Endpoints.Discovery;

/// <summary>
/// Request for importing schema to DataStore configuration.
/// </summary>
public class ImportSchemaRequest
{
    /// <summary>
    /// Gets or sets the connection name (from route).
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DataStore name; when it is not stated, the store takes <see cref="ConventionalDataStoreName"/>.
    /// </summary>
    public string? DataStoreName { get; set; }

    /// <summary>
    /// Gets the name a store imported from a connection takes by convention: the connection's own name.
    /// </summary>
    [PlatformDefault]
    public string ConventionalDataStoreName => ConnectionName;

    /// <summary>
    /// Gets or sets the schemas to import. Null imports all non-system schemas.
    /// </summary>
    public IList<string>? Schemas { get; set; }

    /// <summary>
    /// Gets or sets whether to overwrite existing DataStore. Default is false.
    /// </summary>
    public bool Overwrite { get; set; }
}
