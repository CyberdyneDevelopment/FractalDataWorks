using System.Linq;
using Fdw.Services.Etl.Graphs;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for what a graph states about holding a run past its limit (FDW-3129): the DelayedStart behavior needs exactly one of a delay or a
/// time of day, and any other behavior states none.
/// </summary>
public sealed class GraphDelayedStartValidationTests
{
    private static string Path(GraphProblem problem) => string.Join("/", problem.Path);

    private static GraphPipelineConfiguration Delayed(int? seconds, string? timeOfDay = null, string? zone = null)
    {
        var graph = Graph([DataSetSource("src")], []);
        graph.OverflowBehavior = "DelayedStart";
        graph.DelayedStartSeconds = seconds;
        graph.DelayedStartTimeOfDay = timeOfDay;
        graph.DelayedStartTimeZoneId = zone;
        return graph;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADelayedStartWithADelayRaisesNoProblem()
    {
        GraphShapeValidator.Problems(Delayed(600), NullLogger.Instance).Select(Path).ShouldNotContain("delayedStartSeconds");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADelayedStartWithATimeOfDayAndItsZoneRaisesNoProblem()
    {
        var problems = GraphShapeValidator.Problems(Delayed(null, "02:00", "UTC"), NullLogger.Instance);

        problems.Select(Path).ShouldNotContain("delayedStartTimeOfDay");
        problems.Select(Path).ShouldNotContain("delayedStartTimeZoneId");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADelayedStartThatStatesNeitherIsRefusedNamingTheGraph()
    {
        var problem = GraphShapeValidator.Problems(Delayed(null), NullLogger.Instance).Single(p => Path(p) == "delayedStartSeconds");

        problem.Reason.ShouldContain(Delayed(null).Name);
        problem.Reason.ShouldContain("neither a delay");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ATimeOfDayWithoutAZoneIsRefused()
    {
        GraphShapeValidator.Problems(Delayed(null, "02:00"), NullLogger.Instance).Select(Path).ShouldContain("delayedStartTimeZoneId");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Queue")]
    [InlineData("Refuse")]
    public void ABehaviorThatDoesNotHoldARunRefusesADelayAndATimeOfDay(string behavior)
    {
        var graph = Delayed(600, null, null);
        graph.OverflowBehavior = behavior;

        GraphShapeValidator.Problems(graph, NullLogger.Instance).Select(Path).ShouldContain("delayedStartSeconds");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnUnknownBehaviorIsReportedOnceAndNotJudgedForADelay()
    {
        var graph = Delayed(600);
        graph.OverflowBehavior = "Delay";

        var paths = GraphShapeValidator.Problems(graph, NullLogger.Instance).Select(Path).ToList();

        paths.ShouldContain("overflowBehavior");
        paths.ShouldNotContain("delayedStartSeconds");
    }
}
