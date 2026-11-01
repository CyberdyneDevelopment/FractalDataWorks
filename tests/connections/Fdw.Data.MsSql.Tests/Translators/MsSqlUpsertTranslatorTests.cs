using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Upserts;
using Fdw.Commands.Data.ValueSources;
using Fdw.Data.MsSql.Tests.TestSupport;
using Fdw.Data.MsSql.Translators;
using Fdw.Results;

namespace Fdw.Data.MsSql.Tests.Translators;

[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlUpsertTranslatorTests
{
    private readonly MsSqlUpsertTranslator _sut = new();

    private static List<Dictionary<string, object?>> Rows() =>
    [
        new() { ["Code"] = "A", ["Name"] = "Alpha" },
        new() { ["Code"] = "B", ["Name"] = "Beta" },
    ];

    private static UpsertOptions Plain() => new(["Code"], ["Name"], ["Code", "Name"]);

    private async Task<IGenericResult<Microsoft.Data.SqlClient.SqlCommand>> Run(UpsertOptions options, params string[] columns)
    {
        var container = TestContainers.Table("Customers", TestContainers.Columns(columns));
        return await _sut.Translate(new UpsertCommand<Dictionary<string, object?>>(Rows(), options), container.Object, TestContext.Current.CancellationToken);
    }

    [Fact]
    public void NameIsUpsert() => _sut.Name.ShouldBe("Upsert");

    [Fact]
    public async Task StagesRowsAsOneJsonParameterAndUpdatesThenInserts()
    {
        var result = await Run(Plain(), "Code", "Name");

        result.IsSuccess.ShouldBeTrue();
        var sql = result.Value!.CommandText;
        sql.ShouldContain("OPENJSON(@fdw_rows)");
        sql.ShouldContain("UPDATE t SET t.[Name] = s.[Name] FROM [dbo].[Customers] AS t");
        sql.ShouldContain("INSERT INTO [dbo].[Customers] ([Code], [Name]) SELECT s.[Code], s.[Name]");
        sql.ShouldContain("WHERE NOT EXISTS");
        ((string)result.Value.Parameters["@fdw_rows"].Value).ShouldContain("Alpha");
    }

    [Fact]
    public async Task ValuesAreNeverConcatenatedIntoTheStatement()
    {
        var result = await Run(Plain(), "Code", "Name");

        result.Value!.CommandText.ShouldNotContain("Alpha");
        result.Value.CommandText.ShouldNotContain("Beta");
    }

    [Fact]
    public async Task UnchangedRowsAreExcludedByANullSafeCompare()
    {
        var result = await Run(Plain(), "Code", "Name");

        result.Value!.CommandText.ShouldContain("t.[Name] <> s.[Name] OR (t.[Name] IS NULL AND s.[Name] IS NOT NULL)");
    }

    [Fact]
    public async Task EmptyCompareColumnsUpdateEveryMatchedRow()
    {
        var result = await Run(new UpsertOptions(["Code"], [], ["Code", "Name"]), "Code", "Name");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("WHERE (1 = 1)");
    }

    [Fact]
    public async Task UnknownColumnIsRefusedWithTheUpsertOptionsCode()
    {
        var result = await Run(new UpsertOptions(["Code"], ["Missing"], ["Code", "Missing"]), "Code", "Name");

        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("InvalidUpsertOptions");
    }

    [Fact]
    public async Task UnsafeColumnNameIsRefused()
    {
        var result = await Run(new UpsertOptions(["Code"], ["Name]; DROP TABLE x;--"], ["Code", "Name]; DROP TABLE x;--"]), "Code", "Name");

        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("InvalidUpsertOptions");
    }

    [Fact]
    public async Task CompareColumnThatIsAlsoAKeyIsRefused()
    {
        var result = await Run(new UpsertOptions(["Code"], ["Code"], ["Code", "Name"]), "Code", "Name");

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task SourceRowMissingAColumnIsRefusedNotSentAsNull()
    {
        var container = TestContainers.Table("Customers", TestContainers.Columns("Code", "Name"));
        var rows = new List<Dictionary<string, object?>> { new() { ["Code"] = "A" } };

        var result = await _sut.Translate(
            new UpsertCommand<Dictionary<string, object?>>(rows, Plain()), container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("InvalidUpsertOptions");
    }

    [Fact]
    public async Task VersioningClosesTheCurrentRowAndOpensANewOne()
    {
        var options = new UpsertOptions(["Code"], ["Name"], ["Code", "Name"])
        {
            Versioning = new VersioningOptions("EffectiveFrom", "EffectiveTo") { CurrentFlagColumn = "IsCurrent" },
        };

        var result = await Run(options, "Code", "Name", "EffectiveFrom", "EffectiveTo", "IsCurrent");

        result.IsSuccess.ShouldBeTrue();
        var sql = result.Value!.CommandText;
        sql.ShouldContain("DECLARE @fdw_now datetimeoffset(7) = SYSDATETIMEOFFSET()");
        sql.ShouldContain("UPDATE t SET t.[EffectiveTo] = @fdw_now, t.[IsCurrent] = 0");
        sql.ShouldContain("t.[EffectiveTo] IS NULL AND (");
        sql.ShouldContain("([Code], [Name], [EffectiveFrom], [EffectiveTo], [IsCurrent]) SELECT s.[Code], s.[Name], @fdw_now, NULL, 1");
        sql.ShouldContain("e.[EffectiveTo] IS NULL");
    }

    [Fact]
    public async Task InsertOnlyMarkerRowsAreNeverClosed()
    {
        var options = new UpsertOptions(["Code"], ["Name"], ["Code", "Name"])
        {
            Versioning = new VersioningOptions("EffectiveFrom", "EffectiveTo") { InsertOnlyRule = new InsertOnlyRule("Source", "manual") },
        };

        var result = await Run(options, "Code", "Name", "EffectiveFrom", "EffectiveTo", "Source");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("AND (t.[Source] IS NULL OR t.[Source] <> @fdw_marker)");
        result.Value.Parameters["@fdw_marker"].Value.ShouldBe("manual");
    }

    [Fact]
    public async Task ConstantValueSourceFillsAnInsertOnlyColumnFromAParameter()
    {
        var options = new UpsertOptions(["Code"], ["Name"], ["Code", "Name"])
        {
            ValueSources = [new ColumnValueBinding("LoadedBy", new ConstantValueSource("etl"))],
        };

        var result = await Run(options, "Code", "Name", "LoadedBy");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("[LoadedBy]");
        result.Value.Parameters["@fdw_const1"].Value.ShouldBe("etl");
    }

    [Fact]
    public async Task SequenceValueSourceOrdersByTheListedColumns()
    {
        var options = new UpsertOptions(["Code"], ["Name"], ["Code", "Name"])
        {
            ValueSources = [new ColumnValueBinding("Seq", new NextSequenceValueSource("OrderSeq", ["Name"], "dbo"))],
        };

        var result = await Run(options, "Code", "Name", "Seq");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("NEXT VALUE FOR [dbo].[OrderSeq] OVER (ORDER BY [s].[Name])");
    }

    [Fact]
    public async Task ColumnValueSourceIsRefusedBecauseRowsAreAlreadyKeyedByDestinationColumn()
    {
        var options = new UpsertOptions(["Code"], ["Name"], ["Code", "Name"])
        {
            ValueSources = [new ColumnValueBinding("Alias", new ColumnReferenceValueSource("Name"))],
        };

        var result = await Run(options, "Code", "Name", "Alias");

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task BulkUpsertTranslatesTheSameWay()
    {
        var container = TestContainers.Table("Customers", TestContainers.Columns("Code", "Name"));

        var result = await new MsSqlBulkUpsertTranslator().Translate(
            new BulkUpsertCommand<Dictionary<string, object?>>(Rows(), Plain()), container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("OPENJSON(@fdw_rows)");
    }
}
