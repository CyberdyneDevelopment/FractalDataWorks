using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Workspaces.Endpoints;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces.Map;

/// <summary>FDW-1195: the stream form is scope, nodes, edges, end, in that order, and it states what it left out.</summary>
[Collection(nameof(ServicesTestCollection))]
public class MapNdjsonWriterTests
{
    [Fact]
    public async Task LinesComeScopeNodesEdgesThenEnd()
    {
        var stream = new ListStream(Nodes(3), [Edge("n0", "n1"), Edge("n1", "n2")]);

        var lines = await Write(stream);

        lines.Select(line => (string)line["t"]!).ShouldBe(["scope", "node", "node", "node", "edge", "edge", "end"]);
        var end = lines[^1];
        ((int)end["nodeCount"]!).ShouldBe(3);
        ((int)end["edgeCount"]!).ShouldBe(2);
        end["error"].ShouldBeNull();
        ((bool)end["truncated"]!).ShouldBeFalse();
    }

    [Fact]
    public async Task AnErrorIsCarriedInTheEndLineAfterTheNodesThatWereRead()
    {
        var stream = new ListStream(Nodes(2), []);

        // Title is read-only on this type (FastEndpoints derives it from the status), so the reason travels in Detail.
        var lines = await Write(stream, () => new ProblemDetails { Status = 500, Detail = "read failed" });

        lines.Select(line => (string)line["t"]!).ShouldBe(["scope", "node", "node", "end"]);
        lines[^1]["error"].ShouldNotBeNull();
        lines[^1]["error"]!.ToJsonString().ShouldContain("read failed");
    }

    [Fact]
    public async Task ACapIsStatedWithWhatWasLeftOutUnknown()
    {
        var stream = new ListStream(Nodes(5), [Edge("n0", "n1"), Edge("n3", "n4")]);

        var lines = await Write(stream, maxNodes: 2);

        lines.Count(line => (string)line["t"]! == "node").ShouldBe(2);
        var end = lines[^1];
        ((bool)end["truncated"]!).ShouldBeTrue();
        ((int)end["cap"]!).ShouldBe(2);
        ((bool)end["leftOutKnown"]!).ShouldBeFalse();
        // Only the edge between two nodes that were sent goes out.
        ((int)end["edgeCount"]!).ShouldBe(1);
    }

    [Fact]
    public async Task ACapThatIsNotReachedIsNotTruncation()
    {
        var lines = await Write(new ListStream(Nodes(2), []), maxNodes: 2);

        ((bool)lines[^1]["truncated"]!).ShouldBeFalse();
        lines[^1]["leftOutKnown"].ShouldBeNull();
    }

    [Fact]
    public async Task ProgressIsWrittenEveryHundredNodes()
    {
        var lines = await Write(new ListStream(Nodes(MapNdjsonWriter.ProgressEvery * 2 + 1), []));

        lines.Count(line => (string)line["t"]! == "progress").ShouldBe(2);
    }

    [Fact]
    public async Task AnExplodedChildsEdgeToTheNodeTheClientHoldsIsSent()
    {
        var scope = new MapScopeDto { Kind = MapScopeKinds.Children, ParentNodeId = "parent" };
        var stream = new ListStream(Nodes(1), [Edge("n0", "parent")], scope);

        var lines = await Write(stream);

        lines.Count(line => (string)line["t"]! == "edge").ShouldBe(1);
    }

    [Fact]
    public async Task APageOfChildrenIsDescribedInTheEndLine()
    {
        var stream = new ListStream(Nodes(1), [], page: new MapPageDto { Skip = 20, Take = 10, HasMore = true, Total = 41 });

        var end = (await Write(stream))[^1];

        ((int)end["page"]!["skip"]!).ShouldBe(20);
        ((bool)end["page"]!["hasMore"]!).ShouldBeTrue();
        ((int)end["page"]!["total"]!).ShouldBe(41);
    }

    [Theory]
    [InlineData("application/x-ndjson", true)]
    [InlineData("text/html, application/x-ndjson;q=0.9", true)]
    [InlineData("application/json", false)]
    [InlineData("", false)]
    public void OnlyTheAcceptHeaderOptsIn(string accept, bool wanted)
    {
        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context.Request.Headers.Accept = accept;

        MapNdjsonWriter.Wanted(context.Request).ShouldBe(wanted);
    }

    private static async Task<List<JsonObject>> Write(ListStream stream, Func<ProblemDetails?>? error = null, int? maxNodes = null)
    {
        using var body = new MemoryStream();
        await MapNdjsonWriter.Write(body, stream, error ?? (() => null), maxNodes, TestContext.Current.CancellationToken);

        return Encoding.UTF8.GetString(body.ToArray())
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonNode.Parse(line)!.AsObject())
            .ToList();
    }

    private static List<MapNodeDto> Nodes(int count)
        => Enumerable.Range(0, count).Select(n => new MapNodeDto { Id = "n" + n, Label = "node " + n, Kind = MapKinds.DataSet }).ToList();

    private static MapEdgeDto Edge(string source, string target) => MapEdges.Between(MapRelations.ConsumedBy, source, target);

    private sealed class ListStream(List<MapNodeDto> nodes, List<MapEdgeDto> edges, MapScopeDto? scope = null, MapPageDto? page = null)
        : MapStream(scope ?? new MapScopeDto { Kind = MapScopeKinds.Everything }, NoEditing())
    {
        public override MapPageDto? Page => page;

        public override async IAsyncEnumerable<MapNodeDto> Nodes([EnumeratorCancellation] CancellationToken ct = default)
        {
            foreach (var node in nodes)
                yield return node;

            await Task.CompletedTask.ConfigureAwait(false);
        }

        public override IEnumerable<MapEdgeDto> Edges() => edges;

        private static MapEditing NoEditing()
            => MapEditing.Resolve(MapEditingTests.Authorization().Object, MapEditingTests.Caller, false, CancellationToken.None).GetAwaiter().GetResult().Value!;
    }
}
