using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Converters;

public sealed class DataTypeConverterBaseTests
{

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        // Arrange & Act
        var converter = new TestDataTypeConverter("IntConverter");

        // Assert
        converter.Name.ShouldBe("IntConverter");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsSourceType()
    {
        // Arrange & Act
        var converter = new TestDataTypeConverter("IntConverter", sourceType: "int");

        // Assert
        converter.SourceType.ShouldBe("int");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsTargetClrType()
    {
        // Arrange & Act
        var converter = new TestDataTypeConverter("IntConverter", targetClrType: typeof(int));

        // Assert
        converter.TargetClrType.ShouldBe(typeof(int));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsDbType()
    {
        // Arrange & Act
        var converter = new TestDataTypeConverter("IntConverter", dbType: DbType.Int32);

        // Assert
        converter.DbType.ShouldBe(DbType.Int32);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SizeDefaultsToNull()
    {
        // Arrange & Act
        var converter = new TestDataTypeConverter("IntConverter");

        // Assert
        converter.Size.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PrecisionDefaultsToNull()
    {
        // Arrange & Act
        var converter = new TestDataTypeConverter("IntConverter");

        // Assert
        converter.Precision.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ScaleDefaultsToNull()
    {
        // Arrange & Act
        var converter = new TestDataTypeConverter("IntConverter");

        // Assert
        converter.Scale.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToClrConvertsDbValueToClr()
    {
        // Arrange
        var converter = new TestDataTypeConverter("IntConverter");
        var dbValue = "42";

        // Act
        var result = converter.ToClr(dbValue);

        // Assert
        result.ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToDbConvertsClrValueToDb()
    {
        // Arrange
        var converter = new TestDataTypeConverter("IntConverter");
        var clrValue = 42;

        // Act
        var result = converter.ToDb(clrValue);

        // Assert
        result.ShouldBe("42");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToClrHandlesNullValue()
    {
        // Arrange
        var converter = new TestDataTypeConverter("IntConverter");

        // Act
        var result = converter.ToClr(null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToDbHandlesNullValue()
    {
        // Arrange
        var converter = new TestDataTypeConverter("IntConverter");

        // Act
        var result = converter.ToDb(null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIDataTypeConverter()
    {
        // Arrange
        var converter = new TestDataTypeConverter("IntConverter");

        // Act & Assert
        converter.ShouldBeAssignableTo<IDataTypeConverter>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromTypeOptionBase()
    {
        // Arrange
        var converter = new TestDataTypeConverter("IntConverter");

        // Act & Assert
        converter.ShouldBeAssignableTo<DataTypeConverterBase>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void StringConverterConfiguration()
    {
        // Arrange & Act
        var converter = new TestDataTypeConverter(
            "StringConverter",
            sourceType: "nvarchar",
            targetClrType: typeof(string),
            dbType: DbType.String);

        // Assert
        converter.SourceType.ShouldBe("nvarchar");
        converter.TargetClrType.ShouldBe(typeof(string));
        converter.DbType.ShouldBe(DbType.String);
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestDataTypeConverter : DataTypeConverterBase
    {
        public TestDataTypeConverter(
            string name,
            string sourceType = "int",
            Type? targetClrType = null,
            DbType dbType = DbType.Int32)
            : base(name, sourceType, targetClrType ?? typeof(int), dbType)
        {
        }

        public override object? ToClr(object? dbValue)
        {
            if (dbValue == null) return null;
            if (dbValue is string str) return int.Parse(str);
            return dbValue;
        }

        public override object? ToDb(object? clrValue)
        {
            if (clrValue == null) return null;
            return clrValue.ToString();
        }
    }
}
