using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Fdw.Tds.Endpoints.Tests;

/// <summary>FDW-4404: the parsed nodes a batch is made of reach their handlers through collections keyed by the node's type name, and a node no handler holds is refused as it was before.</summary>
public sealed class SqlNodeDispatchTests
{
    [Theory]
    [InlineData("SELECT * FROM Sales.Orders WHERE (Id = 2)")]
    [InlineData("SELECT * FROM Sales.Orders WHERE Id = 2 AND Customer = 'Acme'")]
    [InlineData("SELECT * FROM Sales.Orders WHERE Id = 2 OR Id = 3")]
    [InlineData("SELECT * FROM Sales.Orders WHERE Customer IS NULL")]
    [InlineData("SELECT * FROM Sales.Orders WHERE Id IN (1, 2)")]
    [InlineData("SELECT * FROM Sales.Orders WHERE Customer LIKE 'Ac%'")]
    [InlineData("SELECT * FROM Sales.Orders WHERE Id BETWEEN 1 AND 2")]
    public async Task EachKindOfConditionTheStoreCanRunIsSentToTheStore(string batch)
    {
        var harness = new SqlBatchHarness();

        await harness.Run(batch);

        harness.Endpoint.Commands.ShouldHaveSingleItem().Filter.ShouldNotBeNull();
    }

    [Fact]
    public async Task AConditionNoHandlerTranslatesIsRefusedWithError40517()
    {
        var harness = new SqlBatchHarness();

        var outcome = await harness.Run("SELECT * FROM Sales.Orders WHERE NOT (Id = 2)");

        outcome.Refusal.ShouldNotBeNull().Number.ShouldBe(40517);
        harness.Endpoint.Commands.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecOfTextRunsTheTextAsABatchOfItsOwn()
    {
        var outcome = await new SqlBatchHarness().Run("EXEC('SELECT 7 AS v')");

        outcome.Tokens.ResultSets.ShouldHaveSingleItem().Rows.ShouldHaveSingleItem().ShouldBe(new object?[] { 7 });
    }

    [Fact]
    public async Task ExecOfAProcedureRunsTheProcedure()
    {
        var outcome = await new SqlBatchHarness().Run("EXEC sp_executesql N'SELECT 8 AS v'");

        outcome.Tokens.ResultSets.ShouldHaveSingleItem().Rows.ShouldHaveSingleItem().ShouldBe(new object?[] { 8 });
    }

    [Fact]
    public async Task ADerivedTableAndACrossJoinAreReadInMemory()
    {
        var derived = await new SqlBatchHarness().Run("SELECT d.name FROM (SELECT t.name FROM sys.tables AS t) AS d ORDER BY d.name");
        var crossed = await new SqlBatchHarness().Run("SELECT a.name FROM sys.tables AS a CROSS JOIN sys.tables AS b");

        derived.Tokens.ResultSets.ShouldHaveSingleItem().Rows.Select(r => r[0]).ShouldBe(["Events", "Orders"]);
        crossed.Tokens.ResultSets.ShouldHaveSingleItem().Rows.Count.ShouldBe(4);
    }

    [Fact]
    public async Task AParenthesizedJoinIsReadLikeTheJoinInsideIt()
    {
        var outcome = await new SqlBatchHarness().Run("SELECT a.name FROM (sys.tables AS a CROSS JOIN sys.tables AS b)");

        outcome.Tokens.ResultSets.ShouldHaveSingleItem().Rows.Count.ShouldBe(4);
    }

    [Fact]
    public async Task AnApplyIsNotReadInMemoryAndIsRefused()
    {
        var outcome = await new SqlBatchHarness().Run("SELECT a.name FROM sys.tables AS a CROSS APPLY (SELECT 1 AS x) AS b");

        outcome.Refusal.ShouldNotBeNull();
    }

    [Fact]
    public async Task TildeKeepsABigintABigintAndMakesAnySmallerWholeNumberAnInt()
    {
        var outcome = await new SqlBatchHarness().Run(
            "SELECT ~5 AS a, ~CAST(5 AS tinyint) AS b, ~CAST(5 AS smallint) AS c, ~CAST(5 AS bigint) AS d");

        outcome.Tokens.ResultSets.ShouldHaveSingleItem().Rows.ShouldHaveSingleItem().ShouldBe(new object?[] { -6, -6, -6, -6L });
    }

    [Fact]
    public async Task TildeOfTextIsRefusedAsIncompatible()
    {
        var outcome = await new SqlBatchHarness().Run("SELECT ~'a' AS v");

        outcome.Refusal.ShouldNotBeNull();
    }
}
