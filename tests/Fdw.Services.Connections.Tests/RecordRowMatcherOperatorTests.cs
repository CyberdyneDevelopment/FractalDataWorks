using System;
using System.Collections.Generic;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.RowQuery;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// The matcher applies a filter to rows in memory, for sources whose translator cannot express it.
/// </summary>
/// <remarks>
/// Every one of these went through ValuesEqual before, so GreaterThan, Contains, In and IsNull all
/// behaved as equality — a file-backed "wins > 8" matched only rows where wins was exactly 8.
/// </remarks>
public sealed class RecordRowMatcherOperatorTests
{
    private static IReadOnlyDictionary<string, object?> Row(params (string Key, object? Value)[] fields)
    {
        var row = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (key, value) in fields) row[key] = value;
        return row;
    }

    private static bool Match(IFilterNode node, IReadOnlyDictionary<string, object?> row) =>
        RecordRowMatcher.Matches(row, null, null, node);

    private static FilterCondition Where(string field, FilterOperatorBase op, object? value) =>
        new() { PropertyName = field, Operator = op, Value = value };

    [Fact]
    public void EqualMatchesOnValue() =>
        Match(Where("team", new EqualOperator(), "Bears"), Row(("team", "Bears"))).ShouldBeTrue();

    [Fact]
    public void EqualComparesNumbersAcrossTypes() =>
        Match(Where("wins", new EqualOperator(), "5"), Row(("wins", 5))).ShouldBeTrue();

    [Theory]
    [InlineData(10, 5, true)]
    [InlineData(3, 5, false)]
    [InlineData(5, 5, false)]
    public void GreaterThanOrdersInsteadOfComparingEquality(int rowValue, int filterValue, bool expected) =>
        Match(Where("wins", new GreaterThanOperator(), filterValue), Row(("wins", rowValue)))
            .ShouldBe(expected);

    [Theory]
    [InlineData(3, 5, true)]
    [InlineData(10, 5, false)]
    public void LessThanOrders(int rowValue, int filterValue, bool expected) =>
        Match(Where("wins", new LessThanOperator(), filterValue), Row(("wins", rowValue)))
            .ShouldBe(expected);

    [Fact]
    public void NotEqualIsTheOppositeOfEqual() =>
        Match(Where("team", new NotEqualOperator(), "Packers"), Row(("team", "Bears"))).ShouldBeTrue();

    [Fact]
    public void ContainsMatchesASubstring() =>
        Match(Where("team", new ContainsOperator(), "ear"), Row(("team", "Bears"))).ShouldBeTrue();

    [Fact]
    public void StartsWithAnchorsAtTheStart()
    {
        Match(Where("team", new StartsWithOperator(), "Bea"), Row(("team", "Bears"))).ShouldBeTrue();
        Match(Where("team", new StartsWithOperator(), "ears"), Row(("team", "Bears"))).ShouldBeFalse();
    }

    [Fact]
    public void InAcceptsACommaSeparatedString() =>
        Match(Where("team", new InOperator(), "Bears, Packers"), Row(("team", "Packers"))).ShouldBeTrue();

    [Fact]
    public void InAcceptsACollection() =>
        Match(Where("team", new InOperator(), new[] { "Bears", "Packers" }), Row(("team", "Bears")))
            .ShouldBeTrue();

    [Fact]
    public void IsNullMatchesAnAbsentValue() =>
        Match(Where("coach", new IsNullOperator(), null), Row(("coach", null))).ShouldBeTrue();

    [Fact]
    public void IsNotNullRejectsAnAbsentValue() =>
        Match(Where("coach", new IsNotNullOperator(), null), Row(("coach", null))).ShouldBeFalse();

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

        Match(group, Row(("team", "Bears"), ("wins", 10))).ShouldBeTrue();
        Match(group, Row(("team", "Bears"), ("wins", 3))).ShouldBeFalse();
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

        Match(group, Row(("team", "Packers"))).ShouldBeTrue();
        Match(group, Row(("team", "Lions"))).ShouldBeFalse();
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

        Match(outer, Row(("team", "Bears"), ("wins", 3), ("coach", "Ditka"))).ShouldBeTrue();
        Match(outer, Row(("team", "Bears"), ("wins", 3), ("coach", "Nagy"))).ShouldBeFalse();
    }
}
