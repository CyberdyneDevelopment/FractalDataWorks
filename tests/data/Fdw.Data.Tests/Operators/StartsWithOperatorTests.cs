using Fdw.Data;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Operators;

public sealed class StartsWithOperatorTests
{
    private readonly StartsWithOperator _sut = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsNameToStartsWith()
    {
        _sut.Name.ShouldBe("StartsWith");
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
    public void ODataOperatorIsStartswith()
    {
        _sut.ODataOperator.ShouldBe("startswith");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueWrapsStringInSingleQuotes()
    {
        _sut.FormatODataValue("Acm").ShouldBe("'Acm'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueEscapesSingleQuotes()
    {
        _sut.FormatODataValue("O'Brien").ShouldBe("'O''Brien'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueHandlesNull()
    {
        _sut.FormatODataValue(null).ShouldBe("''");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void StatesItsPatternShapeOnce()
    {
        _sut.IsPatternMatch.ShouldBeTrue();
        _sut.MatchesAnythingBefore.ShouldBe(false);
        _sut.MatchesAnythingAfter.ShouldBe(true);
    }
}
