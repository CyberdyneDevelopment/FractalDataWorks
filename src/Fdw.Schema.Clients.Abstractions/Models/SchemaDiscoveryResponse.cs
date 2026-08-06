namespace Fdw.Schema.Clients.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Response from schema discovery endpoint.
/// </summary>
public sealed class SchemaDiscoveryResponse
{
    /// <summary>Gets or sets the associated connection name.</summary>
    public string ConnectionName { get; set; } = string.Empty;
    /// <summary>Gets or sets the connection type.</summary>
    public string? ConnectionType { get; set; }
    /// <summary>Gets or sets the database name.</summary>
    public string? DatabaseName { get; set; }
    /// <summary>Gets or sets the list of database schemas.</summary>
    // Why IList rather than IReadOnlyList: this type is now the single declaration used by the
    // server endpoint too, and FastEndpoints needs a mutable collection to bind incoming JSON.
    public IList<DatabaseSchemaPayload> Schemas { get; set; } = [];
    /// <summary>Gets or sets the discovery timestamp.</summary>
    public DateTime IntrospectedAt { get; set; }
    /// <summary>Gets or sets the total count of tables discovered.</summary>
    public int TotalTableCount { get; set; }
    /// <summary>Gets or sets the total count of views discovered.</summary>
    public int TotalViewCount { get; set; }
}
