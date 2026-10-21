using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Connections.Scripts;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Tests;

// FDW-1441: the rows a script selects are read the same way for every SQL connection, whole or not at all.
public sealed class ScriptedRowsTests
{
    private static DataTable Table(params string[] columns)
    {
        var table = new DataTable();
        foreach (var column in columns)
            table.Columns.Add(column, typeof(object));

        return table;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task EachRowIsItsColumnsByNameAndADatabaseNullIsNull()
    {
        var table = Table("Id", "Name");
        table.Rows.Add(1, "Bills");
        table.Rows.Add(2, System.DBNull.Value);

        using var reader = table.CreateDataReader();
        var rows = await ScriptedRows.Read(reader, null, NullLogger.Instance, TestContext.Current.CancellationToken);

        rows.IsSuccess.ShouldBeTrue(rows.CurrentMessage);
        rows.Value!.Count.ShouldBe(2);
        rows.Value[0]["Id"].ShouldBe(1);
        rows.Value[0]["name"].ShouldBe("Bills");
        rows.Value[1]["Name"].ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AColumnNamedTwiceFailsTheReadNamingTheColumn()
    {
        var table = Table("Id", "ID");
        table.Rows.Add(1, 2);

        using var reader = table.CreateDataReader();
        var rows = await ScriptedRows.Read(reader, null, NullLogger.Instance, TestContext.Current.CancellationToken);

        rows.IsSuccess.ShouldBeFalse();
        rows.CurrentMessage.ShouldNotBeNull().ShouldContain("ID");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task MoreRowsThanTheScriptMayReadFailTheReadAndKeepNoPartOfIt()
    {
        var table = Table("Id");
        table.Rows.Add(1);
        table.Rows.Add(2);
        table.Rows.Add(3);

        using var reader = table.CreateDataReader();
        var rows = await ScriptedRows.Read(reader, 2, NullLogger.Instance, TestContext.Current.CancellationToken);

        rows.IsSuccess.ShouldBeFalse();
        rows.CurrentMessage.ShouldNotBeNull().ShouldContain("2 row");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExactlyTheMostRowsIsAllowed()
    {
        var table = Table("Id");
        table.Rows.Add(1);
        table.Rows.Add(2);

        using var reader = table.CreateDataReader();
        var rows = await ScriptedRows.Read(reader, 2, NullLogger.Instance, TestContext.Current.CancellationToken);

        rows.IsSuccess.ShouldBeTrue(rows.CurrentMessage);
        rows.Value!.Select(row => row["Id"]).ShouldBe([1, 2]);
    }
}
