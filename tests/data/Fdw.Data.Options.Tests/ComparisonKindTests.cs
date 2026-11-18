using Fdw.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.Options.Tests;

/// <summary>FDW-6340: each comparison kind adds one condition to a filter, and the value count is checked once.</summary>
public sealed class ComparisonKindTests
{
    private static IRuleKind<FilterExpression> Kind(string name) =>
        (IRuleKind<FilterExpression>)RulePaths.Resolve(NullLogger.Instance, ["DataConfigurationRules", "Comparison", name], "To").Value!;

    [Theory]
    [InlineData("Equal", "Equal")]
    [InlineData("NotEqual", "NotEqual")]
    [InlineData("GreaterThan", "GreaterThan")]
    [InlineData("GreaterThanOrEqual", "GreaterThanOrEqual")]
    [InlineData("LessThan", "LessThan")]
    [InlineData("LessThanOrEqual", "LessThanOrEqual")]
    public void ASingleValueKindAddsOneConditionOnTheField(string kind, string expectedOperator)
    {
        var result = Kind(kind).To(NullLogger.Instance, new FilterExpression(), "Wins", 10);

        result.IsSuccess.ShouldBeTrue();
        var condition = result.Value!.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("Wins");
        condition.Operator.Name.ShouldBe(expectedOperator);
        condition.Value.ShouldBe(10);
    }

    [Theory]
    [InlineData("In", "In")]
    [InlineData("NotIn", "NotIn")]
    public void AListKindAddsOneConditionWithTheWholeList(string kind, string expectedOperator)
    {
        var result = Kind(kind).To(NullLogger.Instance, new FilterExpression(), "Division", new[] { "East", "West" });

        var condition = result.Value!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.Name.ShouldBe(expectedOperator);
        ((IEnumerable<object?>)condition.Value!).ShouldBe(["East", "West"]);
    }

    [Fact]
    public void AListKindTakesASingleValueAsAListOfOne()
    {
        var result = Kind("In").To(NullLogger.Instance, new FilterExpression(), "Division", "East");

        result.IsSuccess.ShouldBeTrue();
        ((IEnumerable<object?>)result.Value!.Root.ShouldBeOfType<FilterCondition>().Value!).ShouldBe(["East"]);
    }

    [Fact]
    public void BetweenAddsTheLowAndTheHighAsTwoConditions()
    {
        var result = Kind("Between").To(NullLogger.Instance, new FilterExpression(), "Wins", 5, 12);

        result.IsSuccess.ShouldBeTrue();
        var group = result.Value!.Root.ShouldBeOfType<FilterGroup>();
        group.Nodes.Count.ShouldBe(2);
        var low = group.Nodes[0].ShouldBeOfType<FilterCondition>();
        var high = group.Nodes[1].ShouldBeOfType<FilterCondition>();
        low.Operator.Name.ShouldBe("GreaterThanOrEqual");
        low.Value.ShouldBe(5);
        high.Operator.Name.ShouldBe("LessThanOrEqual");
        high.Value.ShouldBe(12);
    }

    [Fact]
    public void ASecondKindIsJoinedToTheFirstWithAnd()
    {
        var first = Kind("Equal").To(NullLogger.Instance, new FilterExpression(), "Division", "East").Value!;

        var second = Kind("GreaterThan").To(NullLogger.Instance, first, "Wins", 10);

        var group = second.Value!.Root.ShouldBeOfType<FilterGroup>();
        group.Nodes.Count.ShouldBe(2);
    }

    [Fact]
    public void AKindThatTakesOneValueRefusesAListOfTwo()
    {
        var result = Kind("Equal").To(NullLogger.Instance, new FilterExpression(), "Division", new[] { "East", "West" });

        result.IsFailure.ShouldBeTrue();
        result.Code!.Name.ShouldBe("DataOptionValueCountOutOfRange");
        result.Code.Id.ShouldBe(20001);
    }

    [Fact]
    public void AKindThatTakesTwoValuesRefusesOne()
    {
        var result = Kind("Between").To(NullLogger.Instance, new FilterExpression(), "Wins", 5);

        result.Code!.Id.ShouldBe(20001);
    }

    [Fact]
    public void AListKindRefusesAnEmptyList()
    {
        Kind("In").To(NullLogger.Instance, new FilterExpression(), "Division", Array.Empty<string>()).Code!.Id.ShouldBe(20001);
    }

    [Fact]
    public void AFailureLeavesTheFilterAsItWas()
    {
        var target = new FilterExpression();

        Kind("Equal").To(NullLogger.Instance, target, "Division", new[] { "East", "West" });

        target.Root.ShouldBeNull();
    }
}
