using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Logging;
using Fdw.Commands.Data.Upserts;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Sql;

/// <summary>
/// The two set-based statements of an upsert, for any SQL dialect (FDW-4955): update the matched rows whose compare columns
/// differ (or, with versioning, close their current version), then insert the rows whose key has no (current) row. The same
/// shape the SQL Server upsert runs, so every kind updates a matched row instead of replacing it.
/// </summary>
/// <remarks>
/// The caller supplies the row set (aliased <c>s</c>), how its dialect tests two values differ when either may be null, and the
/// expression of the instant a versioned close and insert share. Column names are the ones the container declares, quoted by the
/// dialect; values are only ever parameters.
/// </remarks>
public static class SetBasedUpsert
{
    /// <summary>The parameter the insert-only rule's marker value is bound to.</summary>
    public const string MarkerParameter = "fdw_marker";

    /// <summary>The parameter the current-flag value of a new version is bound to.</summary>
    public const string CurrentParameter = "fdw_current";

    /// <summary>The parameter the current-flag value of a closed version is bound to.</summary>
    public const string ClosedParameter = "fdw_closed";

    /// <summary>Describes the first thing wrong with the options for the container on a kind that renders no value source, or returns <see langword="null"/>.</summary>
    /// <param name="options">The upsert's options.</param>
    /// <param name="container">The target.</param>
    /// <param name="kind">The connection kind, for the refusal of a value source.</param>
    /// <returns>The refusal, or <see langword="null"/>.</returns>
    public static IGenericMessage? Problem(UpsertOptions options, IStorageContainer container, string kind)
    {
        var general = options.FirstProblem();
        if (general is not null)
            return general;
        if (options.ValueSources is { Count: > 0 })
            return Inconsistent($"the {kind} upsert writes the rows it is given; a value the database supplies (column '{options.ValueSources[0].Column}') is not rendered on this kind");

        var unknown = Referenced(options).FirstOrDefault(column => Field(container, column) is null);
        if (unknown is not null)
            return Inconsistent($"container {container.Name} does not declare column '{unknown}'");

        var keyCompare = options.CompareColumns.FirstOrDefault(column => options.KeyColumns.Contains(column, StringComparer.OrdinalIgnoreCase));
        if (keyCompare is not null)
            return Inconsistent($"compare column '{keyCompare}' is also a key column");

        var computed = options.KeyColumns.Concat(options.CompareColumns).FirstOrDefault(column => Field(container, column)?.IsComputed == true);
        if (computed is not null)
            return Inconsistent($"column '{computed}' is computed and cannot be a key or compare column");

        var identity = options.CompareColumns.FirstOrDefault(column => Field(container, column)?.IsIdentity == true);
        return identity is null ? null : Inconsistent($"compare column '{identity}' is an identity column");
    }

    /// <summary>
    /// Builds the change statement (update, or close with versioning) and the insert statement, in the order they run. The
    /// options must have passed <see cref="Problem"/>.
    /// </summary>
    /// <param name="options">The upsert's options.</param>
    /// <param name="container">The target.</param>
    /// <param name="dialect">The dialect that quotes names and prefixes parameters.</param>
    /// <param name="target">The quoted, qualified target table.</param>
    /// <param name="source">The row set as it follows <c>FROM</c>, aliased <c>s</c>, with the write columns as the container declares them.</param>
    /// <param name="differs">Gives the dialect's test that two values differ, either of them possibly null.</param>
    /// <param name="now">The expression of the instant a versioned close and insert share.</param>
    /// <param name="addParameter">Adds a named parameter (without its prefix) to the command being built.</param>
    /// <returns>The change statement, which may be empty when nothing can change, then the insert statement; neither ends in a semicolon.</returns>
    public static IReadOnlyList<string> Statements(
        UpsertOptions options,
        IStorageContainer container,
        ISqlDialect dialect,
        string target,
        string source,
        Func<string, string, string> differs,
        string now,
        Action<string, object?> addParameter)
    {
        var shape = new Shape(options, container, dialect, target, source, differs, addParameter);
        var versioning = options.Versioning;
        if (versioning is not null && versioning.CurrentFlagColumn is not null)
        {
            addParameter(CurrentParameter, true);
            addParameter(ClosedParameter, false);
        }

        return [versioning is null ? shape.Update() : shape.Close(versioning, now), shape.Insert(versioning, now)];
    }

    /// <summary>
    /// Lists the columns each staged row carries, as the container declares them: the write columns the statements read, which
    /// are the keys and every write column the database does not number or compute.
    /// </summary>
    /// <param name="options">The upsert's options.</param>
    /// <param name="container">The target.</param>
    /// <returns>The declared names, in write-column order.</returns>
    public static IReadOnlyList<string> StagedColumns(UpsertOptions options, IStorageContainer container)
    {
        return SetBasedColumns.Declared(container, options.WriteColumns)
            .Where(column => options.KeyColumns.Contains(column, StringComparer.OrdinalIgnoreCase)
                || Field(container, column) is { IsIdentity: false, IsComputed: false })
            .ToList();
    }

