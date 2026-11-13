using System.Linq;
using Fdw.Services.Etl.Graphs;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>FDW-5291: the output a graph that is read ends in is consumed by the read, so the shape check judges it as consumed and a graph without an end is judged as before.</summary>
public sealed class GraphEndTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl.FDW5291")]
    public void AGraphWithNoEndStillHasItsUnlinkedOutputReportedAsLost()
    {
        var graph = Graph([DataSetSource("src")], []);

        var problems = GraphShapeValidator.Problems(graph, NullLogger.Instance);

        problems.Select(p => string.Join('/', p.Path)).ShouldContain("components/src/outputs/output");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl.FDW5291")]
    public void TheEndsOutputIsConsumedAndIsNotReportedAsLost()
    {
        var graph = Graph([DataSetSource("src")], []);

        var problems = GraphShapeValidator.Problems(graph, new GraphEnd("src", "output"), NullLogger.Instance);

        problems.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl.FDW5291")]
    public void AnotherOutputThatIsNeitherLinkedDroppedNorTheEndIsStillReportedAsLost()
    {
        var graph = Graph([DataSetSource("src"), DataSetSource("other")], []);

        var problems = GraphShapeValidator.Problems(graph, new GraphEnd("src", "output"), NullLogger.Instance);

        problems.Select(p => string.Join('/', p.Path)).ShouldBe(["components/other/outputs/output"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl.FDW5291")]
    public void AnEndThatNamesAComponentTheGraphDoesNotHaveIsAProblemOfItsOwn()
    {
        var graph = Graph([DataSetSource("src")], []);

        var problems = GraphShapeValidator.Problems(graph, new GraphEnd("nothere", "output"), NullLogger.Instance);

        problems.Select(p => string.Join('/', p.Path)).ShouldContain("fieldsOutComponent");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl.FDW5290")]
    public void AnEndThatNamesAnOutputTheComponentDoesNotHaveIsNamedAtTheFieldsOutOutputAndDoesNotThrow()
    {
        var graph = Graph([DataSetSource("src")], []);

        var problems = GraphShapeValidator.Problems(graph, new GraphEnd("src", "nope"), NullLogger.Instance);

        problems.Select(p => string.Join('/', p.Path)).ShouldContain("fieldsOutOutput");
        problems.Select(p => string.Join('/', p.Path)).ShouldNotContain(path => path.StartsWith("links", System.StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl.FDW5290")]
    public void AnEndOnOneOutputOfASplitLeavesTheOtherOutputReportedAsLost()
    {
        var graph = Graph([DataSetSource("src"), Split()], [Link("src", "split")]);

        var problems = GraphShapeValidator.Problems(graph, new GraphEnd("split", "rest"), NullLogger.Instance);

        problems.Select(p => string.Join('/', p.Path)).ShouldBe(["components/split/outputs/big"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl.FDW5290")]
    public void AnEndOnASplitsOtherOutputIsConsumedWhenTheFirstIsDropped()
    {
        var graph = Graph([DataSetSource("src"), Split()], [Link("src", "split")], Dropped("split", "big"));

        var problems = GraphShapeValidator.Problems(graph, new GraphEnd("split", "rest"), NullLogger.Instance);

        problems.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl.FDW5290")]
    public void AnOutputThatIsBothDroppedAndTheEndIsReportedAsBothLinkedAndDropped()
    {
        var graph = Graph([DataSetSource("src")], [], Dropped("src"));

        var problems = GraphShapeValidator.Problems(graph, new GraphEnd("src", "output"), NullLogger.Instance);

        problems.Select(p => string.Join('/', p.Path)).ShouldContain("components/src/outputs/output");
    }

    private static PipelineComponentConfiguration Split()
    {
        var split = new PipelineComponentConfiguration { Name = "split", ComponentType = "ConditionalSplit", Routing = "FirstMatch", UnmatchedOutput = "rest" };
        split.SplitOutputs.Add(new PipelineComponentSplitOutputConfiguration { Ordinal = 0, OutputName = "big", FilterExpression = "Id > 1" });
        return split;
    }
}
