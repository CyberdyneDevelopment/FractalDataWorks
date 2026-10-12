using System.Linq;
using System.Threading.Tasks;
using Fdw.Protocols.Tds;
using Shouldly;
using Xunit;

namespace Fdw.Tds.Endpoints.Tests;

/// <summary>
/// Batches a vendor's client sends, answered as SQL Server answers them: result sets, DONE counts, and the
/// SQL Server error a statement this endpoint will not run is refused with.
/// </summary>
public sealed class SqlBatchTests
{
    [Fact]
    public async Task AnExpressionIsOneCountedRow()
    {
        var outcome = await new SqlBatchHarness().Run("SELECT 1 + 1 AS two");

        outcome.Result.IsSuccess.ShouldBeTrue();
        var set = outcome.Tokens.ResultSets.ShouldHaveSingleItem();
        set.Columns.ShouldBe(["two"]);
        set.Rows.ShouldHaveSingleItem().ShouldBe(new object?[] { 2 });
        var done = outcome.Tokens.Dones.Last();
        (done.Status & TdsDoneStatus.Count).ShouldBe(TdsDoneStatus.Count);
        done.RowCount.ShouldBe(1);
    }

    [Fact]
    public async Task TheServerAndDatabaseAnswerAsConfigured()
    {
        var outcome = await new SqlBatchHarness().Run("SELECT @@SERVERNAME AS s, DB_NAME() AS d");

        outcome.Tokens.ResultSets.ShouldHaveSingleItem().Rows.ShouldHaveSingleItem().ShouldBe(new object?[] { "FDWSQL", "Warehouse" });
    }

    [Fact]
    public async Task AVariableHoldsItsValueAcrossStatements()
    {
        var outcome = await new SqlBatchHarness().Run("DECLARE @x int = 5; SELECT @x * 2 AS v;");

        outcome.Tokens.ResultSets.ShouldHaveSingleItem().Rows.ShouldHaveSingleItem().ShouldBe(new object?[] { 10 });
    }

    [Fact]
    public async Task SelectStarReturnsEveryColumnInDeclaredOrder()
    {
        var outcome = await new SqlBatchHarness().Run("SELECT * FROM Sales.Orders");

        var set = outcome.Tokens.ResultSets.ShouldHaveSingleItem();
        set.Columns.ShouldBe(["Id", "Customer", "Amount"]);
        set.Rows.Count.ShouldBe(3);
        set.Rows[0].ShouldBe(new object?[] { 1, "Acme", 10.50m });
        set.Rows[2].ShouldBe(new object?[] { 3, null, null });
    }

    [Fact]
    public async Task AWhereClauseIsSentToTheStore()
    {
        var harness = new SqlBatchHarness();

        await harness.Run("SELECT Id FROM Sales.Orders WHERE Id = 2");

        harness.Endpoint.Commands.ShouldHaveSingleItem().Filter.ShouldNotBeNull();
    }

    [Fact]
    public async Task AWhereOnAColumnThatIsNotFilterableIsRefusedBeforeReading()
    {
        var harness = new SqlBatchHarness();

        var outcome = await harness.Run("SELECT Id FROM Sales.Orders WHERE Amount > 5");

        outcome.Refusal.ShouldNotBeNull();
        harness.Endpoint.Commands.ShouldBeEmpty();
    }

    [Fact]
    public async Task TopWithOrderByIsOrderedAndPagedByAStoreThatPages()
    {
        var harness = new SqlBatchHarness();

        await harness.Run("SELECT TOP 2 Id FROM Sales.Orders ORDER BY Id DESC");

        var command = harness.Endpoint.Commands.ShouldHaveSingleItem();
        command.Ordering.ShouldNotBeNull();
        command.Paging.ShouldNotBeNull();
    }

