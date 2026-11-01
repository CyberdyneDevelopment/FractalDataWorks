using System.Linq;
using Fdw.Services.Etl.Graphs;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for judging the components and variables of a stored graph (FDW-1403, FDW-2041): how loops hold components, what each kind needs, and that every
/// variable a component names is in scope. Every problem names where it is as a path through the graph the API states.
/// </summary>
public sealed class GraphComponentValidatorTests
{
    private static GraphProblem Only(GraphPipelineConfiguration graph) =>
        GraphShapeValidator.Problems(graph, NullLogger.Instance).ShouldHaveSingleItem();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AGraphOfConnectedComponentsIsValid()
    {
        var graph = Graph([DataSetSource("src"), Mapped("map", ("Key", "Id"))], [Link("src", "map")], Dropped("map"));

        GraphShapeValidator.Problems(graph, NullLogger.Instance).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ALoopWithNoComponentsInItsBodyIsReported()
    {
        var problem = Only(Steps(ForLoop("loop", "I", 1, 3)));

        problem.Path.ShouldBe(["components", "loop"]);
        problem.Reason.ShouldContain("body");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AComponentInTheBodyOfAComponentThatHasNoBodyIsReported()
    {
        var problem = Only(Steps(SourceFlow("flow"), Placed(DataSetSource("inner"), parent: "flow-src")));

        problem.Path.ShouldBe(["components", "inner", "parent"]);
        problem.Reason.ShouldContain("no body");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AComponentInTheBodyOfAComponentThatIsNotInTheGraphIsReported()
    {
        var problem = Only(Steps(Placed(DataSetSource("lost"), parent: "ghost")));

        problem.Path.ShouldBe(["components", "lost", "parent"]);
        problem.Reason.ShouldContain("ghost");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TwoLoopsInsideEachOtherAreReported()
    {
        var graph = Steps(ForLoop("a", "I", 1, 2, parent: "b"), ForLoop("b", "J", 1, 2, parent: "a"));

        var problems = GraphShapeValidator.Problems(graph, NullLogger.Instance);

        problems.ShouldNotBeEmpty();
        problems.ShouldAllBe(problem => problem.Reason.Contains("inside itself", System.StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TwoComponentsWithOneNameAreReported()
    {
        var graph = Graph([DataSetSource("same"), DataSetSource("same")], [], Dropped("same"));

        var problems = GraphShapeValidator.Problems(graph, NullLogger.Instance);

        problems.ShouldContain(problem => problem.Reason.Contains("more than one component named 'same'", System.StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ALoopMissingASettingItsKindRequiresIsReportedNamingTheSetting()
    {
        var loop = ForLoop("loop", "I", 1, 3);
        loop.EndValue = null;

        var problems = GraphShapeValidator.Problems(Steps(loop, SourceFlow("body", parent: "loop")), NullLogger.Instance);

        problems.ShouldHaveSingleItem().Path.ShouldBe(["components", "loop", "settings", "end"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADoWhileWhoseConditionCannotBeReadIsReported()
    {
        var graph = Steps(DoWhile("again", "Attempt <", 3), SourceFlow("body", parent: "again"));

        var problem = Only(graph);

        problem.Path.ShouldBe(["components", "again", "settings", "condition"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASetVariableOfAVariableNobodyDeclaredIsReported()
    {
        var problem = Only(Steps(SetVariable("set", "Missing", "Int32", "1")));

        problem.Path.ShouldBe(["components", "set", "settings", "variable"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ALoopCounterIsInScopeInTheBodyOfItsLoopAndNotOutsideIt()
    {
        var inside = SetVariable("inside", "I", "Int32", "1", parent: "loop");
        var outside = SetVariable("outside", "I", "Int32", "1", ordinal: 1);
        var graph = Steps(ForLoop("loop", "I", 1, 3), inside, outside);

        var problem = Only(graph);

        problem.Path.ShouldBe(["components", "outside", "settings", "variable"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnExecutePipelineThatWaitsButDoesNotSayWhetherItFailsIsReported()
    {
        var start = new PipelineComponentConfiguration { Name = "start", ComponentType = "ExecutePipeline", TargetPipeline = "Child", AwaitCompletion = true };

        var problem = Only(Steps(start));

        problem.Path.ShouldBe(["components", "start", "settings", "failOnChildFailure"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnExecutePipelineThatDoesNotWaitHasNoResultToKeep()
    {
        var start = new PipelineComponentConfiguration
        {
            Name = "start", ComponentType = "ExecutePipeline", TargetPipeline = "Child", AwaitCompletion = false, ResultVariable = "Done",
        };
        var graph = Steps(start);
        graph.Variables.Add(new PipelineVariableConfiguration { Name = "Done", DataType = "Bool", ValueText = "false" });

        var problem = Only(graph);

        problem.Path.ShouldBe(["components", "start", "settings", "awaitCompletion"]);
    }

    private static PipelineComponentConfiguration Starts(string name, string? runPoint, string? parent = null) => new()
    {
        Name = name, ComponentType = "ExecutePipeline", TargetPipeline = "Cleanup", AwaitCompletion = false, RunPoint = runPoint, ParentComponent = parent,
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnExecutePipelineThatStartsOnFailureInsideALoopIsReported()
    {
        var problem = Only(Steps(ForLoop("loop", "I", 1, 3), Starts("tidy", "OnFailure", "loop")));

        problem.Path.ShouldBe(["components", "tidy", "settings", "runPoint"]);
        problem.Reason.ShouldContain("top of the pipeline");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnExecutePipelineWithARunPointThatIsNotOneIsReported()
    {
        var problem = Only(Steps(Starts("tidy", "Sometime")));

        problem.Path.ShouldBe(["components", "tidy", "settings", "runPoint"]);
        problem.Reason.ShouldContain("Sometime");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnExecutePipelineParameterMayNameTheRunsFactsOnlyWhenItHasARunPoint()
    {
        var withPoint = Starts("tidy", "OnFailure");
        withPoint.Parameters.Add(new PipelineComponentParameterConfiguration { ParameterName = "Why", ValueText = "{{Outcome}}" });
        var without = Starts("tidy", null);
        without.Parameters.Add(new PipelineComponentParameterConfiguration { ParameterName = "Why", ValueText = "{{Outcome}}" });

        GraphShapeValidator.Problems(Steps(withPoint), NullLogger.Instance).ShouldBeEmpty();
        GraphShapeValidator.Problems(Steps(without), NullLogger.Instance)
            .ShouldContain(problem => problem.Reason.Contains("Outcome", System.StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AParameterThatNamesAVariableNobodyDeclaredIsReported()
    {
        var start = new PipelineComponentConfiguration
        {
            Name = "start", ComponentType = "ExecutePipeline", TargetPipeline = "Child", AwaitCompletion = true, FailOnChildFailure = true,
        };
        start.Parameters.Add(new PipelineComponentParameterConfiguration { ParameterName = "Year", ValueText = "{{Season}}" });

        var problem = Only(Steps(start));

        problem.Reason.ShouldContain("Season");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AVariableDeclaredTwiceOrWithAValueItsTypeCannotReadIsReported()
    {
        var graph = Steps(SourceFlow("flow"));
        graph.Variables.Add(new PipelineVariableConfiguration { Name = "N", DataType = "Int32", ValueText = "many" });
        graph.Variables.Add(new PipelineVariableConfiguration { Name = "N", DataType = "Int32", ValueText = "1" });

        var problems = GraphShapeValidator.Problems(graph, NullLogger.Instance);

        problems.Select(problem => problem.Path[0]).ShouldAllBe(first => first == "variables");
        problems.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AComponentProblemNamesTheComponentInItsPath()
    {
        var graph = Graph([new PipelineComponentConfiguration { Name = "src", ComponentType = "DataSetSource" }], []);

        var problems = GraphShapeValidator.Problems(graph, NullLogger.Instance);

        problems.ShouldNotBeEmpty();
        problems.ShouldAllBe(problem => problem.Path.Count > 1 && problem.Path[0] == "components" && problem.Path[1] == "src");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AKindThatIsNotAComponentKindIsReported()
    {
        var problem = Only(Steps(new PipelineComponentConfiguration { Name = "odd", ComponentType = "Teleport" }));

        problem.Path.ShouldBe(["components", "odd"]);
    }
}
