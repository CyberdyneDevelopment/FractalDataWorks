using System.Data;
using Fdw.Schema;
using Fdw.Schema.Ddl.MsSql;
using Fdw.Schema.Properties;
using Shouldly;
using Xunit;

namespace Fdw.Schema.Tests;

public class SchemaDefinitionTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ColumnDefinitionImplementsIPropertyDefinition()
    {
        // Arrange
        var column = new ColumnDefinition
        {
            Name = "TestColumn",
            Role = PropertyRoles.Attribute,
            SqlType = SqlDbType.VarChar,
            MaxLength = 50,
            IsRequired = false
        };

        // Act & Assert
        (column is IPropertyDefinition).ShouldBeTrue();
        column.Name.ShouldBe("TestColumn");
        column.Role.ShouldBe(PropertyRoles.Attribute);
        column.SqlType.ShouldBe(SqlDbType.VarChar);
        column.MaxLength.ShouldBe(50);
        column.IsRequired.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MsSqlDdlGeneratorExists()
    {
        // Arrange & Act
        var generator = new MsSqlDdlGenerator();

        // Assert
        generator.ShouldNotBeNull();
    }
}
