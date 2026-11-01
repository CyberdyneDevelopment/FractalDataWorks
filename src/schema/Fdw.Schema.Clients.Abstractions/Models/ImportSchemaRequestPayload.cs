using System.Collections.Generic;

namespace Fdw.Schema.Clients.Models;

/// <summary>
/// Request for importing schema into DataStore configuration via the API client.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ImportSchemaRequestPayload
{
    /// <summary>
    /// Gets or sets the optional DataStore name. Defaults to the connection name if not provided.
    /// </summary>
    public string? DataStoreName { get; set; }

    /// <summary>
    /// Gets or sets the schemas to import. Null imports all non-system schemas.
    /// </summary>
    public IList<string>? Schemas { get; set; }

    /// <summary>
    /// Gets or sets whether to overwrite an existing DataStore. Default is false.
    /// </summary>
    public bool Overwrite { get; set; }
}
