using Fdw.Schema.Ddl.Commands;
using Fdw.Schema.Ddl.Definitions;
using Fdw.Schema.Ddl.MsSql;

namespace Fdw.Schema.Ddl.MsSql.Tests;

public class MsSqlDdlGeneratorTests
{
    private readonly MsSqlDdlGenerator _sut = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TargetDatabaseIsMsSql()
    {
        _sut.TargetDatabase.ShouldBe("MsSql");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlCreateSchemaContainsIfNotExists()
    {
        var cmd = new CreateSchemaCommand { Name = "cfg" };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("IF NOT EXISTS");
        result.Value.ShouldContain("CREATE SCHEMA [cfg]");
        result.Value.ShouldContain("GO");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlDropSchemaContainsIfExists()
    {
        var cmd = new DropSchemaCommand { Name = "etl" };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("IF EXISTS");
        result.Value.ShouldContain("DROP SCHEMA [etl]");
        result.Value.ShouldContain("GO");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlCreateTableIncludesColumnsAndPrimaryKey()
    {
        var columns = new[]
        {
            new DdlColumnDefinition { Name = "Name", SqlType = "VARCHAR", MaxLength = 200, IsNullable = false }
        };

        var cmd = new CreateTableCommand
        {
            SchemaName = "cfg",
            TableName = "Connection",
            Columns = columns,
            PrimaryKeyName = "PK_Connection",
            PrimaryKeyColumns = ["Id"]
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("CREATE TABLE cfg.Connection");
        result.Value.ShouldContain("Id");
        result.Value.ShouldContain("INT");
        result.Value.ShouldContain("Name");
        result.Value.ShouldContain("VARCHAR(200)");
        result.Value.ShouldContain("NOT NULL");
        result.Value.ShouldContain("CONSTRAINT PK_Connection PRIMARY KEY (Id)");
        result.Value.ShouldContain("GO");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlCreateTableWithDefaultValues()
    {
        var columns = new[]
        {
            new DdlColumnDefinition
            {
                Name = "RowId",
                SqlType = "UNIQUEIDENTIFIER",
                IsNullable = false,
                DefaultValue = "NEWSEQUENTIALID()"
            }
        };

        var cmd = new CreateTableCommand
        {
            SchemaName = "cfg",
            TableName = "Test",
            Columns = columns
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("DEFAULT NEWSEQUENTIALID()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlCreateTableWithoutSchemaUsesTableNameOnly()
    {
        var columns = new[]
        {
            new DdlColumnDefinition { Name = "Id", SqlType = "INT", IsNullable = false }
        };

        var cmd = new CreateTableCommand
        {
            SchemaName = null,
            TableName = "TestTable",
            Columns = columns
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("CREATE TABLE TestTable");
        // The table reference itself should not be schema-qualified
        result.Value.ShouldNotContain("CREATE TABLE .");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlCreateTableWithForeignKeys()
    {
        var columns = new[]
        {
            new DdlColumnDefinition { Name = "Id", SqlType = "INT", IsNullable = false },
            new DdlColumnDefinition { Name = "ParentId", SqlType = "INT", IsNullable = false }
        };

        var fks = new[]
        {
            new DdlForeignKeyDefinition
            {
                Name = "FK_Child_Parent",
                ColumnName = "ParentId",
                ReferencedSchema = "cfg",
                ReferencedTable = "Parent",
                ReferencedColumn = "Id",
                OnDelete = DdlForeignKeyActions.Cascade
            }
        };

        var cmd = new CreateTableCommand
        {
            SchemaName = "cfg",
            TableName = "Child",
            Columns = columns,
            ForeignKeys = fks
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("CONSTRAINT FK_Child_Parent FOREIGN KEY (ParentId)");
        result.Value.ShouldContain("REFERENCES cfg.Parent(Id)");
        result.Value.ShouldContain("ON DELETE CASCADE");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlCreateTableWithUniqueColumns()
    {
        var columns = new[]
        {
            new DdlColumnDefinition { Name = "Id", SqlType = "INT", IsNullable = false },
            new DdlColumnDefinition { Name = "Email", SqlType = "VARCHAR", MaxLength = 255, IsUnique = true }
        };

        var cmd = new CreateTableCommand
        {
            SchemaName = "dbo",
            TableName = "Users",
            Columns = columns
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("CONSTRAINT UQ_Users_Email UNIQUE (Email)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlDropTableContainsIfExistsAndSchema()
    {
        var cmd = new DropTableCommand { SchemaName = "cfg", TableName = "Connection" };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("IF EXISTS");
        result.Value.ShouldContain("DROP TABLE cfg.Connection");
        result.Value.ShouldContain("GO");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlCreateIndexNonClustered()
    {
        var indexDef = new DdlIndexDefinition
        {
            Name = "IX_Connection_Name",
            Columns = ["Name"],
            IsUnique = false,
            IsClustered = false
        };

        var cmd = new CreateIndexCommand
        {
            SchemaName = "cfg",
            TableName = "Connection",
            IndexName = "IX_Connection_Name",
            Definition = indexDef
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("CREATE NONCLUSTERED INDEX IX_Connection_Name ON cfg.Connection (Name)");
        result.Value.ShouldContain("GO");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlCreateIndexUniqueWithFilter()
    {
        var indexDef = new DdlIndexDefinition
        {
            Name = "UX_Connection_Id_Current",
            Columns = ["Id"],
            IsUnique = true,
            FilterPredicate = "IsCurrent = 1"
        };

        var cmd = new CreateIndexCommand
        {
            SchemaName = "cfg",
            TableName = "Connection",
            IndexName = "UX_Connection_Id_Current",
            Definition = indexDef
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("UNIQUE");
        result.Value.ShouldContain("WHERE IsCurrent = 1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlCreateIndexWithIncludeColumns()
    {
        var indexDef = new DdlIndexDefinition
        {
            Name = "IX_Covering",
            Columns = ["Status"],
            IncludeColumns = ["Name", "Description"]
        };

        var cmd = new CreateIndexCommand
        {
            SchemaName = "dbo",
            TableName = "Items",
            IndexName = "IX_Covering",
            Definition = indexDef
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("INCLUDE (Name, Description)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlCreateIndexWithFillFactor()
    {
        var indexDef = new DdlIndexDefinition
        {
            Name = "IX_Test",
            Columns = ["Col1"],
            FillFactor = 80
        };

        var cmd = new CreateIndexCommand
        {
            SchemaName = "dbo",
            TableName = "Test",
            IndexName = "IX_Test",
            Definition = indexDef
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("WITH (FILLFACTOR = 80)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlDropIndexContainsIfExists()
    {
        var cmd = new DropIndexCommand
        {
            SchemaName = "cfg",
            TableName = "Connection",
            IndexName = "IX_Connection_Name"
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("IF EXISTS");
        result.Value.ShouldContain("DROP INDEX IX_Connection_Name ON cfg.Connection");
        result.Value.ShouldContain("GO");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateScriptCombinesMultipleCommands()
    {
        var commands = new IDdlCommand[]
        {
            new CreateSchemaCommand { Name = "cfg" },
            new CreateTableCommand
            {
                SchemaName = "cfg",
                TableName = "Test",
                Columns = [new DdlColumnDefinition { Name = "Id", SqlType = "INT" }]
            }
        };

        var result = _sut.GenerateScript(commands);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("CREATE SCHEMA [cfg]");
        result.Value.ShouldContain("CREATE TABLE cfg.Test");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateScriptReturnsFailureWhenCommandFails()
    {
        // InsertDataCommand is not handled by GenerateSql, so it should fail
        var commands = new IDdlCommand[]
        {
            new InsertDataCommand
            {
                TableName = "Test",
                Columns = ["Id"],
                Values = [new object?[] { 1 }]
            }
        };

        var result = _sut.GenerateScript(commands);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlForeignKeyOnUpdateSetDefault()
    {
        var columns = new[]
        {
            new DdlColumnDefinition { Name = "RefId", SqlType = "INT" }
        };

        var fks = new[]
        {
            new DdlForeignKeyDefinition
            {
                Name = "FK_Test",
                ColumnName = "RefId",
                ReferencedSchema = "dbo",
                ReferencedTable = "Ref",
                ReferencedColumn = "Id",
                OnUpdate = DdlForeignKeyActions.SetDefault
            }
        };

        var cmd = new CreateTableCommand
        {
            SchemaName = "dbo",
            TableName = "Test",
            Columns = columns,
            ForeignKeys = fks
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("ON UPDATE SET DEFAULT");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlForeignKeyOnDeleteSetNull()
    {
        var columns = new[]
        {
            new DdlColumnDefinition { Name = "RefId", SqlType = "INT", IsNullable = true }
        };

        var fks = new[]
        {
            new DdlForeignKeyDefinition
            {
                Name = "FK_SetNull",
                ColumnName = "RefId",
                ReferencedSchema = "dbo",
                ReferencedTable = "Parent",
                ReferencedColumn = "Id",
                OnDelete = DdlForeignKeyActions.SetNull
            }
        };

        var cmd = new CreateTableCommand
        {
            SchemaName = "dbo",
            TableName = "Child",
            Columns = columns,
            ForeignKeys = fks
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("ON DELETE SET NULL");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlForeignKeyOnUpdateCascade()
    {
        var columns = new[]
        {
            new DdlColumnDefinition { Name = "RefId", SqlType = "INT" }
        };

        var fks = new[]
        {
            new DdlForeignKeyDefinition
            {
                Name = "FK_Cascade",
                ColumnName = "RefId",
                ReferencedSchema = "dbo",
                ReferencedTable = "Parent",
                ReferencedColumn = "Id",
                OnUpdate = DdlForeignKeyActions.Cascade
            }
        };

        var cmd = new CreateTableCommand
        {
            SchemaName = "dbo",
            TableName = "Child",
            Columns = columns,
            ForeignKeys = fks
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("ON UPDATE CASCADE");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlCreateIndexClusteredUnique()
    {
        var indexDef = new DdlIndexDefinition
        {
            Name = "CIX_Test",
            Columns = ["Id"],
            IsUnique = true,
            IsClustered = true
        };

        var cmd = new CreateIndexCommand
        {
            SchemaName = "dbo",
            TableName = "Test",
            IndexName = "CIX_Test",
            Definition = indexDef
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("UNIQUE CLUSTERED INDEX CIX_Test");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlDropTableWithoutSchemaUsesTableNameOnly()
    {
        var cmd = new DropTableCommand { SchemaName = null, TableName = "PlainTable" };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("DROP TABLE PlainTable");
        result.Value.ShouldNotContain("DROP TABLE .PlainTable");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlDropIndexWithoutSchemaUsesTableNameOnly()
    {
        var cmd = new DropIndexCommand
        {
            SchemaName = null,
            TableName = "PlainTable",
            IndexName = "IX_Test"
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("DROP INDEX IX_Test ON PlainTable");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlCreateIndexWithoutSchemaUsesTableNameOnly()
    {
        var indexDef = new DdlIndexDefinition
        {
            Name = "IX_Plain",
            Columns = ["Col1"]
        };

        var cmd = new CreateIndexCommand
        {
            SchemaName = null,
            TableName = "PlainTable",
            IndexName = "IX_Plain",
            Definition = indexDef
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("ON PlainTable (Col1)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlCreateTableWithNullableColumn()
    {
        var columns = new[]
        {
            new DdlColumnDefinition { Name = "Id", SqlType = "INT", IsNullable = false },
            new DdlColumnDefinition { Name = "Description", SqlType = "VARCHAR", MaxLength = -1, IsNullable = true }
        };

        var cmd = new CreateTableCommand
        {
            SchemaName = "dbo",
            TableName = "Test",
            Columns = columns
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("NULL");
        result.Value.ShouldContain("VARCHAR(MAX)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlForeignKeyNoActionOmitsOnDeleteClause()
    {
        var columns = new[]
        {
            new DdlColumnDefinition { Name = "RefId", SqlType = "INT" }
        };

        var fks = new[]
        {
            new DdlForeignKeyDefinition
            {
                Name = "FK_NoAction",
                ColumnName = "RefId",
                ReferencedSchema = "dbo",
                ReferencedTable = "Parent",
                ReferencedColumn = "Id",
                OnDelete = DdlForeignKeyActions.NoAction,
                OnUpdate = DdlForeignKeyActions.NoAction
            }
        };

        var cmd = new CreateTableCommand
        {
            SchemaName = "dbo",
            TableName = "Child",
            Columns = columns,
            ForeignKeys = fks
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldNotContain("ON DELETE");
        result.Value.ShouldNotContain("ON UPDATE");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateScriptEmptyCommandListReturnsEmpty()
    {
        var commands = Array.Empty<IDdlCommand>();

        var result = _sut.GenerateScript(commands);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlCreateTableWithNoPrimaryKey()
    {
        var columns = new[]
        {
            new DdlColumnDefinition { Name = "Col1", SqlType = "VARCHAR", MaxLength = 100 },
            new DdlColumnDefinition { Name = "Col2", SqlType = "INT" }
        };

        var cmd = new CreateTableCommand
        {
            SchemaName = "dbo",
            TableName = "HeapTable",
            Columns = columns,
            PrimaryKeyColumns = null
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldNotContain("PRIMARY KEY");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlCreateTableWithDecimalColumnIncludesPrecisionAndScale()
    {
        var columns = new[]
        {
            new DdlColumnDefinition
            {
                Name = "Amount",
                SqlType = "DECIMAL",
                Precision = 18,
                Scale = 4,
                IsNullable = false
            }
        };

        var cmd = new CreateTableCommand
        {
            SchemaName = "dbo",
            TableName = "Amounts",
            Columns = columns
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("DECIMAL(18,4)");
    }
}
