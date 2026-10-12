using Fdw.Data;
using Fdw.Data.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Expressions;

/// <summary>
/// Tests for FilterExpressionExtensions fluent factory methods.
/// </summary>
public sealed class FilterExpressionExtensionsTests
{
    // =========================================================================
    // Equal(string, object)
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EqualStringProducesFilterConditionWithCorrectPropertyName()
    {
        var result = FilterExpressionExtensions.Equal("Status", "Active");

        result.ShouldNotBeNull();
        var condition = result.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("Status");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EqualStringProducesFilterConditionWithCorrectValue()
    {
        var result = FilterExpressionExtensions.Equal("Count", 42);

        var condition = result.Root.ShouldBeOfType<FilterCondition>();
        condition.Value.ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EqualStringProducesEqualOperator()
    {
        var result = FilterExpressionExtensions.Equal("Name", "Acme");

        var condition = result.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.SqlOperator.ShouldBe("=");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EqualStringImplementsIFilterExpression()
    {
        var result = FilterExpressionExtensions.Equal("X", 1);

        result.ShouldBeAssignableTo<IFilterExpression>();
    }

    // =========================================================================
    // Equal(IDataField, object)
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EqualDataFieldUsesFieldName()
    {
        var mockField = new Mock<IDataField>();
        mockField.SetupGet(f => f.Name).Returns("CustomerId");

        var result = FilterExpressionExtensions.Equal(mockField.Object, Guid.NewGuid());

        var condition = result.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("CustomerId");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EqualNullDataFieldThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            FilterExpressionExtensions.Equal((IDataField)null!, "value"));
    }

    // =========================================================================
    // And(params FilterExpression[])
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AndSinglePredicateReturnsSamePredicate()
    {
        var predicate = FilterExpressionExtensions.Equal("A", 1);

        var result = FilterExpressionExtensions.And(predicate);

        result.ShouldBe(predicate);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AndTwoPredicatesProducesFilterGroupWithAndOperator()
    {
        var left = FilterExpressionExtensions.Equal("Status", "Active");
        var right = FilterExpressionExtensions.Equal("Region", "US");

        var result = FilterExpressionExtensions.And(left, right);

        var group = result.Root.ShouldBeOfType<FilterGroup>();
        group.Operator.ShouldBe(LogicalOperator.And);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AndTwoPredicatesIncludesBothNodes()
    {
        var left = FilterExpressionExtensions.Equal("A", 1);
        var right = FilterExpressionExtensions.Equal("B", 2);

        var result = FilterExpressionExtensions.And(left, right);

        var group = result.Root.ShouldBeOfType<FilterGroup>();
        group.Nodes.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AndEmptyPredicatesThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            FilterExpressionExtensions.And());
    }

    // =========================================================================
    // And for composite keys — multi-column scenario
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AndThreePredicatesProducesGroupWithThreeNodes()
    {
        var p1 = FilterExpressionExtensions.Equal("TenantId", 1);
        var p2 = FilterExpressionExtensions.Equal("Year", 2024);
        var p3 = FilterExpressionExtensions.Equal("Month", 11);

        var result = FilterExpressionExtensions.And(p1, p2, p3);

        var group = result.Root.ShouldBeOfType<FilterGroup>();
        group.Nodes.Count.ShouldBe(3);
    }

    // =========================================================================
    // Or(params FilterExpression[])
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void OrSinglePredicateReturnsSamePredicate()
    {
        var predicate = FilterExpressionExtensions.Equal("Status", "Active");

        var result = FilterExpressionExtensions.Or(predicate);

        result.ShouldBe(predicate);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void OrTwoPredicatesProducesFilterGroupWithOrOperator()
    {
        var left = FilterExpressionExtensions.Equal("Status", "Active");
        var right = FilterExpressionExtensions.Equal("Status", "Pending");

        var result = FilterExpressionExtensions.Or(left, right);

        var group = result.Root.ShouldBeOfType<FilterGroup>();
        group.Operator.ShouldBe(LogicalOperator.Or);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void OrEmptyPredicatesThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            FilterExpressionExtensions.Or());
    }

    // =========================================================================
    // AndAlso / OrElse instance extension methods
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AndAlsoChainsCombinesWithAnd()
    {
        var left = FilterExpressionExtensions.Equal("A", 1);
        var right = FilterExpressionExtensions.Equal("B", 2);

        var result = left.AndAlso(right);

        var group = result.Root.ShouldBeOfType<FilterGroup>();
        group.Operator.ShouldBe(LogicalOperator.And);
        group.Nodes.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void OrElseCombinesWithOr()
    {
        var left = FilterExpressionExtensions.Equal("Status", "Active");
        var right = FilterExpressionExtensions.Equal("Status", "Disabled");

        var result = left.OrElse(right);

        var group = result.Root.ShouldBeOfType<FilterGroup>();
        group.Operator.ShouldBe(LogicalOperator.Or);
        group.Nodes.Count.ShouldBe(2);
    }
}
