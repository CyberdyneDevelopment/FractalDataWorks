using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Upserts;
using Fdw.Commands.Data.ValueSources;
using Fdw.Data.Abstractions;
using Fdw.Data.PostgreSql.Translators;
using Fdw.Results;
using Moq;
using Npgsql;
using NpgsqlTypes;
using Shouldly;
using Xunit;

namespace Fdw.Data.PostgreSql.Tests.Translators;

/// <summary>
/// The PostgreSQL upsert (FDW-4955): a whole batch is one command, an update of the changed rows then an insert of the new keys,
/// never a delete and an insert per row.
/// </summary>
[Collection(nameof(PostgreSqlTestCollection))]
public sealed class PostgreSqlUpsertTranslatorTests
{
    private const string Target = "\"public\".\"customers\"";

    private static IStorageContainer Container()
    {
        var fields = new[] { "code", "name", "valid_from", "valid_to", "is_current", "status", "row_id" }.Select(name =>
        {
            var field = new Mock<IField>();
            field.Setup(f => f.Name).Returns(name);
            field.Setup(f => f.IsIdentity).Returns(string.Equals(name, "row_id", StringComparison.Ordinal));
            return field.Object;
        }).ToArray();
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(fields);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("customers");
        container.Setup(c => c.Path).Returns(new PostgreSqlDatabasePath(null, "public", "customers"));
        container.Setup(c => c.Schema).Returns(schema.Object);
        container.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        return container.Object;
    }

    private static List<Dictionary<string, object?>> Rows(int count) =>
        Enumerable.Range(1, count).Select(i => new Dictionary<string, object?> { ["code"] = $"C{i}", ["name"] = $"Name {i}" }).ToList();

    private static Task<IGenericResult<NpgsqlCommand>> Run(UpsertOptions options, int rows = 3)
        => new PostgreSqlBulkUpsertTranslator().Translate(
            new BulkUpsertCommand<Dictionary<string, object?>>(Rows(rows), options), Container(), TestContext.Current.CancellationToken);

    private static int Count(string sql, string word) => Regex.Matches(sql, $@"\b{word}\b", RegexOptions.None, TimeSpan.FromSeconds(1)).Count;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ThePostgreSqlTranslatorCollectionAnswersBothUpsertsWithASetBasedTranslator()
    {
        PostgreSqlDataCommandTranslators.ByName("Upsert").ShouldBeOfType<PostgreSqlUpsertTranslator>();
        PostgreSqlDataCommandTranslators.ByName("BulkUpsert").ShouldBeOfType<PostgreSqlBulkUpsertTranslator>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ABatchIsOneCommandUpdatingTheChangedRowsThenInsertingTheNewKeys()
    {
        // Act
        var result = await Run(new UpsertOptions(["code"], ["name"], ["code", "name"]), rows: 500);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sql = result.Value!.CommandText;
        Count(sql, "UPDATE").ShouldBe(1);
        Count(sql, "INSERT").ShouldBe(1);
        Count(sql, "DELETE").ShouldBe(0);
        sql.ShouldBe(
            $"LOCK TABLE {Target} IN SHARE ROW EXCLUSIVE MODE; "
            + $"UPDATE {Target} AS t SET \"name\" = s.\"name\" FROM json_populate_recordset(NULL::{Target}, @fdw_rows) AS s "
            + "WHERE t.\"code\" = s.\"code\" AND (t.\"name\" IS DISTINCT FROM s.\"name\"); "
            + $"INSERT INTO {Target} (\"code\", \"name\") SELECT s.\"code\", s.\"name\" FROM json_populate_recordset(NULL::{Target}, @fdw_rows) AS s "
            + $"WHERE NOT EXISTS (SELECT 1 FROM {Target} AS e WHERE e.\"code\" = s.\"code\")");
        result.Value.Parameters.Single().NpgsqlDbType.ShouldBe(NpgsqlDbType.Json);
        ((string)result.Value.Parameters[0].Value!).ShouldContain("Name 500");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AVersionedUpsertClosesTheChangedVersionAndInsertsTheNewOneAtOneInstant()
    {
        // Arrange
        var options = new UpsertOptions(["code"], ["name"], ["code", "name"])
        {
            Versioning = new VersioningOptions("valid_from", "valid_to") { CurrentFlagColumn = "is_current", InsertOnlyRule = new InsertOnlyRule("status", "locked") },
        };

        // Act
        var result = await Run(options);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sql = result.Value!.CommandText;
        sql.ShouldStartWith($"LOCK TABLE {Target} IN SHARE ROW EXCLUSIVE MODE; UPDATE {Target} AS t SET \"valid_to\" = now(), \"is_current\" = @fdw_closed FROM ");
        sql.ShouldContain("AND t.\"valid_to\" IS NULL AND (t.\"name\" IS DISTINCT FROM s.\"name\") AND (t.\"status\" IS NULL OR t.\"status\" <> @fdw_marker)");
        sql.ShouldContain($"INSERT INTO {Target} (\"code\", \"name\", \"valid_from\", \"valid_to\", \"is_current\") SELECT s.\"code\", s.\"name\", now(), NULL, @fdw_current FROM ");
        sql.ShouldContain("AND e.\"valid_to\" IS NULL)");
        result.Value.Parameters.Select(p => p.ParameterName).OrderBy(n => n, StringComparer.Ordinal)
            .ShouldBe(new[] { "fdw_closed", "fdw_current", "fdw_marker", "fdw_rows" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnIdentityWriteColumnIsNeverWritten()
    {
        var result = await Run(new UpsertOptions(["code"], [], ["code", "name", "row_id"]));

        result.Value!.CommandText.ShouldNotContain("\"row_id\" = s.");
        result.Value.CommandText.ShouldNotContain("s.\"row_id\"");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AValueTheDatabaseSuppliesIsRefused()
    {
        var options = new UpsertOptions(["code"], [], ["code", "name"])
        {
            ValueSources = [new ColumnValueBinding("valid_from", new CurrentTimestampValueSource())],
        };

        var result = await Run(options);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("valid_from");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AColumnTheTargetDoesNotDeclareIsRefused()
    {
        var result = await Run(new UpsertOptions(["code"], ["missing"], ["code", "missing"]));

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("missing");
    }
}
