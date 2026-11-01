using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.ValueSources;

namespace Fdw.Commands.Data.Upserts;

/// <summary>
/// The configuration of an upsert or bulk upsert: which columns match a row, which decide whether it changed,
/// which are written, and the optional versioned-history behaviour (which carries the insert-only rule).
/// </summary>
/// <remarks>
/// Everything is a column name the caller passes; nothing here names a specific table or column. A connection
/// type's translator checks every name against the target container's schema and quotes it.
/// </remarks>
public sealed class UpsertOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpsertOptions"/> class.
    /// </summary>
    /// <param name="keyColumns">The columns that match a source row to a target row; at least one.</param>
    /// <param name="compareColumns">The columns that decide whether a matched row changed; empty means every matched row is updated.</param>
    /// <param name="writeColumns">The columns written from the source rows; includes every key and compare column.</param>
    public UpsertOptions(
        IReadOnlyList<string> keyColumns,
        IReadOnlyList<string> compareColumns,
        IReadOnlyList<string> writeColumns)
    {
        KeyColumns = keyColumns;
        CompareColumns = compareColumns;
        WriteColumns = writeColumns;
    }

    /// <summary>Gets the columns that match a source row to a target row.</summary>
    public IReadOnlyList<string> KeyColumns { get; }

    /// <summary>Gets the columns that decide whether a matched row changed; empty means every matched row counts as changed.</summary>
    public IReadOnlyList<string> CompareColumns { get; }

    /// <summary>Gets the columns written from the source rows.</summary>
    public IReadOnlyList<string> WriteColumns { get; }

    /// <summary>Gets the versioned-history options, or <see langword="null"/> to update changed rows in place.</summary>
    public VersioningOptions? Versioning { get; init; }

    /// <summary>
    /// Gets the columns filled from a value source on INSERT instead of from the source rows (for example a sequence
    /// number or a load timestamp), or <see langword="null"/> for none.
    /// </summary>
    public IReadOnlyList<ColumnValueBinding>? ValueSources { get; init; }

    /// <summary>
    /// Describes the first thing wrong with these options, or returns <see langword="null"/> when they are
    /// consistent. Names are checked against the target's schema by the translator, not here.
    /// </summary>
    /// <returns>A plain-language reason, or <see langword="null"/>.</returns>
    public string? FirstProblem()
    {
        if (KeyColumns.Count == 0)
            return "an upsert names at least one key column";
        if (WriteColumns.Count == 0)
            return "an upsert names at least one write column";
        if (HasDuplicate(KeyColumns) || HasDuplicate(CompareColumns) || HasDuplicate(WriteColumns))
            return "a column is listed twice in the key, compare or write columns";

        var missing = KeyColumns.Concat(CompareColumns)
            .FirstOrDefault(c => !WriteColumns.Contains(c, StringComparer.OrdinalIgnoreCase));
        if (missing is not null)
            return $"key and compare column '{missing}' is not in the write columns";

        return VersioningProblem() ?? ValueSourcesProblem();
    }

    private static bool HasDuplicate(IReadOnlyCollection<string> columns) =>
        columns.Distinct(StringComparer.OrdinalIgnoreCase).Count() != columns.Count;

    private string? VersioningProblem()
    {
        if (Versioning is null)
            return null;
        var named = new List<string> { Versioning.EffectiveFromColumn, Versioning.EffectiveToColumn };
        if (Versioning.CurrentFlagColumn is not null)
            named.Add(Versioning.CurrentFlagColumn);
        if (named.Any(string.IsNullOrWhiteSpace))
            return "versioning names an effective-from column, an effective-to column and, when given, a current-flag column";
        if (HasDuplicate(named))
            return "versioning needs different effective-from, effective-to and current-flag columns";
        if (WriteColumns.Any(c => named.Contains(c, StringComparer.OrdinalIgnoreCase)))
            return "the effective-from, effective-to and current-flag columns belong to versioning, not to the write columns";
        return InsertOnlyProblem(Versioning.InsertOnlyRule);
    }

    private static string? InsertOnlyProblem(InsertOnlyRule? rule) =>
        rule is not null && (string.IsNullOrWhiteSpace(rule.Column) || rule.Value is null)
            ? "the insert-only rule names a marker column and a marker value"
            : null;

    private string? ValueSourcesProblem()
    {
        if (ValueSources is null)
            return null;
        var both = ValueSources.Select(v => v.Column).FirstOrDefault(c => WriteColumns.Contains(c, StringComparer.OrdinalIgnoreCase));
        if (both is not null)
            return $"column '{both}' is both a write column and a value-source column";
        var twice = ValueSources.GroupBy(v => v.Column, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (twice is not null)
            return $"column '{twice.Key}' has more than one value source";
        var row = ValueSources.FirstOrDefault(v => v.Source is ColumnReferenceValueSource);
        return row is null
            ? null
            : $"column '{row.Column}' reads a source field; an upsert is handed rows already keyed by destination column, so map the field before the load";
    }
}
