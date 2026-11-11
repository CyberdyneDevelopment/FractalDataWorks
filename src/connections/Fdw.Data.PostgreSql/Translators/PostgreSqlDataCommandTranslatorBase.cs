using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sql;
using Npgsql;
using NpgsqlTypes;

namespace Fdw.Data.PostgreSql.Translators;

/// <summary>
/// Base class for PostgreSQL data command translators.
/// Derives from the shared <see cref="SqlDataCommandTranslatorBase{TCommand}"/> and supplies
/// only the Npgsql-native <c>CreateCommand</c> and <c>AddParameter</c> helpers.
/// All WHERE/ORDER BY/paging/column-validation helpers are inherited and dialect-parameterized
/// at translate-time via <c>IDatabasePath.Dialect</c> (<see cref="PlPgSqlDialect.Instance"/>).
/// </summary>
/// <remarks>
/// <para>
/// Key PostgreSQL differences from T-SQL:
/// <list type="bullet">
/// <item>Identifiers quoted with double-quotes (not brackets)</item>
/// <item>LIMIT/OFFSET instead of OFFSET/FETCH</item>
/// <item>RETURNING instead of OUTPUT/SCOPE_IDENTITY</item>
/// <item>ILIKE for case-insensitive LIKE</item>
/// <item>FALSE as the always-false predicate</item>
/// </list>
/// </para>
/// </remarks>
public abstract class PostgreSqlDataCommandTranslatorBase : SqlDataCommandTranslatorBase<NpgsqlCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlDataCommandTranslatorBase"/> class.
    /// </summary>
    /// <param name="name">Name of the translator (must match TypeOption attribute).</param>
    protected PostgreSqlDataCommandTranslatorBase(string name)
        : base(name, "PostgreSql")
    {
    }

    /// <summary>
    /// Creates a new <see cref="NpgsqlCommand"/> with the supplied SQL text.
    /// </summary>
    /// <param name="sql">The SQL command text.</param>
    protected static NpgsqlCommand CreateCommand(string sql) => new NpgsqlCommand(sql);

    /// <summary>
    /// Adds a named parameter to <paramref name="command"/>.
    /// </summary>
    /// <param name="command">The Npgsql command.</param>
    /// <param name="name">Parameter name without the <c>@</c> prefix.</param>
    /// <param name="value">Parameter value; <c>null</c> maps to <see cref="System.DBNull.Value"/>.</param>
    protected static void AddParameter(NpgsqlCommand command, string name, object? value)
    {
        command.Parameters.Add(new NpgsqlParameter(name, value ?? System.DBNull.Value));
    }

    /// <summary>
    /// Adds a parameter to <paramref name="command"/> with an explicit <see cref="NpgsqlDbType"/>.
    /// </summary>
    /// <param name="command">The Npgsql command.</param>
    /// <param name="name">Parameter name without the <c>@</c> prefix.</param>
    /// <param name="value">Parameter value; <c>null</c> maps to <see cref="System.DBNull.Value"/>.</param>
    /// <param name="dbType">The PostgreSQL database type.</param>
    protected static void AddParameter(NpgsqlCommand command, string name, object? value, NpgsqlDbType dbType)
    {
        command.Parameters.Add(new NpgsqlParameter(name, dbType) { Value = value ?? System.DBNull.Value });
    }

    /// <summary>
    /// Wraps a bulk update or delete whose command reports its unmatched keys (FDW-4988): the target is locked against other writers, the first of the staged keys
    /// with no target row are kept, the write runs as a CTE in one statement that also counts those keys from the same snapshot, and the kept keys follow as the
    /// second result set. A command that does not report gets <paramref name="write"/> as it is.
    /// </summary>
    /// <param name="write">The single UPDATE or DELETE statement, without a semicolon; it may reference <paramref name="source"/>.</param>
    /// <param name="sample">How many unmatched keys the answer carries, or <see langword="null"/> when the command does not report.</param>
    /// <param name="target">The quoted, qualified target table.</param>
    /// <param name="source">The staged rows, aliased <c>s</c>.</param>
    /// <param name="keys">The key columns.</param>
    /// <param name="dialect">The dialect that quotes identifiers.</param>
    /// <param name="command">The command the sample size parameter is added to.</param>
    /// <returns>The statements to run.</returns>
    protected static string ReportingUnmatched(
        string write, int? sample, string target, string source, System.Collections.Generic.IReadOnlyList<string> keys, ISqlDialect dialect, NpgsqlCommand command)
    {
        if (sample is not { } size)
            return write;

        AddParameter(command, "fdw_unmatched_sample", size, NpgsqlDbType.Integer);
        var columns = string.Join(", ", keys.Select(key => $"s.{dialect.QuoteIdentifier(key)}"));
        var names = string.Join(", ", keys.Select(dialect.QuoteIdentifier));
        var on = string.Join(" AND ", keys.Select(key => $"t.{dialect.QuoteIdentifier(key)} = s.{dialect.QuoteIdentifier(key)}"));
        var absent = $"FROM {source} WHERE NOT EXISTS (SELECT 1 FROM {target} AS t WHERE {on})";
        var kept = "fdw_um_" + Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);
        return $"LOCK TABLE {target} IN SHARE ROW EXCLUSIVE MODE; "
               + $"CREATE TEMP TABLE {kept} AS SELECT {columns} {absent} ORDER BY {columns} LIMIT {dialect.ParameterPrefix}fdw_unmatched_sample; "
               + $"WITH w AS ({write} RETURNING 1) SELECT (SELECT count(*) FROM w) AS {BulkWriteAnswer.AffectedColumn}, (SELECT count(*) {absent}) AS {BulkWriteAnswer.UnmatchedColumn}; "
               + $"SELECT {names} FROM {kept} ORDER BY {names}; "
               + $"DROP TABLE {kept}";
    }
}
