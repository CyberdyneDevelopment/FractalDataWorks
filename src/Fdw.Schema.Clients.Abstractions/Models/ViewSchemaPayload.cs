using System;
using System.Collections.Generic;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Schema.Clients.Models;

/// <summary>
/// Represents a view schema for UI display.
/// </summary>
public sealed class ViewSchemaPayload
{
    /// <summary>Gets or sets the view name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the schema name.</summary>
    public string Schema { get; set; } = string.Empty;
    /// <summary>Gets or sets the list of columns.</summary>
    public IReadOnlyList<ColumnSchemaPayload> Columns { get; set; } = Array.Empty<ColumnSchemaPayload>();
}
