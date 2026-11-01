using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Upserts;
using Fdw.Commands.Data.ValueSources;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.ValueSources;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.SqlClient;

namespace Fdw.Data.MsSql.Translators;

/// <summary>
/// Builds the set-based T-SQL batch for an upsert or bulk upsert: stage the rows, update (or, with versioning,
/// close) the rows whose compare columns changed, then insert the keys that have no (current) row. Column names are
/// validated and quoted; values are only ever parameters.
/// </summary>
internal sealed class MsSqlUpsertStatement
{
    private readonly UpsertOptions _options;
    private readonly IStorageContainer _container;
    private readonly ISqlDialect _dialect;
    private readonly string _target;
    private readonly string _temp = MsSqlRowStaging.NewTempTableName();

    internal MsSqlUpsertStatement(UpsertOptions options, IStorageContainer container, ISqlDialect dialect, string target)
    {
        _options = options;
        _container = container;
        _dialect = dialect;
        _target = target;
    }

    /// <summary>Describes the first thing wrong with <paramref name="options"/> for <paramref name="container"/>, or returns <see langword="null"/>.</summary>
    internal static string? Problem(UpsertOptions options, IStorageContainer container, ISqlDialect dialect)
    {
        var general = options.FirstProblem();
        if (general is not null)
            return general;

        var unknown = MsSqlLoadColumns.UnknownOrUnsafe(container, ReferencedColumns(options));
        if (unknown is not null)
            return unknown;

        return StructureProblem(options, container) ?? ValueSourceProblem(options, dialect);
    }

    private static List<string> ReferencedColumns(UpsertOptions options)
    {
        var columns = new List<string>();
        columns.AddRange(options.KeyColumns);
        columns.AddRange(options.CompareColumns);
        columns.AddRange(options.WriteColumns);
        if (options.ValueSources is not null)
            columns.AddRange(options.ValueSources.Select(v => v.Column));
        if (options.Versioning is null)
            return columns;

        columns.Add(options.Versioning.EffectiveFromColumn);
        columns.Add(options.Versioning.EffectiveToColumn);
        if (options.Versioning.CurrentFlagColumn is not null)
            columns.Add(options.Versioning.CurrentFlagColumn);
        if (options.Versioning.InsertOnlyRule is not null)
            columns.Add(options.Versioning.InsertOnlyRule.Column);
        return columns;
    }

    private static string? StructureProblem(UpsertOptions options, IStorageContainer container)
    {
        var key = options.CompareColumns.FirstOrDefault(c => options.KeyColumns.Contains(c, StringComparer.OrdinalIgnoreCase));
        if (key is not null)
            return $"compare column '{key}' is also a key column";

        var unmovable = options.CompareColumns.Concat(options.KeyColumns)
            .FirstOrDefault(c => MsSqlLoadColumns.IsComputed(container, c));
        if (unmovable is not null)
            return $"column '{unmovable}' is computed and cannot be a key or compare column";

        var identityCompare = options.CompareColumns.FirstOrDefault(c => MsSqlLoadColumns.IsIdentity(container, c));
        return identityCompare is null ? null : $"compare column '{identityCompare}' is an identity column";
    }

    private static string? ValueSourceProblem(UpsertOptions options, ISqlDialect dialect)
    {
        var context = new MsSqlValueSourceContext(new SqlCommand(), dialect, "s");
        var bindings = new List<ColumnValueBinding>();
        if (options.ValueSources is not null)
            bindings.AddRange(options.ValueSources);

        foreach (var binding in bindings)
        {
            if (Render(binding.Source, context) is null)
                return $"the value source for column '{binding.Column}' cannot be rendered";
            if (binding.Source is NextSequenceValueSource sequence
                && sequence.OrderBy.Any(o => !options.WriteColumns.Contains(o, StringComparer.OrdinalIgnoreCase)))
                return $"the sequence for column '{binding.Column}' orders by a column that is not a write column";
        }

        return null;
    }

    /// <summary>Builds the whole batch and adds its parameters (the rows are added by the caller) to <paramref name="command"/>.</summary>
    internal string? Build(SqlCommand command)
    {
        var context = new MsSqlValueSourceContext(command, _dialect, "s");
        var versioning = _options.Versioning;
        var insert = Insert(context);
        if (insert is null)
            return null;

        var declarations = versioning is null ? string.Empty : "DECLARE @fdw_now datetimeoffset(7) = SYSDATETIMEOFFSET(); ";
        var identity = _options.WriteColumns.Any(c => MsSqlLoadColumns.IsIdentity(_container, c));
        var staging = MsSqlRowStaging.Sql(_temp, _target, _options.WriteColumns, _dialect, identity);
        var change = versioning is null ? Update(command) : Close(command, versioning);
        return MsSqlLoadBatch.Wrap(declarations, staging + "SET NOCOUNT OFF; " + change + insert, _temp, useTransaction: true);
    }

