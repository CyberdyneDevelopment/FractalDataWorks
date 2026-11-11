using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Deletes;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data.MsSql.Tests.TestSupport;
using Fdw.Data.MsSql.Translators;
using Fdw.Results;
using Microsoft.Data.SqlClient;

namespace Fdw.Data.MsSql.Tests.Translators;

/// <summary>
/// The SQL Server bulk delete (FDW-4954): a whole batch's keys are staged and one DELETE (or soft-delete UPDATE) joined to them
/// removes the matched rows, never one statement per row.
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlBulkDeleteTranslatorTests
{
    private readonly MsSqlBulkDeleteTranslator _sut = new();

    // Codes are "code_q<n>": 'o', '_' and 'q' are not hex digits, so a code never shows up by chance in the random #fdw_<guid> staging name.
    private static List<Dictionary<string, object?>> Keys(int count) =>
        Enumerable.Range(1, count).Select(i => new Dictionary<string, object?> { ["Code"] = $"code_q{i}" }).ToList();

    private async Task<IGenericResult<SqlCommand>> Run(IEnumerable<Dictionary<string, object?>> rows, BulkDeleteOptions options, SoftDeleteOptions? soft = null)
    {
        var container = TestContainers.Table("Customers", TestContainers.Columns("Code", "Name", "Status", "IsDeleted"));
        var command = new BulkDeleteCommand<Dictionary<string, object?>>(rows, options) { SoftDelete = soft };
        return await _sut.Translate(command, container.Object, TestContext.Current.CancellationToken);
    }

    private static int Count(string sql, string pattern) => Regex.Matches(sql, pattern, RegexOptions.None, TimeSpan.FromSeconds(1)).Count;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheMsSqlTranslatorCollectionAnswersBulkDeleteWithThisTranslator()
    {
        MsSqlDataCommandTranslators.ByName("BulkDelete").ShouldBeOfType<MsSqlBulkDeleteTranslator>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ABatchIsOneDeleteJoinedToTheStagedKeys()
    {
        // Act
        var result = await Run(Keys(500), new BulkDeleteOptions(["Code"]));

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sql = result.Value!.CommandText;
        Count(sql, @"\bDELETE t\b").ShouldBe(1);
        sql.ShouldContain("OPENJSON(@fdw_rows)");
        sql.ShouldContain("DELETE t FROM [dbo].[Customers] AS t INNER JOIN #fdw_");
        sql.ShouldContain("ON t.[Code] = s.[Code]");
        sql.ShouldNotContain("code_q500");
        ((string)result.Value.Parameters["@fdw_rows"].Value).ShouldContain("code_q500");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AMarkerSoftDeleteFlagsTheMatchedMarkedRowsInOneUpdate()
    {
        // Arrange
        var soft = new SoftDeleteOptions("IsDeleted", SoftDeleteModes.MarkerMatches) { FlagValue = true, Marker = new SoftDeleteMarker("Status", "closed") };

        // Act
        var result = await Run(Keys(3), new BulkDeleteOptions(["Code"]), soft);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sql = result.Value!.CommandText;
        Count(sql, @"\bDELETE\b").ShouldBe(0); // the flag column is IsDeleted, so not a substring check
        Count(sql, @"\bUPDATE t\b").ShouldBe(1);
        sql.ShouldContain("UPDATE t SET t.[IsDeleted] = @fdw_sd_flag FROM [dbo].[Customers] AS t INNER JOIN #fdw_");
        sql.ShouldContain("WHERE t.[Status] = @fdw_sd_marker AND (t.[IsDeleted] IS NULL OR t.[IsDeleted] <> @fdw_sd_flag)");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnAbsentKeySoftDeleteIsRefused()
    {
        var soft = new SoftDeleteOptions("IsDeleted", SoftDeleteModes.AbsentFromSource)
        {
            FlagValue = true,
            AbsentFromSource = new AbsentKeySet(["Code"], []),
        };

        var result = await Run(Keys(1), new BulkDeleteOptions(["Code"]), soft);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AKeyTheTargetDoesNotDeclareIsRefused()
    {
        var result = await Run(Keys(1), new BulkDeleteOptions(["Missing"]));

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Missing");
    }
}
