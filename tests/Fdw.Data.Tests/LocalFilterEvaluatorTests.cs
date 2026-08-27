using System;
using System.Collections.Generic;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests;

/// <summary>
/// The in-memory half of query capability: what a source that cannot express a filter has to do
/// with the rows instead.
/// </summary>
public sealed class LocalFilterEvaluatorTests
{
    private static IReadOnlyDictionary<string, object?> Row(params (string Key, object? Value)[] fields)
    {
        var row = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (key, value) in fields) row[key] = value;
        return row;
    }

    private static FilterCondition Where(string field, FilterOperatorBase op, object? value) =>
        new() { PropertyName = field, Operator = op, Value = value };

    [Fact]
    public void EqualMatchesOnValue()
    {
        LocalFilterEvaluator.Matches(
            Where("team", new EqualOperator(), "Bears"), Row(("team", "Bears"))).ShouldBeTrue();
    }

    [Fact]
    public void EqualComparesNumbersAcrossTypes()
    {
        // Why: the filter came off a wire or a form and carries no type, while the row holds an int.
        LocalFilterEvaluator.Matches(
            Where("wins", new EqualOperator(), "5"), Row(("wins", 5))).ShouldBeTrue();
    }

    [Fact]
    public void EqualIgnoresCase()
    {
        LocalFilterEvaluator.Matches(
            Where("team", new EqualOperator(), "bears"), Row(("team", "Bears"))).ShouldBeTrue();
    }

    [Theory]
    [InlineData(10, 5, true)]
    [InlineData(3, 5, false)]
    [InlineData(5, 5, false)]
    public void GreaterThanOrders(int rowValue, int filterValue, bool expected)
    {
        LocalFilterEvaluator.Matches(
            Where("wins", new GreaterThanOperator(), filterValue), Row(("wins", rowValue)))
            .ShouldBe(expected);
    }

    [Fact]
    public void ContainsMatchesASubstring()
    {
        LocalFilterEvaluator.Matches(
            Where("team", new ContainsOperator(), "ear"), Row(("team", "Bears"))).ShouldBeTrue();
    }

    [Fact]
    public void InAcceptsACommaSeparatedString()
    {
        // Why both shapes: a collection when the filter came from code, a delimited string off a wire.
        LocalFilterEvaluator.Matches(
            Where("team", new InOperator(), "Bears, Packers"), Row(("team", "Packers"))).ShouldBeTrue();
    }

    [Fact]
    public void InAcceptsACollection()
    {
        LocalFilterEvaluator.Matches(
            Where("team", new InOperator(), new[] { "Bears", "Packers" }), Row(("team", "Bears")))
            .ShouldBeTrue();
    }

    [Fact]
    public void IsNullMatchesAMissingValueNotAMissingField()
    {
        LocalFilterEvaluator.Matches(
            Where("coach", new IsNullOperator(), null), Row(("coach", null))).ShouldBeTrue();
    }

    [Fact]
    public void AFieldTheRowDoesNotHaveDoesNotMatch()
    {
        // Why false and not true: nothing can be said about a column the row does not carry, and
        // treating absence as a match lets a typo silently widen the filter.
        LocalFilterEvaluator.Matches(
            Where("nickname", new EqualOperator(), "Da Bears"), Row(("team", "Bears"))).ShouldBeFalse();
    }

    [Fact]
    public void AndRequiresEveryCondition()
    {
        var group = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes = new IFilterNode[]
            {
                Where("team", new EqualOperator(), "Bears"),
                Where("wins", new GreaterThanOperator(), 8),
            },
        };

        LocalFilterEvaluator.Matches(group, Row(("team", "Bears"), ("wins", 10))).ShouldBeTrue();
        LocalFilterEvaluator.Matches(group, Row(("team", "Bears"), ("wins", 3))).ShouldBeFalse();
    }

    [Fact]
    public void OrTakesEither()
    {
        var group = new FilterGroup
        {
            Operator = LogicalOperator.Or,
            Nodes = new IFilterNode[]
            {
                Where("team", new EqualOperator(), "Bears"),
                Where("team", new EqualOperator(), "Packers"),
            },
        };

        LocalFilterEvaluator.Matches(group, Row(("team", "Packers"))).ShouldBeTrue();
        LocalFilterEvaluator.Matches(group, Row(("team", "Lions"))).ShouldBeFalse();
    }

    [Fact]
    public void NestedGroupsCompose()
    {
        // team = Bears AND (wins > 8 OR coach = 'Ditka')
        var inner = new FilterGroup
        {
            Operator = LogicalOperator.Or,
            Nodes = new IFilterNode[]
            {
                Where("wins", new GreaterThanOperator(), 8),
                Where("coach", new EqualOperator(), "Ditka"),
            },
        };
        var outer = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes = new IFilterNode[] { Where("team", new EqualOperator(), "Bears"), inner },
        };

        LocalFilterEvaluator.Matches(outer, Row(("team", "Bears"), ("wins", 3), ("coach", "Ditka"))).ShouldBeTrue();
        LocalFilterEvaluator.Matches(outer, Row(("team", "Bears"), ("wins", 3), ("coach", "Nagy"))).ShouldBeFalse();
    }

    [Fact]
    public void AnEmptyGroupConstrainsNothing()
    {
        var group = new FilterGroup { Operator = LogicalOperator.And, Nodes = Array.Empty<IFilterNode>() };
        LocalFilterEvaluator.Matches(group, Row(("team", "Bears"))).ShouldBeTrue();
    }
}
