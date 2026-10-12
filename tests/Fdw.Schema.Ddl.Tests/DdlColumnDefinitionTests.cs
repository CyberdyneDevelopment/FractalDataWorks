using Fdw.Schema.Ddl.Definitions;

namespace Fdw.Schema.Ddl.Tests;

public class DdlColumnDefinitionTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFullSqlTypeReturnsBaseTypeWhenNoModifiers()
    {
        var column = new DdlColumnDefinition { Name = "Id", SqlType = "INT" };

        column.GetFullSqlType().ShouldBe("INT");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFullSqlTypeAppendsMaxLength()
    {
        var column = new DdlColumnDefinition { Name = "Name", SqlType = "VARCHAR", MaxLength = 255 };

        column.GetFullSqlType().ShouldBe("VARCHAR(255)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFullSqlTypeAppendsMaxForNegativeOneLength()
    {
        var column = new DdlColumnDefinition { Name = "Data", SqlType = "VARCHAR", MaxLength = -1 };

        column.GetFullSqlType().ShouldBe("VARCHAR(MAX)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFullSqlTypeAppendsPrecisionOnly()
    {
        var column = new DdlColumnDefinition { Name = "Score", SqlType = "DECIMAL", Precision = 18 };

        column.GetFullSqlType().ShouldBe("DECIMAL(18)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFullSqlTypeAppendsPrecisionAndScale()
    {
        var column = new DdlColumnDefinition { Name = "Price", SqlType = "DECIMAL", Precision = 18, Scale = 2 };

        column.GetFullSqlType().ShouldBe("DECIMAL(18,2)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MaxLengthTakesPriorityOverPrecision()
    {
        var column = new DdlColumnDefinition { Name = "Test", SqlType = "VARCHAR", MaxLength = 100, Precision = 18 };

        column.GetFullSqlType().ShouldBe("VARCHAR(100)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultValuesAreCorrect()
    {
        var column = new DdlColumnDefinition { Name = "Col", SqlType = "INT" };

        column.IsNullable.ShouldBeTrue();
        column.IsIdentity.ShouldBeFalse();
        column.IsUnique.ShouldBeFalse();
        column.DefaultValue.ShouldBeNull();
        column.Collation.ShouldBeNull();
        column.ComputedExpression.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InitPropertiesSetCorrectly()
    {
        var column = new DdlColumnDefinition
        {
            Name = "Id",
            SqlType = "UNIQUEIDENTIFIER",
            IsNullable = false,
            DefaultValue = "NEWSEQUENTIALID()"
        };

        column.Name.ShouldBe("Id");
        column.SqlType.ShouldBe("UNIQUEIDENTIFIER");
        column.IsNullable.ShouldBeFalse();
        column.DefaultValue.ShouldBe("NEWSEQUENTIALID()");
    }
}
