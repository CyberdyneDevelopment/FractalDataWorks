using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Loads;
using Fdw.Commands.Data.ValueSources;
using Fdw.Data.MsSql.Tests.TestSupport;
using static Fdw.Data.MsSql.Tests.RealMsSql.RealMsSqlSupport;

namespace Fdw.Data.MsSql.Tests.RealMsSql;

/// <summary>InsertFromQuery against a real SQL Server: set-based copy with sequenced order, and the row count.</summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class RealMsSqlInsertFromQueryTests(RealMsSqlFixture fixture) : IClassFixture<RealMsSqlFixture>
{
    private async Task<(string Staging, string Orders, string Sequence)> Create()
    {
        var staging = fixture.Unique("Staging");
        var orders = fixture.Unique("Orders");
        var sequence = fixture.Unique("OrderSeq");
        await fixture.Exec(
            $"CREATE SEQUENCE [{fixture.Schema}].[{sequence}] AS bigint START WITH 100 INCREMENT BY 1; "
            + $"CREATE TABLE [{fixture.Schema}].[{staging}] (Code nvarchar(20) NOT NULL, Name nvarchar(50) NULL, Priority int NOT NULL); "
            + $"CREATE TABLE [{fixture.Schema}].[{orders}] (OrderNo bigint NOT NULL, Code nvarchar(20) NOT NULL, Name nvarchar(50) NULL, LoadedAt datetimeoffset(7) NOT NULL, Batch int NOT NULL); "
            + $"INSERT INTO [{fixture.Schema}].[{staging}] (Code, Name, Priority) VALUES (N'X', N'Ex', 3), (N'Y', N'Why', 1), (N'Z', N'Zed', 2);");
        return (staging, orders, sequence);
    }

    private InsertFromQueryCommand Command(string staging, string sequence, string? database = null) =>
        new(new InsertFromQueryOptions(["Code", "Name"], new QuerySource(staging, fixture.Schema, database))
        {
            ValueSources =
            [
                new ColumnValueBinding("OrderNo", new NextSequenceValueSource(sequence, ["Priority"], fixture.Schema)),
                new ColumnValueBinding("LoadedAt", new CurrentTimestampValueSource()),
                new ColumnValueBinding("Batch", new ConstantValueSource(7)),
            ],
        });

    private static readonly TestContainers.Column[] OrderColumns = TestContainers.Columns("OrderNo", "Code", "Name", "LoadedAt", "Batch");

    [Fact]
    public async Task CopiesTheRowsNumbersThemInSourceOrderAndReturnsTheCount()
    {
        fixture.RequireDatabase();
        var (staging, orders, sequence) = await Create();
        var container = Container(fixture, orders, OrderColumns);

        var affected = await Run(fixture, Command(staging, sequence), container.Object);

        affected.ShouldBe(3);
        var rows = await fixture.Rows($"SELECT OrderNo, Code, Batch, LoadedAt FROM [{fixture.Schema}].[{orders}] ORDER BY OrderNo");
        rows.Select(r => (string)r["Code"]!).ToArray().ShouldBe(new[] { "Y", "Z", "X" });
        rows.Select(r => (long)r["OrderNo"]!).ToArray().ShouldBe(new[] { 100L, 101L, 102L });
        rows.ShouldAllBe(r => (int)r["Batch"]! == 7 && r["LoadedAt"] != null);
    }

    [Fact]
    public async Task ASourceQualifiedWithTheCurrentDatabaseCopiesTheSameWay()
    {
        fixture.RequireDatabase();
        var (staging, orders, sequence) = await Create();
        var database = (string)(await fixture.Scalar("SELECT DB_NAME()"))!;
        if (!database.All(c => char.IsLetterOrDigit(c) || c == '_'))
            Assert.Skip($"Database name '{database}' is not a plain identifier, so a database-qualified source is refused by design.");
        var container = Container(fixture, orders, OrderColumns);

        var affected = await Run(fixture, Command(staging, sequence, database), container.Object);

        affected.ShouldBe(3);
        (await fixture.Scalar($"SELECT COUNT(*) FROM [{fixture.Schema}].[{orders}]")).ShouldBe(3);
    }
}
