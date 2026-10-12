using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>A table a Built transformation writes: how many rows it has and the columns each row is made of.</summary>
public sealed class TableOutputPayload
{
    /// <summary>Gets or sets the output's name; whatever uses the transformation routes it by this name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets how the rows are decided.</summary>
    public RowSourcePayload? Rows { get; set; }

    /// <summary>Gets or sets the columns, in order.</summary>
    public IList<OutputColumnPayload> Columns { get; set; } = [];
}
