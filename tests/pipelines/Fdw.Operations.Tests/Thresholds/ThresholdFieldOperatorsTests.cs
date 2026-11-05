using Fdw.Data;
using Fdw.Operations.Abstractions.Thresholds;

namespace Fdw.Operations.Tests.Thresholds;

/// <summary>Tests for the comparisons a threshold field accepts (FDW-4001): one rule from the value type, every name a FilterOperators option.</summary>
public sealed class ThresholdFieldOperatorsTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void EveryNameTheRuleCanAnswerIsAFilterOperatorsOption()
    {
        foreach (var name in ThresholdFieldOperators.All)
        {
            ReferenceEquals(FilterOperators.ByName(name), FilterOperators.NotFound).ShouldBeFalse(name);
        }

        new[] { typeof(long), typeof(int), typeof(double), typeof(decimal), typeof(TimeSpan), typeof(DateTimeOffset), typeof(bool), typeof(string), typeof(Guid) }
            .SelectMany(ThresholdFieldOperators.For)
            .Distinct(StringComparer.Ordinal)
            .ShouldAllBe(name => ThresholdFieldOperators.All.Contains(name));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void NumbersTimesAndDurationsAreOrderedBooleansAreNotAndStringsMatchText()
    {
        ThresholdFieldOperators.For(typeof(long)).ShouldBe(["Equal", "NotEqual", "GreaterThan", "GreaterThanOrEqual", "LessThan", "LessThanOrEqual"]);
        ThresholdFieldOperators.For(typeof(TimeSpan)).ShouldBe(ThresholdFieldOperators.For(typeof(long)));
        ThresholdFieldOperators.For(typeof(long?)).ShouldBe(ThresholdFieldOperators.For(typeof(long)));
        ThresholdFieldOperators.For(typeof(bool)).ShouldBe(["Equal", "NotEqual"]);
        ThresholdFieldOperators.For(typeof(string)).ShouldBe(["Equal", "NotEqual", "Contains", "StartsWith", "EndsWith"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AFieldReadsItsOperatorsFromItsValueType()
    {
        new ThresholdField("rowsIn", typeof(long), "rows", "Rows in.").Operators.ShouldContain("GreaterThan");
        new ThresholdField("state", typeof(string), "text", "State.").Operators.ShouldContain("Contains");
        new ThresholdField("ok", typeof(bool), "flag", "Ok.").Operators.ShouldNotContain("GreaterThan");
    }
}
