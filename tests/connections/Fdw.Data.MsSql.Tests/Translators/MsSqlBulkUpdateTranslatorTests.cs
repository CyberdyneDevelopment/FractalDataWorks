using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Commands.Data.Updates;
using Fdw.Data.MsSql.Tests.TestSupport;
using Fdw.Data.MsSql.Translators;
using Fdw.Results;
using Microsoft.Data.SqlClient;

namespace Fdw.Data.MsSql.Tests.Translators;

/// <summary>
/// The SQL Server bulk update (FDW-4953): a whole batch is staged and applied by one UPDATE joined to the stage, never one
/// statement per row.
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlBulkUpdateTranslatorTests
{
    private readonly MsSqlBulkUpdateTranslator _sut = new();

    // Codes are "code_q<n>": 'o', '_' and 'q' are not hex digits, so a code never shows up by chance in the random #fdw_<guid> staging name.
    private static List<Dictionary<string, object?>> Rows(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new Dictionary<string, object?> { ["Code"] = $"code_q{i}", ["Name"] = $"Name {i}", ["Status"] = "open" })
            .ToList();

    private async Task<IGenericResult<SqlCommand>> Run(
        IEnumerable<Dictionary<string, object?>> rows, BulkUpdateOptions options, SoftDeleteOptions? soft = null, params string[] columns)
    {
        var container = TestContainers.Table("Customers", TestContainers.Columns(columns.Length == 0 ? ["Code", "Name", "Status"] : columns));
        var command = new BulkUpdateCommand<Dictionary<string, object?>>(rows, options) { SoftDelete = soft };
        return await _sut.Translate(command, container.Object, TestContext.Current.CancellationToken);
    }

    private static int Count(string sql, string pattern) => Regex.Matches(sql, pattern, RegexOptions.None, TimeSpan.FromSeconds(1)).Count;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheMsSqlTranslatorCollectionAnswersBulkUpdateWithThisTranslator()
    {
        MsSqlDataCommandTranslators.ByName("BulkUpdate").ShouldBeOfType<MsSqlBulkUpdateTranslator>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ABatchIsOneCommandWithOneUpdateJoinedToTheStagedRows()
    {
        // Act
        var result = await Run(Rows(500), new BulkUpdateOptions(["Code"], ["Name"]));

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sql = result.Value!.CommandText;
        Count(sql, @"\bUPDATE t\b").ShouldBe(1);
        sql.ShouldContain("OPENJSON(@fdw_rows)");
        sql.ShouldContain("UPDATE t SET t.[Name] = s.[Name] FROM [dbo].[Customers] AS t INNER JOIN #fdw_");
        sql.ShouldContain("ON t.[Code] = s.[Code]");
        sql.ShouldNotContain("t.[Code] = s.[Code],");
        ((string)result.Value.Parameters["@fdw_rows"].Value).ShouldContain("Name 500");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ValuesAreNeverConcatenatedIntoTheStatement()
    {
        var result = await Run(Rows(2), new BulkUpdateOptions(["Code"], ["Name"]));

        result.Value!.CommandText.ShouldNotContain("Name 1");
        result.Value.CommandText.ShouldNotContain("code_q2");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AKeyTwoRowsShareKeepsTheLastRowAsARowByRowUpdateWould()
    {
        // Arrange
        var rows = new[]
        {
            new Dictionary<string, object?> { ["Code"] = "A", ["Name"] = "first" },
            new Dictionary<string, object?> { ["Code"] = "B", ["Name"] = "other" },
            new Dictionary<string, object?> { ["Code"] = "A", ["Name"] = "last" },
        };

        // Act
        var result = await Run(rows, new BulkUpdateOptions(["Code"], ["Name"]));

        // Assert
        var json = (string)result.Value!.Parameters["@fdw_rows"].Value;
        json.ShouldContain("last");
        json.ShouldContain("other");
        json.ShouldNotContain("first");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheModifyAuditColumnsAreRestampedAsTheSingleRowUpdateDoes()
    {
        var result = await Run(Rows(2), new BulkUpdateOptions(["Code"], ["Name"]), null, "Code", "Name", "ModifyDate");

        result.Value!.CommandText.ShouldContain("t.[ModifyDate] = SYSDATETIMEOFFSET()");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AColumnTheTargetDoesNotDeclareIsRefused()
    {
        var result = await Run(Rows(2), new BulkUpdateOptions(["Code"], ["Missing"]));

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Missing");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AMarkerSoftDeleteFlagsTheMarkedRowsInTheSameStatement()
    {
        // Arrange
        var soft = new SoftDeleteOptions("IsDeleted", SoftDeleteModes.MarkerMatches) { FlagValue = true, Marker = new SoftDeleteMarker("Status", "closed") };

        // Act
        var result = await Run(Rows(3), new BulkUpdateOptions(["Code"], ["Name"]), soft, "Code", "Name", "Status", "IsDeleted");

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sql = result.Value!.CommandText;
        Count(sql, @"\bUPDATE t\b").ShouldBe(1);
        sql.ShouldContain("t.[IsDeleted] = CASE WHEN t.[Status] = @fdw_sd_marker THEN @fdw_sd_flag ELSE t.[IsDeleted] END");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnAbsentKeySoftDeleteIsRefusedOnAnUpdate()
    {
        var soft = new SoftDeleteOptions("IsDeleted", SoftDeleteModes.AbsentFromSource)
        {
            FlagValue = true,
            AbsentFromSource = new AbsentKeySet(["Code"], []),
        };

        var result = await Run(Rows(1), new BulkUpdateOptions(["Code"], ["Name"]), soft, "Code", "Name", "IsDeleted");

        result.IsSuccess.ShouldBeFalse();
    }
}
