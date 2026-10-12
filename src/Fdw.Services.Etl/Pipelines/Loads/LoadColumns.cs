using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// Reads the column lists of a load row by the role each column carries: the roles state what they are as flags, so no
/// caller branches on a role's name (FDW-1109).
/// </summary>
public static class LoadColumns
{
    /// <summary>Lists the key columns (the match key), in ordinal order.</summary>
    /// <param name="row">The load row.</param>
    /// <returns>The column names.</returns>
    public static IReadOnlyList<string> Keys(PipelineLoadStepConfiguration row) => Of(row.Columns, role => role.IsMatchKey);

    /// <summary>Lists the compare columns, in ordinal order.</summary>
    /// <param name="row">The load row.</param>
    /// <returns>The column names.</returns>
    public static IReadOnlyList<string> Compares(PipelineLoadStepConfiguration row) => Of(row.Columns, role => role.IsComparison);

    /// <summary>Lists the write columns, in ordinal order.</summary>
    /// <param name="row">The load row.</param>
    /// <returns>The column names.</returns>
    public static IReadOnlyList<string> Writes(PipelineLoadStepConfiguration row) => Of(row.Columns, role => role.IsWritten);

    /// <summary>Lists the columns that order a sequenced value source, in ordinal order.</summary>
    /// <param name="source">The value source.</param>
    /// <returns>The column names.</returns>
    public static IReadOnlyList<string> SequenceOrder(PipelineLoadValueSourceConfiguration source)
        => source.SequenceOrderColumns.OrderBy(column => column.Ordinal).Select(column => column.ColumnName).ToList();

    private static List<string> Of(IEnumerable<PipelineLoadColumnConfiguration> columns, Func<IPipelineLoadColumnRole, bool> pick)
        => columns
            .Where(column => pick(PipelineLoadColumnRoles.ByName(column.Role)))
            .OrderBy(column => column.Ordinal)
            .Select(column => column.ColumnName)
            .ToList();
}
