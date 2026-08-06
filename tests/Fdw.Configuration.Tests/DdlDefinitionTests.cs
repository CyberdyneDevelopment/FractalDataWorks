using Fdw.Configuration.Persistence.Schema;

namespace Fdw.Configuration.Tests;

/// <summary>
/// Tests for DdlDefinition, ColumnDefinition, IndexDefinition, and ForeignKeyDefinition.
/// </summary>
public class DdlDefinitionTests
{
    #region DdlDefinition Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DdlDefinition_DefaultValues_AreCorrect()
    {
        // Act
        var ddl = new DdlDefinition();

        // Assert
        ddl.Schema.ShouldBe("cfg");
        ddl.TableName.ShouldBe("");
        ddl.ConfigurationTypeName.ShouldBe("");
        ddl.Columns.ShouldNotBeNull();
        ddl.Columns.ShouldBeEmpty();
        ddl.Indexes.ShouldNotBeNull();
        ddl.Indexes.ShouldBeEmpty();
        ddl.ForeignKeys.ShouldNotBeNull();
        ddl.ForeignKeys.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DdlDefinition_FullTableName_ReturnsFormattedName()
    {
        // Arrange
        var ddl = new DdlDefinition { Schema = "cfg", TableName = "Connection" };

        // Act & Assert
        ddl.FullTableName.ShouldBe("[cfg].[Connection]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DdlDefinition_FullTableName_WithCustomSchema()
    {
        // Arrange
        var ddl = new DdlDefinition { Schema = "auth", TableName = "User" };

        // Assert
        ddl.FullTableName.ShouldBe("[auth].[User]");
    }

    #endregion

    #region ColumnDefinition Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ColumnDefinition_DefaultValues_AreCorrect()
    {
        // Act
        var col = new ColumnDefinition();

        // Assert
        col.Name.ShouldBe("");
        col.SqlType.ShouldBe("");
        col.MaxLength.ShouldBeNull();
        col.Precision.ShouldBeNull();
        col.Scale.ShouldBeNull();
        col.IsNullable.ShouldBeFalse();
        col.IsIdentity.ShouldBeFalse();
        col.IsUnique.ShouldBeFalse();
        col.DefaultValue.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ColumnDefinition_GetFullSqlType_WithMaxLength_ReturnsLengthSuffix()
    {
        // Arrange
        var col = new ColumnDefinition { SqlType = "nvarchar", MaxLength = 100 };

        // Act & Assert
        col.GetFullSqlType().ShouldBe("nvarchar(100)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ColumnDefinition_GetFullSqlType_WithMaxLengthMax_ReturnsMax()
    {
        // Arrange
        var col = new ColumnDefinition { SqlType = "nvarchar", MaxLength = -1 };

        // Act & Assert
        col.GetFullSqlType().ShouldBe("nvarchar(max)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ColumnDefinition_GetFullSqlType_WithPrecisionAndScale_ReturnsFormatted()
    {
        // Arrange
        var col = new ColumnDefinition { SqlType = "decimal", Precision = 18, Scale = 2 };

        // Act & Assert
        col.GetFullSqlType().ShouldBe("decimal(18,2)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ColumnDefinition_GetFullSqlType_WithPrecisionOnly_ReturnsFormatted()
    {
        // Arrange
        var col = new ColumnDefinition { SqlType = "decimal", Precision = 10 };

        // Act & Assert
        col.GetFullSqlType().ShouldBe("decimal(10)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ColumnDefinition_GetFullSqlType_WithNoSuffix_ReturnsBaseType()
    {
        // Arrange
        var col = new ColumnDefinition { SqlType = "int" };

        // Act & Assert
        col.GetFullSqlType().ShouldBe("int");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ColumnDefinition_GetFullSqlType_WithUniqueidentifier_ReturnsBaseType()
    {
        // Arrange
        var col = new ColumnDefinition { SqlType = "uniqueidentifier" };

        // Act & Assert
        col.GetFullSqlType().ShouldBe("uniqueidentifier");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ColumnDefinition_GetFullSqlType_MaxLengthTakesPrecedenceOverPrecision()
    {
        // Arrange - If MaxLength is set, it takes precedence
        var col = new ColumnDefinition { SqlType = "varchar", MaxLength = 50, Precision = 10, Scale = 2 };

        // Act & Assert
        col.GetFullSqlType().ShouldBe("varchar(50)");
    }

    #endregion

    #region IndexDefinition Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IndexDefinition_DefaultValues_AreCorrect()
    {
        // Act
        var idx = new IndexDefinition();

        // Assert
        idx.Name.ShouldBe("");
        idx.Columns.ShouldBeEmpty();
        idx.IsUnique.ShouldBeFalse();
        idx.IsClustered.ShouldBeFalse();
        idx.IncludeColumns.ShouldBeNull();
        idx.FilterPredicate.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IndexDefinition_CanSetAllProperties()
    {
        // Act
        var idx = new IndexDefinition
        {
            Name = "IX_Connection_Name",
            Columns = ["Name", "ServiceType"],
            IsUnique = true,
            IsClustered = false,
            IncludeColumns = ["Id"],
            FilterPredicate = "[IsCurrent] = 1"
        };

        // Assert
        idx.Name.ShouldBe("IX_Connection_Name");
        idx.Columns.Length.ShouldBe(2);
        idx.IsUnique.ShouldBeTrue();
        idx.IncludeColumns!.Length.ShouldBe(1);
        idx.FilterPredicate.ShouldBe("[IsCurrent] = 1");
    }

    #endregion

    #region ForeignKeyDefinition Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ForeignKeyDefinition_DefaultValues_AreCorrect()
    {
        // Act
        var fk = new ForeignKeyDefinition();

        // Assert
        fk.Name.ShouldBe("");
        fk.Column.ShouldBe("");
        fk.ReferencedSchema.ShouldBe("");
        fk.ReferencedTable.ShouldBe("");
        fk.ReferencedColumn.ShouldBe("");
        fk.OnDelete.ShouldBe(ForeignKeyActions.NoAction);
        fk.OnUpdate.ShouldBe(ForeignKeyActions.NoAction);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ForeignKeyDefinition_CanSetCascadeActions()
    {
        // Act
        var fk = new ForeignKeyDefinition
        {
            Name = "FK_MsSqlConnection_Connection",
            Column = "ConnectionId",
            ReferencedSchema = "cfg",
            ReferencedTable = "Connection",
            ReferencedColumn = "Id",
            OnDelete = ForeignKeyActions.Cascade,
            OnUpdate = ForeignKeyActions.SetNull
        };

        // Assert
        fk.OnDelete.ShouldBe(ForeignKeyActions.Cascade);
        fk.OnUpdate.ShouldBe(ForeignKeyActions.SetNull);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ForeignKeyAction_HasExpectedValues()
    {
        // Assert
        ForeignKeyActions.All().Count.ShouldBe(4);
        ForeignKeyActions.NoAction.Id.ShouldBe(1);
        ForeignKeyActions.Cascade.Id.ShouldBe(2);
        ForeignKeyActions.SetNull.Id.ShouldBe(3);
        ForeignKeyActions.SetDefault.Id.ShouldBe(4);
    }

    #endregion
}