    private static List<string> Referenced(UpsertOptions options)
    {
        var columns = new List<string>();
        columns.AddRange(options.KeyColumns);
        columns.AddRange(options.CompareColumns);
        columns.AddRange(options.WriteColumns);
        if (options.Versioning is not { } versioning)
            return columns;

        columns.Add(versioning.EffectiveFromColumn);
        columns.Add(versioning.EffectiveToColumn);
        if (versioning.CurrentFlagColumn is not null)
            columns.Add(versioning.CurrentFlagColumn);
        if (versioning.InsertOnlyRule is not null)
            columns.Add(versioning.InsertOnlyRule.Column);
        return columns;
    }

    private static IField? Field(IStorageContainer container, string column)
        => container.Schema.Fields.FirstOrDefault(field => string.Equals(field.Name, column, StringComparison.OrdinalIgnoreCase));

    private static IGenericMessage Inconsistent(string reason)
        => CommandOptionsLog.UpsertOptionsInconsistent(NullLogger.Instance, reason);

    // The parts both statements share: the declared, quoted names and the predicates over them.
    private sealed class Shape
    {
        private readonly UpsertOptions _options;
        private readonly IStorageContainer _container;
        private readonly ISqlDialect _dialect;
        private readonly string _target;
        private readonly string _source;
        private readonly Func<string, string, string> _differs;
        private readonly Action<string, object?> _addParameter;

        internal Shape(
            UpsertOptions options, IStorageContainer container, ISqlDialect dialect, string target, string source,
            Func<string, string, string> differs, Action<string, object?> addParameter)
        {
            _options = options;
            _container = container;
            _dialect = dialect;
            _target = target;
            _source = source;
            _differs = differs;
            _addParameter = addParameter;
        }

        internal string Update()
        {
            var sets = Written().Where(column => !_options.KeyColumns.Contains(column, StringComparer.OrdinalIgnoreCase)).ToList();
            return sets.Count == 0
                ? string.Empty
                : $"UPDATE {_target} AS t SET {string.Join(", ", sets.Select(column => $"{Q(column)} = s.{Q(column)}"))} "
                  + $"FROM {_source} WHERE {KeysOn("t")} AND ({Differs()}){Marker()}";
        }

        internal string Close(VersioningOptions versioning, string now)
        {
            var flag = versioning.CurrentFlagColumn is null ? string.Empty : $", {Declared(versioning.CurrentFlagColumn)} = {P(ClosedParameter)}";
            return $"UPDATE {_target} AS t SET {Declared(versioning.EffectiveToColumn)} = {now}{flag} "
                   + $"FROM {_source} WHERE {KeysOn("t")} AND t.{Declared(versioning.EffectiveToColumn)} IS NULL AND ({Differs()}){Marker()}";
        }

        internal string Insert(VersioningOptions? versioning, string now)
        {
            var columns = Written().ToList();
            var targets = columns.Select(Q).ToList();
            var values = columns.Select(column => $"s.{Q(column)}").ToList();
            var currentOnly = string.Empty;
            if (versioning is not null)
            {
                targets.Add(Declared(versioning.EffectiveFromColumn));
                values.Add(now);
                targets.Add(Declared(versioning.EffectiveToColumn));
                values.Add("NULL");
                if (versioning.CurrentFlagColumn is not null)
                {
                    targets.Add(Declared(versioning.CurrentFlagColumn));
                    values.Add(P(CurrentParameter));
                }

                currentOnly = $" AND e.{Declared(versioning.EffectiveToColumn)} IS NULL";
            }

            return $"INSERT INTO {_target} ({string.Join(", ", targets)}) SELECT {string.Join(", ", values)} FROM {_source} "
                   + $"WHERE NOT EXISTS (SELECT 1 FROM {_target} AS e WHERE {KeysOn("e")}{currentOnly})";
        }

        // The write columns the database does not number or compute, as the container declares them.
        private IEnumerable<string> Written()
            => SetBasedColumns.Declared(_container, _options.WriteColumns)
                .Where(column => Field(_container, column) is { IsIdentity: false, IsComputed: false });

        private string KeysOn(string alias)
            => string.Join(" AND ", SetBasedColumns.Declared(_container, _options.KeyColumns).Select(key => $"{alias}.{Q(key)} = s.{Q(key)}"));

        private string Differs()
            => _options.CompareColumns.Count == 0
                ? "1 = 1"
                : string.Join(" OR ", SetBasedColumns.Declared(_container, _options.CompareColumns).Select(column => _differs($"t.{Q(column)}", $"s.{Q(column)}")));

        private string Marker()
        {
            var rule = _options.Versioning?.InsertOnlyRule;
            if (rule is null)
                return string.Empty;
            _addParameter(MarkerParameter, rule.Value);
            var column = Declared(rule.Column);
            return $" AND (t.{column} IS NULL OR t.{column} <> {P(MarkerParameter)})";
        }

        private string Declared(string column) => Q(SetBasedColumns.Declared(_container, column));

        private string Q(string column) => _dialect.QuoteIdentifier(column);

        private string P(string parameter) => $"{_dialect.ParameterPrefix}{parameter}";
    }
}
