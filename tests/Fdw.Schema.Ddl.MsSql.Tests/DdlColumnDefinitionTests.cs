using Fdw.Schema.Ddl.Definitions;

namespace Fdw.Schema.Ddl.MsSql.Tests;

/// <summary>
/// Tests for <see cref="DdlColumnDefinition.GetFullSqlType"/> method and properties.
/// </summary>
public sealed class DdlColumnDefinitionTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFullSqlTypeReturnsBaseTypeWhenNoModifiers()
    {
        var col = new DdlColumnDefinition { Name = "Id", SqlType = "INT" };

        col.GetFullSqlType().ShouldBe("INT");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFullSqlTypeAppendsMaxLengthInParentheses()
    {
        var col = new DdlColumnDefinition { Name = "Name", SqlType = "VARCHAR", MaxLength = 200 };

        col.GetFullSqlType().ShouldBe("VARCHAR(200)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFullSqlTypeAppendsMaxForNegativeOneLength()
    {
        var col = new DdlColumnDefinition { Name = "Data", SqlType = "VARCHAR", MaxLength = -1 };

        col.GetFullSqlType().ShouldBe("VARCHAR(MAX)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFullSqlTypeAppendsPrecisionOnly()
    {
        var col = new DdlColumnDefinition { Name = "Value", SqlType = "DECIMAL", Precision = 18 };

        col.GetFullSqlType().ShouldBe("DECIMAL(18)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFullSqlTypeAppendsPrecisionAndScale()
    {
        var col = new DdlColumnDefinition { Name = "Amount", SqlType = "DECIMAL", Precision = 18, Scale = 4 };

        col.GetFullSqlType().ShouldBe("DECIMAL(18,4)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFullSqlTypeMaxLengthTakesPriorityOverPrecision()
    {
        // When MaxLength is set, Precision is ignored (the if/else if structure)
        var col = new DdlColumnDefinition { Name = "Col", SqlType = "DECIMAL", MaxLength = 100, Precision = 18 };

        col.GetFullSqlType().ShouldBe("DECIMAL(100)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFullSqlTypeScaleWithoutPrecisionIsIgnored()
    {
        // Scale alone without Precision does nothing
        var col = new DdlColumnDefinition { Name = "Col", SqlType = "INT", Scale = 2 };

        col.GetFullSqlType().ShouldBe("INT");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFullSqlTypeUniqueIdentifierHasNoModifiers()
    {
        var col = new DdlColumnDefinition { Name = "RowId", SqlType = "UNIQUEIDENTIFIER" };

        col.GetFullSqlType().ShouldBe("UNIQUEIDENTIFIER");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFullSqlTypeVarbinaryWithMax()
    {
        var col = new DdlColumnDefinition { Name = "BinaryData", SqlType = "VARBINARY", MaxLength = -1 };

        col.GetFullSqlType().ShouldBe("VARBINARY(MAX)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFullSqlTypeNvarcharWithLength()
    {
        var col = new DdlColumnDefinition { Name = "UnicodeText", SqlType = "NVARCHAR", MaxLength = 4000 };

        col.GetFullSqlType().ShouldBe("NVARCHAR(4000)");
    }

    // --- Property defaults ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultIsNullableIsTrue()
    {
        var col = new DdlColumnDefinition { Name = "Col", SqlType = "INT" };

        col.IsNullable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultIsIdentityIsFalse()
    {
        var col = new DdlColumnDefinition { Name = "Col", SqlType = "INT" };

        col.IsIdentity.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultIsUniqueIsFalse()
    {
        var col = new DdlColumnDefinition { Name = "Col", SqlType = "INT" };

        col.IsUnique.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultValueIsNull()
    {
        var col = new DdlColumnDefinition { Name = "Col", SqlType = "INT" };

        col.DefaultValue.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CollationIsNullByDefault()
    {
        var col = new DdlColumnDefinition { Name = "Col", SqlType = "VARCHAR" };

        col.Collation.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ComputedExpressionIsNullByDefault()
    {
        var col = new DdlColumnDefinition { Name = "Col", SqlType = "INT" };

        col.ComputedExpression.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllPropertiesCanBeSet()
    {
        var col = new DdlColumnDefinition
        {
            Name = "Amount",
            SqlType = "DECIMAL",
            MaxLength = null,
            Precision = 18,
            Scale = 4,
            IsNullable = false,
            IsIdentity = false,
            IsUnique = true,
            DefaultValue = "0",
            Collation = "Latin1_General_CI_AS",
            ComputedExpression = null
        };

        col.Name.ShouldBe("Amount");
        col.SqlType.ShouldBe("DECIMAL");
        col.Precision.ShouldBe(18);
        col.Scale.ShouldBe(4);
        col.IsNullable.ShouldBeFalse();
        col.IsUnique.ShouldBeTrue();
        col.DefaultValue.ShouldBe("0");
        col.Collation.ShouldBe("Latin1_General_CI_AS");
    }
}
