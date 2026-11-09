using System;
using System.Collections;
using System.Data;
using System.Text.Json;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.MsSql.Logging;
using Fdw.Services.Connections.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Conventions;

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
    /// <summary>What the database is given for a parameter whose value is null: ADO.NET's own null.</summary>
    [PlatformDefault]
    private static readonly object DatabaseNull = System.DBNull.Value;

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
    protected static SqlCommand CreateCommand(string sql) => new SqlCommand(sql);

    /// <inheritdoc />
    /// <remarks>
    /// A statement that reads starts with SELECT and one that writes does not, and the connection tells the two apart by that first word (it runs the first
    /// with a reader and the second for its row count). The key tables are therefore made first in a SELECT ... INTO, for a statement that reads; a statement
    /// that writes gets SET NOCOUNT around the key-table statements, so the row count it returns is its own and not theirs.
    /// </remarks>
    protected override void WrapStatement(SqlCommand command, string before, string after)
    {
        var statement = command.CommandText;
        command.CommandText = statement.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            ? string.Concat(before, statement, "; ", after)
            : string.Concat("SET NOCOUNT ON; ", before, "SET NOCOUNT OFF; ", statement, "; ", after);
    }

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
    protected static void AddParameter(SqlCommand command, string name, object? value)
    {
        object? materialized = value;
        // Why byte[] is excluded: it is IEnumerable, but it is one binary value (varbinary), not a
        // collection. Serialized to JSON it reached a binary column as nvarchar, SQL 257 (FDW-889).
        if (value is IEnumerable enumerable && value is not string && value is not byte[])
        {
            MsSqlDataCommandTranslatorBaseLog.SerializingEnumerableParameter(
                NullLogger<MsSqlDataCommandTranslatorBase>.Instance, name);
            materialized = JsonSerializer.Serialize(enumerable);
        }

        var param = new SqlParameter($"@{name}", materialized ?? DatabaseNull);
        command.Parameters.Add(param);
    }

    /// <summary>
    /// Adds a named parameter whose value was read from a property, typing a null by the property's
    /// declared type.
    /// </summary>
    /// <param name="command">The SQL command.</param>
    /// <param name="name">Parameter name without the <c>@</c> prefix.</param>
    /// <param name="value">The property's value; <c>null</c> maps to <see cref="System.DBNull.Value"/>.</param>
    /// <param name="declaredType">The property's declared type.</param>
    /// <remarks>
    /// A null reaches ADO.NET as an untyped <see cref="System.DBNull"/>, which SqlClient sends as
    /// NVARCHAR. SQL Server converts an NVARCHAR NULL implicitly to every column type except BINARY and
    /// VARBINARY, so a null <c>byte[]</c> has to say it is binary or the statement fails with SQL 257
    /// (FDW-918: a new session's null <c>CsrfTokenHash</c>). Every other value binds exactly as
    /// <see cref="AddParameter(SqlCommand, string, object?)"/> binds it.
    /// </remarks>
    protected static void AddParameter(SqlCommand command, string name, object? value, Type declaredType)
    {
        if (value is null && declaredType == typeof(byte[]))
        {
            MsSqlDataCommandTranslatorBaseLog.BindingNullBinaryParameter(
                NullLogger<MsSqlDataCommandTranslatorBase>.Instance, name);
            AddParameter(command, name, null, SqlDbType.VarBinary);
            return;
        }

        AddParameter(command, name, value);
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
            Value = value ?? DatabaseNull
        };
        command.Parameters.Add(param);
    }
}
