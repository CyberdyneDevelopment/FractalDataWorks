using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Commands.Data.Updates;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Moq;
using Npgsql;
using NpgsqlTypes;
using Shouldly;
using Xunit;

namespace Fdw.Data.PostgreSql.Tests.Translators;

/// <summary>
/// The PostgreSQL bulk update (FDW-4953): a whole batch travels as one JSON parameter typed by the target's row type and is
/// applied by one UPDATE ... FROM, never one statement per row.
/// </summary>
[Collection(nameof(PostgreSqlTestCollection))]
public sealed class PostgreSqlBulkUpdateTranslatorTests
{
    private readonly PostgreSqlBulkUpdateTranslator _sut = new();

    private static IStorageContainer Container(params string[] columns)
    {
        var fields = columns.Select(name =>
        {
            var field = new Mock<IField>();
            field.Setup(f => f.Name).Returns(name);
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
        Enumerable.Range(1, count)
            .Select(i => new Dictionary<string, object?> { ["code"] = $"C{i}", ["name"] = $"Name {i}" })
            .ToList();

    private Task<IGenericResult<NpgsqlCommand>> Run(IEnumerable<Dictionary<string, object?>> rows, BulkUpdateOptions options, SoftDeleteOptions? soft = null)
        => _sut.Translate(
            new BulkUpdateCommand<Dictionary<string, object?>>(rows, options) { SoftDelete = soft },
            Container("code", "name", "is_deleted", "status"),
            TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ThePostgreSqlTranslatorCollectionAnswersBulkUpdateWithThisTranslator()
    {
        PostgreSqlDataCommandTranslators.ByName("BulkUpdate").ShouldBeOfType<PostgreSqlBulkUpdateTranslator>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ABatchIsOneUpdateFromTheRowsTypedByTheTargetsRowType()
    {
        // Act
        var result = await Run(Rows(500), new BulkUpdateOptions(["Code"], ["Name"]));

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sql = result.Value!.CommandText;
        Regex.Matches(sql, @"\bUPDATE\b", RegexOptions.None, TimeSpan.FromSeconds(1)).Count.ShouldBe(1);
        sql.ShouldBe("UPDATE \"public\".\"customers\" AS t SET \"name\" = s.\"name\" "
            + "FROM json_populate_recordset(NULL::\"public\".\"customers\", @fdw_rows) AS s WHERE t.\"code\" = s.\"code\"");
        result.Value.Parameters.Count.ShouldBe(1);
        result.Value.Parameters[0].NpgsqlDbType.ShouldBe(NpgsqlDbType.Json);
        ((string)result.Value.Parameters[0].Value!).ShouldContain("\"name\":\"Name 500\"");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AKeyTwoRowsShareKeepsTheLastRow()
    {
        var rows = new[]
        {
            new Dictionary<string, object?> { ["code"] = "A", ["name"] = "first" },
            new Dictionary<string, object?> { ["code"] = "A", ["name"] = "last" },
        };

        var json = (string)(await Run(rows, new BulkUpdateOptions(["code"], ["name"]))).Value!.Parameters[0].Value!;

        json.ShouldContain("last");
        json.ShouldNotContain("first");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AColumnTheTargetDoesNotDeclareIsRefused()
    {
        var result = await Run(Rows(1), new BulkUpdateOptions(["code"], ["missing"]));

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("missing");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASoftDeleteIsRefusedAsTheSingleRowUpdateRefusesIt()
    {
        var soft = new SoftDeleteOptions("is_deleted", SoftDeleteModes.MarkerMatches) { FlagValue = true, Marker = new SoftDeleteMarker("status", "closed") };

        var result = await Run(Rows(1), new BulkUpdateOptions(["code"], ["name"]), soft);

        result.IsSuccess.ShouldBeFalse();
    }
}
