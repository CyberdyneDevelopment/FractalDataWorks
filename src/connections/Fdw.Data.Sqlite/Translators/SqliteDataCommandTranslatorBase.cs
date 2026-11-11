using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Rows;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sql;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Conventions;

namespace Fdw.Data.Sqlite.Translators;

/// <summary>
/// Base class for SQLite data command translators.
/// Implements the two <see cref="SqlDataCommandTranslatorBase{TCommand}"/> hooks for
/// <see cref="SqliteCommand"/> and <see cref="SqliteParameter"/>.
/// </summary>
/// <remarks>
/// Shared SQL-emission helpers (WHERE, ORDER BY, column validation, parameter plumbing)
/// live in <see cref="SqlDataCommandTranslatorBase{TCommand}"/> and are dialect-parameterized
/// at translate-time via <c>IDatabasePath.Dialect</c>.
/// This class only adds the <c>SqliteCommand</c>/<c>SqliteParameter</c> creation hooks.
/// </remarks>
public abstract class SqliteDataCommandTranslatorBase : SqlDataCommandTranslatorBase<SqliteCommand>
{
    /// <summary>What the database is given for a parameter whose value is null: ADO.NET's own null.</summary>
    [PlatformDefault]
    private static readonly object DatabaseNull = System.DBNull.Value;

    /// <summary>
    /// Null logger used by translator catch blocks to satisfy the structured-logging contract.
    /// </summary>
    protected static readonly ILogger TranslatorLogger = NullLogger.Instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteDataCommandTranslatorBase"/> class.
    /// </summary>
    /// <param name="name">Name of the translator (must match the <c>[TypeOption]</c> attribute).</param>
    protected SqliteDataCommandTranslatorBase(string name)
        : base(name, "Sqlite")
    {
    }

    /// <summary>
    /// Creates a new <see cref="SqliteCommand"/> with the supplied SQL text.
    /// </summary>
    /// <param name="sql">The SQL command text.</param>
    protected static SqliteCommand CreateCommand(string sql) => new SqliteCommand(sql);

    /// <summary>
    /// Adds a named parameter to <paramref name="command"/>, serializing <see cref="IEnumerable"/>
    /// values (except <see cref="string"/>) to JSON text for SQLite TEXT columns.
    /// </summary>
    /// <param name="command">The SQLite command.</param>
    /// <param name="name">Parameter name without the <c>@</c> prefix.</param>
    /// <param name="value">Parameter value; <c>null</c> maps to <see cref="System.DBNull.Value"/>.</param>
    protected static void AddParameter(SqliteCommand command, string name, object? value)
    {
        // Why byte[] is excluded: it is one blob value, not a collection (FDW-889).
        var materialized = value is IEnumerable enumerable && value is not string && value is not byte[]
            ? JsonSerializer.Serialize(enumerable)
            : value;

        var param = new SqliteParameter($"@{name}", materialized ?? DatabaseNull);
        command.Parameters.Add(param);
    }

    /// <summary>
    /// Binds a set-based write's rows as <c>VALUES</c> sets (FDW-4953, FDW-4954): one <c>WITH s (columns) AS (VALUES ...)</c>
    /// clause for as many rows as one statement may bind, each value its own parameter on <paramref name="command"/>. A batch
    /// within the parameter limit is one clause, and so one statement.
    /// </summary>
    /// <param name="command">The command the parameters are added to.</param>
    /// <param name="rows">The rows.</param>
    /// <param name="columns">The columns each row binds, as the container declares them.</param>
    /// <param name="container">The target, for the refusal of a row without a column.</param>
    /// <param name="dialect">The dialect that quotes the column names.</param>
    /// <returns>One <c>WITH</c> clause for each group of rows, in order.</returns>
    /// <exception cref="InvalidOperationException">A row has no value for one of the columns.</exception>
    protected static IReadOnlyList<string> ValueSets(
        SqliteCommand command, IReadOnlyList<object> rows, IReadOnlyList<string> columns, IStorageContainer container, ISqlDialect dialect)
    {
        var columnList = string.Join(", ", columns.Select(dialect.QuoteIdentifier));
        var rowsPerStatement = Math.Max(1, KeyListSpillScope.ParameterBudget / columns.Count);
        var sets = new List<string>();
        for (var start = 0; start < rows.Count; start += rowsPerStatement)
        {
            var values = rows.Skip(start).Take(rowsPerStatement)
                .Select(row => $"({string.Join(", ", columns.Select(column => Bind(command, row, column, container)))})");
            sets.Add($"WITH s ({columnList}) AS (VALUES {string.Join(", ", values)}) ");
        }

        return sets;
    }

