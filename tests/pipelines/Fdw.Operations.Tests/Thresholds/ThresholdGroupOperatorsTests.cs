using Fdw.Data.Abstractions;
using Fdw.Operations.Thresholds;

namespace Fdw.Operations.Tests.Thresholds;

/// <summary>
/// FDW-4402: a stored filter group names its operator by the word All or Any, as it always did; the words are looked up through
/// the collection's ByWord, and a word that is neither is not found.
/// </summary>
public class ThresholdGroupOperatorsTests
{
    [Fact]
    public void AllMeansAndAndAnyMeansOr()
    {
        ThresholdGroupOperators.ByWord("All").Logical.ShouldBe(LogicalOperator.And);
        ThresholdGroupOperators.ByWord("Any").Logical.ShouldBe(LogicalOperator.Or);
    }

    [Theory]
    [InlineData("all")]
    [InlineData("And")]
    [InlineData("Or")]
    [InlineData("")]
    public void AnyOtherWordIsNotFound(string word)
    {
        ReferenceEquals(ThresholdGroupOperators.ByWord(word), ThresholdGroupOperators.NotFound).ShouldBeTrue();
    }
}
