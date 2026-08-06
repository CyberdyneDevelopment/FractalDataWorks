using Fdw.Data;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Expressions;

public sealed class FilterExpressionTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RootCanBeNull()
    {
        var sut = new FilterExpression { Root = null };
        sut.Root.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RootCanBeFilterCondition()
    {
        var condition = new FilterCondition
        {
            PropertyName = "Name",
            Operator = new EqualOperator(),
            Value = "Acme"
        };

        var sut = new FilterExpression { Root = condition };
        sut.Root.ShouldBe(condition);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RootCanBeFilterGroup()
    {
        var group = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterCondition { PropertyName = "A", Operator = new EqualOperator(), Value = 1 },
                new FilterCondition { PropertyName = "B", Operator = new EqualOperator(), Value = 2 }
            ]
        };

        var sut = new FilterExpression { Root = group };
        sut.Root.ShouldBe(group);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIFilterExpression()
    {
        var sut = new FilterExpression { Root = null };
        sut.ShouldBeAssignableTo<IFilterExpression>();
    }
}