    private string Update(SqlCommand command)
    {
        var updateColumns = _options.WriteColumns.Where(c =>
            !_options.KeyColumns.Contains(c, StringComparer.OrdinalIgnoreCase)
            && !MsSqlLoadColumns.IsIdentity(_container, c)
            && !MsSqlLoadColumns.IsComputed(_container, c));
        var set = string.Join(", ", updateColumns.Select(c => $"t.{Q(c)} = s.{Q(c)}"));
        if (set.Length == 0)
            return string.Empty;
        return $"UPDATE t SET {set} FROM {_target} AS t WITH (UPDLOCK, HOLDLOCK) INNER JOIN {_temp} AS s ON {KeysOn("t")} "
               + $"WHERE ({Differs()}){MarkerPredicate(command)}; ";
    }

    private string Close(SqlCommand command, VersioningOptions versioning) =>
        $"UPDATE t SET t.{Q(versioning.EffectiveToColumn)} = @fdw_now{ClearFlag(versioning)} FROM {_target} AS t WITH (UPDLOCK, HOLDLOCK) "
        + $"INNER JOIN {_temp} AS s ON {KeysOn("t")} "
        + $"WHERE t.{Q(versioning.EffectiveToColumn)} IS NULL AND ({Differs()}){MarkerPredicate(command)}; ";

    private string? Insert(MsSqlValueSourceContext context)
    {
        var columns = _options.WriteColumns
            .Where(c => !MsSqlLoadColumns.IsIdentity(_container, c) && !MsSqlLoadColumns.IsComputed(_container, c))
            .ToList();
        var targets = columns.Select(Q).ToList();
        var values = columns.Select(c => $"s.{Q(c)}").ToList();

        var bindings = new List<ColumnValueBinding>();
        if (_options.ValueSources is not null)
            bindings.AddRange(_options.ValueSources);
        foreach (var binding in bindings)
        {
            var expression = Render(binding.Source, context);
            if (expression is null)
                return null;
            targets.Add(Q(binding.Column));
            values.Add(expression);
        }

        var versioning = _options.Versioning;
        if (versioning is not null)
        {
            targets.Add(Q(versioning.EffectiveFromColumn));
            values.Add("@fdw_now");
            targets.Add(Q(versioning.EffectiveToColumn));
            values.Add("NULL");
            if (versioning.CurrentFlagColumn is not null)
            {
                targets.Add(Q(versioning.CurrentFlagColumn));
                values.Add("1");
            }
        }

        var currentOnly = versioning is null ? string.Empty : $" AND e.{Q(versioning.EffectiveToColumn)} IS NULL";
        return $"INSERT INTO {_target} ({string.Join(", ", targets)}) SELECT {string.Join(", ", values)} FROM {_temp} AS s "
               + $"WHERE NOT EXISTS (SELECT 1 FROM {_target} AS e WITH (UPDLOCK, HOLDLOCK) WHERE {KeysOn("e")}{currentOnly}); ";
    }

    private string MarkerPredicate(SqlCommand command)
    {
        var marker = _options.Versioning?.InsertOnlyRule;
        if (marker is null)
            return string.Empty;
        command.Parameters.Add(new SqlParameter("@fdw_marker", marker.Value));
        return $" AND (t.{Q(marker.Column)} IS NULL OR t.{Q(marker.Column)} <> @fdw_marker)";
    }

    private string KeysOn(string targetAlias) =>
        string.Join(" AND ", _options.KeyColumns.Select(k => $"{targetAlias}.{Q(k)} = s.{Q(k)}"));

    private string ClearFlag(VersioningOptions versioning) =>
        versioning.CurrentFlagColumn is null ? string.Empty : $", t.{Q(versioning.CurrentFlagColumn)} = 0";

    private string Differs() =>
        _options.CompareColumns.Count == 0
            ? "1 = 1"
            : string.Join(" OR ", _options.CompareColumns.Select(c =>
                $"(t.{Q(c)} <> s.{Q(c)} OR (t.{Q(c)} IS NULL AND s.{Q(c)} IS NOT NULL) OR (t.{Q(c)} IS NOT NULL AND s.{Q(c)} IS NULL))"));

    private string Q(string name) => _dialect.QuoteIdentifier(name);

    private static string? Render(ColumnValueSource source, MsSqlValueSourceContext context) =>
        MsSqlValueSourceRenderers.ByName(source.Kind).Render(source, context);
}
