using System.Collections;
using System.Data;
using System.Text.Json;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.MsSql.Logging;
using Fdw.Services.Connections.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql.Translators;

/// <summary>
/// Base class for MS SQL Server (T-SQL) data command translators.
/// Implements the two <see cref="SqlDataCommandTranslatorBase{TCommand}"/> hooks for
/// <see cref="SqlCommand"/> and <see cref="SqlParameter"/>.
/// </summary>
/// <remarks>
/// <para>
/// Shared SQL-emission helpers (WHERE, ORDER BY, column validation, parameter plumbing)
/// live in <see cref="SqlDataCommandTranslatorBase{TCommand}"/> and are dialect-parameterized
/// at translate-time via <c>IDatabasePath.Dialect</c> — no T-SQL quoting is hardcoded there.
/// </para>
/// <para>
/// This class only adds the <c>SqlCommand</c>/<c>SqlParameter</c> creation + a convenience
/// <c>AddParameter</c> overload with an explicit <see cref="SqlDbType"/> for callers that know
/// the exact type.
/// </para>
/// <para>
/// Boxing Optimization: <see cref="SqlParameter"/> objects are created with explicit
/// <see cref="SqlDbType"/>, so boxing is minimized and only occurs at the ADO.NET level.
/// </para>
/// </remarks>
public abstract class MsSqlDataCommandTranslatorBase : SqlDataCommandTranslatorBase<SqlCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlDataCommandTranslatorBase"/> class.
    /// </summary>
    /// <param name="name">Name of the translator (must match the <c>[TypeOption]</c> attribute).</param>
    protected MsSqlDataCommandTranslatorBase(string name)
        : base(name, "MsSql")
    {
    }

    /// <summary>
    /// Creates a new <see cref="SqlCommand"/> with the supplied SQL text.
    /// </summary>
    /// <param name="sql">The SQL command text.</param>
    // Why: defined here (not as abstract on the shared SqlDataCommandTranslatorBase<TCommand>)
    // to avoid the TypeCollection source generator emitting 'public override' stubs (CS0507).
    // Why static: no instance state required; static avoids CA1822 in Release.
    protected static SqlCommand CreateCommand(string sql) => new SqlCommand(sql);

    /// <summary>
    /// Adds a named parameter to <paramref name="command"/>, serializing <see cref="IEnumerable"/>
    /// values (except <see cref="string"/>) to JSON for NVARCHAR(MAX) JSON columns.
    /// </summary>
    /// <param name="command">The SQL command.</param>
    /// <param name="name">Parameter name without the <c>@</c> prefix.</param>
    /// <param name="value">Parameter value; <c>null</c> maps to <see cref="System.DBNull.Value"/>.</param>
    /// <remarks>
    /// Why: <see cref="SqlParameter"/> cannot marshal <see cref="IEnumerable"/> types
    /// (e.g., <c>List&lt;string&gt;</c>) to SQL Server native types. Columns that hold
    /// collections are modelled as NVARCHAR(MAX) JSON blobs; non-string enumerables are
    /// serialized before handing to ADO.NET.
    /// Why (defined here): same CS0507 reason as CreateCommand — no abstract on shared base.
    /// </remarks>
    // Why static: same CS0507/CA1822 rationale as CreateCommand.
    protected static void AddParameter(SqlCommand command, string name, object? value)
    {
        object? materialized = value;
        if (value is IEnumerable enumerable && value is not string)
        {
            MsSqlDataCommandTranslatorBaseLog.SerializingEnumerableParameter(
                NullLogger<MsSqlDataCommandTranslatorBase>.Instance, name);
            materialized = JsonSerializer.Serialize(enumerable);
        }

        var param = new SqlParameter($"@{name}", materialized ?? (object)System.DBNull.Value);
        command.Parameters.Add(param);
    }

    /// <summary>
    /// Adds a parameter to <see cref="SqlCommand"/> with explicit <see cref="SqlDbType"/>.
    /// Use this for optimal performance when the type is known at the call site.
    /// </summary>
    /// <param name="command">The SQL command.</param>
    /// <param name="name">Parameter name (without @ prefix).</param>
    /// <param name="value">Parameter value.</param>
    /// <param name="dbType">The SQL database type.</param>
    protected static void AddParameter(SqlCommand command, string name, object? value, SqlDbType dbType)
    {
        var param = new SqlParameter($"@{name}", dbType)
        {
            Value = value ?? (object)System.DBNull.Value
        };
        command.Parameters.Add(param);
    }
}
