using System.Collections;
using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions;
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
    // Why: defined here (not as abstract on the shared SqlDataCommandTranslatorBase<TCommand>)
    // to avoid the TypeCollection source generator emitting 'public override' stubs (CS0507).
    // Why static: no instance state required; static avoids CA1822 in Release.
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
}
