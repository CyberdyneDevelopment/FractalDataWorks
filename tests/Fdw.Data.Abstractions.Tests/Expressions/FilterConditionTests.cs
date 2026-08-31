using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Expressions;

public sealed class FilterConditionTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange & Act
        var condition = new FilterCondition
        {
            PropertyName = "TestProperty",
            Operator = new TestFilterOperator(),
            Value = "TestValue"
        };

        // Assert
        condition.PropertyName.ShouldBe("TestProperty");
        condition.Operator.ShouldNotBeNull();
        condition.Value.ShouldBe("TestValue");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CanCreateWithNullValue()
    {
        // Arrange & Act
        var condition = new FilterCondition
        {
            PropertyName = "NullableProperty",
            Operator = new TestFilterOperator(),
            Value = null
        };

        // Assert
        condition.PropertyName.ShouldBe("NullableProperty");
        condition.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RecordEqualityWorksCorrectly()
    {
        // Arrange
        var operator1 = new TestFilterOperator();
        var condition1 = new FilterCondition
        {
            PropertyName = "Name",
            Operator = operator1,
            Value = "Test"
        };

        var condition2 = new FilterCondition
        {
            PropertyName = "Name",
            Operator = operator1,
            Value = "Test"
        };

        // Act & Assert
        condition1.ShouldBe(condition2);
        (condition1 == condition2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RecordInequalityWorksForDifferentPropertyNames()
    {
        // Arrange
        var operator1 = new TestFilterOperator();
        var condition1 = new FilterCondition
        {
            PropertyName = "Name",
            Operator = operator1,
            Value = "Test"
        };

        var condition2 = new FilterCondition
        {
            PropertyName = "DifferentName",
            Operator = operator1,
            Value = "Test"
        };

        // Act & Assert
        condition1.ShouldNotBe(condition2);
        (condition1 != condition2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RecordInequalityWorksForDifferentValues()
    {
        // Arrange
        var operator1 = new TestFilterOperator();
        var condition1 = new FilterCondition
        {
            PropertyName = "Name",
            Operator = operator1,
            Value = "Test1"
        };

        var condition2 = new FilterCondition
        {
            PropertyName = "Name",
            Operator = operator1,
            Value = "Test2"
        };

        // Act & Assert
        condition1.ShouldNotBe(condition2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetHashCodeIsConsistent()
    {
        // Arrange
        var operator1 = new TestFilterOperator();
        var condition = new FilterCondition
        {
            PropertyName = "Name",
            Operator = operator1,
            Value = "Test"
        };

        // Act
        var hash1 = condition.GetHashCode();
        var hash2 = condition.GetHashCode();

        // Assert
        hash1.ShouldBe(hash2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ToStringReturnsValue()
    {
        // Arrange
        var condition = new FilterCondition
        {
            PropertyName = "TestProperty",
            Operator = new TestFilterOperator(),
            Value = 42
        };

        // Act
        var result = condition.ToString();

        // Assert
        result.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIFilterConditionInterface()
    {
        // Arrange
        var condition = new FilterCondition
        {
            PropertyName = "Test",
            Operator = new TestFilterOperator(),
            Value = "Value"
        };

        // Act & Assert
        condition.ShouldBeAssignableTo<IFilterCondition>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIFilterNodeInterface()
    {
        // Arrange
        var condition = new FilterCondition
        {
            PropertyName = "Test",
            Operator = new TestFilterOperator(),
            Value = "Value"
        };

        // Act & Assert
        condition.ShouldBeAssignableTo<IFilterNode>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Regression")]
    public void RecordEqualityAndHashCodeAreValueBasedForDistinctEnumerableValueInstances()
    {
        var operator1 = new TestFilterOperator();
        var condition1 = new FilterCondition
        {
            PropertyName = "Status",
            Operator = operator1,
            Value = new List<string> { "Active", "Pending" }
        };
        var condition2 = new FilterCondition
        {
            PropertyName = "Status",
            Operator = operator1,
            Value = new List<string> { "Active", "Pending" }
        };

        condition1.ShouldBe(condition2);
        condition1.GetHashCode().ShouldBe(condition2.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Regression")]
    public void RecordInequalityDistinguishesDifferentEnumerableValueContent()
    {
        var operator1 = new TestFilterOperator();
        var condition1 = new FilterCondition
        {
            PropertyName = "Status",
            Operator = operator1,
            Value = new List<string> { "Active", "Pending" }
        };
        var condition2 = new FilterCondition
        {
            PropertyName = "Status",
            Operator = operator1,
            Value = new List<string> { "Active", "Review" }
        };

        condition1.ShouldNotBe(condition2);
    }

    private sealed class TestFilterOperator : IFilterOperator
    {
        public int Id => 999;
        object ITypeOption.Id => Id;
        public string Name => "TestOperator";
        public string Category => "Test";
        public string SqlOperator => "=";
        public string ODataOperator => "eq";
        public bool RequiresValue => true;

        public string FormatSqlParameter(string paramName) => $"@{paramName}";
        public string FormatODataValue(object? value) => value?.ToString() ?? "null";
        public string PreprocessSqlValue(string value) => value;
    }
}
