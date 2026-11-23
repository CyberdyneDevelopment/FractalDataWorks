using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Tests.Pipelines;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// FDW-6135: an Execute pipeline step may start the pipeline a variable names, from the list the step states. A name on the list starts that pipeline and asks for an offer; a name that is not on
/// the list, or a variable with no value, fails the step with its own message and starts nothing; the component states the pipeline once, and a variable needs its list.
/// </summary>
public sealed class FlowExecutePipelineTargetTests
{
    private static readonly FlowRunIdentity Identity = new(
        Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002"), ["etl-runner"]);

    private static FlowStepContext Context(FakeRunStarter starter, string? chosen)
    {
        var variables = new FlowVariables();
        variables.Declare("Chosen", chosen);
        return new(variables, new FlowRunTally(new FlowExecutor()), Options(), starter, Identity);
    }

    private static FlowExecutePipeline Step() =>
        new("ask", FlowPipelineTarget.FromVariable("Chosen", ["Cleanup", "Archive"]), [], false, null, false);

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AVariableNamingAPipelineOnTheListStartsItAndAsksForAnOffer()
    {
        var starter = new FakeRunStarter();

        var ran = await Step().Run(Context(starter, "archive"), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        var start = starter.Started.ShouldHaveSingleItem();
        start.PipelineName.ShouldBe("Archive", "the name is the listed one, whatever its case");
        start.RequiresOffer.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APipelineTheAuthorNamedAsksForNoOffer()
    {
        var starter = new FakeRunStarter();
        var step = new FlowExecutePipeline("ask", "Cleanup", [], false, null, false);

        var ran = await step.Run(Context(starter, null), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        starter.Started.ShouldHaveSingleItem().RequiresOffer.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ANameNotOnTheListFailsTheStepNamingTheNameAndTheListAndStartsNothing()
    {
        var starter = new FakeRunStarter();

        var ran = await Step().Run(Context(starter, "DropEverything"), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("DropEverything");
        ran.CurrentMessage.ShouldContain("Cleanup, Archive");
        starter.Started.ShouldBeEmpty();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task AVariableWithNoValueFailsTheStepAndStartsNothing(string? chosen)
    {
        var starter = new FakeRunStarter();

        var ran = await Step().Run(Context(starter, chosen), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("'Chosen'");
        starter.Started.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AVariableTargetWithoutAListIsRefusedWhenTheStepIsMade() =>
        Should.Throw<ArgumentException>(() => FlowPipelineTarget.FromVariable("Chosen", []));

    private static PipelineComponentConfiguration Execute(string? pipeline, string? variable, string? targets)
    {
        return new PipelineComponentConfiguration
        {
            Name = "ask", ComponentType = "ExecutePipeline", TargetPipeline = pipeline, AwaitCompletion = false, TargetVariable = variable, AllowedTargets = targets,
        };
    }

    private static string[] Reasons(PipelineComponentConfiguration component)
    {
        var graph = new GraphPipelineConfiguration { Name = "Sample" };
        graph.Variables.Add(new PipelineVariableConfiguration { Name = "Chosen", DataType = "String", ValueText = "Cleanup" });
        graph.Components.Add(component);
        return [.. GraphComponentValidator.Problems(graph, [], NullLogger.Instance).Select(problem => problem.Reason)];
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ASoundComponentOfEitherKindHasNoProblem()
    {
        Reasons(Execute("Cleanup", null, null)).ShouldBeEmpty();
        Reasons(Execute(null, "Chosen", "Cleanup, Archive")).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ABothOrNeitherOfAPipelineAndAVariableIsRefusedAndAVariableWithNoListIsRefused()
    {
        Reasons(Execute("Cleanup", "Chosen", "Cleanup")).ShouldContain(reason => reason.Contains("exactly once") && reason.Contains("states both"));
        Reasons(Execute(null, null, null)).ShouldContain(reason => reason.Contains("exactly once") && reason.Contains("states neither"));
        Reasons(Execute(null, "Chosen", null)).ShouldContain(reason => reason.Contains("states no targets"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheListedTargetsAreThePipelinesTheComponentCallsInTheLineage()
    {
        var graph = new GraphPipelineConfiguration { Name = "Sample" };
        graph.Components.Add(Execute(null, "Chosen", "Cleanup, Archive"));

        var uses = GraphPipelineUses.Of(graph);

        uses.Select(use => use.Name).ShouldBe(["Cleanup", "Archive"], ignoreOrder: true);
    }
}
