using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.ValueSources;

namespace Fdw.Commands.Data.Loads;

/// <summary>
/// The configuration of an insert-from-query: the destination columns copied from the source's columns of the
/// same name, the columns a value source supplies instead, and the source rows they are selected from.
/// </summary>
public sealed class InsertFromQueryOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InsertFromQueryOptions"/> class.
    /// </summary>
    /// <param name="writeColumns">The destination columns copied from the source column of the same name.</param>
    /// <param name="source">The source table or view the rows are selected from.</param>
    public InsertFromQueryOptions(IReadOnlyList<string> writeColumns, QuerySource source)
    {
        WriteColumns = writeColumns;
        Source = source;
    }

    /// <summary>Gets the destination columns copied from the source column of the same name.</summary>
    public IReadOnlyList<string> WriteColumns { get; }

    /// <summary>Gets the source table or view the rows are selected from.</summary>
    public QuerySource Source { get; }

    /// <summary>
    /// Gets the destination columns a value source supplies (a source field of another name, a constant, the current
    /// timestamp or the next sequence value), or <see langword="null"/> for none.
    /// </summary>
    public IReadOnlyList<ColumnValueBinding>? ValueSources { get; init; }

    /// <summary>Gets every destination column the insert fills: the write columns, then the value-source columns.</summary>
    public IReadOnlyList<string> TargetColumns
    {
        get
        {
            var columns = new List<string>(WriteColumns);
            if (ValueSources is not null)
                columns.AddRange(ValueSources.Select(v => v.Column));
            return columns;
        }
    }

    /// <summary>
    /// Describes the first thing wrong with these options, or returns <see langword="null"/> when they are
    /// consistent. Names are checked by the translator against the target's schema and for safe characters.
    /// </summary>
    /// <returns>A plain-language reason, or <see langword="null"/>.</returns>
    public string? FirstProblem()
    {
        if (TargetColumns.Count == 0)
            return "an insert-from-query names at least one write column or value-source column";
        if (string.IsNullOrWhiteSpace(Source.Table))
            return "an insert-from-query names its source table";
        var twice = TargetColumns.GroupBy(c => c, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        return twice is null
            ? null
            : $"column '{twice.Key}' is listed twice in the write columns and value sources";
    }
}