    [Fact]
    public async Task OrderByOnAStoreThatDoesNotOrderIsRefusedBeforeReading()
    {
        var harness = new SqlBatchHarness();

        var outcome = await harness.Run("SELECT Name FROM Sales.Events ORDER BY Name");

        outcome.Refusal.ShouldNotBeNull().Number.ShouldBe(40517);
        harness.Endpoint.Commands.ShouldBeEmpty();
    }

    [Fact]
    public async Task CountRunsOverTheRowsTheStoreReturns()
    {
        var outcome = await new SqlBatchHarness().Run("SELECT COUNT(*) AS n FROM Sales.Orders");

        outcome.Tokens.ResultSets.ShouldHaveSingleItem().Rows.ShouldHaveSingleItem().ShouldBe(new object?[] { 3 });
    }

    [Theory]
    [InlineData("SELECT * FROM Sales.Missing")]
    [InlineData("SELECT * FROM Orders")]
    [InlineData("SELECT * FROM Elsewhere.Sales.Orders")]
    public async Task ATableTheCallerCannotReadDoesNotExist(string text)
    {
        var outcome = await new SqlBatchHarness().Run(text);

        outcome.Refusal.ShouldNotBeNull().Number.ShouldBe(208);
    }

    [Theory]
    [InlineData("INSERT INTO Sales.Orders (Id) VALUES (4)")]
    [InlineData("UPDATE Sales.Orders SET Customer = 'x'")]
    [InlineData("DELETE FROM Sales.Orders")]
    [InlineData("CREATE TABLE Sales.Copy (Id int)")]
    [InlineData("DROP TABLE Sales.Orders")]
    public async Task WritesAndDdlAreRefusedAsByAReadOnlyDatabase(string text)
    {
        var harness = new SqlBatchHarness();

        var outcome = await harness.Run(text);

        outcome.Refusal.ShouldNotBeNull().Number.ShouldBe(3906);
        harness.Endpoint.Commands.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("SELECT , 1", 102)]
    [InlineData("SELEC 1", 2812)]
    [InlineData("SELECT @nope", 137)]
    [InlineData("SELECT 1 / 0", 8134)]
    [InlineData("SELECT NoSuchColumn FROM Sales.Orders", 207)]
    [InlineData("EXEC sp_no_such_procedure", 2812)]
    [InlineData("COMMIT", 3902)]
    public async Task AStatementThatCannotRunIsRefusedWithSqlServersNumber(string text, int number)
    {
        var outcome = await new SqlBatchHarness().Run(text);

        outcome.Refusal.ShouldNotBeNull().Number.ShouldBe(number);
    }

    [Fact]
    public async Task ARefusedStatementEndsTheBatch()
    {
        var outcome = await new SqlBatchHarness().Run("SELECT 1 AS a; SELECT 1 / 0; SELECT 3 AS c;");

        outcome.Refusal.ShouldNotBeNull().Number.ShouldBe(8134);
        outcome.Tokens.ResultSets.ShouldHaveSingleItem().Columns.ShouldBe(["a"]);
    }

    [Fact]
    public async Task NoCountDropsTheCountFromDone()
    {
        var outcome = await new SqlBatchHarness().Run("SET NOCOUNT ON; SELECT 1 AS one;");

        (outcome.Tokens.Dones.Last().Status & TdsDoneStatus.Count).ShouldBe((ushort)0);
    }

    [Fact]
    public async Task SessionOptionsLastAcrossBatches()
    {
        var harness = new SqlBatchHarness();

        await harness.Run("SET NOCOUNT ON");
        var outcome = await harness.Run("SELECT 1 AS one");

        (outcome.Tokens.Dones.Last().Status & TdsDoneStatus.Count).ShouldBe((ushort)0);
    }

