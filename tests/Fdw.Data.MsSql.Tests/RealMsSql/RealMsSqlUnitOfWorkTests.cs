using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Upserts;
using Fdw.Commands.Data.ValueSources;
using Fdw.Data.MsSql.Tests.TestSupport;
using Fdw.Services.Connections.Abstractions.UnitOfWork;
using static Fdw.Data.MsSql.Tests.RealMsSql.RealMsSqlSupport;

namespace Fdw.Data.MsSql.Tests.RealMsSql;

/// <summary>The unit of work against a real SQL Server: ordered commands, one transaction, all or nothing.</summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class RealMsSqlUnitOfWorkTests(RealMsSqlFixture fixture) : IClassFixture<RealMsSqlFixture>
{
    private sealed record Tables(string Orders, string Customer, string Strict);

    private async Task<Tables> Create()
    {
        var tables = new Tables(fixture.Unique("Orders"), fixture.Unique("Customer"), fixture.Unique("Strict"));
        await fixture.Exec(
            $"CREATE TABLE [{fixture.Schema}].[{tables.Orders}] (Code nvarchar(20) NOT NULL, Name nvarchar(50) NULL); "
            + $"CREATE TABLE [{fixture.Schema}].[{tables.Customer}] (Code nvarchar(20) NOT NULL PRIMARY KEY, Name nvarchar(50) NULL); "
            + $"CREATE TABLE [{fixture.Schema}].[{tables.Strict}] (Code nvarchar(20) NOT NULL PRIMARY KEY, Qty int NOT NULL CHECK (Qty > 0));");
        return tables;
    }

    private UnitOfWorkStep[] Steps(Tables tables, int strictQuantity)
    {
        var orders = new UpsertCommand<Dictionary<string, object?>>(
            [Row(("Code", "A"), ("Name", "Alpha")), Row(("Code", "B"), ("Name", "Beta"))],
            new UpsertOptions(["Code"], ["Name"], ["Code", "Name"]));
        var customers = new UpsertCommand<Dictionary<string, object?>>(
            [Row(("Code", "C1"), ("Name", "One")), Row(("Code", "C2"), ("Name", "Two"))],
            new UpsertOptions(["Code"], ["Name"], ["Code", "Name"]));
        var strict = new UpsertCommand<Dictionary<string, object?>>(
            [Row(("Code", "S1"), ("Qty", strictQuantity))],
            new UpsertOptions(["Code"], ["Qty"], ["Code", "Qty"]));

        return
        [
            new UnitOfWorkStep(orders, Container(fixture, tables.Orders, TestContainers.Columns("Code", "Name")).Object),
            new UnitOfWorkStep(customers, Container(fixture, tables.Customer, TestContainers.Columns("Code", "Name")).Object),
            new UnitOfWorkStep(strict, Container(fixture, tables.Strict, TestContainers.Columns("Code", "Qty")).Object),
        ];
    }

    [Fact]
    public async Task AFailureOnTheThirdCommandRollsBackTheFirstTwo()
    {
        fixture.RequireDatabase();
        var tables = await Create();
        await using var connection = await fixture.Open();

        var result = await new DataUnitOfWork().Run(TransactionalConnection(connection).Object, Steps(tables, strictQuantity: -1), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        (await fixture.Scalar($"SELECT COUNT(*) FROM [{fixture.Schema}].[{tables.Orders}]")).ShouldBe(0);
        (await fixture.Scalar($"SELECT COUNT(*) FROM [{fixture.Schema}].[{tables.Customer}]")).ShouldBe(0);
        (await fixture.Scalar($"SELECT COUNT(*) FROM [{fixture.Schema}].[{tables.Strict}]")).ShouldBe(0);
    }

    [Fact]
    public async Task WhenEveryCommandSucceedsAllOfThemAreCommittedAndTheRowsAreSummed()
    {
        fixture.RequireDatabase();
        var tables = await Create();
        await using var connection = await fixture.Open();

        var result = await new DataUnitOfWork().Run(TransactionalConnection(connection).Object, Steps(tables, strictQuantity: 5), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(2 + 2 + 1);
        (await fixture.Scalar($"SELECT COUNT(*) FROM [{fixture.Schema}].[{tables.Orders}]")).ShouldBe(2);
        (await fixture.Scalar($"SELECT COUNT(*) FROM [{fixture.Schema}].[{tables.Customer}]")).ShouldBe(2);
        (await fixture.Scalar($"SELECT COUNT(*) FROM [{fixture.Schema}].[{tables.Strict}]")).ShouldBe(1);
    }
}
