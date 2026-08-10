using System.Collections.Generic;

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
    /// Gets or sets the optional DataStore name. Defaults to ConnectionName if not provided.
    /// </summary>
    public string? DataStoreName { get; set; }

    /// <summary>
    /// Gets or sets the schemas to import. Null imports all non-system schemas.
    /// </summary>
    public IList<string>? Schemas { get; set; }

    /// <summary>
    /// Gets or sets whether to overwrite existing DataStore. Default is false.
    /// </summary>
    public bool Overwrite { get; set; }
}
