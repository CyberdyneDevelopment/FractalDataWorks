using Fdw.Tds.Endpoints.Sql.Queries;
using Shouldly;
using Xunit;

namespace Fdw.Tds.Endpoints.Tests;

/// <summary>FDW-4404: a LIKE pattern is read as the operator its % marks make it; the expected values are what the switch on the two marks answered.</summary>
public sealed class LikePatternsTests
{
    [Theory]
    [InlineData("%ac%", "Contains", "ac")]
    [InlineData("%ac", "EndsWith", "ac")]
    [InlineData("ac%", "StartsWith", "ac")]
    [InlineData("ac", "Equal", "ac")]
    public void APatternIsTheOperatorItsPercentMarksMakeIt(string pattern, string filterOperator, string literal)
    {
        LikePatterns.TryRead(pattern, out var read, out var text).ShouldBeTrue();

        read.ShouldBe(filterOperator);
        text.ShouldBe(literal);
    }

    [Theory]
    [InlineData("%")]
    [InlineData("%%")]
    [InlineData("a_c")]
    [InlineData("[a]c")]
    [InlineData("a%c")]
    public void APatternThatIsNotOneOfThoseOperatorsIsNotRead(string pattern)
    {
        LikePatterns.TryRead(pattern, out _, out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void EveryPairOfMarksHasAShape(bool leading, bool trailing)
    {
        ReferenceEquals(LikeWildcardShapes.Of(leading, trailing), LikeWildcardShapes.NotFound).ShouldBeFalse();
    }
}