    /// <summary>
    /// Joins a set-based write's statements into one command text inside a savepoint (FDW-4955), so a batch split at the parameter
    /// limit lands whole or not at all: inside a unit of work's transaction or on its own.
    /// </summary>
    /// <param name="savepoint">The savepoint's name.</param>
    /// <param name="statements">The statements, each ending in a semicolon.</param>
    /// <returns>The command text.</returns>
    protected static string InSavepoint(string savepoint, IEnumerable<string> statements)
        => $"SAVEPOINT {savepoint};\n{string.Join("\n", statements)}\nRELEASE {savepoint};";

    /// <summary>
    /// Builds a bulk update or delete whose command reports its unmatched keys (FDW-4988), in one savepoint: every group of rows is staged in one temporary
    /// table, the staged keys with no target row are counted and the first of them kept before the write, the write runs once against the stage, and the
    /// summary (<c>changes()</c> and the count) and the kept keys follow as two result sets. SQLite has no statement that writes and selects at once, so the
    /// counts are kept in temporary tables; its one writer at a time holds them true until the write.
    /// </summary>
    /// <param name="savepoint">The savepoint's name.</param>
    /// <param name="command">The command the values and the sample size are bound to.</param>
    /// <param name="rows">The rows, one for each distinct key.</param>
    /// <param name="staged">The columns staged.</param>
    /// <param name="keys">The key columns.</param>
    /// <param name="sample">How many unmatched keys the answer carries.</param>
    /// <param name="target">The quoted, qualified target table.</param>
    /// <param name="container">The target container, which binds each value.</param>
    /// <param name="dialect">The dialect that quotes identifiers.</param>
    /// <param name="write">Builds the one write statement, ending with a semicolon, from the stage's name (aliased <c>s</c> by the statement).</param>
    /// <returns>The statements to run.</returns>
    protected static string ReportingUnmatched(
        string savepoint, SqliteCommand command, IReadOnlyList<object> rows, IReadOnlyList<string> staged, IReadOnlyList<string> keys, int sample, string target,
        IStorageContainer container, ISqlDialect dialect, Func<string, string> write)
    {
        var id = Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);
        var stage = $"fdw_s_{id}";
        var counted = $"fdw_n_{id}";
        var kept = $"fdw_um_{id}";
        var columns = string.Join(", ", staged.Select(dialect.QuoteIdentifier));
        var keyColumns = string.Join(", ", keys.Select(key => $"s.{dialect.QuoteIdentifier(key)}"));
        var names = string.Join(", ", keys.Select(dialect.QuoteIdentifier));
        var absent = $"FROM {stage} AS s WHERE NOT EXISTS (SELECT 1 FROM {target} AS t WHERE {string.Join(" AND ", keys.Select(key => $"t.{dialect.QuoteIdentifier(key)} = s.{dialect.QuoteIdentifier(key)}"))})";

        var statements = new List<string> { $"CREATE TEMP TABLE {stage} AS SELECT {columns} FROM {target} WHERE 0;" };
        statements.AddRange(ValueSets(command, rows, staged, container, dialect).Select(values => $"{values}INSERT INTO {stage} ({columns}) SELECT {columns} FROM s;"));
        AddParameter(command, "fdw_unmatched_sample", sample);
        statements.Add($"CREATE TEMP TABLE {counted} AS SELECT count(*) AS n {absent};");
        statements.Add($"CREATE TEMP TABLE {kept} AS SELECT {keyColumns} {absent} ORDER BY {keyColumns} LIMIT @fdw_unmatched_sample;");
        statements.Add(write(stage));
        statements.Add($"SELECT changes() AS {BulkWriteAnswer.AffectedColumn}, (SELECT n FROM {counted}) AS {BulkWriteAnswer.UnmatchedColumn};");
        statements.Add($"SELECT {names} FROM {kept} ORDER BY {names};");
        statements.Add($"DROP TABLE {stage}; DROP TABLE {counted}; DROP TABLE {kept};");
        return InSavepoint(savepoint, statements);
    }

    // Each value is bound as its own parameter, read the one way every route reads a row (RowValues), so a dictionary row binds as
    // an entity row does. A row without the column is refused: the write would otherwise use a NULL it was never given.
    private static string Bind(SqliteCommand command, object row, string column, IStorageContainer container)
    {
        if (!RowValues.TryRead(row, column, out var value, out _))
            throw new InvalidOperationException($"A source row for container {container.Name} has no column '{column}'");

        var name = $"p{command.Parameters.Count}";
        AddParameter(command, name, value);
        return $"@{name}";
    }
}
