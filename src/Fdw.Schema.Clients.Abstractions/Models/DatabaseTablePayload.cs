using System;
using System.Collections.Generic;

namespace Fdw.Schema.Clients.Models;

/// <summary>
/// Table or view from database discovery.
/// </summary>
public sealed class DatabaseTablePayload
{
    /// <summary>Gets or sets the object name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the object type (Table, View).</summary>
    public string ObjectType { get; set; } = string.Empty;
    /// <summary>Gets or sets the list of columns in the object.</summary>
    // Why IList rather than IReadOnlyList: this type is now the single declaration used by the
    // server endpoint too, and FastEndpoints needs a mutable collection to bind incoming JSON.
    public IList<DatabaseColumnPayload> Columns { get; set; } = [];
    /// <summary>Gets or sets the list of primary key column names.</summary>
    public IList<string> PrimaryKeyColumns { get; set; } = [];
    /// <summary>Gets or sets the estimated row count.</summary>
    public long? EstimatedRowCount { get; set; }
}
