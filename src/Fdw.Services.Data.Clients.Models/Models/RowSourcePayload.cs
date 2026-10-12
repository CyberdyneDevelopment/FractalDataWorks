using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>How many rows a table output has: one per row of a table, a number worked out by a formula, or one per group.</summary>
public sealed class RowSourcePayload
{
    /// <summary>Gets or sets the kind: Each (one row per row of <see cref="Table"/>), Count (a number of rows worked out by <see cref="Count"/>) or EachGroup (one row per group of <see cref="Table"/>).</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the table the rows are made from (Each, EachGroup, and Count when repeated for each row of a table); null for a Count made once.</summary>
    public string? Table { get; set; }

    /// <summary>Gets or sets a condition a row must meet to produce an output row (Each only); null keeps every row.</summary>
    public BuiltFormulaNodePayload? Where { get; set; }

    /// <summary>Gets or sets the formula for how many rows (Count only); it must give a whole number from 0 up to the row limit.</summary>
    public BuiltFormulaNodePayload? Count { get; set; }

    /// <summary>Gets or sets the name a Count's position 1 to N is readable under (Count only).</summary>
    public string? IndexColumn { get; set; }

    /// <summary>Gets or sets the columns whose values define a group (EachGroup only).</summary>
    public IList<string>? By { get; set; }
}
