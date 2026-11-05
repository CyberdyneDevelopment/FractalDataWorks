using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Operations.Configuration;
using Fdw.Operations.Thresholds;
using Fdw.Web.RestEndpoints.Query;

namespace Fdw.Operations.Tests.Thresholds;

/// <summary>Tests for reading a stored rule's criteria (FDW-3936): the filter group and condition rows make the filter model, and rows that do not make one are refused.</summary>
public sealed class ThresholdCriteriaReaderTests
{
    private static ThresholdFilterConditionConfiguration Condition(int ordinal, string field, string op, string? value, string dataType) =>
        new() { Id = Guid.NewGuid(), Ordinal = ordinal, FieldName = field, Operator = op, Value = value, DataType = dataType };

    private static ThresholdFilterGroupConfiguration Group(string op, Guid? parent, int ordinal, params ThresholdFilterConditionConfiguration[] conditions) =>
        new() { Id = Guid.NewGuid(), ParentGroupId = parent, Operator = op, Ordinal = ordinal, Conditions = [.. conditions] };

    private static ThresholdRuleConfiguration Rule(params ThresholdFilterGroupConfiguration[] groups) =>
        new() { Id = Guid.NewGuid(), Ordinal = 2, Escalation = "Alert", FilterGroups = [.. groups] };

    private static ThresholdRuleConfiguration Nested()
    {
        var root = Group("Any", null, 1, Condition(1, "consecutiveFailures", "GreaterThanOrEqual", "3", "Int64"));
        var inner = Group("All", root.Id, 1, Condition(1, "rejected", "GreaterThan", "10", "Int64"), Condition(2, "runTime", "GreaterThan", "00:05:00", "TimeSpan"));
        return Rule(root, inner);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void RowsMakeTheNestedFilterModel()
    {
        var read = ThresholdCriteriaReader.From("FailureRule", Nested());

        read.IsSuccess.ShouldBeTrue();
        var root = read.Value.ShouldBeOfType<FilterGroup>();
        root.Operator.ShouldBe(LogicalOperator.Or);
        root.Nodes.Count.ShouldBe(2);
        root.Nodes[0].ShouldBeOfType<FilterCondition>().Value.ShouldBe(3L);
        root.Nodes[1].ShouldBeOfType<FilterGroup>().Operator.ShouldBe(LogicalOperator.And);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheModelIsJudgedByTheFilterModelsOwnLogicWithTypedValues()
    {
        var criteria = ThresholdCriteriaReader.From("FailureRule", Nested()).Value!;

        InMemoryQuery.Matches(new Dictionary<string, object?> { ["consecutiveFailures"] = 3L, ["rejected"] = 0L, ["runTime"] = TimeSpan.Zero }, criteria).ShouldBeTrue();
        InMemoryQuery.Matches(new Dictionary<string, object?> { ["consecutiveFailures"] = 1L, ["rejected"] = 11L, ["runTime"] = TimeSpan.FromMinutes(6) }, criteria).ShouldBeTrue();
        InMemoryQuery.Matches(new Dictionary<string, object?> { ["consecutiveFailures"] = 1L, ["rejected"] = 11L, ["runTime"] = TimeSpan.FromMinutes(1) }, criteria).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AConditionWithoutAValueOperatorNeedsNoValue()
    {
        var read = ThresholdCriteriaReader.From("T", Rule(Group("All", null, 1, Condition(1, "state", "IsNull", null, "String"))));

        read.Value.ShouldBeOfType<FilterGroup>().Nodes.ShouldHaveSingleItem().ShouldBeOfType<FilterCondition>().Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void RowsThatDoNotMakeACriteriaAreRefused()
    {
        var rootId = Guid.NewGuid();
        var cycleRoot = new ThresholdFilterGroupConfiguration { Id = rootId, ParentGroupId = null, Operator = "All", Ordinal = 1, Conditions = [Condition(1, "x", "Equal", "1", "Int64")] };
        var cycleA = new ThresholdFilterGroupConfiguration { Id = Guid.NewGuid(), ParentGroupId = rootId, Operator = "All", Ordinal = 1 };
        cycleA.Conditions = [Condition(1, "x", "Equal", "1", "Int64")];
        var cycleB = new ThresholdFilterGroupConfiguration { Id = Guid.NewGuid(), ParentGroupId = cycleA.Id, Operator = "All", Ordinal = 1, Conditions = [Condition(1, "x", "Equal", "1", "Int64")] };
        cycleA.ParentGroupId = cycleB.Id;

        ThresholdCriteriaReader.From("T", Rule()).IsFailure.ShouldBeTrue();
        ThresholdCriteriaReader.From("T", Rule(Group("All", null, 1, Condition(1, "x", "Equal", "1", "Int64")), Group("All", null, 2, Condition(1, "x", "Equal", "1", "Int64")))).IsFailure.ShouldBeTrue();
        ThresholdCriteriaReader.From("T", Rule(Group("Maybe", null, 1, Condition(1, "x", "Equal", "1", "Int64")))).IsFailure.ShouldBeTrue();
        ThresholdCriteriaReader.From("T", Rule(Group("All", null, 1))).IsFailure.ShouldBeTrue();
        ThresholdCriteriaReader.From("T", Rule(Group("All", null, 1, Condition(1, "x", "NoSuchOperator", "1", "Int64")))).IsFailure.ShouldBeTrue();
        ThresholdCriteriaReader.From("T", Rule(Group("All", null, 1, Condition(1, "x", "Equal", "one", "Int64")))).IsFailure.ShouldBeTrue();
        ThresholdCriteriaReader.From("T", Rule(Group("All", null, 1, Condition(1, "x", "Equal", null, "Int64")))).IsFailure.ShouldBeTrue();
        ThresholdCriteriaReader.From("T", Rule(Group("All", null, 1, Condition(1, "x", "Equal", "1", "Decimal")))).IsFailure.ShouldBeTrue();
        ThresholdCriteriaReader.From("T", Rule(Group("All", null, 1, Condition(1, "x", "In", "1", "Int64")))).IsFailure.ShouldBeTrue();
        ThresholdCriteriaReader.From("T", Rule(cycleRoot, cycleA, cycleB)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AGroupThatHangsFromNoGroupOfTheRuleIsRefusedNamingIt()
    {
        var root = Group("All", null, 1, Condition(1, "x", "Equal", "1", "Int64"));
        var stray = new ThresholdFilterGroupConfiguration { Id = Guid.NewGuid(), ParentGroupId = Guid.NewGuid(), Operator = "All", Ordinal = 2, Conditions = [Condition(1, "x", "Equal", "1", "Int64")] };

        var result = ThresholdCriteriaReader.From("T", Rule(root, stray));

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("hangs from no group of the rule");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void GroupsThatMakeACycleAmongThemselvesAreRefusedNamingTheCycle()
    {
        var root = Group("All", null, 1, Condition(1, "x", "Equal", "1", "Int64"));
        var a = new ThresholdFilterGroupConfiguration { Id = Guid.NewGuid(), ParentGroupId = Guid.NewGuid(), Operator = "All", Ordinal = 2, Conditions = [Condition(1, "x", "Equal", "1", "Int64")] };
        var b = new ThresholdFilterGroupConfiguration { Id = Guid.NewGuid(), ParentGroupId = a.Id, Operator = "All", Ordinal = 3, Conditions = [Condition(1, "x", "Equal", "1", "Int64")] };
        a.ParentGroupId = b.Id;

        var result = ThresholdCriteriaReader.From("T", Rule(root, a, b));

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("make a cycle");
    }
}
