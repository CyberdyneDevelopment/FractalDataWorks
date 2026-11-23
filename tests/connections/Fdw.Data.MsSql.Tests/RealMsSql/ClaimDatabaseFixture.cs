using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.Tests.TestSupport;
using Fdw.Data.MsSql.Translators;
using Microsoft.Data.SqlClient;

namespace Fdw.Data.MsSql.Tests.RealMsSql;

/// <summary>
/// Gives the durable-claim tests (FDW-6629) a database of their own: created before the tests, filled with the OpsDb table and the procedures exactly as the databases repository
/// states them, and dropped after, so nothing of the server the connection names is read, written or kept.
/// </summary>
/// <remarks>
/// <para>
/// The connection string comes from <c>FDW_TEST_MSSQL_CONNECTION</c> (a login that may create a database, such as the build owner's scratch container). When it is not set the tests
/// report a visible skip, like the other real-SQL tests. When it IS set, <c>FDW_TEST_DATABASES_PATH</c> must name a checkout of the databases repository and every file named below must be
/// there: a missing variable, path or file fails the fixture and so every test, naming what is missing. A test that cannot run for want of its input never passes by skipping.
/// </para>
/// <para>
/// The database is created in the collation of the dev ConfigurationDb (<c>Latin1_General_100_CI_AS_SC_UTF8</c>), so a key with a non-Latin letter is kept as it is written.
/// </para>
/// </remarks>
public sealed class ClaimDatabaseFixture : IAsyncLifetime
{
    /// <summary>The environment variable that names the databases repository checkout.</summary>
    public const string DatabasesPathVariable = "FDW_TEST_DATABASES_PATH";

    /// <summary>The files of the databases repository the claim needs, in the order they are run.</summary>
    public static readonly IReadOnlyList<string> Files =
    [
        "OpsDb/etl/Tables/etl.PipelineRunClaim.sql",
        "OpsDb/etl/Tables/etl.PipelineKeyHold.sql",
        "OpsDb/etl/StoredProcedures/etl.PipelineRunClaimIsBlocked.sql",
        "OpsDb/etl/StoredProcedures/etl.ClaimPipelineRun.sql",
        "OpsDb/etl/StoredProcedures/etl.RenewPipelineRunClaims.sql",
        "OpsDb/etl/StoredProcedures/etl.ReleasePipelineRunClaims.sql",
        "OpsDb/etl/StoredProcedures/etl.PlaceKeyHold.sql",
        "OpsDb/etl/StoredProcedures/etl.ReleaseKeyHold.sql",
    ];

    private readonly string? _connectionString = Environment.GetEnvironmentVariable(RealMsSqlFixture.ConnectionVariable);

    /// <summary>Gets the name of the throwaway database this run owns.</summary>
    public string Database { get; } = $"fdwclaim_{Guid.NewGuid():N}"[..28];

    /// <summary>Skips the calling test visibly when no SQL Server was configured.</summary>
    public void RequireDatabase()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            Assert.Skip($"Set {RealMsSqlFixture.ConnectionVariable} to a SQL Server connection string that may create a database to run the durable-claim tests.");
    }

    /// <summary>Opens a connection to the throwaway database.</summary>
    public async Task<SqlConnection> Open()
    {
        RequireDatabase();
        var connection = new SqlConnection(new SqlConnectionStringBuilder(_connectionString) { InitialCatalog = Database, Pooling = false }.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        return connection;
    }

    /// <summary>Runs a query in the throwaway database and returns its rows as dictionaries keyed by column name.</summary>
    public async Task<List<Dictionary<string, object?>>> Rows(string sql)
    {
        await using var connection = await Open();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        return await ReadRows(reader);
    }

    /// <summary>Runs a stored procedure through <see cref="ExecuteProcedureCommand{T}"/> and its translator and returns every result set it answers, each as rows keyed by column name.</summary>
    /// <param name="procedure">The procedure's name in the etl schema.</param>
    /// <param name="parameters">The scalar parameters, null stated.</param>
    /// <returns>The result sets in the order the procedure answers them.</returns>
    public async Task<List<List<Dictionary<string, object?>>>> Execute(string procedure, params (string Name, object? Value)[] parameters)
    {
        var container = TestContainers.Create("etl", procedure, "StoredProcedure");
        var operations = new[] { "Execute" };
        container.Setup(c => c.SupportedOperations).Returns(operations);
        container.As<IStorageContainer>().Setup(c => c.SupportedOperations).Returns(operations);
        var command = new ExecuteProcedureCommand<object> { Parameters = parameters.ToDictionary(p => p.Name, p => p.Value, StringComparer.Ordinal) };

        var translated = await new MsSqlProcedureTranslator().Translate(command, container.Object, TestContext.Current.CancellationToken);
        translated.IsSuccess.ShouldBeTrue($"translation of {procedure} failed: {translated.CurrentMessage}");

        await using var connection = await Open();
        var native = translated.Value!;
        native.Connection = connection;
        await using var reader = await native.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var sets = new List<List<Dictionary<string, object?>>>();
        do
        {
            sets.Add(await ReadRows(reader));
        }
        while (await reader.NextResultAsync(TestContext.Current.CancellationToken));

        return sets;
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return;

        var root = Environment.GetEnvironmentVariable(DatabasesPathVariable);
        if (string.IsNullOrWhiteSpace(root))
            throw new InvalidOperationException($"{RealMsSqlFixture.ConnectionVariable} is set, so the durable-claim tests run, and they need {DatabasesPathVariable} to name a checkout of the databases repository. It is not set.");

        var missing = Files.Where(file => !File.Exists(Path.Combine(root, file))).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException($"{DatabasesPathVariable} is '{root}', and it does not hold: {string.Join(", ", missing)}.");

        await using (var master = new SqlConnection(_connectionString))
        {
            await master.OpenAsync(TestContext.Current.CancellationToken);
            await using var create = new SqlCommand($"CREATE DATABASE [{Database}] COLLATE Latin1_General_100_CI_AS_SC_UTF8", master);
            await create.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        await using var connection = await Open();
        await using (var schema = new SqlCommand("CREATE SCHEMA etl", connection))
            await schema.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        foreach (var file in Files)
        {
            var text = await File.ReadAllTextAsync(Path.Combine(root, file), TestContext.Current.CancellationToken);
            foreach (var batch in Regex.Split(text, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase).Where(b => !string.IsNullOrWhiteSpace(b)))
            {
                await using var run = new SqlCommand(batch, connection);
                await run.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return;

        await using var master = new SqlConnection(_connectionString);
        await master.OpenAsync(TestContext.Current.CancellationToken);
        await using var drop = new SqlCommand(
            $"IF DB_ID(N'{Database}') IS NOT NULL BEGIN ALTER DATABASE [{Database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{Database}]; END",
            master);
        await drop.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<List<Dictionary<string, object?>>> ReadRows(SqlDataReader reader)
    {
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
}
