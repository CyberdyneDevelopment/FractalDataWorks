using System;
using System.Data;
using System.Linq;
using Fdw.Commands.Data.Ddl;

namespace Fdw.Commands.Data.Tests.Ddl;

/// <summary>
/// Comprehensive tests for the <see cref="CreateTableCommand"/> class.
/// Tests fluent API, column definitions, foreign keys, and indexes.
/// </summary>
public sealed class CreateTableCommandTests
{
    #region Constructor Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_WithTableName_CreatesCommandWithCorrectProperties()
    {
        // Arrange
        const string tableName = "TestTable";

        // Act
        var command = new CreateTableCommand(tableName);

        // Assert
        command.TableName.ShouldBe(tableName);
        command.DdlCommandType.ShouldBe(DdlCommandTypes.CreateTable);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_InitializesEmptyCollections()
    {
        // Arrange
        const string tableName = "TestTable";

        // Act
        var command = new CreateTableCommand(tableName);

        // Assert
        command.Columns.ShouldBeEmpty();
        command.ForeignKeys.ShouldBeEmpty();
        command.Indexes.ShouldBeEmpty();
    }

    #endregion

    #region WithColumn Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithColumn_AddsColumnToCollection()
    {
        // Arrange
        var command = new CreateTableCommand("TestTable");

        // Act
        command.WithColumn("Id", SqlDbType.Int);

        // Assert
        command.Columns.Count.ShouldBe(1);
        command.Columns[0].Name.ShouldBe("Id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithColumn_ReturnsCommandForChaining()
    {
        // Arrange
        var command = new CreateTableCommand("TestTable");

        // Act
        var result = command.WithColumn("Id", SqlDbType.Int);

        // Assert
        result.ShouldBe(command);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithColumn_WithAllParameters_SetsAllProperties()
    {
        // Arrange
        var command = new CreateTableCommand("TestTable");

        // Act
        command.WithColumn(
            name: "Name",
            type: SqlDbType.NVarChar,
            maxLength: 255,
            precision: null,
            scale: null,
            isRequired: true,
            isPrimaryKey: false,
            isIdentity: false,
            defaultValue: "'Unknown'",
            isUnique: true,
            collation: "Latin1_General_CI_AS");

        // Assert
        var column = command.Columns[0];
        column.Name.ShouldBe("Name");
        column.Type.ShouldBe(SqlDbType.NVarChar);
        column.MaxLength.ShouldBe(255);
        column.IsRequired.ShouldBeTrue();
        column.IsUnique.ShouldBeTrue();
        column.Collation.ShouldBe("Latin1_General_CI_AS");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithColumn_WithNullOrWhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreateTableCommand("TestTable");

        // Act & Assert
        Should.Throw<ArgumentException>(() => command.WithColumn(null!, SqlDbType.Int));
        Should.Throw<ArgumentException>(() => command.WithColumn("", SqlDbType.Int));
        Should.Throw<ArgumentException>(() => command.WithColumn("   ", SqlDbType.Int));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithColumn_MultipleColumns_AddsAllColumns()
    {
        // Arrange
        var command = new CreateTableCommand("TestTable");

        // Act
        command
            .WithColumn("Id", SqlDbType.Int, isPrimaryKey: true, isIdentity: true)
            .WithColumn("Name", SqlDbType.NVarChar, maxLength: 100, isRequired: true)
            .WithColumn("CreatedAt", SqlDbType.DateTime2, isRequired: true);

        // Assert
        command.Columns.Count.ShouldBe(3);
        command.Columns[0].Name.ShouldBe("Id");
        command.Columns[1].Name.ShouldBe("Name");
        command.Columns[2].Name.ShouldBe("CreatedAt");
    }

    #endregion

    #region WithComputedColumn Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithComputedColumn_AddsComputedColumn()
    {
        // Arrange
        var command = new CreateTableCommand("TestTable");

        // Act
        command.WithComputedColumn("FullName", SqlDbType.NVarChar, "FirstName + ' ' + LastName");

        // Assert
        command.Columns.Count.ShouldBe(1);
        var column = command.Columns[0];
        column.Name.ShouldBe("FullName");
        column.IsComputed.ShouldBeTrue();
        column.ComputedExpression.ShouldBe("FirstName + ' ' + LastName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithComputedColumn_WithNullOrWhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreateTableCommand("TestTable");

        // Act & Assert
        Should.Throw<ArgumentException>(() => command.WithComputedColumn(null!, SqlDbType.NVarChar, "expr"));
        Should.Throw<ArgumentException>(() => command.WithComputedColumn("", SqlDbType.NVarChar, "expr"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithComputedColumn_WithNullOrWhitespaceExpression_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreateTableCommand("TestTable");

        // Act & Assert
        Should.Throw<ArgumentException>(() => command.WithComputedColumn("Col", SqlDbType.NVarChar, null!));
        Should.Throw<ArgumentException>(() => command.WithComputedColumn("Col", SqlDbType.NVarChar, ""));
    }

    #endregion

    #region WithForeignKey Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithForeignKey_AddsForeignKeyConstraint()
    {
        // Arrange
        var command = new CreateTableCommand("Orders");

        // Act
        command.WithForeignKey("CustomerId", "Customers", "Id");

        // Assert
        command.ForeignKeys.Count.ShouldBe(1);
        var fk = command.ForeignKeys[0];
        fk.ColumnName.ShouldBe("CustomerId");
        fk.ReferencedTable.ShouldBe("Customers");
        fk.ReferencedColumn.ShouldBe("Id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithForeignKey_WithAllParameters_SetsAllProperties()
    {
        // Arrange
        var command = new CreateTableCommand("Orders");

        // Act
        command.WithForeignKey(
            columnName: "CustomerId",
            referencedTable: "Customers",
            referencedColumn: "Id",
            onDelete: ForeignKeyActions.Cascade,
            onUpdate: ForeignKeyActions.SetNull,
            referencedSchema: "dbo",
            constraintName: "FK_Orders_Customers");

        // Assert
        var fk = command.ForeignKeys[0];
        fk.ColumnName.ShouldBe("CustomerId");
        fk.ReferencedTable.ShouldBe("Customers");
        fk.ReferencedColumn.ShouldBe("Id");
        fk.OnDelete.ShouldBe(ForeignKeyActions.Cascade);
        fk.OnUpdate.ShouldBe(ForeignKeyActions.SetNull);
        fk.ReferencedSchema.ShouldBe("dbo");
        fk.Name.ShouldBe("FK_Orders_Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithForeignKey_WithNullOrWhitespaceColumnName_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreateTableCommand("Orders");

        // Act & Assert
        Should.Throw<ArgumentException>(() => command.WithForeignKey(null!, "Customers", "Id"));
        Should.Throw<ArgumentException>(() => command.WithForeignKey("", "Customers", "Id"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithForeignKey_WithNullOrWhitespaceReferencedTable_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreateTableCommand("Orders");

        // Act & Assert
        Should.Throw<ArgumentException>(() => command.WithForeignKey("CustomerId", null!, "Id"));
        Should.Throw<ArgumentException>(() => command.WithForeignKey("CustomerId", "", "Id"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithForeignKey_WithNullOrWhitespaceReferencedColumn_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreateTableCommand("Orders");

        // Act & Assert
        Should.Throw<ArgumentException>(() => command.WithForeignKey("CustomerId", "Customers", null!));
        Should.Throw<ArgumentException>(() => command.WithForeignKey("CustomerId", "Customers", ""));
    }

    #endregion

    #region WithIndex Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithIndex_AddsIndexDefinition()
    {
        // Arrange
        var command = new CreateTableCommand("Orders");

        // Act
        command.WithIndex("IX_Orders_CreatedAt", new[] { "CreatedAt" });

        // Assert
        command.Indexes.Count.ShouldBe(1);
        var index = command.Indexes[0];
        index.Name.ShouldBe("IX_Orders_CreatedAt");
        index.ColumnNames.ShouldBe(new[] { "CreatedAt" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithIndex_WithAllParameters_SetsAllProperties()
    {
        // Arrange
        var command = new CreateTableCommand("Orders");

        // Act
        command.WithIndex(
            indexName: "IX_Orders_Composite",
            columnNames: new[] { "CustomerId", "OrderDate" },
            isUnique: true,
            isClustered: false,
            includeColumns: new[] { "TotalAmount" },
            filterCondition: "IsDeleted = 0",
            fillFactor: 80);

        // Assert
        var index = command.Indexes[0];
        index.Name.ShouldBe("IX_Orders_Composite");
        index.ColumnNames.ShouldBe(new[] { "CustomerId", "OrderDate" });
        index.IsUnique.ShouldBeTrue();
        index.IsClustered.ShouldBeFalse();
        index.IncludeColumns.ShouldBe(new[] { "TotalAmount" });
        index.FilterCondition.ShouldBe("IsDeleted = 0");
        index.FillFactor.ShouldBe(80);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithIndex_WithNullOrWhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreateTableCommand("Orders");

        // Act & Assert
        Should.Throw<ArgumentException>(() => command.WithIndex(null!, new[] { "Col" }));
        Should.Throw<ArgumentException>(() => command.WithIndex("", new[] { "Col" }));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithIndex_WithNullOrEmptyColumnNames_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreateTableCommand("Orders");

        // Act & Assert
        Should.Throw<ArgumentException>(() => command.WithIndex("IX_Test", null!));
        Should.Throw<ArgumentException>(() => command.WithIndex("IX_Test", Array.Empty<string>()));
    }

    #endregion

    #region Fluent Chaining Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FluentChaining_AllMethodsReturnCommandInstance()
    {
        // Arrange
        var command = new CreateTableCommand("Orders");

        // Act
        var result = command
            .WithColumn("Id", SqlDbType.Int, isPrimaryKey: true, isIdentity: true)
            .WithColumn("OrderDate", SqlDbType.DateTime2, isRequired: true)
            .WithForeignKey("CustomerId", "Customers", "Id", onDelete: ForeignKeyActions.Cascade)
            .WithIndex("IX_Orders_OrderDate", new[] { "OrderDate" });

        // Assert
        result.ShouldBe(command);
        command.Columns.Count.ShouldBe(2);
        command.ForeignKeys.Count.ShouldBe(1);
        command.Indexes.Count.ShouldBe(1);
    }

    #endregion

    #region Complex Scenario Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ComplexScenario_EmailConfigurationsTable_CreatesCorrectStructure()
    {
        // Arrange & Act
        var command = new CreateTableCommand("EmailConfigurations")
        {
            SchemaName = "config",
            IfNotExists = true
        }
        .WithColumn("Id", SqlDbType.Int, isPrimaryKey: true, isIdentity: true, isRequired: true)
        .WithColumn("SmtpHost", SqlDbType.NVarChar, maxLength: 255, isRequired: true)
        .WithColumn("SmtpPort", SqlDbType.Int, isRequired: true, defaultValue: "587")
        .WithColumn("CreatedAt", SqlDbType.DateTime2, isRequired: true, defaultValue: "GETUTCDATE()")
        .WithForeignKey("ConnectionTypeId", "ConnectionTypes", "Id", onDelete: ForeignKeyActions.Cascade)
        .WithIndex("IX_EmailConfigurations_CreatedAt", new[] { "CreatedAt" });

        // Assert
        command.TableName.ShouldBe("EmailConfigurations");
        command.SchemaName.ShouldBe("config");
        command.IfNotExists.ShouldBeTrue();
        command.Columns.Count.ShouldBe(4);
        command.ForeignKeys.Count.ShouldBe(1);
        command.Indexes.Count.ShouldBe(1);
    }

    #endregion
}
