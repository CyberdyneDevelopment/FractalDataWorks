using Fdw.Data;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Expressions;

public sealed class PagingExpressionTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SkipAndTakeArePreserved()
    {
        var sut = new PagingExpression { Skip = 20, Take = 10 };
        sut.Skip.ShouldBe(20);
        sut.Take.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TakeCanBeNull()
    {
        var sut = new PagingExpression { Skip = 0, Take = null };
        sut.Take.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SkipDefaultsToZero()
    {
        var sut = new PagingExpression { Skip = 0 };
        sut.Skip.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIPagingExpression()
    {
        var sut = new PagingExpression { Skip = 0 };
        sut.ShouldBeAssignableTo<IPagingExpression>();
    }
}
