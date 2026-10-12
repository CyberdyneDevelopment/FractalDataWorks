using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// A table a Built transformation reads, by the shape it expects. It names no source: whatever uses the transformation binds a
/// real table to it.
/// </summary>
public sealed class TableInputPayload
{
    /// <summary>Gets or sets the table's name inside the transformation.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the columns every row is expected to carry.</summary>
    public IList<TableColumnPayload> Columns { get; set; } = [];
}
