using Fdw.Data;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Expressions;

public sealed class JoinExpressionTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PropertiesArePreserved()
    {
        var sut = new JoinExpression
        {
            TargetContainerName = "OrderItems",
            JoinType = "INNER",
            JoinConditions = [("OrderId", "OrderId")]
        };

        sut.TargetContainerName.ShouldBe("OrderItems");
        sut.JoinType.ShouldBe("INNER");
        sut.JoinConditions.Count.ShouldBe(1);
        sut.JoinConditions[0].LeftField.ShouldBe("OrderId");
        sut.JoinConditions[0].RightField.ShouldBe("OrderId");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MultipleJoinConditionsAreSupported()
    {
        var sut = new JoinExpression
        {
            TargetContainerName = "Products",
            JoinType = "LEFT",
            JoinConditions =
            [
                ("ProductId", "Id"),
                ("TenantId", "TenantId")
            ]
        };

        sut.JoinConditions.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIJoinExpression()
    {
        var sut = new JoinExpression
        {
            TargetContainerName = "X",
            JoinType = "INNER",
            JoinConditions = []
        };
        sut.ShouldBeAssignableTo<IJoinExpression>();
    }
}
