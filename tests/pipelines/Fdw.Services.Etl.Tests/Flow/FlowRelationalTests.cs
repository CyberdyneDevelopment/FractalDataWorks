using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for the relational components (FDW-1386): merge join, lookup, cross apply and the error output of a map.
/// </summary>
public sealed class FlowRelationalTests
{
    private readonly FlowExecutor _sut = new();

    private static IDictionary<string, object?> Person(int id, string name) => Fields(("PersonId", id), ("Name", name));

    private static IDictionary<string, object?> Order(int id, int personId) => Fields(("OrderId", id), ("PersonId", personId));

    private static FlowGraph Join(IJoinKind kind, string prefix, IDictionary<string, object?>[] left, IDictionary<string, object?>[] right,
        out List<IDictionary<string, object?>> rows)
    {
        var (sink, collected) = Sink("dst");
        rows = collected;
        return Graph(
            [
                FromRows("left", left),
                FromRows("right", right),
                new FlowMergeJoin("join", ["PersonId"], ["PersonId"], kind, prefix, batchSize: 3),
                sink
            ],
            [Link("left", "join", input: "left"), Link("right", "join", input: "right"), Link("join", "dst")]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnInnerJoinKeepsOnlyRowsThatHaveAPartnerAndPairsEveryPartner()
    {
        var graph = Join(
            JoinKinds.ByName("Inner"), "p_",
            [Order(1, 10), Order(2, 10), Order(3, 99)],
            [Person(10, "Ada"), Person(11, "Bob")],
            out var rows);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        rows.Select(r => (int)r["OrderId"]!).OrderBy(i => i).ShouldBe(new[] { 1, 2 });
        rows.ShouldAllBe(r => (string)r["p_Name"]! == "Ada" && (int)r["p_PersonId"]! == 10);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ALeftOuterJoinKeepsUnmatchedLeftRowsWithTheRightFieldsNull()
    {
        var graph = Join(JoinKinds.ByName("LeftOuter"), "p_", [Order(1, 10), Order(3, 99)], [Person(10, "Ada")], out var rows);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        rows.Count.ShouldBe(2);
        var orphan = rows.Single(r => (int)r["OrderId"]! == 3);
        orphan["p_Name"].ShouldBeNull();
        orphan.ContainsKey("p_Name").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFullOuterJoinAlsoKeepsUnmatchedRightRowsWithTheLeftFieldsNull()
    {
        var graph = Join(JoinKinds.ByName("FullOuter"), "p_", [Order(1, 10)], [Person(10, "Ada"), Person(11, "Bob")], out var rows);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        rows.Count.ShouldBe(2);
        var bob = rows.Single(r => (string?)r["p_Name"] == "Bob");
        bob["OrderId"].ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARowWhoseKeyIsNullNeverMatches()
    {
        var graph = Join(
            JoinKinds.ByName("Inner"), "p_",
            [Fields(("OrderId", 1), ("PersonId", null))],
            [Fields(("PersonId", null), ("Name", "Nobody"))],
            out var rows);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        rows.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFieldNameBothSidesWouldWriteFailsTheRunNamingTheField()
    {
        var graph = Join(JoinKinds.ByName("Inner"), string.Empty, [Order(1, 10)], [Person(10, "Ada")], out _);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("PersonId");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ABothSidesFedByOneUpstreamOutputDoesNotDeadlockTheJoin()
    {
        var (sink, rows) = Sink("dst");
        var graph = Graph(
            [
                Source("src", 500, batchSize: 5),
                Mapped("map", FlowFieldMap.From("Id", "Id")),
                new FlowMergeJoin("join", ["Id"], ["Id"], JoinKinds.ByName("Inner"), "r_", batchSize: 50),
                sink
            ],
            [Link("src", "map"), Link("map", "join", input: "left"), Link("map", "join", input: "right"), Link("join", "dst")]);
        using var guard = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));

        var result = await _sut.Run(graph, Options(capacity: 1), guard.Token);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        rows.Count.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ALookupBringsTheReferenceColumnsOntoMatchedRowsAndSendsTheRestToTheUnmatchedOutput()
    {
        var (matched, matchedRows) = Sink("matched");
        var (missing, missingRows) = Sink("missing");
        var lookup = new FlowLookup(
            "lookup", ["PersonId"], ["PersonId"], [FlowFieldMap.From("PersonName", "Name")], FlowUnmatched.SendTo("nomatch"), batchSize: 2);
        var graph = Graph(
            [FromRows("orders", Order(1, 10), Order(2, 99)), FromRows("people", Person(10, "Ada")), lookup, matched, missing],
            [
                Link("orders", "lookup", input: "input"),
                Link("people", "lookup", input: "reference"),
                Link("lookup", "matched"),
                Link("lookup", "missing", output: "nomatch")
            ]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        matchedRows.ShouldHaveSingleItem()["PersonName"].ShouldBe("Ada");
        ((int)missingRows.ShouldHaveSingleItem()["OrderId"]!).ShouldBe(2);
        missingRows[0].ContainsKey("PersonName").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ALookupSetToFailStopsTheRunOnARowWithNoReference()
    {
        var (matched, _) = Sink("matched");
        var lookup = new FlowLookup(
            "lookup", ["PersonId"], ["PersonId"], [FlowFieldMap.From("PersonName", "Name")], FlowUnmatched.Fail, batchSize: 2);
        var graph = Graph(
            [FromRows("orders", Order(2, 99)), FromRows("people", Person(10, "Ada")), lookup, matched],
            [Link("orders", "lookup", input: "input"), Link("people", "lookup", input: "reference"), Link("lookup", "matched")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("lookup");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TwoReferenceRowsForOneKeyFailTheLookupNeverPickingOne()
    {
        var (matched, _) = Sink("matched");
        var lookup = new FlowLookup(
            "lookup", ["PersonId"], ["PersonId"], [FlowFieldMap.From("PersonName", "Name")], FlowUnmatched.Fail, batchSize: 2);
        var graph = Graph(
            [FromRows("orders", Order(1, 10)), FromRows("people", Person(10, "Ada"), Person(10, "Ada Again")), lookup, matched],
            [Link("orders", "lookup", input: "input"), Link("people", "lookup", input: "reference"), Link("lookup", "matched")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("more than one reference row");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ACrossApplyMakesOneRowPerListElementAndSplitsTextOnTheStatedDelimiter()
    {
        var (sink, rows) = Sink("dst");
        var apply = new FlowCrossApply("apply", "Tags", "Tag", delimiter: ",", keepsEmpty: false);
        var graph = Graph(
            [
                FromRows("src", Fields(("Id", 1), ("Tags", "a,b")), Fields(("Id", 2), ("Tags", new[] { "x", "y", "z" })), Fields(("Id", 3), ("Tags", null))),
                apply,
                sink
            ],
            [Link("src", "apply"), Link("apply", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        rows.Select(r => $"{r["Id"]}:{r["Tag"]}").ShouldBe(new[] { "1:a", "1:b", "2:x", "2:y", "2:z" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnOuterCrossApplyKeepsARowWithNoElementsOnceWithTheTargetNull()
    {
        var (sink, rows) = Sink("dst");
        var apply = new FlowCrossApply("apply", "Tags", "Tag", delimiter: null, keepsEmpty: true);
        var graph = Graph(
            [FromRows("src", Fields(("Id", 3), ("Tags", null))), apply, sink],
            [Link("src", "apply"), Link("apply", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var row = rows.ShouldHaveSingleItem();
        row["Tag"].ShouldBeNull();
        row["Id"].ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TextInACrossApplyWithNoDelimiterFailsTheRun()
    {
        var (sink, _) = Sink("dst");
        var apply = new FlowCrossApply("apply", "Tags", "Tag", delimiter: null, keepsEmpty: false);
        var graph = Graph([FromRows("src", Fields(("Tags", "a,b"))), apply, sink], [Link("src", "apply"), Link("apply", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("delimiter");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AMappedInputFailsTheRunNamingTheComponentTheInputAndTheFieldWhenARowLacksIt()
    {
        var (good, goodRows) = Sink("good");
        var graph = Graph(
            [FromRows("src", Fields(("Id", 1)), Fields(("Other", 2)), Fields(("Id", 3))), Mapped("map", FlowFieldMap.From("Key", "Id")), good],
            [Link("src", "map"), Link("map", "good")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        var message = result.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain("map");
        message.ShouldContain("input");
        message.ShouldContain("Id");
        goodRows.ShouldBeEmpty();
    }
}
