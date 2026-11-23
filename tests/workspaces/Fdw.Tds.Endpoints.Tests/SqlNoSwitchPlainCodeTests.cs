using System.Threading.Tasks;
using Fdw.Protocols.Tds;
using Fdw.Protocols.Tds.Types;
using Fdw.Tds.Endpoints.Sql.Expressions;
using Shouldly;
using Xunit;

namespace Fdw.Tds.Endpoints.Tests;

/// <summary>FDW-4404: the answers of SET TEXTSIZE, a scalar subquery and a type's declaration, which were switches over a number's range or a row count; the expected values are what the switches answered.</summary>
public sealed class SqlNoSwitchPlainCodeTests
{
    [Theory]
    [InlineData("SET TEXTSIZE 100", 100)]
    [InlineData("SET TEXTSIZE 0", 4096)]
    [InlineData("SET TEXTSIZE -1", int.MaxValue)]
    [InlineData("SET TEXTSIZE 2147483647", int.MaxValue)]
    public async Task SetTextSizeResetsAtZeroAndCapsAtTheLargestInt(string batch, int expected)
    {
        var harness = new SqlBatchHarness();

        await harness.Run(batch);

        harness.Session.TextSize.ShouldBe(expected);
    }

    [Fact]
    public async Task AScalarSubqueryWithOneRowIsItsValue()
    {
        var outcome = await new SqlBatchHarness().Run("SELECT (SELECT t.name FROM sys.tables AS t WHERE t.name = 'Orders') AS n");

        outcome.Tokens.ResultSets.ShouldHaveSingleItem().Rows.ShouldHaveSingleItem().ShouldBe(new object?[] { "Orders" });
    }

    [Fact]
    public async Task AScalarSubqueryWithNoRowIsNull()
    {
        var outcome = await new SqlBatchHarness().Run("SELECT (SELECT t.name FROM sys.tables AS t WHERE t.name = 'Missing') AS n");

        outcome.Tokens.ResultSets.ShouldHaveSingleItem().Rows.ShouldHaveSingleItem().ShouldBe(new object?[] { null });
    }

    [Fact]
    public async Task AScalarSubqueryWithMoreThanOneRowIsError512()
    {
        var outcome = await new SqlBatchHarness().Run("SELECT (SELECT t.name FROM sys.tables AS t) AS n");

        outcome.Refusal.ShouldNotBeNull().Number.ShouldBe(512);
    }

    [Fact]
    public void ASizedTypeIsDeclaredWithItsLengthAndMaxWithMax()
    {
        Target("nvarchar", "NVarChar", 50, 0, 0).ToString().ShouldBe("nvarchar(50)");
        Target("nvarchar", "NVarChar", TdsColumnType.Max, 0, 0).ToString().ShouldBe("nvarchar(max)");
    }

    [Fact]
    public void ADecimalIsDeclaredWithItsPrecisionAndScaleAndAnythingElseByName()
    {
        Target("decimal", "Decimal", 0, 10, 2).ToString().ShouldBe("decimal(10,2)");
        Target("int", "Int", 0, 0, 0).ToString().ShouldBe("int");
    }

    private static SqlConversionTarget Target(string sqlName, string option, int length, byte precision, byte scale)
        => new(sqlName, SqlDataTypes.ByName(option), length, precision, scale, static (value, _) => value);
}
