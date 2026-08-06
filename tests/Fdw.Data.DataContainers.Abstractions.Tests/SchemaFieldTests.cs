using Fdw.Data.DataContainers.Abstractions.Results;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataContainers.Abstractions.Tests;

public class SchemaFieldTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange & Act
        var field = new SchemaField("TestField", typeof(string), 5);

        // Assert
        field.Name.ShouldBe("TestField");
        field.DisplayName.ShouldBe("TestField");
        field.DataType.ShouldBe(typeof(string));
        field.Ordinal.ShouldBe(5);
        field.IsRequired.ShouldBeFalse();
        field.IsIndexed.ShouldBeFalse();
        field.MaxLength.ShouldBeNull();
        field.DefaultValue.ShouldBeNull();
        field.Description.ShouldBeNull();
        field.Constraints.ShouldBeEmpty();
        field.Metadata.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenNameIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SchemaField(null!, typeof(string), 0));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenDataTypeIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SchemaField("Field", null!, 0));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void InitPropertiesSetCorrectly()
    {
        // Arrange & Act
        var field = new SchemaField("TestField", typeof(string), 0)
        {
            IsRequired = true,
            IsIndexed = true,
            MaxLength = 50,
            DefaultValue = "default",
            Description = "Test description"
        };

        // Assert
        field.IsRequired.ShouldBeTrue();
        field.IsIndexed.ShouldBeTrue();
        field.MaxLength.ShouldBe(50);
        field.DefaultValue.ShouldBe("default");
        field.Description.ShouldBe("Test description");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateValueReturnsSuccessForValidValue()
    {
        // Arrange
        var field = new SchemaField("TestField", typeof(string), 0)
        {
            IsRequired = false
        };

        // Act
        var result = field.ValidateValue("test");

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateValueReturnsFailureForNullWhenRequired()
    {
        // Arrange
        var field = new SchemaField("TestField", typeof(string), 0)
        {
            IsRequired = true
        };

        // Act
        var result = field.ValidateValue(null);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code.Name.ShouldBe("FieldRequired");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateValueReturnsSuccessForNullWhenNotRequired()
    {
        // Arrange
        var field = new SchemaField("TestField", typeof(string), 0)
        {
            IsRequired = false
        };

        // Act
        var result = field.ValidateValue(null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateValueReturnsFailureForTypeMismatch()
    {
        // Arrange
        var field = new SchemaField("TestField", typeof(int), 0);

        // Act
        var result = field.ValidateValue("not an int");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code.Name.ShouldBe("FieldTypeMismatch");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateValueReturnsSuccessForCorrectType()
    {
        // Arrange
        var field = new SchemaField("TestField", typeof(int), 0);

        // Act
        var result = field.ValidateValue(42);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConvertValueReturnsSuccessForValidValue()
    {
        // Arrange
        var field = new SchemaField("TestField", typeof(int), 0);

        // Act
        var result = field.ConvertValue(42);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConvertValueConvertsStringToInt()
    {
        // Arrange
        var field = new SchemaField("TestField", typeof(int), 0);

        // Act
        var result = field.ConvertValue("42");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConvertValueReturnsFailureForInvalidConversion()
    {
        // Arrange
        var field = new SchemaField("TestField", typeof(int), 0);

        // Act
        var result = field.ConvertValue("not a number");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code.Name.ShouldBe("FieldConversionFailed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConvertValueReturnsSuccessForNullWhenNotRequired()
    {
        // Arrange
        var field = new SchemaField("TestField", typeof(int), 0)
        {
            IsRequired = false
        };

        // Act
        var result = field.ConvertValue(null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConvertValueReturnsFailureForNullWhenRequired()
    {
        // Arrange
        var field = new SchemaField("TestField", typeof(int), 0)
        {
            IsRequired = true
        };

        // Act
        var result = field.ConvertValue(null);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code.Name.ShouldBe("FieldRequired");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConvertValueHandlesDoubleToDecimal()
    {
        // Arrange
        var field = new SchemaField("TestField", typeof(decimal), 0);

        // Act
        var result = field.ConvertValue(42.5);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42.5m);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConvertValueHandlesBooleanToString()
    {
        // Arrange
        var field = new SchemaField("TestField", typeof(string), 0);

        // Act
        var result = field.ConvertValue(true);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("True");
    }
}
