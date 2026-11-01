using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>What running a script answered.</summary>
public class ScriptResultPayload
{
    /// <summary>Gets or sets the kind that ran.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the columns of a query's rows, in the order the query selected them; null for a non-query.</summary>
    public IList<ScriptColumnPayload>? Columns { get; set; }

    /// <summary>Gets or sets the rows a query selected, each by column name; null for a non-query.</summary>
    public IList<IDictionary<string, object?>>? Rows { get; set; }

    /// <summary>Gets or sets whether the query selected more rows than were answered; null for a non-query.</summary>
    public bool? HasMore { get; set; }

    /// <summary>Gets or sets how many rows a non-query affected; null for a query.</summary>
    public int? RowsAffected { get; set; }
}
