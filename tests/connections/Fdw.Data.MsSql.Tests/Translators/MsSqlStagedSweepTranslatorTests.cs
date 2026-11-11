using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data.MsSql.Tests.TestSupport;

namespace Fdw.Data.MsSql.Tests.Translators;

/// <summary>
/// The SQL Server sweep of a staged key set (FDW-4956): one batch counts the load's stage rows, raises a coded error when they are not the rows the load
/// appended, and only then flags the target rows whose keys have no stage row. No row is sent; the keys were staged batch by batch.
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlStagedSweepTranslatorTests
{
    private readonly MsSqlDeleteTranslator _sut = new();

    private static SoftDeleteOptions Sweep(long expected, params string[] stageColumns) =>
        new("IsDeleted", SoftDeleteModes.AbsentFromStage)
        {
            FlagValue = true,
            DeletedAtColumn = "DeletedAt",
            AbsentFromStage = new StagedKeySet(
                TestContainers.Table("CustomersStage", TestContainers.Columns(stageColumns.Length == 0 ? ["Code", "StageKey"] : stageColumns)).Object,
                "StageKey",
                "00000000000000000000000000000001/load",
                ["Code"],
                expected),
        };

    private async Task<Fdw.Results.IGenericResult<Microsoft.Data.SqlClient.SqlCommand>> Run(SoftDeleteOptions options)
    {
        var container = TestContainers.Table("Customers", TestContainers.Columns("Code", "Status", "IsDeleted", "DeletedAt"));
        return await _sut.Translate(new DeleteCommand { SoftDelete = options }, container.Object, TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheSweepCountsTheStageThenFlagsTheTargetRowsWithNoStagedKeyInOneBatch()
    {
        var result = await Run(Sweep(5));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sql = result.Value!.CommandText;
        sql.ShouldContain("DECLARE @fdw_stage_found bigint = (SELECT COUNT_BIG(*) FROM [dbo].[CustomersStage] WHERE [StageKey] = @fdw_stage_key);");
        sql.ShouldContain($"THROW {MsSqlDeleteTranslator.StageRowCountMismatchError}, @fdw_stage_message, 1;");
        sql.ShouldContain("UPDATE t SET t.[IsDeleted] = @fdw_sd_flag, t.[DeletedAt] = SYSDATETIMEOFFSET() FROM [dbo].[Customers] AS t WHERE ");
        sql.ShouldContain("NOT EXISTS (SELECT 1 FROM [dbo].[CustomersStage] AS k WHERE k.[StageKey] = @fdw_stage_key AND k.[Code] = t.[Code])");
        sql.IndexOf("THROW", System.StringComparison.Ordinal).ShouldBeLessThan(sql.IndexOf("UPDATE t", System.StringComparison.Ordinal));
        Regex.Matches(sql, @"\bUPDATE t\b", RegexOptions.None, System.TimeSpan.FromSeconds(1)).Count.ShouldBe(1);
        Regex.Matches(sql, @"\bDELETE\b", RegexOptions.None, System.TimeSpan.FromSeconds(1)).Count.ShouldBe(0);
        sql.ShouldNotContain("OPENJSON");
        result.Value.Parameters["@fdw_stage_key"].Value.ShouldBe("00000000000000000000000000000001/load");
        result.Value.Parameters["@fdw_stage_expected"].Value.ShouldBe(5L);
        result.Value.Parameters.Contains("@fdw_rows").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStageThatDoesNotDeclareTheStageKeyColumnIsRefused()
    {
        var result = await Run(Sweep(5, "Code"));

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("StageKey");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStageThatDoesNotDeclareAKeyColumnIsRefused()
    {
        var result = await Run(Sweep(5, "StageKey"));

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Code");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASweepThatStagedNoRowsIsRefusedSoAnEmptyStageNeverFlagsTheWholeTarget()
    {
        var result = await Run(Sweep(0));

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("at least one");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASweepThatAlsoCarriesASourceSetIsRefused()
    {
        var staged = Sweep(5);
        var both = new SoftDeleteOptions("IsDeleted", SoftDeleteModes.AbsentFromStage)
        {
            FlagValue = true,
            AbsentFromStage = staged.AbsentFromStage,
            AbsentFromSource = new AbsentKeySet(["Code"], [new Dictionary<string, object?> { ["Code"] = "A" }]),
        };

        var result = await Run(both);

        result.IsFailure.ShouldBeTrue();
    }
}
