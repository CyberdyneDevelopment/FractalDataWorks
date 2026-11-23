using System.Linq;
using Fdw.Schema.Ddl.Commands;
using Fdw.Schema.Endpoints.Ddl;
using Fdw.Services.Connections.Clients.Models;

namespace Fdw.Schema.Endpoints.Tests;

/// <summary>
/// The CREATE TABLE an execute-ddl request asks for (FDW-3617): the request's columns, their nullability and identity, and the key it marks;
/// a key column is NOT NULL, the key is named for its table, and a request with no key makes a table with none.
/// </summary>
public sealed class ExecuteDdlCommandsTests
{
    private static ExecuteDdlRequest Request(params DdlColumnRequest[] columns) => new()
    {
        Name = "Main",
        SchemaName = "dbo",
        TableName = "Teams",
        Columns = columns,
    };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TheColumnsAndTheKeyTheRequestMarksAreTheTablesAndAKeyColumnIsNotNull()
    {
        var command = (CreateTableCommand)ExecuteDdlCommands.CreateTable(Request(
            new DdlColumnRequest { Name = "Id", SqlType = "INT", IsPrimaryKey = true, IsIdentity = true },
            new DdlColumnRequest { Name = "Name", SqlType = "NVARCHAR", MaxLength = 50, IsRequired = true },
            new DdlColumnRequest { Name = "Note", SqlType = "NVARCHAR", MaxLength = -1 }));

        command.SchemaName.ShouldBe("dbo");
        command.TableName.ShouldBe("Teams");
        command.PrimaryKeyName.ShouldBe("PK_Teams");
        command.PrimaryKeyColumns.ShouldBe(["Id"]);
        command.Columns.Select(column => (column.Name, column.IsNullable, column.IsIdentity)).ShouldBe(
            [("Id", false, true), ("Name", false, false), ("Note", true, false)]);
        command.Columns[1].MaxLength.ShouldBe(50);
        command.Columns[2].MaxLength.ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ARequestThatMarksNoKeyMakesATableWithNone()
    {
        var command = (CreateTableCommand)ExecuteDdlCommands.CreateTable(Request(new DdlColumnRequest { Name = "Note", SqlType = "NVARCHAR" }));

        command.PrimaryKeyName.ShouldBeNull();
        command.PrimaryKeyColumns.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AKeyOfSeveralColumnsIsTheWholeKey()
    {
        var command = (CreateTableCommand)ExecuteDdlCommands.CreateTable(Request(
            new DdlColumnRequest { Name = "TenantId", SqlType = "INT", IsPrimaryKey = true },
            new DdlColumnRequest { Name = "UserId", SqlType = "INT", IsPrimaryKey = true }));

        command.PrimaryKeyColumns.ShouldBe(["TenantId", "UserId"]);
    }
}
