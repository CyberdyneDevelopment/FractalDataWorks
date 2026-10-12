using Shouldly;
using Xunit;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataContainers.Abstractions;
using System;
using Moq;

namespace Fdw.Data.DataSets.Abstractions.Tests;

public sealed class CalculatedDataFieldTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesPropertiesWithValidInput()
    {
        // Arrange
        Func<IDataRow, object> calculator = row => 42;

        // Act
        var field = new CalculatedDataField(
            name: "CalculatedTotal",
            type: typeof(decimal),
            calculator: calculator,
            description: "Sum of all values");

        // Assert
        field.Name.ShouldBe("CalculatedTotal");
        field.FieldType.ShouldBe(typeof(decimal));
        field.Calculator.ShouldBe(calculator);
        field.Description.ShouldBe("Sum of all values");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenNameIsNull()
    {
        // Arrange
        Func<IDataRow, object> calculator = row => 42;

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => new CalculatedDataField(
            name: null!,
            type: typeof(int),
            calculator: calculator));

        exception.ParamName.ShouldBe("name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenNameIsEmpty()
    {
        // Arrange
        Func<IDataRow, object> calculator = row => 42;

        // Act & Assert
        Should.Throw<ArgumentException>(() => new CalculatedDataField(
            name: string.Empty,
            type: typeof(int),
            calculator: calculator));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenNameIsWhitespace()
    {
        // Arrange
        Func<IDataRow, object> calculator = row => 42;

        // Act & Assert
        Should.Throw<ArgumentException>(() => new CalculatedDataField(
            name: "   ",
            type: typeof(int),
            calculator: calculator));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenTypeIsNull()
    {
        // Arrange
        Func<IDataRow, object> calculator = row => 42;

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() => new CalculatedDataField(
            name: "Field1",
            type: null!,
            calculator: calculator));

        exception.ParamName.ShouldBe("type");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenCalculatorIsNull()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() => new CalculatedDataField(
            name: "Field1",
            type: typeof(int),
            calculator: null!));

        exception.ParamName.ShouldBe("calculator");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsCalculatedReturnsTrue()
    {
        // Arrange
        var field = new CalculatedDataField(
            name: "Calc",
            type: typeof(int),
            calculator: row => 0);

        // Act & Assert
        field.IsCalculated.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsKeyReturnsFalse()
    {
        // Arrange
        var field = new CalculatedDataField(
            name: "Calc",
            type: typeof(int),
            calculator: row => 0);

        // Act & Assert
        field.IsKey.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsRequiredReturnsFalse()
    {
        // Arrange
        var field = new CalculatedDataField(
            name: "Calc",
            type: typeof(int),
            calculator: row => 0);

        // Act & Assert
        field.IsRequired.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MaxLengthReturnsNull()
    {
        // Arrange
        var field = new CalculatedDataField(
            name: "Calc",
            type: typeof(string),
            calculator: row => "test");

        // Act & Assert
        field.MaxLength.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultValueReturnsNull()
    {
        // Arrange
        var field = new CalculatedDataField(
            name: "Calc",
            type: typeof(int),
            calculator: row => 0);

        // Act & Assert
        field.DefaultValue.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DescriptionIsNullWhenNotProvided()
    {
        // Arrange
        var field = new CalculatedDataField(
            name: "Calc",
            type: typeof(int),
            calculator: row => 0);

        // Act & Assert
        field.Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CalculatorReturnsProvidedFunction()
    {
        // Arrange
        var mockRow = Mock.Of<IDataRow>();
        Func<IDataRow, object> calculator = row => 42;
        var field = new CalculatedDataField(
            name: "Calc",
            type: typeof(int),
            calculator: calculator);

        // Act
        var result = field.Calculator!(mockRow);

        // Assert
        result.ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CalculatorCanAccessDataRow()
    {
        // Arrange
        var mockRow = new Mock<IDataRow>();
        mockRow.Setup(r => r.GetValue<decimal>("Price")).Returns(100m);
        mockRow.Setup(r => r.GetValue<int>("Quantity")).Returns(5);

        Func<IDataRow, object> calculator = row => row.GetValue<decimal>("Price") * row.GetValue<int>("Quantity");

        var field = new CalculatedDataField(
            name: "Total",
            type: typeof(decimal),
            calculator: calculator);

        // Act
        var result = field.Calculator!(mockRow.Object);

        // Assert
        result.ShouldBe(500m);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIDataFieldInterface()
    {
        // Arrange
        var field = new CalculatedDataField(
            name: "Calc",
            type: typeof(int),
            calculator: row => 0);

        // Assert
        field.ShouldBeAssignableTo<IDataField>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FieldTypeReturnsProvidedType()
    {
        // Arrange
        var field = new CalculatedDataField(
            name: "Calc",
            type: typeof(DateTime),
            calculator: row => DateTime.Now);

        // Act & Assert
        field.FieldType.ShouldBe(typeof(DateTime));
    }
}