    [Fact]
    public async Task ATransactionIsCountedAndEnded()
    {
        var harness = new SqlBatchHarness();

        var outcome = await harness.Run("BEGIN TRANSACTION; SELECT @@TRANCOUNT AS c; COMMIT;");

        outcome.Result.IsSuccess.ShouldBeTrue();
        outcome.Tokens.ResultSets.ShouldHaveSingleItem().Rows.ShouldHaveSingleItem().ShouldBe(new object?[] { 1 });
        harness.Session.TransactionCount.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteSqlRunsItsStatementWithItsParameters()
    {
        var outcome = await new SqlBatchHarness().Run("EXEC sp_executesql N'SELECT @p + 1 AS v', N'@p int', @p = 41");

        outcome.Result.IsSuccess.ShouldBeTrue();
        outcome.Tokens.ResultSets.ShouldHaveSingleItem().Rows.ShouldHaveSingleItem().ShouldBe(new object?[] { 42 });
        outcome.Tokens.Dones.ShouldContain(d => d.Token == TdsTokenTypes.DoneInProcedure);
    }

    [Fact]
    public async Task ExecuteSqlRefusesAParameterItWasNotGiven()
    {
        var outcome = await new SqlBatchHarness().Run("EXEC sp_executesql N'SELECT @p AS v', N'@p int'");

        outcome.Refusal.ShouldNotBeNull().Number.ShouldBe(8178);
    }

    [Fact]
    public async Task ABatchThatRunsItselfStopsAtSqlServersNestingLimit()
    {
        var outcome = await new SqlBatchHarness().Run(
            "DECLARE @s nvarchar(4000) = N'EXEC sp_executesql @s, N''@s nvarchar(4000)'', @s = @s'; EXEC sp_executesql @s, N'@s nvarchar(4000)', @s = @s;");

        outcome.Refusal.ShouldNotBeNull().Number.ShouldBe(217);
    }

    [Fact]
    public async Task APreparedStatementRunsByItsHandleUntilUnprepared()
    {
        var harness = new SqlBatchHarness();

        var ran = await harness.Run(
            "DECLARE @h int; EXEC sp_prepare @h OUTPUT, N'@p int', N'SELECT @p AS v'; EXEC sp_execute @h, 7; EXEC sp_unprepare @h; EXEC sp_execute @h, 8;");

        ran.Tokens.ResultSets.ShouldHaveSingleItem().Rows.ShouldHaveSingleItem().ShouldBe(new object?[] { 7 });
        ran.Refusal.ShouldNotBeNull().Number.ShouldBe(8179);
    }

    [Fact]
    public async Task TheCatalogListsEveryDataSetAsABaseTable()
    {
        var outcome = await new SqlBatchHarness().Run(
            "SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE FROM INFORMATION_SCHEMA.TABLES ORDER BY TABLE_NAME");

        var rows = outcome.Tokens.ResultSets.ShouldHaveSingleItem().Rows;
        rows.Count.ShouldBe(2);
        rows[0].ShouldBe(new object?[] { "Sales", "Events", "BASE TABLE" });
        rows[1].ShouldBe(new object?[] { "Sales", "Orders", "BASE TABLE" });
    }

    [Fact]
    public async Task SysTablesNamesTheDataSets()
    {
        var outcome = await new SqlBatchHarness().Run("SELECT t.name FROM sys.tables AS t JOIN sys.schemas AS s ON s.schema_id = t.schema_id WHERE s.name = 'Sales' ORDER BY t.name");

        outcome.Tokens.ResultSets.ShouldHaveSingleItem().Rows.Select(r => r[0]).ShouldBe(["Events", "Orders"]);
    }

    [Fact]
    public async Task TheCatalogDescribesEachColumnsType()
    {
        var outcome = await new SqlBatchHarness().Run(
            "SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS " +
            "WHERE TABLE_SCHEMA = 'Sales' AND TABLE_NAME = 'Orders' ORDER BY ORDINAL_POSITION");

        var rows = outcome.Tokens.ResultSets.ShouldHaveSingleItem().Rows;
        rows[0].ShouldBe(new object?[] { "Id", "int", null, "NO" });
        rows[1].ShouldBe(new object?[] { "Customer", "nvarchar", 100, "YES" });
        rows[2][1].ShouldBe("decimal");
    }
}
