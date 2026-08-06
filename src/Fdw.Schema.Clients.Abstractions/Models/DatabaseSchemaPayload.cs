using System;
using System.Collections.Generic;

namespace Fdw.Schema.Clients.Models;

/// <summary>
/// Schema information from database discovery.
/// </summary>
public sealed class DatabaseSchemaPayload
{
    /// <summary>Gets or sets the schema name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the list of tables in the schema.</summary>
    // Why IList rather than IReadOnlyList: this type is now the single declaration used by the
    // server endpoint too, and FastEndpoints needs a mutable collection to bind incoming JSON.
    public IList<DatabaseTablePayload> Tables { get; set; } = [];
    /// <summary>Gets or sets the list of views in the schema.</summary>
    public IList<DatabaseTablePayload> Views { get; set; } = [];
}
