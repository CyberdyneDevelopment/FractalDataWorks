using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Query;

namespace Fdw.Web.RestEndpoints.Tests.Query;

/// <summary>
/// FDW-1045: the part of a query that names a calculated field runs here, over rows already read. A row is kept or dropped by
/// the operators' own rules, a missing value matches no operator that needs one (as a store treats a null), a sort puts a
/// missing value first when ascending and last when descending, and the page is cut after both.
/// </summary>
public sealed class InMemoryQueryTests
{
    private static Dictionary<string, object?> Row(object? score, string? name = null)
        => new() { ["Score"] = score, ["Name"] = name };

    private static QueryPlan PlanOf(QueryRequest request, int take = 10, int skip = 0)
    {
        request.Skip = skip;
        var translator = QueryTranslator.Create(
            [
                new QueryableField("score", "Score", typeof(int), IsCalculated: true),
                new QueryableField("name", "Name", typeof(string), IsCalculated: true),
            ],
            "rows",
            new Refusals(),
            allowCalculated: true).Value!;
        return translator.Plan(request, take, 100).Value!;
    }

    private static QueryFilterNode Leaf(string field, string op, string? valueJson = null)
        => new() { Field = field, Operator = op, Value = valueJson is null ? null : Json(valueJson) };

    private static object?[] Scores(IEnumerable<Dictionary<string, object?>> rows) => rows.Select(r => r["Score"]).ToArray();

    [Fact]
    public void AFilterKeepsOnlyTheRowsThatMatch()
    {
        var plan = PlanOf(new QueryRequest { Filter = Leaf("score", "GreaterThanOrEqual", "2") });

        Scores(InMemoryQuery.Apply([Row(1), Row(2), Row(3)], plan)).ShouldBe(new object?[] { 2, 3 });
    }

    [Fact]
    public void AMissingValueMatchesNoOperatorThatNeedsOne()
    {
        var rows = new[] { Row(null), Row(5) };

        Scores(InMemoryQuery.Apply(rows, PlanOf(new QueryRequest { Filter = Leaf("score", "NotEqual", "3") }))).ShouldBe(new object?[] { 5 });
        Scores(InMemoryQuery.Apply(rows, PlanOf(new QueryRequest { Filter = Leaf("score", "Equal", "3") }))).ShouldBeEmpty();
        Scores(InMemoryQuery.Apply(rows, PlanOf(new QueryRequest { Filter = Leaf("score", "GreaterThan", "0") }))).ShouldBe(new object?[] { 5 });
    }

    [Fact]
    public void IsNullAndIsNotNullTellAMissingValueFromAPresentOne()
    {
        var rows = new[] { Row(null), Row(5) };

        Scores(InMemoryQuery.Apply(rows, PlanOf(new QueryRequest { Filter = Leaf("score", "IsNull") }))).ShouldBe(new object?[] { null });
        Scores(InMemoryQuery.Apply(rows, PlanOf(new QueryRequest { Filter = Leaf("score", "IsNotNull") }))).ShouldBe(new object?[] { 5 });
    }

    [Fact]
    public void AnAndKeepsTheRowsThatSatisfyEveryNodeAndAnOrThoseThatSatisfyAny()
    {
        var rows = new[] { Row(1, "ann"), Row(5, "bob"), Row(9, "ann") };
        var big = Leaf("score", "GreaterThan", "4");
        var ann = Leaf("name", "Equal", "\"ann\"");

        var both = PlanOf(new QueryRequest { Filter = new QueryFilterNode { Logic = "And", Nodes = [big, ann] } });
        var either = PlanOf(new QueryRequest { Filter = new QueryFilterNode { Logic = "Or", Nodes = [big, ann] } });

        Scores(InMemoryQuery.Apply(rows, both)).ShouldBe(new object?[] { 9 });
        Scores(InMemoryQuery.Apply(rows, either)).ShouldBe(new object?[] { 1, 5, 9 });
    }

    [Fact]
    public void TextIsComparedWithoutRegardToCaseAndTheRowsKeysAreMatchedTheSameWay()
    {
        var row = new Dictionary<string, object?> { ["SCORE"] = 7, ["NAME"] = "Alice" };

        var kept = InMemoryQuery.Apply([row], PlanOf(new QueryRequest { Filter = Leaf("name", "Equal", "\"ALICE\"") }));

        kept.ShouldHaveSingleItem()["Score"].ShouldBe(7);
    }

    [Fact]
    public void AnAscendingSortPutsAMissingValueFirstAndADescendingOneLast()
    {
        var rows = new[] { Row(3), Row(null), Row(1), Row(2) };

        var ascending = PlanOf(new QueryRequest { Sort = [new QuerySort { Field = "score", Direction = "Ascending" }] });
        var descending = PlanOf(new QueryRequest { Sort = [new QuerySort { Field = "score", Direction = "Descending" }] });

        Scores(InMemoryQuery.Apply(rows, ascending)).ShouldBe(new object?[] { null, 1, 2, 3 });
        Scores(InMemoryQuery.Apply(rows, descending)).ShouldBe(new object?[] { 3, 2, 1, null });
    }

    [Fact]
    public void ASecondSortFieldBreaksTiesInTheFirst()
    {
        var rows = new[] { Row(1, "b"), Row(2, "a"), Row(3, "b"), Row(4, "a") };
        var plan = PlanOf(new QueryRequest
        {
            Sort = [new QuerySort { Field = "name", Direction = "Ascending" }, new QuerySort { Field = "score", Direction = "Descending" }],
        });

        Scores(InMemoryQuery.Apply(rows, plan)).ShouldBe(new object?[] { 4, 2, 3, 1 });
    }

    [Fact]
    public void ThePageIsCutAfterTheFilterAndTheSortAndKeepsOneRowPastIt()
    {
        var rows = new[] { Row(5), Row(1), Row(4), Row(2), Row(3), Row(0) };
        var plan = PlanOf(
            new QueryRequest
            {
                Filter = Leaf("score", "GreaterThan", "0"),
                Sort = [new QuerySort { Field = "score", Direction = "Ascending" }],
            },
            take: 2,
            skip: 1);

        // Ascending and above 0: 1 2 3 4 5. Skip one, take two, plus one more to show a following page exists.
        Scores(InMemoryQuery.Apply(rows, plan)).ShouldBe(new object?[] { 2, 3, 4 });
    }

    [Fact]
    public void AFilterThatNamesNoCalculatedFieldLeavesTheRowsAsTheyWereRead()
    {
        var plan = PlanOf(new QueryRequest());

        plan.RunsInMemory.ShouldBeFalse();
        Scores(InMemoryQuery.Apply([Row(1), Row(2)], plan)).ShouldBe(new object?[] { 1, 2 });
    }

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed class Refusals : IQueryRefusals
    {
        public IGenericResult<T> Invalid<T>(string reason) => GenericResult<T>.Failure(new GenericMessage(reason));
    }
}
