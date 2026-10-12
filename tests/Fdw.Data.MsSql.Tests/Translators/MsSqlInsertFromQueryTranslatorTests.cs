using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Loads;
using Fdw.Commands.Data.ValueSources;
using Fdw.Data.MsSql.Tests.TestSupport;
using Fdw.Data.MsSql.Translators;

namespace Fdw.Data.MsSql.Tests.Translators;

[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlInsertFromQueryTranslatorTests
{
    private readonly MsSqlInsertFromQueryTranslator _sut = new();

    private async Task<Fdw.Results.IGenericResult<Microsoft.Data.SqlClient.SqlCommand>> Run(
        InsertFromQueryOptions options,
        params TestContainers.Column[] targetColumns)
    {
        var container = TestContainers.Table("Orders", targetColumns);
        return await _sut.Translate(new InsertFromQueryCommand(options), container.Object, TestContext.Current.CancellationToken);
    }

    [Fact]
    public void NameIsInsertFromQuery() => _sut.Name.ShouldBe("InsertFromQuery");

    [Fact]
    public async Task CopiesWriteColumnsFromTheSourceInOneStatement()
    {
        var options = new InsertFromQueryOptions(["Code", "Name"], new QuerySource("Staging", "dbo"));

        var result = await Run(options, TestContainers.Columns("Code", "Name"));

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldBe(
            "INSERT INTO [dbo].[Orders] ([Code], [Name]) SELECT [src].[Code], [src].[Name] FROM [dbo].[Staging] AS [src]");
    }

    [Fact]
    public async Task CrossDatabaseSourceIsQualifiedWithTheDatabase()
    {
        var options = new InsertFromQueryOptions(["Code"], new QuerySource("Staging", "dbo", "OtherDb"));

        var result = await Run(options, TestContainers.Columns("Code"));

        result.Value!.CommandText.ShouldContain("FROM [OtherDb].[dbo].[Staging] AS [src]");
    }

    [Fact]
    public async Task SequenceValueSourceNumbersRowsInTheOrderOfTheListedColumns()
    {
        var options = new InsertFromQueryOptions(["Code"], new QuerySource("Staging", "dbo"))
        {
            ValueSources = [new ColumnValueBinding("Seq", new NextSequenceValueSource("OrderSeq", ["Code"], "dbo"))],
        };

        var result = await Run(options, TestContainers.Columns("Code", "Seq"));

        result.Value!.CommandText.ShouldContain("NEXT VALUE FOR [dbo].[OrderSeq] OVER (ORDER BY [src].[Code])");
    }

    [Fact]
    public async Task ColumnValueSourceReadsAnotherSourceField()
    {
        var options = new InsertFromQueryOptions([], new QuerySource("Staging", "dbo"))
        {
            ValueSources = [new ColumnValueBinding("Code", new ColumnReferenceValueSource("LegacyCode"))],
        };

        var result = await Run(options, TestContainers.Columns("Code"));

        result.Value!.CommandText.ShouldContain("SELECT [src].[LegacyCode]");
    }

    [Fact]
    public async Task ConstantAndTimestampSourcesRenderAsParameterAndClockExpression()
    {
        var options = new InsertFromQueryOptions(["Code"], new QuerySource("Staging", "dbo"))
        {
            ValueSources =
            [
                new ColumnValueBinding("Batch", new ConstantValueSource(7)),
                new ColumnValueBinding("LoadedAt", new CurrentTimestampValueSource()),
            ],
        };

        var result = await Run(options, TestContainers.Columns("Code", "Batch", "LoadedAt"));

        result.Value!.CommandText.ShouldContain("@fdw_const1, SYSDATETIMEOFFSET()");
        result.Value.Parameters["@fdw_const1"].Value.ShouldBe(7);
    }

    [Fact]
    public async Task UnsafeSourceNameIsRefused()
    {
        var options = new InsertFromQueryOptions(["Code"], new QuerySource("Staging]; DROP TABLE x;--", "dbo"));

        var result = await Run(options, TestContainers.Columns("Code"));

        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("InvalidInsertFromQueryOptions");
    }

    [Fact]
    public async Task IdentityTargetColumnIsRefused()
    {
        var options = new InsertFromQueryOptions(["Id"], new QuerySource("Staging", "dbo"));

        var result = await Run(options, new TestContainers.Column("Id", IsIdentity: true));

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task ColumnListedAsWriteColumnAndValueSourceIsRefused()
    {
        var options = new InsertFromQueryOptions(["Code"], new QuerySource("Staging", "dbo"))
        {
            ValueSources = [new ColumnValueBinding("Code", new ConstantValueSource("x"))],
        };

        var result = await Run(options, TestContainers.Columns("Code"));

        result.IsSuccess.ShouldBeFalse();
    }
}
