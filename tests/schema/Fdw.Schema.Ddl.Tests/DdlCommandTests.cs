using Fdw.Schema.Ddl.Commands;
using Fdw.Schema.Ddl.Definitions;

namespace Fdw.Schema.Ddl.Tests;

public class DdlCommandTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateSchemaCommandHasCorrectProperties()
    {
        var cmd = new CreateSchemaCommand { Name = "cfg" };

        cmd.CommandType.ShouldBe(DdlCommandTypes.CreateSchema);
        cmd.SchemaName.ShouldBe("cfg");
        cmd.ObjectName.ShouldBe("cfg");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DropSchemaCommandHasCorrectProperties()
    {
        var cmd = new DropSchemaCommand { Name = "etl" };

        cmd.CommandType.ShouldBe(DdlCommandTypes.DropSchema);
        cmd.SchemaName.ShouldBe("etl");
        cmd.ObjectName.ShouldBe("etl");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateTableCommandHasCorrectProperties()
    {
        var columns = new[]
        {
            new DdlColumnDefinition { Name = "Name", SqlType = "VARCHAR", MaxLength = 200 }
        };

        var cmd = new CreateTableCommand
        {
            SchemaName = "cfg",
            TableName = "Connection",
            Columns = columns,
            PrimaryKeyName = "PK_Connection",
            PrimaryKeyColumns = ["Id"]
        };

        cmd.CommandType.ShouldBe(DdlCommandTypes.CreateTable);
        cmd.SchemaName.ShouldBe("cfg");
        cmd.ObjectName.ShouldBe("Connection");
        cmd.TableName.ShouldBe("Connection");
        cmd.Columns.Count.ShouldBe(1);
        cmd.PrimaryKeyName.ShouldBe("PK_Connection");
        cmd.PrimaryKeyColumns.ShouldNotBeNull();
        cmd.PrimaryKeyColumns!.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DropTableCommandHasCorrectProperties()
    {
        var cmd = new DropTableCommand { SchemaName = "cfg", TableName = "Connection" };

        cmd.CommandType.ShouldBe(DdlCommandTypes.DropTable);
        cmd.SchemaName.ShouldBe("cfg");
        cmd.ObjectName.ShouldBe("Connection");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateIndexCommandHasCorrectProperties()
    {
        var indexDef = new DdlIndexDefinition
        {
            Name = "IX_Connection_Name",
            Columns = ["Name"],
            IsUnique = true
        };

        var cmd = new CreateIndexCommand
        {
            SchemaName = "cfg",
            TableName = "Connection",
            IndexName = "IX_Connection_Name",
            Definition = indexDef
        };

        cmd.CommandType.ShouldBe(DdlCommandTypes.CreateIndex);
        cmd.SchemaName.ShouldBe("cfg");
        cmd.ObjectName.ShouldBe("IX_Connection_Name");
        cmd.TableName.ShouldBe("Connection");
        cmd.Definition.IsUnique.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DropIndexCommandHasCorrectProperties()
    {
        var cmd = new DropIndexCommand
        {
            SchemaName = "cfg",
            TableName = "Connection",
            IndexName = "IX_Connection_Name"
        };

        cmd.CommandType.ShouldBe(DdlCommandTypes.DropIndex);
        cmd.SchemaName.ShouldBe("cfg");
        cmd.ObjectName.ShouldBe("IX_Connection_Name");
        cmd.TableName.ShouldBe("Connection");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InsertDataCommandHasCorrectProperties()
    {
        var cmd = new InsertDataCommand
        {
            SchemaName = "types",
            TableName = "TypeOption",
            Columns = ["Id", "Name"],
            Values = [new object?[] { 1, "Equal" }, new object?[] { 2, "NotEqual" }],
            IdentityInsert = true
        };

        cmd.CommandType.ShouldBe(DdlCommandTypes.InsertData);
        cmd.SchemaName.ShouldBe("types");
        cmd.ObjectName.ShouldBe("TypeOption");
        cmd.Columns.Count.ShouldBe(2);
        cmd.Values.Count.ShouldBe(2);
        cmd.IdentityInsert.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InsertDataCommandIdentityInsertDefaultsFalse()
    {
        var cmd = new InsertDataCommand
        {
            TableName = "Test",
            Columns = ["Id"],
            Values = [new object?[] { 1 }]
        };

        cmd.IdentityInsert.ShouldBeFalse();
    }
}
