using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Reads the columns a load row names, by where the body names them.</summary>
public static class LoadRowColumns
{
    private const string WriteRole = "Write";

    /// <summary>The columns of the destination a row names: key, compare and write columns, versioning, soft delete and value sources.</summary>
    /// <param name="row">The load row.</param>
    /// <param name="prefix">The body field the row's options sit under.</param>
    /// <param name="namesParameters">Whether the row's value sources name container parameters (Execute) rather than columns.</param>
    /// <returns>The named columns.</returns>
    public static IEnumerable<NamedColumn> OfDestination(PipelineLoadStepConfiguration row, string prefix, bool namesParameters)
    {
        var named = row.Columns.Select(c => new NamedColumn($"{prefix}.{c.Role}Columns", c.ColumnName))
            .Concat(NamedIn($"{prefix}.Versioning", row.EffectiveFromColumn, row.EffectiveToColumn, row.CurrentFlagColumn, row.InsertOnlyRuleColumn))
            .Concat(NamedIn($"{prefix}.SoftDelete", row.DeletedFlagColumn, row.DeletedAtColumn, row.SoftDeleteMarkerColumn));
        return namesParameters
            ? named
            : named.Concat(row.ValueSources.Select(s => new NamedColumn("ValueSources", s.ColumnName)));
    }

    /// <summary>The columns a row reads from its source data set: Column value sources' source fields, and write columns of a set-based copy.</summary>
    /// <param name="row">The load row.</param>
    /// <param name="prefix">The body field the row's options sit under.</param>
    /// <returns>The named source-side columns.</returns>
    public static IEnumerable<NamedColumn> OfSource(PipelineLoadStepConfiguration row, string prefix) =>
        row.Columns.Where(c => string.Equals(c.Role, WriteRole, StringComparison.OrdinalIgnoreCase)).Select(c => new NamedColumn($"{prefix}.WriteColumns", c.ColumnName))
            .Concat(row.ValueSources.Where(s => !string.IsNullOrWhiteSpace(s.SourceField)).Select(s => new NamedColumn("ValueSources", s.SourceField!)))
            .Concat(row.ValueSources.SelectMany(s => s.SequenceOrderColumns).Select(c => new NamedColumn("ValueSources", c.ColumnName)));

    /// <summary>The columns of the destination that a row's sequence-ordered value sources order by, for a row that takes rows.</summary>
    /// <param name="row">The load row.</param>
    /// <returns>The named ordering columns.</returns>
    public static IEnumerable<NamedColumn> SequenceOrderOfDestination(PipelineLoadStepConfiguration row) =>
        row.ValueSources.SelectMany(s => s.SequenceOrderColumns).Select(c => new NamedColumn("ValueSources", c.ColumnName));

    private static IEnumerable<NamedColumn> NamedIn(string field, params string?[] columns) =>
        columns.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => new NamedColumn(field, c!));
}
