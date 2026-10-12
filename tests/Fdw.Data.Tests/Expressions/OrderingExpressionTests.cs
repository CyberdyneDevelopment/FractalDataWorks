using Fdw.Data;
using Fdw.Data.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Expressions;

public sealed class OrderingExpressionTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OrderedFieldsArePreserved()
    {
        var ascending = new AscendingDirection();
        var descending = new DescendingDirection();

        var field1 = new Mock<IOrderedField>();
        field1.Setup(f => f.PropertyName).Returns("Name");
        field1.Setup(f => f.Direction).Returns(ascending);

        var field2 = new Mock<IOrderedField>();
        field2.Setup(f => f.PropertyName).Returns("Id");
        field2.Setup(f => f.Direction).Returns(descending);

        var sut = new OrderingExpression
        {
            OrderedFields = [field1.Object, field2.Object]
        };

        sut.OrderedFields.Count.ShouldBe(2);
        sut.OrderedFields[0].PropertyName.ShouldBe("Name");
        sut.OrderedFields[0].Direction.IsAscending.ShouldBeTrue();
        sut.OrderedFields[1].PropertyName.ShouldBe("Id");
        sut.OrderedFields[1].Direction.IsAscending.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIOrderingExpression()
    {
        var sut = new OrderingExpression { OrderedFields = [] };
        sut.ShouldBeAssignableTo<IOrderingExpression>();
    }
}
