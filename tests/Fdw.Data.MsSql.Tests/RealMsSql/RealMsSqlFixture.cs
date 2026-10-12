using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Fdw.Data.MsSql.Tests.RealMsSql;

/// <summary>
/// Gives the acceptance tests a real SQL Server to run against: a schema of its own, created before the tests and
/// dropped (with everything in it) after them. Nothing outside that schema is read, written or dropped, so demo and
/// seeded data are never touched.
/// </summary>
/// <remarks>
/// The connection string comes from the <c>FDW_TEST_MSSQL_CONNECTION</c> environment variable (a login that can
/// create a schema, tables and sequences, such as the dev data database's DDL login). When it is not set the tests
/// report a visible skip, never a silent pass.
/// </remarks>
public sealed class RealMsSqlFixture : IAsyncLifetime
{
    /// <summary>The environment variable that holds the connection string.</summary>
    public const string ConnectionVariable = "FDW_TEST_MSSQL_CONNECTION";

    private readonly string? _connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
    private int _counter;

    /// <summary>Gets the schema this run owns.</summary>
    public string Schema { get; } = $"fdw1107_{Guid.NewGuid():N}"[..16];

    /// <summary>Skips the calling test visibly when no SQL Server was configured.</summary>
    public void RequireDatabase()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            Assert.Skip($"Set {ConnectionVariable} to a dev SQL Server connection string to run the real-MsSql acceptance tests.");
    }

    /// <summary>Returns a name no other test in this run uses, built from <paramref name="stem"/>.</summary>
    public string Unique(string stem) => $"{stem}_{Interlocked.Increment(ref _counter)}";

    /// <summary>Opens a new connection to the configured server.</summary>
    public async Task<SqlConnection> Open()
    {
        RequireDatabase();
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        return connection;
    }

    /// <summary>Runs <paramref name="sql"/> and returns the rows affected.</summary>
    public async Task<int> Exec(string sql)
    {
        await using var connection = await Open();
        await using var command = new SqlCommand(sql, connection);
        return await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Runs a query and returns its rows as dictionaries keyed by column name.</summary>
    public async Task<List<Dictionary<string, object?>>> Rows(string sql)
    {
        await using var connection = await Open();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = await reader.IsDBNullAsync(i, TestContext.Current.CancellationToken) ? null : reader.GetValue(i);
            rows.Add(row);
        }

        return rows;
    }

    /// <summary>Runs a query that returns one value.</summary>
    public async Task<object?> Scalar(string sql)
    {
        var rows = await Rows(sql);
        return rows[0].Values.First();
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return;
        await Exec($"CREATE SCHEMA [{Schema}]");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return;

        await Exec(
            "DECLARE @sql nvarchar(max) = N''; "
            + $"SELECT @sql += CASE o.type WHEN 'U' THEN N'DROP TABLE ' ELSE N'DROP SEQUENCE ' END + QUOTENAME(s.name) + N'.' + QUOTENAME(o.name) + N'; ' "
            + $"FROM sys.objects o JOIN sys.schemas s ON s.schema_id = o.schema_id WHERE s.name = N'{Schema}' AND o.type IN ('U', 'SO') "
            + "ORDER BY CASE o.type WHEN 'U' THEN 0 ELSE 1 END; "
            + $"SET @sql += N'DROP SCHEMA [{Schema}];'; EXEC sp_executesql @sql;");
    }
}
