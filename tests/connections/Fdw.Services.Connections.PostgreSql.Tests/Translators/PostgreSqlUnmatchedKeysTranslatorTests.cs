using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Deletes;
using Fdw.Commands.Data.Updates;
using Fdw.Data.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.PostgreSql.Tests.Translators;

/// <summary>
/// A PostgreSQL bulk update or delete that reports its unmatched keys (FDW-4988) locks its target, keeps the first unmatched keys, writes in a CTE that counts
/// them from the same snapshot, and selects the kept keys; one that does not report is translated as before.
/// </summary>
[Collection(nameof(PostgreSqlTestCollection))]
public sealed class PostgreSqlUnmatchedKeysTranslatorTests
{
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
        [.. Enumerable.Range(1, count).Select(i => new Dictionary<string, object?> { ["code"] = $"C{i}", ["name"] = $"Name {i}" })];

    private static void ShouldReport(string sql, string write)
    {
        sql.ShouldStartWith("LOCK TABLE \"public\".\"customers\" IN SHARE ROW EXCLUSIVE MODE; CREATE TEMP TABLE fdw_um_");
        sql.ShouldContain("LIMIT @fdw_unmatched_sample;");
        sql.ShouldContain($"WITH w AS ({write}");
        sql.ShouldContain("RETURNING 1) SELECT (SELECT count(*) FROM w) AS fdw_affected, (SELECT count(*) FROM json_populate_recordset(");
        sql.ShouldContain("NOT EXISTS (SELECT 1 FROM \"public\".\"customers\" AS t WHERE t.\"code\" = s.\"code\")) AS fdw_unmatched;");
        sql.ShouldContain("SELECT \"code\" FROM fdw_um_");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ABulkUpdateThatReportsCountsFromTheSnapshotItWritesIn()
    {
        var command = new BulkUpdateCommand<Dictionary<string, object?>>(Rows(3), new BulkUpdateOptions(["code"], ["name"])) { UnmatchedKeySample = 5 };

        var result = await new PostgreSqlBulkUpdateTranslator().Translate(command, Container("code", "name"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        ShouldReport(result.Value!.CommandText, "UPDATE \"public\".\"customers\" AS t SET");
        result.Value.Parameters["fdw_unmatched_sample"].Value.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ABulkDeleteThatReportsKeepsItsSampleBeforeItDeletes()
    {
        var command = new BulkDeleteCommand<Dictionary<string, object?>>(Rows(3), new BulkDeleteOptions(["code"])) { UnmatchedKeySample = 2 };

        var result = await new PostgreSqlBulkDeleteTranslator().Translate(command, Container("code"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        ShouldReport(result.Value!.CommandText, "DELETE FROM \"public\".\"customers\" AS t USING");
        result.Value.CommandText.IndexOf("CREATE TEMP TABLE", System.StringComparison.Ordinal)
            .ShouldBeLessThan(result.Value.CommandText.IndexOf("DELETE FROM", System.StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ACommandThatDoesNotReportIsTranslatedAsBefore()
    {
        var command = new BulkUpdateCommand<Dictionary<string, object?>>(Rows(3), new BulkUpdateOptions(["code"], ["name"]));

        var result = await new PostgreSqlBulkUpdateTranslator().Translate(command, Container("code", "name"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.CommandText.ShouldStartWith("UPDATE \"public\".\"customers\" AS t SET");
        result.Value.CommandText.ShouldNotContain("fdw_unmatched");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ANegativeSampleSizeIsRefused()
    {
        var command = new BulkDeleteCommand<Dictionary<string, object?>>(Rows(1), new BulkDeleteOptions(["code"])) { UnmatchedKeySample = -3 };

        var result = await new PostgreSqlBulkDeleteTranslator().Translate(command, Container("code"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }
}
