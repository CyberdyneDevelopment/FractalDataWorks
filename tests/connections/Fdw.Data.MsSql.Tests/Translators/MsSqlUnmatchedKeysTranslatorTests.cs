using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Deletes;
using Fdw.Commands.Data.Updates;
using Fdw.Data.MsSql.Tests.TestSupport;
using Fdw.Data.MsSql.Translators;

namespace Fdw.Data.MsSql.Tests.Translators;

/// <summary>
/// A SQL Server bulk update or delete that reports its unmatched keys (FDW-4988) counts the staged keys with no target row and keeps the first of them before
/// the write, in the same batch, then selects the summary and the keys; one that does not report is translated as before.
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlUnmatchedKeysTranslatorTests
{
    private static List<Dictionary<string, object?>> Rows(int count) =>
        [.. Enumerable.Range(1, count).Select(i => new Dictionary<string, object?> { ["Code"] = $"C{i}", ["Name"] = $"Name {i}" })];

    private static Fdw.Data.Abstractions.IStorageContainer Customers() => TestContainers.Table("Customers", TestContainers.Columns("Code", "Name")).Object;

    private static void ShouldReport(string sql, string write)
    {
        sql.ShouldContain("DECLARE @fdw_unmatched bigint = (SELECT COUNT_BIG(*) FROM #fdw_");
        sql.ShouldContain("NOT EXISTS (SELECT 1 FROM [dbo].[Customers] AS t WITH (UPDLOCK, HOLDLOCK) WHERE t.[Code] = s.[Code])");
        sql.ShouldContain("SELECT TOP (@fdw_unmatched_sample) s.[Code] INTO #fdw_");
        sql.ShouldContain("DECLARE @fdw_affected bigint = @@ROWCOUNT; SELECT @fdw_affected AS fdw_affected, @fdw_unmatched AS fdw_unmatched; SELECT [Code] FROM #fdw_");
        sql.IndexOf("COUNT_BIG", System.StringComparison.Ordinal).ShouldBeLessThan(sql.IndexOf(write, System.StringComparison.Ordinal));
        sql.IndexOf(write, System.StringComparison.Ordinal).ShouldBeLessThan(sql.IndexOf("@@ROWCOUNT", System.StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ABulkUpdateThatReportsCountsAndSamplesBeforeItWrites()
    {
        var command = new BulkUpdateCommand<Dictionary<string, object?>>(Rows(3), new BulkUpdateOptions(["Code"], ["Name"])) { UnmatchedKeySample = 5 };

        var result = await new MsSqlBulkUpdateTranslator().Translate(command, Customers(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        ShouldReport(result.Value!.CommandText, "UPDATE t SET");
        result.Value.Parameters["@fdw_unmatched_sample"].Value.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ABulkDeleteThatReportsCountsBeforeItDeletesInATransactionOfItsOwn()
    {
        var command = new BulkDeleteCommand<Dictionary<string, object?>>(Rows(3), new BulkDeleteOptions(["Code"])) { UnmatchedKeySample = 0 };

        var result = await new MsSqlBulkDeleteTranslator().Translate(command, TestContainers.Table("Customers", TestContainers.Columns("Code")).Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        ShouldReport(result.Value!.CommandText, "DELETE t");
        result.Value.CommandText.ShouldContain("IF @fdw_own = 1 BEGIN TRANSACTION;");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ACommandThatDoesNotReportIsTranslatedAsBefore()
    {
        var command = new BulkDeleteCommand<Dictionary<string, object?>>(Rows(3), new BulkDeleteOptions(["Code"]));

        var result = await new MsSqlBulkDeleteTranslator().Translate(command, TestContainers.Table("Customers", TestContainers.Columns("Code")).Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.CommandText.ShouldNotContain("fdw_unmatched");
        result.Value.CommandText.ShouldNotContain("BEGIN TRANSACTION");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ANegativeSampleSizeIsRefused()
    {
        var command = new BulkUpdateCommand<Dictionary<string, object?>>(Rows(1), new BulkUpdateOptions(["Code"], ["Name"])) { UnmatchedKeySample = -1 };

        var result = await new MsSqlBulkUpdateTranslator().Translate(command, Customers(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("at least 0");
    }
}
