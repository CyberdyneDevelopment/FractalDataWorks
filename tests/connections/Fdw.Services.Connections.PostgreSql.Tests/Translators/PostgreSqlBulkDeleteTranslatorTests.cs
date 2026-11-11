using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Deletes;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Moq;
using Npgsql;
using NpgsqlTypes;
using Shouldly;
using Xunit;

namespace Fdw.Data.PostgreSql.Tests.Translators;

/// <summary>
/// The PostgreSQL bulk delete (FDW-4954): a whole batch's keys travel as one JSON parameter and one DELETE ... USING removes the
/// matched rows, never one statement per row.
/// </summary>
[Collection(nameof(PostgreSqlTestCollection))]
public sealed class PostgreSqlBulkDeleteTranslatorTests
{
    private readonly PostgreSqlBulkDeleteTranslator _sut = new();

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

    private static List<Dictionary<string, object?>> Keys(int count) =>
        Enumerable.Range(1, count).Select(i => new Dictionary<string, object?> { ["code"] = $"C{i}" }).ToList();

    private Task<IGenericResult<NpgsqlCommand>> Run(IEnumerable<Dictionary<string, object?>> rows, BulkDeleteOptions options, SoftDeleteOptions? soft = null)
        => _sut.Translate(
            new BulkDeleteCommand<Dictionary<string, object?>>(rows, options) { SoftDelete = soft },
            Container("code", "name", "is_deleted", "status"),
            TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ThePostgreSqlTranslatorCollectionAnswersBulkDeleteWithThisTranslator()
    {
        PostgreSqlDataCommandTranslators.ByName("BulkDelete").ShouldBeOfType<PostgreSqlBulkDeleteTranslator>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ABatchIsOneDeleteUsingTheKeysTypedByTheTargetsRowType()
    {
        // Act
        var result = await Run(Keys(500), new BulkDeleteOptions(["Code"]));

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.CommandText.ShouldBe("DELETE FROM \"public\".\"customers\" AS t "
            + "USING json_populate_recordset(NULL::\"public\".\"customers\", @fdw_rows) AS s WHERE t.\"code\" = s.\"code\"");
        result.Value.Parameters.Count.ShouldBe(1);
        result.Value.Parameters[0].NpgsqlDbType.ShouldBe(NpgsqlDbType.Json);
        ((string)result.Value.Parameters[0].Value!).ShouldContain("\"code\":\"C500\"");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AMarkerSoftDeleteFlagsTheMatchedRowsInOneUpdate()
    {
        // Arrange
        var soft = new SoftDeleteOptions("is_deleted", SoftDeleteModes.MarkerMatches) { FlagValue = true, Marker = new SoftDeleteMarker("status", "closed") };

        // Act
        var result = await Run(Keys(3), new BulkDeleteOptions(["code"]), soft);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sql = result.Value!.CommandText;
        Regex.Matches(sql, @"\bUPDATE\b", RegexOptions.None, TimeSpan.FromSeconds(1)).Count.ShouldBe(1);
        sql.ShouldStartWith("UPDATE \"public\".\"customers\" AS t SET \"is_deleted\" = @fdw_sd_flag WHERE (EXISTS (SELECT 1 FROM json_populate_recordset(");
        sql.ShouldContain("\"status\" = @fdw_sd_match0");
        Regex.Matches(sql, @"\bDELETE\b", RegexOptions.None, TimeSpan.FromSeconds(1)).Count.ShouldBe(0); // the flag column is is_deleted, so not a substring check
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AKeyTheTargetDoesNotDeclareIsRefused()
    {
        var result = await Run(Keys(1), new BulkDeleteOptions(["missing"]));

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("missing");
    }
}
