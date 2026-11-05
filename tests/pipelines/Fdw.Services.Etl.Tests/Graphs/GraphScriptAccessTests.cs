using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Graphs;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for who may save a graph that holds script text (FDW-3676): a caller who may write scripts; the question is asked only when the graph holds script text.
/// </summary>
public sealed class GraphScriptAccessTests
{
    private static PipelineComponentConfiguration SqlSource(string name) =>
        new() { Name = name, ComponentType = "SqlSource", DataStoreName = "Warehouse", ScriptText = "SELECT Id FROM dbo.T", MaxRows = 10, BatchSize = 2 };

    // A question that counts how often it is asked.
    private sealed class Caller(bool mayWrite)
    {
        public int Asked { get; private set; }

        public Task<bool> May(CancellationToken cancellationToken)
        {
            Asked++;
            return Task.FromResult(mayWrite);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallerWhoMayNotWriteScriptsIsRefusedAGraphThatHoldsOneAndTheRefusalNamesTheComponent()
    {
        var caller = new Caller(false);
        var graph = Graph([SqlSource("read")], []);

        var result = await GraphScriptAccess.Check(graph, caller.May, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("'read'");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(GraphScriptAccess.WritePolicy);
        caller.Asked.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallerWhoMayWriteScriptsSavesAGraphThatHoldsOne()
    {
        var caller = new Caller(true);
        var graph = Graph([SqlSource("read")], []);

        var result = await GraphScriptAccess.Check(graph, caller.May, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AGraphWithNoScriptIsNotAskedAboutAndSavesForAnyone()
    {
        var caller = new Caller(false);
        var graph = Graph([DataSetSource("src"), Mapped("map", ("A", "B"))], [Link("src", "map")]);

        var result = await GraphScriptAccess.Check(graph, caller.May, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        caller.Asked.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task EveryComponentThatHoldsAScriptIsNamedInTheRefusal()
    {
        var caller = new Caller(false);
        var graph = Graph([SqlSource("one"), SqlSource("two")], []);

        var result = await GraphScriptAccess.Check(graph, caller.May, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.CurrentMessage.ShouldNotBeNull().ShouldContain("'one', 'two'");
    }
}
