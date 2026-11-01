using System;
using System.Data;
using System.Linq;
using Fdw.Commands.Data.Ddl;

namespace Fdw.Commands.Data.Tests.Ddl;

/// <summary>
/// Comprehensive tests for the <see cref="AlterTableCommand"/> class.
/// Tests add/drop/modify/rename column operations and constraint management.
/// </summary>
public sealed class AlterTableCommandTests
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
        var command = new AlterTableCommand(tableName);

        // Assert
        command.TableName.ShouldBe(tableName);
        command.DdlCommandType.ShouldBe(DdlCommandTypes.AlterTable);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_InitializesEmptyOperationsList()
    {
        // Arrange
        const string tableName = "TestTable";

        // Act
        var command = new AlterTableCommand(tableName);

        // Assert
        command.Operations.ShouldBeEmpty();
    }

    #endregion

    #region AddColumn Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddColumn_AddsOperationToList()
    {
        // Arrange
        var command = new AlterTableCommand("TestTable");

        // Act
        command.AddColumn("NewColumn", SqlDbType.Int);

        // Assert
        command.Operations.Count.ShouldBe(1);
        command.Operations[0].OperationType.ShouldBe(AlterTableOperationTypes.AddColumn);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddColumn_ReturnsCommandForChaining()
    {
        // Arrange
        var command = new AlterTableCommand("TestTable");

        // Act
        var result = command.AddColumn("NewColumn", SqlDbType.Int);

        // Assert
        result.ShouldBe(command);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddColumn_WithAllParameters_SetsAllProperties()
    {
        // Arrange
        var command = new AlterTableCommand("TestTable");

        // Act
        command.AddColumn(
            name: "NewColumn",
            type: SqlDbType.NVarChar,
            maxLength: 500,
            precision: null,
            scale: null,
            isRequired: true,
            defaultValue: "'Default'",
            isUnique: true,
            collation: "Latin1_General_CI_AS");

        // Assert
        var operation = command.Operations[0];
        var column = operation.ColumnDefinition;
        column.ShouldNotBeNull();
        column.Name.ShouldBe("NewColumn");
        column.Type.ShouldBe(SqlDbType.NVarChar);
        column.MaxLength.ShouldBe(500);
        column.IsRequired.ShouldBeTrue();
        column.DefaultValue.ShouldBe("'Default'");
        column.IsUnique.ShouldBeTrue();
        column.Collation.ShouldBe("Latin1_General_CI_AS");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddColumn_WithNullOrWhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var command = new AlterTableCommand("TestTable");

        // Act & Assert
        Should.Throw<ArgumentException>(() => command.AddColumn(null!, SqlDbType.Int));
        Should.Throw<ArgumentException>(() => command.AddColumn("", SqlDbType.Int));
        Should.Throw<ArgumentException>(() => command.AddColumn("   ", SqlDbType.Int));
    }

    #endregion

    #region DropColumn Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DropColumn_AddsOperationToList()
    {
        // Arrange
        var command = new AlterTableCommand("TestTable");

        // Act
        command.DropColumn("OldColumn");

        // Assert
        command.Operations.Count.ShouldBe(1);
        command.Operations[0].OperationType.ShouldBe(AlterTableOperationTypes.DropColumn);
        command.Operations[0].ColumnName.ShouldBe("OldColumn");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DropColumn_ReturnsCommandForChaining()
    {
        // Arrange
        var command = new AlterTableCommand("TestTable");

        // Act
        var result = command.DropColumn("OldColumn");

        // Assert
        result.ShouldBe(command);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DropColumn_WithNullOrWhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var command = new AlterTableCommand("TestTable");

        // Act & Assert
        Should.Throw<ArgumentException>(() => command.DropColumn(null!));
        Should.Throw<ArgumentException>(() => command.DropColumn(""));
        Should.Throw<ArgumentException>(() => command.DropColumn("   "));
    }

    #endregion

    #region ModifyColumn Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ModifyColumn_AddsOperationToList()
    {
        // Arrange
        var command = new AlterTableCommand("TestTable");

        // Act
        command.ModifyColumn("ExistingColumn", SqlDbType.NVarChar, maxLength: 500);

        // Assert
        command.Operations.Count.ShouldBe(1);
        command.Operations[0].OperationType.ShouldBe(AlterTableOperationTypes.ModifyColumn);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ModifyColumn_ReturnsCommandForChaining()
    {
        // Arrange
        var command = new AlterTableCommand("TestTable");

        // Act
        var result = command.ModifyColumn("ExistingColumn", SqlDbType.NVarChar, maxLength: 500);

        // Assert
        result.ShouldBe(command);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ModifyColumn_WithAllParameters_SetsAllProperties()
    {
        // Arrange
        var command = new AlterTableCommand("TestTable");

        // Act
        command.ModifyColumn(
            name: "ExistingColumn",
            newType: SqlDbType.NVarChar,
            maxLength: 500,
            precision: null,
            scale: null,
            isRequired: true,
            defaultValue: "'NewDefault'",
            collation: "SQL_Latin1_General_CP1_CI_AS");

        // Assert
        var operation = command.Operations[0];
        var column = operation.ColumnDefinition;
        column.ShouldNotBeNull();
        column.Name.ShouldBe("ExistingColumn");
        column.Type.ShouldBe(SqlDbType.NVarChar);
        column.MaxLength.ShouldBe(500);
        column.IsRequired.ShouldBeTrue();
        column.DefaultValue.ShouldBe("'NewDefault'");
        column.Collation.ShouldBe("SQL_Latin1_General_CP1_CI_AS");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ModifyColumn_WithNullOrWhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var command = new AlterTableCommand("TestTable");

        // Act & Assert
        Should.Throw<ArgumentException>(() => command.ModifyColumn(null!, SqlDbType.Int));
        Should.Throw<ArgumentException>(() => command.ModifyColumn("", SqlDbType.Int));
    }

    #endregion

    #region RenameColumn Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RenameColumn_AddsOperationToList()
    {
        // Arrange
        var command = new AlterTableCommand("TestTable");

        // Act
        command.RenameColumn("OldName", "NewName");

        // Assert
        command.Operations.Count.ShouldBe(1);
        var operation = command.Operations[0];
        operation.OperationType.ShouldBe(AlterTableOperationTypes.RenameColumn);
        operation.ColumnName.ShouldBe("OldName");
        operation.NewColumnName.ShouldBe("NewName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RenameColumn_ReturnsCommandForChaining()
    {
        // Arrange
        var command = new AlterTableCommand("TestTable");

        // Act
        var result = command.RenameColumn("OldName", "NewName");

        // Assert
        result.ShouldBe(command);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RenameColumn_WithNullOrWhitespaceOldName_ThrowsArgumentException()
    {
        // Arrange
        var command = new AlterTableCommand("TestTable");

        // Act & Assert
        Should.Throw<ArgumentException>(() => command.RenameColumn(null!, "NewName"));
        Should.Throw<ArgumentException>(() => command.RenameColumn("", "NewName"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RenameColumn_WithNullOrWhitespaceNewName_ThrowsArgumentException()
    {
        // Arrange
        var command = new AlterTableCommand("TestTable");

        // Act & Assert
        Should.Throw<ArgumentException>(() => command.RenameColumn("OldName", null!));
        Should.Throw<ArgumentException>(() => command.RenameColumn("OldName", ""));
    }

    #endregion

    #region AddForeignKey Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddForeignKey_AddsOperationToList()
    {
        // Arrange
        var command = new AlterTableCommand("Orders");

        // Act
        command.AddForeignKey("CustomerId", "Customers", "Id");

        // Assert
        command.Operations.Count.ShouldBe(1);
        var operation = command.Operations[0];
        operation.OperationType.ShouldBe(AlterTableOperationTypes.AddForeignKey);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddForeignKey_WithAllParameters_SetsAllProperties()
    {
        // Arrange
        var command = new AlterTableCommand("Orders");

        // Act
        command.AddForeignKey(
            columnName: "CustomerId",
            referencedTable: "Customers",
            referencedColumn: "Id",
            onDelete: ForeignKeyActions.Cascade,
            onUpdate: ForeignKeyActions.SetNull,
            referencedSchema: "dbo",
            constraintName: "FK_Orders_Customers");

        // Assert
        var operation = command.Operations[0];
        var fk = operation.ForeignKeyDefinition;
        fk.ShouldNotBeNull();
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
    public void AddForeignKey_WithNullOrWhitespaceColumnName_ThrowsArgumentException()
    {
        // Arrange
        var command = new AlterTableCommand("Orders");

        // Act & Assert
        Should.Throw<ArgumentException>(() => command.AddForeignKey(null!, "Customers", "Id"));
        Should.Throw<ArgumentException>(() => command.AddForeignKey("", "Customers", "Id"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddForeignKey_WithNullOrWhitespaceReferencedTable_ThrowsArgumentException()
    {
        // Arrange
        var command = new AlterTableCommand("Orders");

        // Act & Assert
        Should.Throw<ArgumentException>(() => command.AddForeignKey("CustomerId", null!, "Id"));
        Should.Throw<ArgumentException>(() => command.AddForeignKey("CustomerId", "", "Id"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddForeignKey_WithNullOrWhitespaceReferencedColumn_ThrowsArgumentException()
    {
        // Arrange
        var command = new AlterTableCommand("Orders");

        // Act & Assert
        Should.Throw<ArgumentException>(() => command.AddForeignKey("CustomerId", "Customers", null!));
        Should.Throw<ArgumentException>(() => command.AddForeignKey("CustomerId", "Customers", ""));
    }

    #endregion

    #region DropConstraint Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DropConstraint_AddsOperationToList()
    {
        // Arrange
        var command = new AlterTableCommand("Orders");

        // Act
        command.DropConstraint("FK_Orders_Customers");

        // Assert
        command.Operations.Count.ShouldBe(1);
        var operation = command.Operations[0];
        operation.OperationType.ShouldBe(AlterTableOperationTypes.DropConstraint);
        operation.ConstraintName.ShouldBe("FK_Orders_Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DropConstraint_ReturnsCommandForChaining()
    {
        // Arrange
        var command = new AlterTableCommand("Orders");

        // Act
        var result = command.DropConstraint("FK_Orders_Customers");

        // Assert
        result.ShouldBe(command);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DropConstraint_WithNullOrWhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var command = new AlterTableCommand("Orders");

        // Act & Assert
        Should.Throw<ArgumentException>(() => command.DropConstraint(null!));
        Should.Throw<ArgumentException>(() => command.DropConstraint(""));
    }

    #endregion

    #region Fluent Chaining Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FluentChaining_AllMethodsReturnCommandInstance()
    {
        // Arrange
        var command = new AlterTableCommand("EmailConfigurations");

        // Act
        var result = command
            .AddColumn("MaxAttachmentSize", SqlDbType.Int, isRequired: true, defaultValue: "10485760")
            .AddColumn("AllowExternalRecipients", SqlDbType.Bit, isRequired: true, defaultValue: "1")
            .ModifyColumn("SmtpHost", SqlDbType.NVarChar, maxLength: 500)
            .DropColumn("OldColumnName");

        // Assert
        result.ShouldBe(command);
        command.Operations.Count.ShouldBe(4);
    }

    #endregion

    #region Complex Scenario Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ComplexScenario_MultipleOperations_CreatesCorrectStructure()
    {
        // Arrange & Act
        var command = new AlterTableCommand("EmailConfigurations")
        {
            SchemaName = "config"
        }
        .AddColumn("MaxAttachmentSize", SqlDbType.Int, isRequired: true, defaultValue: "10485760")
        .AddColumn("AllowExternalRecipients", SqlDbType.Bit, isRequired: true, defaultValue: "1")
        .ModifyColumn("SmtpHost", SqlDbType.NVarChar, maxLength: 500)
        .RenameColumn("OldName", "NewName")
        .DropColumn("ObsoleteColumn")
        .AddForeignKey("ConnectionTypeId", "ConnectionTypes", "Id", onDelete: ForeignKeyActions.Cascade)
        .DropConstraint("FK_Old_Constraint");

        // Assert
        command.TableName.ShouldBe("EmailConfigurations");
        command.SchemaName.ShouldBe("config");
        command.Operations.Count.ShouldBe(7);
        command.Operations.Count(o => o.OperationType == AlterTableOperationTypes.AddColumn).ShouldBe(2);
        command.Operations.Count(o => o.OperationType == AlterTableOperationTypes.ModifyColumn).ShouldBe(1);
        command.Operations.Count(o => o.OperationType == AlterTableOperationTypes.RenameColumn).ShouldBe(1);
        command.Operations.Count(o => o.OperationType == AlterTableOperationTypes.DropColumn).ShouldBe(1);
        command.Operations.Count(o => o.OperationType == AlterTableOperationTypes.AddForeignKey).ShouldBe(1);
        command.Operations.Count(o => o.OperationType == AlterTableOperationTypes.DropConstraint).ShouldBe(1);
    }

    #endregion
}
