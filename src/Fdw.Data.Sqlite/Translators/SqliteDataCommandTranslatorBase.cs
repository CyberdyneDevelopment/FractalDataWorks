using System.Collections;
using System.Text.Json;
using Fdw.Services.Connections.Sql;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
    /// <summary>
    /// Null logger used by translator catch blocks to satisfy the structured-logging contract.
    /// </summary>
    // Why: translators are TypeOption singletons created without DI — NullLogger.Instance
    // is the only safe static logger here. It ensures TranslationFailed() log calls are
    // structurally correct (exception IS passed to the method; infrastructure would capture it
    // if a real logger were ever wired). Callers with real loggers log via their own fields.
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
    // Why: static for CA1822; defined here (not as abstract on the shared base) to avoid
    // CS0507 from the TypeCollection source generator emitting 'public override' stubs.
    protected static SqliteCommand CreateCommand(string sql) => new SqliteCommand(sql);

    /// <summary>
    /// Adds a named parameter to <paramref name="command"/>, serializing <see cref="IEnumerable"/>
    /// values (except <see cref="string"/>) to JSON text for SQLite TEXT columns.
    /// </summary>
    /// <param name="command">The SQLite command.</param>
    /// <param name="name">Parameter name without the <c>@</c> prefix.</param>
    /// <param name="value">Parameter value; <c>null</c> maps to <see cref="System.DBNull.Value"/>.</param>
    // Why: SqliteParameter cannot marshal IEnumerable types natively; serialize to JSON text to
    // match how MsSql handles JSON-column values. Same pattern for consistency.
    // Why static: same CS0507/CA1822 rationale as CreateCommand.
    protected static void AddParameter(SqliteCommand command, string name, object? value)
    {
        var materialized = value is IEnumerable enumerable && value is not string
            ? JsonSerializer.Serialize(enumerable)
            : value;

        var param = new SqliteParameter($"@{name}", materialized ?? (object)System.DBNull.Value);
        command.Parameters.Add(param);
    }
}
