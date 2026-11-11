using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Deletes;
using Fdw.Commands.Data.Updates;
using Fdw.Commands.Data.Upserts;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.Tests.TestSupport;
using Fdw.Data.MsSql.Translators;

namespace Fdw.Data.MsSql.Tests.Translators;

/// <summary>
/// A large SQL Server set-based write is staged by bulk copy (FDW-4958): the temporary table is created like the target, the rows are copied into
/// it typed, it is indexed on the keys, and the same set-based statement applies them. No JSON is sent.
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlStagedWriteTests
{
    private static List<Dictionary<string, object?>> Rows(int count) =>
        Enumerable.Range(1, count).Select(i => new Dictionary<string, object?> { ["Code"] = $"C{i}", ["Name"] = $"Name {i}" }).ToList();

    private static Mock<IDataContainer> Customers() => TestContainers.Table("Customers", TestContainers.Columns("Code", "Name"));

    private static void ShouldBeStaged(MsSqlStagedWrite staged, int rows, params string[] columns)
    {
        staged.CreateSql.ShouldStartWith($"SET NOCOUNT ON; SELECT TOP 0 {string.Join(", ", columns.Select(c => $"[{c}]"))} INTO #fdw_");
        staged.CreateSql.ShouldEndWith(" FROM [dbo].[Customers];");
        staged.Copy.Destination.ShouldStartWith("#fdw_");
        staged.CreateSql.ShouldContain(staged.Copy.Destination);
        staged.Copy.Columns.ShouldBe(columns);
        staged.Copy.Rows.Rows.Count.ShouldBe(rows);
        staged.Statement.CommandText.ShouldContain($"CREATE CLUSTERED INDEX fdw_keys ON {staged.Copy.Destination} ([Code])");
        staged.Statement.CommandText.ShouldContain($"DROP TABLE {staged.Copy.Destination}");
        staged.Statement.CommandText.ShouldNotContain("OPENJSON");
        staged.Statement.Parameters.Contains("@fdw_rows").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheSetBasedTranslatorsCanStage()
    {
        MsSqlDataCommandTranslators.ByName("BulkUpdate").ShouldBeAssignableTo<IMsSqlStagingTranslator>();
        MsSqlDataCommandTranslators.ByName("BulkDelete").ShouldBeAssignableTo<IMsSqlStagingTranslator>();
        MsSqlDataCommandTranslators.ByName("BulkUpsert").ShouldBeAssignableTo<IMsSqlStagingTranslator>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ABulkUpdateStagesItsRowsAndUpdatesFromTheIndexedTable()
    {
        var staged = new MsSqlBulkUpdateTranslator().Stage(
            new BulkUpdateCommand<Dictionary<string, object?>>(Rows(5000), new BulkUpdateOptions(["Code"], ["Name"])), Customers().Object);

        staged.IsSuccess.ShouldBeTrue(staged.CurrentMessage);
        ShouldBeStaged(staged.Value!, 5000, "Code", "Name");
        staged.Value!.Statement.CommandText.ShouldContain($"UPDATE t SET t.[Name] = s.[Name] FROM [dbo].[Customers] AS t INNER JOIN {staged.Value.Copy.Destination} AS s ON t.[Code] = s.[Code]");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ABulkDeleteStagesItsKeysAndDeletesFromTheIndexedTable()
    {
        var keys = Rows(5000).Select(row => new Dictionary<string, object?> { ["Code"] = row["Code"] }).ToList();

        var staged = new MsSqlBulkDeleteTranslator().Stage(new BulkDeleteCommand<Dictionary<string, object?>>(keys, new BulkDeleteOptions(["Code"])), Customers().Object);

        staged.IsSuccess.ShouldBeTrue(staged.CurrentMessage);
        ShouldBeStaged(staged.Value!, 5000, "Code");
        staged.Value!.Statement.CommandText.ShouldContain($"DELETE t FROM [dbo].[Customers] AS t INNER JOIN {staged.Value.Copy.Destination} AS s ON t.[Code] = s.[Code]");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnUpsertStagesItsRowsThenUpdatesAndInsertsFromTheIndexedTable()
    {
        var staged = new MsSqlBulkUpsertTranslator().Stage(
            new BulkUpsertCommand<Dictionary<string, object?>>(Rows(5000), new UpsertOptions(["Code"], ["Name"], ["Code", "Name"])), Customers().Object);

        staged.IsSuccess.ShouldBeTrue(staged.CurrentMessage);
        ShouldBeStaged(staged.Value!, 5000, "Code", "Name");
        staged.Value!.Statement.CommandText.ShouldContain("UPDATE t SET t.[Name] = s.[Name] FROM [dbo].[Customers] AS t WITH (UPDLOCK, HOLDLOCK) INNER JOIN");
        staged.Value.Statement.CommandText.ShouldContain("INSERT INTO [dbo].[Customers] ([Code], [Name]) SELECT s.[Code], s.[Name]");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AStagedWriteIsRefusedForTheSameReasonsAsTheInlineOne()
    {
        var staged = new MsSqlBulkUpdateTranslator().Stage(
            new BulkUpdateCommand<Dictionary<string, object?>>(Rows(10), new BulkUpdateOptions(["Code"], ["Missing"])), Customers().Object);

        staged.IsSuccess.ShouldBeFalse();
        staged.CurrentMessage.ShouldNotBeNull().ShouldContain("Missing");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async System.Threading.Tasks.Task TheInlineFormOfASmallBatchStillSendsOneJsonParameter()
    {
        var translated = await new MsSqlBulkUpdateTranslator().Translate(
            new BulkUpdateCommand<Dictionary<string, object?>>(Rows(3), new BulkUpdateOptions(["Code"], ["Name"])), Customers().Object, TestContext.Current.CancellationToken);

        translated.IsSuccess.ShouldBeTrue(translated.CurrentMessage);
        translated.Value!.CommandText.ShouldContain("OPENJSON(@fdw_rows)");
        translated.Value.CommandText.ShouldNotContain("CREATE CLUSTERED INDEX");
    }
}
