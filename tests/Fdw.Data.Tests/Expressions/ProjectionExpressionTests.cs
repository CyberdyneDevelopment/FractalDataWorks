using Fdw.Data;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Expressions;

public sealed class ProjectionExpressionTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PropertyNamesReturnsFieldPropertyNames()
    {
        var sut = new ProjectionExpression
        {
            Fields =
            [
                new ProjectionField { PropertyName = "Id" },
                new ProjectionField { PropertyName = "Name" },
                new ProjectionField { PropertyName = "Status" }
            ]
        };

        sut.PropertyNames.ShouldNotBeNull();
        sut.PropertyNames.Count.ShouldBe(3);
        sut.PropertyNames[0].ShouldBe("Id");
        sut.PropertyNames[1].ShouldBe("Name");
        sut.PropertyNames[2].ShouldBe("Status");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PropertyNamesReturnsEmptyForEmptyFields()
    {
        var sut = new ProjectionExpression
        {
            Fields = []
        };

        sut.PropertyNames.ShouldNotBeNull();
        sut.PropertyNames.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FieldsPreservesAliases()
    {
        var sut = new ProjectionExpression
        {
            Fields =
            [
                new ProjectionField { PropertyName = "FirstName", Alias = "FName" },
                new ProjectionField { PropertyName = "LastName" }
            ]
        };

        sut.Fields[0].Alias.ShouldBe("FName");
        sut.Fields[1].Alias.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIProjectionExpression()
    {
        var sut = new ProjectionExpression { Fields = [] };
        sut.ShouldBeAssignableTo<IProjectionExpression>();
    }
}
