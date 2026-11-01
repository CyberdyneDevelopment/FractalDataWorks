using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Upserts;
using Fdw.Commands.Data.ValueSources;
using Fdw.Data.MsSql.Tests.TestSupport;
using static Fdw.Data.MsSql.Tests.RealMsSql.RealMsSqlSupport;

namespace Fdw.Data.MsSql.Tests.RealMsSql;

/// <summary>Upsert and versioned upsert against a real SQL Server, in a schema the run creates and drops.</summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class RealMsSqlUpsertTests(RealMsSqlFixture fixture) : IClassFixture<RealMsSqlFixture>
{
    [Fact]
    public async Task UpsertInsertsNewKeysUpdatesTheChangedRowAndLeavesTheUnchangedRowUntouched()
    {
        fixture.RequireDatabase();
        var table = fixture.Unique("Customer");
        await fixture.Exec(
            $"CREATE TABLE [{fixture.Schema}].[{table}] (Code nvarchar(20) NOT NULL PRIMARY KEY, Name nvarchar(100) NULL, City nvarchar(100) NULL, Version rowversion); "
            + $"INSERT INTO [{fixture.Schema}].[{table}] (Code, Name, City) VALUES (N'B', N'Beta', N'Y'), (N'C', N'Gamma', N'OldCity');");
        var unchangedBefore = await fixture.Scalar($"SELECT CAST(Version AS bigint) FROM [{fixture.Schema}].[{table}] WHERE Code = N'B'");
        var container = Container(fixture, table, TestContainers.Columns("Code", "Name", "City"));
        var options = new UpsertOptions(["Code"], ["Name", "City"], ["Code", "Name", "City"]);
        var rows = new List<Dictionary<string, object?>>
        {
            Row(("Code", "B"), ("Name", "Beta"), ("City", "Y")),
            Row(("Code", "C"), ("Name", "Gamma"), ("City", null)),
            Row(("Code", "N1"), ("Name", "One"), ("City", "P")),
            Row(("Code", "N2"), ("Name", "Two"), ("City", "Q")),
            Row(("Code", "N3"), ("Name", "Three"), ("City", "R")),
        };

        var affected = await Run(fixture, new UpsertCommand<Dictionary<string, object?>>(rows, options), container.Object);

        affected.ShouldBe(4);
        (await fixture.Scalar($"SELECT COUNT(*) FROM [{fixture.Schema}].[{table}]")).ShouldBe(5);
        (await fixture.Scalar($"SELECT CAST(Version AS bigint) FROM [{fixture.Schema}].[{table}] WHERE Code = N'B'")).ShouldBe(unchangedBefore);
        (await fixture.Rows($"SELECT City FROM [{fixture.Schema}].[{table}] WHERE Code = N'C'")).Single()["City"].ShouldBeNull();
        (await fixture.Scalar($"SELECT Name FROM [{fixture.Schema}].[{table}] WHERE Code = N'N2'")).ShouldBe("Two");
    }

    [Fact]
    public async Task RunningTheSameUpsertAgainChangesNothing()
    {
        fixture.RequireDatabase();
        var table = fixture.Unique("Customer");
        await fixture.Exec($"CREATE TABLE [{fixture.Schema}].[{table}] (Code nvarchar(20) NOT NULL PRIMARY KEY, Name nvarchar(100) NULL);");
        var container = Container(fixture, table, TestContainers.Columns("Code", "Name"));
        var options = new UpsertOptions(["Code"], ["Name"], ["Code", "Name"]);
        var rows = new List<Dictionary<string, object?>> { Row(("Code", "A"), ("Name", "Alpha")), Row(("Code", "B"), ("Name", "Beta")) };

        var first = await Run(fixture, new BulkUpsertCommand<Dictionary<string, object?>>(rows, options), container.Object);
        var second = await Run(fixture, new BulkUpsertCommand<Dictionary<string, object?>>(rows, options), container.Object);

        first.ShouldBe(2);
        second.ShouldBe(0);
    }

    [Fact]
    public async Task VersionedUpsertClosesTheOldRowOpensANewCurrentRowAndNeverTouchesTheInsertOnlyMarkerRow()
    {
        fixture.RequireDatabase();
        var table = fixture.Unique("Product");
        var target = $"[{fixture.Schema}].[{table}]";
        await fixture.Exec(
            $"CREATE TABLE {target} (Id int IDENTITY(1,1) PRIMARY KEY, Sku nvarchar(20) NOT NULL, Name nvarchar(50) NULL, Price decimal(10,2) NULL, "
            + "EffectiveFrom datetimeoffset(7) NULL, EffectiveTo datetimeoffset(7) NULL, IsCurrent bit NULL, Source nvarchar(20) NULL, Version rowversion); "
            + $"INSERT INTO {target} (Sku, Name, Price, EffectiveFrom, EffectiveTo, IsCurrent, Source) VALUES "
            + "(N'S1', N'One', 10.00, '2020-01-01', NULL, 1, N'feed'), "
            + "(N'S2', N'Two', 20.00, '2020-01-01', NULL, 1, N'manual'), "
            + "(N'S3', N'Three', 30.00, '2020-01-01', NULL, 1, N'feed');");
        var unchangedBefore = await fixture.Scalar($"SELECT CAST(Version AS bigint) FROM {target} WHERE Sku = N'S3'");
        var markerBefore = await fixture.Scalar($"SELECT CAST(Version AS bigint) FROM {target} WHERE Sku = N'S2'");
        var container = Container(fixture, table, TestContainers.Columns("Id", "Sku", "Name", "Price", "EffectiveFrom", "EffectiveTo", "IsCurrent", "Source"));
        var options = new UpsertOptions(["Sku"], ["Name", "Price"], ["Sku", "Name", "Price"])
        {
            Versioning = new VersioningOptions("EffectiveFrom", "EffectiveTo")
            {
                CurrentFlagColumn = "IsCurrent",
                InsertOnlyRule = new InsertOnlyRule("Source", "manual"),
            },
            ValueSources = [new ColumnValueBinding("Source", new ConstantValueSource("feed"))],
        };
        var rows = new List<Dictionary<string, object?>>
        {
            Row(("Sku", "S1"), ("Name", "One"), ("Price", 11.50m)),
            Row(("Sku", "S2"), ("Name", "Two"), ("Price", 99.00m)),
            Row(("Sku", "S3"), ("Name", "Three"), ("Price", 30.00m)),
            Row(("Sku", "S4"), ("Name", "Four"), ("Price", 40.00m)),
        };

        var affected = await Run(fixture, new UpsertCommand<Dictionary<string, object?>>(rows, options), container.Object);

        affected.ShouldBe(3);
        var s1 = await fixture.Rows($"SELECT Price, EffectiveTo, IsCurrent FROM {target} WHERE Sku = N'S1' ORDER BY Id");
        s1.Count.ShouldBe(2);
        s1[0]["EffectiveTo"].ShouldNotBeNull();
        s1[0]["IsCurrent"].ShouldBe(false);
        s1[0]["Price"].ShouldBe(10.00m);
        s1[1]["EffectiveTo"].ShouldBeNull();
        s1[1]["IsCurrent"].ShouldBe(true);
        s1[1]["Price"].ShouldBe(11.50m);
        (await fixture.Scalar($"SELECT COUNT(*) FROM {target} WHERE Sku = N'S2'")).ShouldBe(1);
        (await fixture.Scalar($"SELECT CAST(Version AS bigint) FROM {target} WHERE Sku = N'S2'")).ShouldBe(markerBefore);
        (await fixture.Scalar($"SELECT CAST(Version AS bigint) FROM {target} WHERE Sku = N'S3'")).ShouldBe(unchangedBefore);
        var s4 = (await fixture.Rows($"SELECT EffectiveFrom, EffectiveTo, IsCurrent, Source FROM {target} WHERE Sku = N'S4'")).Single();
        s4["EffectiveFrom"].ShouldNotBeNull();
        s4["EffectiveTo"].ShouldBeNull();
        s4["IsCurrent"].ShouldBe(true);
        s4["Source"].ShouldBe("feed");
        (await fixture.Scalar($"SELECT COUNT(*) FROM {target} WHERE EffectiveTo IS NULL")).ShouldBe(4);
    }
}
