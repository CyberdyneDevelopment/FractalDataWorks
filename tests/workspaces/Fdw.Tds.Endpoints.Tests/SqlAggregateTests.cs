using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Fdw.Tds.Endpoints.Tests;

/// <summary>FDW-4404: SUM and AVG keep the type of their total, as SQL Server does; the expected values are what the code before the switch was removed answered.</summary>
public sealed class SqlAggregateTests
{
    [Fact]
    public async Task SumAndAverageOfIntegersAreIntegersAndAverageDividesWholeNumbers()
    {
        var outcome = await new SqlBatchHarness().Run("SELECT SUM(Id) AS s, AVG(Id) AS a FROM Sales.Orders");

        outcome.Tokens.ResultSets.ShouldHaveSingleItem().Rows.ShouldHaveSingleItem().ShouldBe(new object?[] { 6, 2 });
    }

    [Fact]
    public async Task SumAndAverageOfDecimalsAreDecimalsOverTheRowsThatHaveAValue()
    {
        var outcome = await new SqlBatchHarness().Run("SELECT SUM(Amount) AS s, AVG(Amount) AS a FROM Sales.Orders");

        outcome.Tokens.ResultSets.ShouldHaveSingleItem().Rows.ShouldHaveSingleItem().ShouldBe(new object?[] { 30.75m, 15.375m });
    }
}
