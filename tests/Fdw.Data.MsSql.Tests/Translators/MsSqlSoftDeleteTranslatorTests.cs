using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data.MsSql.Tests.TestSupport;

namespace Fdw.Data.MsSql.Tests.Translators;

[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlSoftDeleteTranslatorTests
{
    private readonly MsSqlDeleteTranslator _sut = new();

    private static readonly TestContainers.Column[] Columns = TestContainers.Columns("Code", "Status", "IsDeleted", "DeletedAt");

    private async Task<Fdw.Results.IGenericResult<Microsoft.Data.SqlClient.SqlCommand>> Run(SoftDeleteOptions options)
    {
        var container = TestContainers.Table("Customers", Columns);
        return await _sut.Translate(new DeleteCommand { SoftDelete = options }, container.Object, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MarkerMatchesFlagsRatherThanDeletes()
    {
        var options = new SoftDeleteOptions("IsDeleted", SoftDeleteModes.MarkerMatches)
        {
            DeletedAtColumn = "DeletedAt",
            Marker = new SoftDeleteMarker("Status", "gone"),
        };

        var result = await Run(options);

        result.IsSuccess.ShouldBeTrue();
        var sql = result.Value!.CommandText;
        sql.ShouldNotContain("DELETE FROM");
        sql.ShouldContain("UPDATE t SET t.[IsDeleted] = @fdw_sd_flag, t.[DeletedAt] = SYSDATETIMEOFFSET()");
        sql.ShouldContain("t.[Status] = @fdw_sd_marker");
        sql.ShouldContain("(t.[IsDeleted] IS NULL OR t.[IsDeleted] <> @fdw_sd_flag)");
        result.Value.Parameters["@fdw_sd_marker"].Value.ShouldBe("gone");
        result.Value.Parameters["@fdw_sd_flag"].Value.ShouldBe(true);
    }

    [Fact]
    public async Task FilterMatchesFlagsWithoutAMarkerOrAStagedSet()
    {
        var options = new SoftDeleteOptions("IsDeleted", SoftDeleteModes.FilterMatches) { DeletedAtColumn = "DeletedAt" };

        var result = await Run(options);

        result.IsSuccess.ShouldBeTrue();
        var sql = result.Value!.CommandText;
        sql.ShouldNotContain("DELETE FROM");
        sql.ShouldContain("UPDATE t SET t.[IsDeleted] = @fdw_sd_flag, t.[DeletedAt] = SYSDATETIMEOFFSET()");
        sql.ShouldNotContain("@fdw_sd_marker");
        sql.ShouldNotContain("OPENJSON");
        sql.ShouldContain("(t.[IsDeleted] IS NULL OR t.[IsDeleted] <> @fdw_sd_flag)");
    }

    [Fact]
    public async Task AFalseFlagValueClosesTheRowsInsteadOfFlaggingThem()
    {
        var options = new SoftDeleteOptions("IsDeleted", SoftDeleteModes.FilterMatches) { FlagValue = false };

        var result = await Run(options);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Parameters["@fdw_sd_flag"].Value.ShouldBe(false);
        result.Value.CommandText.ShouldContain("(t.[IsDeleted] IS NULL OR t.[IsDeleted] <> @fdw_sd_flag)");
    }

    [Fact]
    public async Task AbsentFromSourceStagesTheKeysAndFlagsTheOthers()
    {
        var keys = new List<IReadOnlyDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Code"] = "A" },
        };
        var options = new SoftDeleteOptions("IsDeleted", SoftDeleteModes.AbsentFromSource)
        {
            AbsentFromSource = new AbsentKeySet(["Code"], keys),
        };

        var result = await Run(options);

        result.IsSuccess.ShouldBeTrue();
        var sql = result.Value!.CommandText;
        sql.ShouldContain("OPENJSON(@fdw_rows)");
        sql.ShouldContain("NOT EXISTS (SELECT 1 FROM #fdw_");
        sql.ShouldNotContain("DELETE FROM");
    }

    [Fact]
    public async Task MarkerModeWithASourceSetIsRefused()
    {
        var options = new SoftDeleteOptions("IsDeleted", SoftDeleteModes.MarkerMatches)
        {
            Marker = new SoftDeleteMarker("Status", "gone"),
            AbsentFromSource = new AbsentKeySet(["Code"], new List<IReadOnlyDictionary<string, object?>>()),
        };

        var result = await Run(options);

        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("InvalidSoftDeleteOptions");
    }

    [Fact]
    public async Task UnknownFlagColumnIsRefused()
    {
        var options = new SoftDeleteOptions("NoSuchFlag", SoftDeleteModes.MarkerMatches)
        {
            Marker = new SoftDeleteMarker("Status", "gone"),
        };

        var result = await Run(options);

        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("InvalidSoftDeleteOptions");
    }

    [Fact]
    public async Task DeleteWithoutSoftDeleteOrFilterDeletesEveryRow()
    {
        var container = TestContainers.Table("Customers", Columns);

        var result = await _sut.Translate(new DeleteCommand(), container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldStartWith("DELETE FROM");
        result.Value.CommandText.ShouldNotContain("WHERE");
    }
}
