using Fdw.Data;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Operators;

public sealed class EndsWithOperatorTests
{
    private readonly EndsWithOperator _sut = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsNameToEndsWith()
    {
        _sut.Name.ShouldBe("EndsWith");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SqlOperatorIsLike()
    {
        _sut.SqlOperator.ShouldBe("LIKE");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ODataOperatorIsEndswith()
    {
        _sut.ODataOperator.ShouldBe("endswith");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueWrapsStringInSingleQuotes()
    {
        _sut.FormatODataValue("Corp").ShouldBe("'Corp'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueEscapesSingleQuotes()
    {
        _sut.FormatODataValue("Inc's").ShouldBe("'Inc''s'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void StatesItsPatternShapeOnce()
    {
        _sut.IsPatternMatch.ShouldBeTrue();
        _sut.MatchesAnythingBefore.ShouldBe(true);
        _sut.MatchesAnythingAfter.ShouldBe(false);
    }
}
