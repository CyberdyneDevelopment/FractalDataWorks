using System.Linq;
using Fdw.Services.Etl.Graphs;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for the check of a whole stored graph (FDW-1396): it lists every problem at once, each with the path to the part it
/// concerns, so the person building the graph fixes them in one pass.
/// </summary>
public sealed class GraphShapeValidatorTests
{
    private static string Path(GraphProblem problem) => string.Join("/", problem.Path);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AGraphThatCanRunHasNoProblems()
    {
        var graph = Graph([DataSetSource("src"), Mapped("map", ("A", "B"))], [Link("src", "map")], Dropped("map"));

        GraphShapeValidator.Problems(graph, NullLogger.Instance).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AGraphWhoseStagesGoForwardHasNoProblemsAndOneThatGoesBackIsRefused()
    {
        var source = DataSetSource("src");
        var map = Mapped("map", ("A", "B"));
        source.Stage = 1;
        map.Stage = 2;
        var forward = Graph([source, map], [Link("src", "map")], Dropped("map"));
        GraphShapeValidator.Problems(forward, NullLogger.Instance).ShouldBeEmpty();

        source.Stage = 3;
        var back = GraphShapeValidator.Problems(forward, NullLogger.Instance);

        back.Select(problem => problem.Reason).ShouldContain(reason => reason.Contains("runs from stage 3 to stage 2", System.StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EveryProblemIsListedAtOnceNotJustTheFirst()
    {
        var source = DataSetSource("src");
        source.BatchSize = null;
        var graph = Graph([source, Mapped("map", ("A", "B"))], []);
        graph.LinkCapacity = 0;

        var problems = GraphShapeValidator.Problems(graph, NullLogger.Instance);

        problems.Select(Path).ShouldContain("linkCapacity");
        problems.Select(Path).ShouldContain("components/src/settings/batchSize");
        problems.Select(Path).ShouldContain("components/map/inputs/input");
        problems.Select(Path).ShouldContain("components/map/outputs/output");
        problems.Count.ShouldBeGreaterThanOrEqualTo(4);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AGraphThatDoesNotStateItsParallelRunsIsRefusedNamingEachValue()
    {
        var graph = Graph([DataSetSource("src")], []);
        graph.MaxParallelRuns = 0;
        graph.OverflowBehavior = "Delay";

        var problems = GraphShapeValidator.Problems(graph, NullLogger.Instance);

        problems.Select(Path).ShouldContain("maxParallelRuns");
        problems.Select(Path).ShouldContain("overflowBehavior");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AGraphThatStatesItsParallelRunsRaisesNoProblemForThem()
    {
        var graph = Graph([DataSetSource("src")], []);
        graph.MaxParallelRuns = 5;
        graph.OverflowBehavior = "Refuse";

        var problems = GraphShapeValidator.Problems(graph, NullLogger.Instance);

        problems.Select(Path).ShouldNotContain("maxParallelRuns");
        problems.Select(Path).ShouldNotContain("overflowBehavior");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ALinkToAComponentThatIsNotThereIsPlacedOnThatLink()
    {
        var graph = Graph([DataSetSource("src")], [Link("src", "ghost")], Dropped("src"));

        var problems = GraphShapeValidator.Problems(graph, NullLogger.Instance);

        problems.Select(Path).ShouldContain("links/0");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AComponentWhoseSettingsAreWrongDoesNotAlsoReportItsMissingNodes()
    {
        var split = new PipelineComponentConfiguration { Name = "split", ComponentType = "ConditionalSplit" };
        var graph = Graph([DataSetSource("src"), split], [Link("src", "split")]);

        var problems = GraphShapeValidator.Problems(graph, NullLogger.Instance);

        problems.Select(Path).ShouldContain("components/split/settings/outputs");
        problems.Select(Path).ShouldNotContain(path => path.StartsWith("components/split/outputs", System.StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnUnknownKindIsOneProblemOnTheComponent()
    {
        var thing = new PipelineComponentConfiguration { Name = "thing", ComponentType = "Teleport" };
        var graph = Graph([thing], []);

        var problems = GraphShapeValidator.Problems(graph, NullLogger.Instance);

        problems.Count(problem => Path(problem) == "components/thing").ShouldBe(1);
    }
}
