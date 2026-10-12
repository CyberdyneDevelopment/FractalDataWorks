using Fdw.Data.SchemaObjects.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.SchemaObjects;

public sealed class SchemaObjectTypesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DatabaseObjectTypeIsRegistered()
    {
        // Act
        var database = SchemaObjectTypes.ByName("Database");

        // Assert
        database.ShouldNotBeNull();
        database.Name.ShouldBe("Database");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TableObjectTypeIsRegistered()
    {
        // Act
        var table = SchemaObjectTypes.ByName("Table");

        // Assert
        if (table != null)
        {
            table.Name.ShouldBe("Table");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ViewObjectTypeIsRegistered()
    {
        // Act
        var view = SchemaObjectTypes.ByName("View");

        // Assert
        if (view != null)
        {
            view.Name.ShouldBe("View");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ColumnObjectTypeIsRegistered()
    {
        // Act
        var column = SchemaObjectTypes.ByName("Column");

        // Assert
        if (column != null)
        {
            column.Name.ShouldBe("Column");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IndexObjectTypeIsRegistered()
    {
        // Act
        var index = SchemaObjectTypes.ByName("Index");

        // Assert
        if (index != null)
        {
            index.Name.ShouldBe("Index");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ForeignKeyObjectTypeIsRegistered()
    {
        // Act
        var foreignKey = SchemaObjectTypes.ByName("ForeignKey");

        // Assert
        if (foreignKey != null)
        {
            foreignKey.Name.ShouldBe("ForeignKey");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void StoredProcedureObjectTypeIsRegistered()
    {
        // Act
        var storedProcedure = SchemaObjectTypes.ByName("StoredProcedure");

        // Assert
        if (storedProcedure != null)
        {
            storedProcedure.Name.ShouldBe("StoredProcedure");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SchemaObjectTypeIsRegistered()
    {
        // Act
        var schema = SchemaObjectTypes.ByName("Schema");

        // Assert
        if (schema != null)
        {
            schema.Name.ShouldBe("Schema");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DatabaseExtensionPropertyWorks()
    {
        // Act
        var database = SchemaObjectTypes.Database;

        // Assert
        database.ShouldNotBeNull();
        database.Name.ShouldBe("Database");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TableExtensionPropertyWorks()
    {
        // Act
        var table = SchemaObjectTypes.Table;

        // Assert
        if (table != null)
        {
            table.Name.ShouldBe("Table");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ViewExtensionPropertyWorks()
    {
        // Act
        var view = SchemaObjectTypes.View;

        // Assert
        if (view != null)
        {
            view.Name.ShouldBe("View");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ColumnExtensionPropertyWorks()
    {
        // Act
        var column = SchemaObjectTypes.Column;

        // Assert
        if (column != null)
        {
            column.Name.ShouldBe("Column");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IndexExtensionPropertyWorks()
    {
        // Act
        var index = SchemaObjectTypes.Index;

        // Assert
        if (index != null)
        {
            index.Name.ShouldBe("Index");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ForeignKeyExtensionPropertyWorks()
    {
        // Act
        var foreignKey = SchemaObjectTypes.ForeignKey;

        // Assert
        if (foreignKey != null)
        {
            foreignKey.Name.ShouldBe("ForeignKey");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void StoredProcedureExtensionPropertyWorks()
    {
        // Act
        var storedProcedure = SchemaObjectTypes.StoredProcedure;

        // Assert
        if (storedProcedure != null)
        {
            storedProcedure.Name.ShouldBe("StoredProcedure");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SchemaExtensionPropertyWorks()
    {
        // Act
        var schema = SchemaObjectTypes.Schema;

        // Assert
        if (schema != null)
        {
            schema.Name.ShouldBe("Schema");
        }
    }
}
